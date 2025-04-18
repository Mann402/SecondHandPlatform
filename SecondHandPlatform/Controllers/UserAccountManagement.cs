using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using SecondHandPlatform.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatformTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserAccountManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/useraccountmanagement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserAccountManagement>>> GetUsers()
        {
            return await _context.UserAccountManagements.ToListAsync();
        }

        // GET: api/useraccountmanagement/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserAccountManagement>> GetUser(int id)
        {
            var user = await _context.UserAccountManagements.FindAsync(id);
            if (user == null) return NotFound();
            return user;
        }

        // POST: api/useraccountmanagement
        [HttpPost]
        public async Task<ActionResult<UserAccountManagement>> CreateUser(UserAccountManagement user)
        {
            _context.UserAccountManagements.Add(user);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = user.userID }, user);
        }

        // PUT: api/useraccountmanagement/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UserAccountManagement user)
        {
            if (id != user.userID) return BadRequest();

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/useraccountmanagement/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.UserAccountManagements.FindAsync(id);
            if (user == null) return NotFound();

            _context.UserAccountManagements.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}