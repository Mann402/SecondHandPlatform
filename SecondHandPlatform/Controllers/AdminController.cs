using Microsoft.AspNetCore.Mvc;
using SecondHandPlatform.Data;
using SecondHandPlatform.Models;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.DTOs;
using System;

namespace SecondHandPlatformTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/admin
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Admin>>> GetAdmins()
        {
            return await _context.Admins.ToListAsync();
        }

        // GET: api/admin/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Admin>> GetAdmin(string id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();
            return admin;
        }

        // POST: api/admin/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto loginDto)
        {
            var admin = await _context.Admins
                .FirstOrDefaultAsync(a =>
                    a.AdminEmail == loginDto.Email &&
                    a.AdminPassword == loginDto.Password);

            if (admin == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Normally you return a JWT token, but here we'll just return admin info
            return Ok(new
            {
                token = "mock-token", // Replace with real token if you implement auth
                adminData = new
                {
                    admin.AdminID,
                    admin.AdminName,
                    admin.AdminEmail
                }
            });
        }

        // POST: api/admin
        [HttpPost]
        public async Task<ActionResult<Admin>> CreateAdmin(Admin admin)
        {
            _context.Admins.Add(admin);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAdmin), new { id = admin.AdminID }, admin);
        }

        // PUT: api/admin/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAdmin(string id, Admin admin)
        {
            if (id != admin.AdminID) return BadRequest();
            _context.Entry(admin).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/admin/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAdmin(string id)
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) return NotFound();
            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}