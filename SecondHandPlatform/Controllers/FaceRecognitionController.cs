using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using SecondHandPlatform.DTOs;
using SecondHandPlatform.Services;
using SecondHandPlatform.Models;
using System.Text;
using System.Linq;
using System.Security.Cryptography;


namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceRecognitionController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IUserService _userService;


        public FaceRecognitionController(IHttpClientFactory httpClientFactory, IUserService userService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _userService = userService;

        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterFace([FromForm] FaceRegisterDto dto)
        {
            if (dto.FaceImage == null)
                return BadRequest("No face image provided.");

            // Forward the registration request to the Python service
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(dto.FaceImage.OpenReadStream());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(fileContent, "face_image", dto.FaceImage.FileName);

            var response = await _httpClient.PostAsync("http://127.0.0.1:5000/register", content);
            var result = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, result);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyFace([FromForm] FaceVerificationDto dto)
        {
            // Now require the live image and the user's email
            if (dto.LiveImage == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.TempId))
                return BadRequest("Missing live image or email.");

            // Forward the verification request to the Python service
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(dto.LiveImage.OpenReadStream());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            // Use "webcam_image" as the key since Python expects that
            content.Add(fileContent, "LiveImage", dto.LiveImage.FileName);

            content.Add(new StringContent(dto.Email), "Email");
            content.Add(new StringContent(dto.TempId), "temp_id");

            var response = await _httpClient.PostAsync("http://127.0.0.1:5000/verify", content);
            var result = await response.Content.ReadAsStringAsync();

            return StatusCode((int)response.StatusCode, result);
        }

        /*
        // Save verified user to the database after face recognition
        [HttpPost("save-user")]
        public async Task<IActionResult> SaveVerifiedUser([FromBody] VerifiedUserDto verifiedUser)
        {
            if (verifiedUser == null)
            {
                return BadRequest(new { message = "Invalid user data." });
            }

            try
            {
                // ✅ Convert Base64 string to byte[] before saving to DB
                byte[] studentCardBytes = Convert.FromBase64String(verifiedUser.StudentCardPicture);

                var newUser = new User
                {
                    FirstName = verifiedUser.FirstName,
                    LastName = verifiedUser.LastName,
                    Email = verifiedUser.Email,
                    Password = HashPassword(verifiedUser.Password), // Hash the password
                    StudentCardPicture = studentCardBytes, // Save as byte[]
                    UserStatus = "Active"
                };

                var (success, message) = await _userService.RegisterUserAsync(newUser);

                if (!success)
                {
                    return BadRequest(new { message });
                }

                return Ok(new { message = "User registered successfully after verification!" });
            }
            catch (FormatException)
            {
                // Handle invalid base64 error
                return BadRequest(new { message = "Invalid base64 string for StudentCardPicture." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal server error.", error = ex.Message });
            }
        }


        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return string.Concat(bytes.Select(b => b.ToString("x2")));
        }*/
    }

}