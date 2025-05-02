// Controllers/UserAddressesController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAddressesController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;

        public UserAddressesController(SecondhandplatformContext context)
        {
            _context = context;
        }

        // GET: api/UserAddresses/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<UserAddress>> GetAddressForUser(int userId)
        {
            try
            {
                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.UserId == userId);

                if (address == null)
                {
                    // Return an empty object to indicate no address found
                    return Ok(new { });
                }

                return Ok(address);
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error fetching address for user {userId}: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving address", error = ex.Message });
            }
        }


        // GET: api/UserAddresses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAddress>> GetUserAddressById(int id)
        {
            try
            {
                var userAddress = await _context.UserAddresses.FindAsync(id);

                if (userAddress == null)
                {
                    return NotFound(new { message = "Address not found" });
                }

                return userAddress;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error fetching address {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error retrieving address", error = ex.Message });
            }
        }

        // GET: api/UserAddresses/cities/state
        [HttpGet("cities/{state}")]
        public ActionResult<IEnumerable<string>> GetCitiesByState(string state)
        {
            // This is a simplified example - in a real application, you'd likely fetch from a database
            var citiesByState = new Dictionary<string, List<string>>
            {
                { "Selangor", new List<string> { "Shah Alam", "Petaling Jaya", "Subang Jaya", "Klang", "Kajang", "Jenjarom" } },
                { "Kuala Lumpur", new List<string> { "Kuala Lumpur", "Batu Caves", "Sentul", "Cheras", "Kepong" } },
                { "Penang", new List<string> { "George Town", "Butterworth", "Bukit Mertajam", "Nibong Tebal", "Balik Pulau" } },
                { "Johor", new List<string> { "Johor Bahru", "Muar", "Batu Pahat", "Kluang", "Segamat" } },
                { "Sabah", new List<string> { "Kota Kinabalu", "Sandakan", "Tawau", "Lahad Datu", "Keningau" } },
                { "Sarawak", new List<string> { "Kuching", "Miri", "Sibu", "Bintulu", "Limbang" } },
                { "Perak", new List<string> { "Ipoh", "Taiping", "Teluk Intan", "Sitiawan", "Kampar" } },
                { "Negeri Sembilan", new List<string> { "Seremban", "Port Dickson", "Nilai", "Bahau", "Kuala Pilah" } },
                { "Melaka", new List<string> { "Melaka City", "Alor Gajah", "Jasin", "Klebang", "Ayer Keroh" } },
                { "Kedah", new List<string> { "Alor Setar", "Sungai Petani", "Kulim", "Langkawi", "Jitra" } },
                { "Pahang", new List<string> { "Kuantan", "Temerloh", "Bentong", "Raub", "Cameron Highlands" } },
                { "Terengganu", new List<string> { "Kuala Terengganu", "Kemaman", "Dungun", "Marang", "Besut" } },
                { "Kelantan", new List<string> { "Kota Bharu", "Pasir Mas", "Tanah Merah", "Kuala Krai", "Bachok" } },
                { "Perlis", new List<string> { "Kangar", "Arau", "Padang Besar", "Simpang Empat", "Kuala Perlis" } }
            };

            if (citiesByState.ContainsKey(state))
            {
                return Ok(citiesByState[state]);
            }

            return NotFound("No cities found for the specified state");
        }

        // POST: api/UserAddresses
        [HttpPost]
        public async Task<ActionResult<UserAddress>> CreateUserAddress([FromBody] UserAddressDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    message = "Invalid address data",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });

            var entity = new UserAddress
            {
                UserId = dto.UserId,
                Address = dto.Address,
                City = dto.City,
                Postcode = dto.Postcode,
                State = dto.State,
                PhoneNumber = dto.PhoneNumber,
                IsDefault = dto.IsDefault,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _context.UserAddresses.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserAddressById),
                                   new { id = entity.UserAddressId },
                                   entity);
        }

            // PUT: api/UserAddresses/5
            [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserAddress(int id, [FromBody] UserAddress userAddress)
        {
            try
            {
                // Log what we received
                Console.WriteLine($"Received address update request for ID={id}: " +
                                 $"UserAddressId={userAddress.UserAddressId}, UserId={userAddress.UserId}, " +
                                 $"Address={userAddress.Address}, City={userAddress.City}, " +
                                 $"State={userAddress.State}, Postcode={userAddress.Postcode}");

                if (id != userAddress.UserAddressId)
                {
                    Console.WriteLine($"ID mismatch: Path ID={id}, Body ID={userAddress.UserAddressId}");
                    return BadRequest(new { message = "ID mismatch between URL and body" });
                }

                // Validate the model
                if (!ModelState.IsValid)
                {
                    Console.WriteLine("Model validation failed: " + string.Join(", ",
                        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                    return BadRequest(new
                    {
                        message = "Invalid address data",
                        errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                // Check if address exists
                var existingAddress = await _context.UserAddresses.FindAsync(id);
                if (existingAddress == null)
                {
                    Console.WriteLine($"Address with ID {id} not found");
                    return NotFound(new { message = "Address not found" });
                }

                // Update properties manually to ensure we're only changing what we need to
                existingAddress.Address = userAddress.Address;
                existingAddress.City = userAddress.City;
                existingAddress.State = userAddress.State;
                existingAddress.Postcode = userAddress.Postcode;
                existingAddress.PhoneNumber = userAddress.PhoneNumber;
                existingAddress.IsDefault = userAddress.IsDefault;
                existingAddress.ModifiedDate = DateTime.Now;

                // If this is being set as default, unset any existing defaults
                if (userAddress.IsDefault)
                {
                    var otherDefaults = await _context.UserAddresses
                        .Where(ua => ua.UserId == userAddress.UserId && ua.IsDefault && ua.UserAddressId != id)
                        .ToListAsync();

                    foreach (var other in otherDefaults)
                    {
                        other.IsDefault = false;
                    }
                }

                try
                {
                    // Save changes
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Successfully updated address ID {id}");
                    return Ok(new { message = "Address updated successfully" });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Console.WriteLine($"Concurrency exception: {ex.Message}");
                    if (!UserAddressExists(id))
                    {
                        return NotFound(new { message = "Address not found" });
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error updating address {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error updating address", error = ex.Message });
            }
        }

      


        // DELETE: api/UserAddresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserAddress(int id)
        {
            var userAddress = await _context.UserAddresses.FindAsync(id);
            if (userAddress == null)
            {
                return NotFound();
            }

            _context.UserAddresses.Remove(userAddress);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserAddressExists(int id)
        {
            return _context.UserAddresses.Any(e => e.UserAddressId == id);
        }
    }

    public class UserAddressDto
    {
        public int? UserAddressId { get; set; }
        public int UserId { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Postcode { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsDefault { get; set; } = true;
    }
}