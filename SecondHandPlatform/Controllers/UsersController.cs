using Microsoft.AspNetCore.Mvc;
using SecondHandPlatform.DTOs;
using SecondHandPlatform.Models;
using SecondHandPlatform.Services;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json; // For dynamic jsonRes usage

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHttpClientFactory _httpClientFactory;

        // Store partial user data in memory
        private static ConcurrentDictionary<string, PendingUserInfo> _pendingUsers
            = new ConcurrentDictionary<string, PendingUserInfo>();

        private readonly string _tempFolder;

        public UsersController(IUserService userService, IHttpClientFactory httpClientFactory)
        {
            _userService = userService;
            _httpClientFactory = httpClientFactory;

            // We'll store images in "tempUploads"
            _tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "tempUploads");
            if (!Directory.Exists(_tempFolder))
            {
                Directory.CreateDirectory(_tempFolder);
            }
        }

        // STEP 1: TEMP UPLOAD
        // user uploads student card => we store partial user info in memory
        [HttpPost("temp-upload")]
        public async Task<IActionResult> TempUpload([FromForm] RegisterUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Generate a tempId
            string tempId = Guid.NewGuid().ToString();

            // Create a file name
            string fileName = tempId + "_" + dto.StudentCardFile.FileName;
            string filePath = Path.Combine(_tempFolder, fileName);

            // Save the student card in temp folder
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await dto.StudentCardFile.CopyToAsync(fs);
            }

            // Store partial user data in memory
            var pendingInfo = new PendingUserInfo
            {
                TempId = tempId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Password = dto.Password,
                CardFileName = fileName
            };
            _pendingUsers[tempId] = pendingInfo;

            return Ok(new { temp_id = tempId, message = "Temp upload success." });
        }

        // STEP 2: FACE VERIFY
        // user sends webcam image + tempId => compare with student card
        // if match => create user in DB, else discard
        [HttpPost("face-verify")]
        public async Task<IActionResult> FaceVerify([FromForm] UserRegistrationFaceVerifyDto dto)
        {
            // Ensure necessary fields exist (LiveImage, Email, TempId)
            if (dto.LiveImage == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.TempId))
                return BadRequest(new { message = "Missing live image, email, or temp_id." });

            Console.WriteLine($"Received tempId: {dto.TempId}");
            Console.WriteLine($"Available tempIds: {string.Join(", ", _pendingUsers.Keys)}");

            // Retrieve pending data using the TempId from the DTO
            if (!_pendingUsers.TryGetValue(dto.TempId, out var pendingInfo))
            {
                return BadRequest(new { message = "Invalid tempId or expired." });
            }


            // Save webcam image in temp folder
            string webcamName = dto.TempId + "_webcam_" + dto.LiveImage.FileName;
            string webcamPath = Path.Combine(_tempFolder, webcamName);
            using (var fs = new FileStream(webcamPath, FileMode.Create))
            {
                await dto.LiveImage.CopyToAsync(fs);
            }

            // Call python /compare
            var client = _httpClientFactory.CreateClient();
            using var content = new MultipartFormDataContent();

            // Attach the student card image (from temp upload)
            byte[] cardBytes = System.IO.File.ReadAllBytes(Path.Combine(_tempFolder, pendingInfo.CardFileName));
            var cardContent = new ByteArrayContent(cardBytes);
            cardContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(cardContent, "card_image", pendingInfo.CardFileName);

            byte[] webcamBytes = System.IO.File.ReadAllBytes(webcamPath);
            var webcamContent = new ByteArrayContent(webcamBytes);
            webcamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(webcamContent, "webcam_image", webcamName);

            // Attach Email and temp_id for identification (as strings)
            content.Add(new StringContent(pendingInfo.Email), "email");
            content.Add(new StringContent(dto.TempId), "temp_id");

            HttpResponseMessage response = null;
            try
            {
                response = await client.PostAsync("http://127.0.0.1:5000/verify", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling Python API: " + ex.Message);
                // Clean up temporary files
                if (System.IO.File.Exists(webcamPath))
                    System.IO.File.Delete(webcamPath);
                _pendingUsers.TryRemove(dto.TempId, out _);
                return StatusCode(500, new { message = "Error calling Python API", error = ex.Message });
            }

            if (!response.IsSuccessStatusCode)
            {
                if (System.IO.File.Exists(webcamPath))
                    System.IO.File.Delete(webcamPath);
                _pendingUsers.TryRemove(dto.TempId, out _);
                return BadRequest(new { message = "Python compare error." });
            }

            string compareResult = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Raw Python response: " + compareResult);
            dynamic jsonRes;
            try
            {
                jsonRes = JsonConvert.DeserializeObject(compareResult);
            }
            catch (Exception ex)
            {
                if (System.IO.File.Exists(webcamPath))
                    System.IO.File.Delete(webcamPath);
                _pendingUsers.TryRemove(dto.TempId, out _);
                return StatusCode(500, new { message = "Error parsing Python response", error = ex.Message });
            }

            bool faceMatch = jsonRes.success;
            // Fallback: if "message" is not in the response, use "error" or set a default string.
            string confidenceMsg = (jsonRes.message != null) ? (string)jsonRes.message :
                                     (jsonRes.error != null) ? (string)jsonRes.error : "No message";

            if (!faceMatch)
            {
                if (System.IO.File.Exists(webcamPath))
                    System.IO.File.Delete(webcamPath);
                _pendingUsers.TryRemove(dto.TempId, out _);
                return BadRequest(new { message = "Face mismatch. Registration aborted." });
            }

            // Face matched – create a permanent user.
            byte[] studentCardBytes = System.IO.File.ReadAllBytes(Path.Combine(_tempFolder, pendingInfo.CardFileName));
            var user = new User
            {
                FirstName = pendingInfo.FirstName,
                LastName = pendingInfo.LastName,
                Email = pendingInfo.Email,
                Password = pendingInfo.Password, // Remember to hash passwords in production!
                StudentCardPicture = studentCardBytes,
                UserStatus = "Active"
            };

            var (dbSuccess, dbMsg) = await _userService.RegisterUserAsync(user);
            if (!dbSuccess)
            {
                if (System.IO.File.Exists(webcamPath))
                    System.IO.File.Delete(webcamPath);
                _pendingUsers.TryRemove(dto.TempId, out _);
                return BadRequest(new { message = dbMsg });
            }

            // Clean up temporary files and pending data.
            if (System.IO.File.Exists(webcamPath))
                System.IO.File.Delete(webcamPath);
            _pendingUsers.TryRemove(dto.TempId, out _);

            return Ok(new
            {
                success = true,
                message = "Face verified, user created successfully! " + confidenceMsg,
                email = user.Email
            });
        }


        // Add this endpoint to your UsersController.cs class
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Create user object from DTO
                var user = new User
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    Password = dto.Password,
                    // Convert student card file to bytes if needed
                    // StudentCardPicture = Convert.FromBase64String(dto.StudentCardBase64)
                };

                var (success, message) = await _userService.RegisterUserAsync(user);
                if (!success)
                    return BadRequest(new { message });

                return Ok(new { message = "User registered successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try { 
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.LoginUserAsync(new User
            {
                Email = loginDto.Email,
                Password = loginDto.Password
            });

            if (user == null)
                return Unauthorized(new { message = "Invalid email or password." });

            return Ok(new { message = "Login successful", user });
        }
          catch (Exception ex)
            {
                // Return 500 with the actual exception for debugging
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace
    });
            }
        }


        // GET: api/Users/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Return user data without sensitive information
                return Ok(new
                {
                    userId = user.UserId,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving user data", error = ex.Message });
            }
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userDto)
        {
            try
            {
                if (id != userDto.UserId)
                {
                    return BadRequest(new { message = "User ID mismatch." });
                }

                var (success, message) = await _userService.UpdateUserAsync(id, userDto);
                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating user {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error updating user data", error = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var (success, message) = await _userService.DeleteUserAsync(id);
            if (!success)
                return BadRequest(new { message });

            return Ok(new { message });
        }
    }

    // store partial user data
    public class PendingUserInfo
    {
        public string TempId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string CardFileName { get; set; }
    }
}
