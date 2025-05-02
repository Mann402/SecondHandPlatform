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

            var purchasedItem = await _context.OrderItems
               .Include(oi => oi.Order)
               .FirstOrDefaultAsync(oi =>
                   oi.Order.UserId == feedbackRequest.UserId &&
                   oi.ProductId == feedbackRequest.ProductId &&
                   oi.Order.OrderStatus == "Completed"
               );

            if (purchasedItem == null)
            {
                return BadRequest("You can only review products you have purchased and verified.");
            }

            // Prevent Duplicate Feedback
            var alreadyReviewed = await _context.Feedback
                .AnyAsync(f =>
                    f.UserId == feedbackRequest.UserId &&
                    f.ProductId == feedbackRequest.ProductId
                );

            if (alreadyReviewed)
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

            return Ok(new { message = "Thank you for your feedback! Your review helps our community." });
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

        // 3. Get a specific user's feedback for a specific product
        // This endpoint is needed for the RateFeedback page to check if a user has already reviewed
        [HttpGet("product/{productId}/user/{userId}")]
        public async Task<ActionResult<Feedback>> GetUserProductFeedback(int productId, int userId)
        {
            var feedback = await _context.Feedback
                .FirstOrDefaultAsync(f => f.ProductId == productId && f.UserId == userId);

            if (feedback == null)
            {
                return NotFound("No feedback found from this user for this product.");
            }

            return Ok(feedback);
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

        // 5. Get community feedback (for announcement/community page)
        [HttpGet("community")]
        public async Task<ActionResult<List<CommunityFeedbackDto>>> GetCommunityFeedback()
        {
            var communityFeedback = await _context.Feedback
                .Include(f => f.User)
                .Include(f => f.Product)
                    .ThenInclude(p => p.User) // This gives you the seller
                .OrderByDescending(f => f.DateSubmitted) // Ensures newest feedback appears first
                .Take(50) // Limit to most recent 50 for performance
                .Select(f => new CommunityFeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    ProductId = f.ProductId,
                    ProductName = f.Product.ProductName,
                    BuyerName = $"{f.User.FirstName} {f.User.LastName}",
                    SellerName = $"{f.Product.User.FirstName} {f.Product.User.LastName}",
                    Rating = f.Rating,
                    Comment = f.Comment,
                    DateSubmitted = f.DateSubmitted,
                    ProductImage = f.Product.ProductImage != null
                        ? Convert.ToBase64String(f.Product.ProductImage)
                        : null
                })
                .ToListAsync();

            if (!communityFeedback.Any())
            {
                return NotFound("No feedback has been submitted yet.");
            }

            return Ok(communityFeedback);
        }



        // Add this method to your FeedbackController
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<UserFeedbackDto>>> GetUserFeedback(int userId)
        {
            var userFeedback = await _context.Feedback
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                    .ThenInclude(p => p.User)
                .OrderByDescending(f => f.DateSubmitted)
                .Select(f => new UserFeedbackDto
                {
                    FeedbackId = f.FeedbackId,
                    ProductId = f.ProductId,
                    Product = new ProductInfoDto
                    {
                        ProductId = f.Product.ProductId,
                        ProductName = f.Product.ProductName,
                        ProductImage = f.Product.ProductImage != null
                            ? Convert.ToBase64String(f.Product.ProductImage)
                            : null,
                        User = new UserInfoDto
                        {
                            UserId = f.Product.User.UserId,
                            Username = $"{f.Product.User.FirstName} {f.Product.User.LastName}"
                        }
                    },
                    Rating = f.Rating,
                    Comment = f.Comment,
                    DateSubmitted = f.DateSubmitted
                })
                .ToListAsync();

            if (!userFeedback.Any())
            {
                return NotFound("You haven't submitted any reviews yet.");
            }

            return Ok(userFeedback);
        }

        // Add this method to your FeedbackController
        [HttpPut("{feedbackId}")]
        public async Task<IActionResult> UpdateFeedback(int feedbackId, [FromBody] Feedback updatedFeedback)
        {
            if (updatedFeedback == null)
            {
                return BadRequest("Feedback data is required.");
            }

            var existingFeedback = await _context.Feedback.FindAsync(feedbackId);
            if (existingFeedback == null)
            {
                return NotFound("Feedback not found.");
            }

            // Verify user owns this feedback
            if (existingFeedback.UserId != updatedFeedback.UserId)
            {
                return BadRequest("You can only update your own feedback.");
            }

            // Verify product was purchased
            var purchasedItem = await _context.OrderItems
               .Include(oi => oi.Order)
               .FirstOrDefaultAsync(oi =>
                   oi.Order.UserId == updatedFeedback.UserId &&
                   oi.ProductId == updatedFeedback.ProductId &&
                   oi.Order.OrderStatus == "Completed"
               );

            if (purchasedItem == null)
            {
                return BadRequest("You can only review products you have purchased and verified.");
            }

            // Update the fields
            existingFeedback.Rating = updatedFeedback.Rating;
            existingFeedback.Comment = updatedFeedback.Comment;
            existingFeedback.DateSubmitted = DateTime.UtcNow;

            _context.Feedback.Update(existingFeedback);
            await _context.SaveChangesAsync();

            return Ok(existingFeedback);
        }

        // DTO classes for user feedback
        public class UserFeedbackDto
        {
            public int FeedbackId { get; set; }
            public int ProductId { get; set; }
            public ProductInfoDto Product { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime DateSubmitted { get; set; }
        }

        public class ProductInfoDto
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductImage { get; set; }
            public UserInfoDto User { get; set; }
        }

        public class UserInfoDto
        {
            public int UserId { get; set; }
            public string Username { get; set; }
        }

        // DTO for community feedback
        public class CommunityFeedbackDto
        {
            public int FeedbackId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductImage { get; set; } // Added product image
            public string BuyerName { get; set; }
            public string SellerName { get; set; }
            public int Rating { get; set; }
            public string Comment { get; set; }
            public DateTime DateSubmitted { get; set; }

            // You can also add a formatted date string for easier frontend display
            public string FormattedDate => DateSubmitted.ToString("MMM dd, yyyy");
        }


    }
}
    

