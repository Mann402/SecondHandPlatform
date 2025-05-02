using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecondHandPlatform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stripe;
using Stripe.Checkout;

namespace SecondHandPlatform.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;
        private readonly string _stripeSecretKey;
        private readonly string _stripePublishableKey;
        private readonly string _webhookSecret;

        public PaymentController(SecondhandplatformContext context, IConfiguration configuration)
        {
            _context = context;
            _stripeSecretKey = configuration["Stripe:SecretKey"];
            _stripePublishableKey = configuration["Stripe:PublishableKey"];
            _webhookSecret = configuration["Stripe:WebhookSecret"];
            StripeConfiguration.ApiKey = _stripeSecretKey;
        }

        // Get Stripe publishable key for the frontend
        [HttpGet("config")]
        public ActionResult GetPublishableKey()
        {
            return Ok(new { publishableKey = _stripePublishableKey });
        }

        // Create a payment intent for Stripe
        [HttpPost("create-payment-intent")]
        public async Task<ActionResult> CreatePaymentIntent([FromBody] PaymentIntentCreateRequest request)
        {
            try
            {
                // Validate request
                if (request.Amount <= 0)
                {
                    return BadRequest("Amount must be greater than 0.");
                }

                if (request.UserId <= 0)
                {
                    return BadRequest("Valid user ID is required.");
                }

                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest("At least one item is required.");
                }

                // Create a new payment intent with Stripe
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)request.Amount, // Amount in cents
                    Currency = request.Currency.ToLower(),
                    PaymentMethodTypes = new List<string> { "card" },
                    Metadata = new Dictionary<string, string>
                    {
                        { "userId", request.UserId.ToString() },
                        { "itemCount", request.Items.Count.ToString() }
                    }
                };

                var service = new PaymentIntentService();
                var paymentIntent = await service.CreateAsync(options);

                return Ok(new { clientSecret = paymentIntent.ClientSecret });
            }
            catch (StripeException e)
            {
                return StatusCode(500, new { Error = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Error = e.Message });
            }
        }

        // Process direct payments (for non-card payment methods)
        [HttpPost("process-payment")]
        public async Task<IActionResult> ProcessPayment([FromBody] DirectPaymentRequest request)
        {
            // ── BASIC REQUEST VALIDATION ────────────────────────────────
            if (request == null)
                return BadRequest("Request body is required.");

            if (request.UserId <= 0)
                return BadRequest("A valid UserId is required.");

            if (string.IsNullOrEmpty(request.PaymentMethod))
                return BadRequest("A payment method is required.");

            // ── LOAD CART & ENSURE NOT EMPTY ────────────────────────────
            var cartItems = await _context.Carts
                .Where(c => c.UserId == request.UserId)
                .Include(c => c.Product)
                .ToListAsync();

            if (!cartItems.Any())
                return BadRequest("Your cart is empty.");

            // ── ITEM-LEVEL VALIDATIONS ──────────────────────────────────
            foreach (var c in cartItems)
            {
                var p = c.Product;
                if (p == null)
                    return BadRequest($"Product ID {c.ProductId} not found.");

                // Has this product ever been ordered before?
                bool alreadyOrdered = await _context.OrderItems
                    .AnyAsync(oi => oi.ProductId == p.ProductId);
                if (alreadyOrdered)
                    return BadRequest($"The product '{p.ProductName}' has already been ordered by another user.");

                if (p.IsSold)
                    return BadRequest($"The product '{p.ProductName}' is already sold.");
            }


            // Calculate total amount
            decimal totalAmount = cartItems.Sum(c => c.Product!.ProductPrice);

            using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // ── 1) CREATE A SINGLE ORDER ────────────────────────────
        var order = new Order
        {
            UserId      = request.UserId,
            OrderDate   = DateTime.UtcNow,
            TotalAmount = totalAmount,
            OrderStatus = "Processing"
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();  // populates order.OrderId

        // ── 2) CREATE ONE ORDERITEM PER CART ENTRY ─────────────
        var items = cartItems.Select(c => new OrderItem
        {
            OrderId   = order.OrderId,
            ProductId = c.ProductId,
            Quantity  = 1
        });
        _context.OrderItems.AddRange(items);
        await _context.SaveChangesAsync();

        // ── 3) RECORD A SINGLE PAYMENT ──────────────────────────
        var payment = new Payment
        {
            OrderId       = order.OrderId,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "Completed",
            Amount        = totalAmount,
            PaymentDate   = DateTime.UtcNow
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // ── 4) MARK PRODUCTS AS SOLD ────────────────────────────
        foreach (var c in cartItems)
        {
            var prod = await _context.Products.FindAsync(c.ProductId);
            prod!.IsSold       = true;
            prod.ProductStatus = "Sold";
        }
        await _context.SaveChangesAsync();

        // ── 5) REMOVE CART ITEMS ────────────────────────────────
        _context.Carts.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        return Ok(new { message = "Payment processed successfully!", orderId = order.OrderId });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return StatusCode(500, new { error = $"An error occurred: {ex.Message}" });
    }
}

        // Handle Stripe webhook events
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleStripeWebhook()
        {
            var json = await new System.IO.StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _webhookSecret
                );

                // Handle the event based on its type
                if (stripeEvent.Type == "payment_intent.succeeded")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandleSuccessfulPayment(paymentIntent);
                }
                else if (stripeEvent.Type == "payment_intent.payment_failed")
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    await HandleFailedPayment(paymentIntent);
                }

                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest(new { Error = e.Message });
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Error = e.Message });
            }
        }

        // Helper method to handle successful Stripe payments
        private async Task HandleSuccessfulPayment(PaymentIntent paymentIntent)
        {
            // Extract userId
            if (!paymentIntent.Metadata.TryGetValue("userId", out var userIdStr)
                || !int.TryParse(userIdStr, out var userId))
            {
                return; // can't proceed
            }

            // Get cart items for the user
            var cartItems = await _context.Carts
                    .Where(c => c.UserId == userId)
                    .Include(c => c.Product)
                    .ToListAsync();
            if (!cartItems.Any())
                return; // nothing to do

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Sum up total
                var totalAmount = cartItems.Sum(c => c.Product!.ProductPrice);

                // Create one Order
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    OrderStatus = "Processing"
                };
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create OrderItems
                var orderItems = cartItems.Select(c => new OrderItem
                {
                    OrderId = order.OrderId,
                    ProductId = c.ProductId,
                    Quantity = 1
                });
                _context.OrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();

                // Create single Payment
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentMethod = "Credit Card (Stripe)",
                    PaymentStatus = "Completed",
                    Amount = totalAmount,
                    PaymentDate = DateTime.UtcNow
                };
                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Mark products sold
                foreach (var c in cartItems)
                {
                    var p = await _context.Products.FindAsync(c.ProductId);
                    if (p != null)
                    {
                        p.IsSold = true;
                        p.ProductStatus = "Sold";
                    }
                }
                await _context.SaveChangesAsync();

                // Remove cart items
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        // Helper method to handle failed Stripe payments
        private async Task HandleFailedPayment(PaymentIntent paymentIntent)
        {
            // Log the failed payment
            Console.WriteLine($"Payment failed: {paymentIntent.Id}, Amount: {paymentIntent.Amount}, Error: {paymentIntent.LastPaymentError?.Message}");

            // You could also store this in your database if needed
        }
    }

    // DTOs for payment requests
    public class PaymentIntentCreateRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "myr";
        public int UserId { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }

    public class DirectPaymentRequest
    {
        public int UserId { get; set; }
        public string PaymentMethod { get; set; }
        public BillingDetails BillingDetails { get; set; }
        public List<OrderItemRequest> Items { get; set; }
    }

    public class BillingDetails
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

    public class OrderItemRequest
    {
        public int ProductId { get; set; }
        public decimal Price { get; set; }
    }
}