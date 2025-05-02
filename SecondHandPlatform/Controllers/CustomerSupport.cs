using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using SecondHandPlatform.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatformTest.Controllers
{
    // DTO for the “respond” payload
    public class RespondDto
    {
        public string Response { get; set; }
        public DateTime ResolveDate { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CustomerSupportController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;
        private readonly IEmailService _email;

        public CustomerSupportController(
            SecondhandplatformContext context,
            IEmailService email)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _email = email ?? throw new ArgumentNullException(nameof(email));
        }

        // GET: api/customersupport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerSupport>>> GetTickets()
            => await _context.CustomerSupports.ToListAsync();

        // GET: api/customersupport/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerSupport>> GetTicket(int id)
        {
            var ticket = await _context.CustomerSupports.FindAsync(id);
            return ticket == null ? NotFound() : ticket;
        }

        // POST: api/customersupport
        [HttpPost]
        public async Task<ActionResult<CustomerSupport>> CreateTicket(CustomerSupport ticket)
        {
            _context.CustomerSupports.Add(ticket);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetTicket), new { id = ticket.ticketID }, ticket);
        }

        // DELETE: api/customersupport/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTicket(int id)
        {
            var ticket = await _context.CustomerSupports.FindAsync(id);
            if (ticket == null) return NotFound();
            _context.CustomerSupports.Remove(ticket);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/customersupport/{id}/respond
        // Updates response + resolveDate and sends the email in one call.
        [HttpPost("{id}/respond")]
        public async Task<IActionResult> RespondToTicket(
            int id,
            [FromBody] RespondDto dto)
        {
            var ticket = await _context.CustomerSupports.FindAsync(id);
            if (ticket == null) return NotFound();

            // 1) Update the record
            ticket.response = dto.Response;
            ticket.resolveDate = dto.ResolveDate;
            await _context.SaveChangesAsync();

            // 2) Send the email
            var subject = $"Response to your ticket #{ticket.ticketID}";
            var body = $"Hello {ticket.userName},\n\n"
                        + $"{ticket.response}\n\n"
                        + "Best regards,\nSecondHand Admin";
            await _email.SendEmailAsync(ticket.userEmail, subject, body);

            return NoContent();
        }
    }
}