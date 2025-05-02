using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContentManagementController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;

        public ContentManagementController(SecondhandplatformContext context)
        {
            _context = context;
        }

        // GET: api/contentmanagement
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ContentManagement>>> GetAllContents()
        {
            return await _context.ContentManagements.ToListAsync();
        }

        // GET: api/contentmanagement/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ContentManagement>> GetContent(int id)
        {
            var content = await _context.ContentManagements.FindAsync(id);
            if (content == null) return NotFound();
            return content;
        }

        // POST: api/contentmanagement
        [HttpPost]
        public async Task<ActionResult<ContentManagement>> CreateContent(ContentManagement content)
        {
            _context.ContentManagements.Add(content);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetContent), new { id = content.announcementID }, content);
        }

        // PUT: api/contentmanagement/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateContent(int id, ContentManagement content)
        {
            if (id != content.announcementID) return BadRequest();

            _context.Entry(content).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/contentmanagement/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteContent(int id)
        {
            var content = await _context.ContentManagements.FindAsync(id);
            if (content == null) return NotFound();

            _context.ContentManagements.Remove(content);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}