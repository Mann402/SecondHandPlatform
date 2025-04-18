using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;

        public FeedbackController(SecondhandplatformContext context)
        {
            _context = context;
        }

        // 1. Submit Feedback (Only Verified Buyers)
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] Feedback feedbackRequest)
        {
            if (feedbackRequest == null || feedbackRequest.UserId == 0 || feedbackRequest.ProductId == 0)
            {
                return BadRequest("UserId and ProductId are required.");
            }

            var order = await _context.Orders
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.UserId == feedbackRequest.UserId
                                          && o.ProductId == feedbackRequest.ProductId
                                          && o.OrderStatus == "Verified");

            if (order == null)
            {
                return BadRequest("You can only review products you have purchased and verified.");
            }

            // Prevent Duplicate Feedback
            var existingFeedback = await _context.Feedback
                .FirstOrDefaultAsync(f => f.UserId == feedbackRequest.UserId && f.ProductId == feedbackRequest.ProductId);

            if (existingFeedback != null)
            {
                return BadRequest("You have already reviewed this product.");
            }

            // Add Feedback
            var feedback = new Feedback
            {
                ProductId = feedbackRequest.ProductId,
                UserId = feedbackRequest.UserId,
                Rating = feedbackRequest.Rating,
                Comment = feedbackRequest.Comment,
                DateSubmitted = DateTime.UtcNow
            };

            _context.Feedback.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Feedback submitted successfully!" });
        }

        // 2. Get Product Feedback (For Buyers)
        [HttpGet("Product/{productId}")]
        public async Task<ActionResult<List<Feedback>>> GetProductFeedback(int productId)
        {
            var feedbackList = await _context.Feedback
                .Where(f => f.ProductId == productId)
                .Include(f => f.User)
                .ToListAsync();

            if (!feedbackList.Any())
            {
                return NotFound("No feedback found for this product.");
            }

            return Ok(feedbackList);
        }

        //  3. Get Seller's Reviews (Using Product Table)
        [HttpGet("Seller/{userId}")]
        public async Task<ActionResult<List<Feedback>>> GetSellerFeedback(int userId)
        {
            var feedbackList = await _context.Feedback
                .Where(f => _context.Products.Any(p => p.ProductId == f.ProductId && p.UserId == userId))
                .Include(f => f.User)
                .ToListAsync();

            if (!feedbackList.Any())
            {
                return NotFound("No feedback found for this seller.");
            }

            return Ok(feedbackList);
        }

        // ✅ 4. Delete Feedback (Admin Action)
        [HttpDelete("{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {
            var feedback = await _context.Feedback.FindAsync(feedbackId);
            if (feedback == null)
            {
                return NotFound("Feedback not found.");
            }

            _context.Feedback.Remove(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Feedback deleted successfully!" });
        }
    }
}
