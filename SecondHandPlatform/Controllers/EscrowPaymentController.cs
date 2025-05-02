using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SecondHandPlatformTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EscrowPaymentController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;

        public EscrowPaymentController(SecondhandplatformContext context)
        {
            _context = context;
        }

        // GET: api/escrowpayment
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EscrowPayment>>> GetPayments()
        {
            return await _context.EscrowPayments.ToListAsync();
        }

        // GET: api/escrowpayment/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<EscrowPayment>> GetPayment(int id)
        {
            var payment = await _context.EscrowPayments.FindAsync(id);
            if (payment == null) return NotFound();
            return payment;
        }

        // POST: api/escrowpayment
        [HttpPost]
        public async Task<ActionResult<EscrowPayment>> CreatePayment(EscrowPayment payment)
        {
            _context.EscrowPayments.Add(payment);



            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPayment), new { id = payment.paymentID }, payment);
        }

        // PUT: api/escrowpayment/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePayment(int id, EscrowPayment payment)
        {
            if (id != payment.paymentID) return BadRequest();

            _context.Entry(payment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/escrowpayment/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(string id)
        {
            var payment = await _context.EscrowPayments.FindAsync(id);
            if (payment == null) return NotFound();

            _context.EscrowPayments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}