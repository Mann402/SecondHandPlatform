using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecondHandPlatform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SecondHandPlatformTest.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly SecondhandplatformContext _context;
        private readonly ILogger<ReportController> _logger;

        public ReportController(SecondhandplatformContext context, ILogger<ReportController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Report/CategorySummary
        [HttpGet("CategorySummary")]
        public async Task<IActionResult> GetCategorySummary()
        {
            try
            {
                // Filter out records with null price values before grouping
                var summary = await _context.Products
                    .Include(p => p.Category)
                    .GroupBy(p => new {
                        p.CategoryId,
                        Name = p.Category.Name
                    })
                    .Select(g => new CategorySummaryDto
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.Name,

                        // All products in the category
                        ProductCount = g.Count(),

                        AverageBasePrice = g.Average(x => x.ProductPrice),

                        // Same for verifiedPrice
                        AverageVerifiedPrice = g.Any(x => x.VerifiedPrice != null)
                            ? g.Where(x => x.VerifiedPrice != null)
                               .Average(x => x.VerifiedPrice.Value)
                            : 0m
                    })
                    .ToListAsync();

                var debugList = await _context.Products
                    .Include(p => p.Category)
                    .Select(p => new {
                        p.ProductId,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category.Name,

                        p.ProductPrice,
                        p.VerifiedPrice
                    })
                    .ToListAsync();


                return Ok(summary);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while fetching category summary.");
                return StatusCode(500, ex.Message);
            }
        }

        // GET: api/Report/PricingPatterns
        [HttpGet("PricingPatterns")]
        public async Task<IActionResult> GetPricingPatterns()
        {
            try
            {
                // Retrieve base prices into memory
                var basePrices = await _context.Products
                    .Select(p => p.ProductPrice)
                    .ToListAsync();

                // Retrieve verified prices into memory
                var verifiedPrices = await _context.Products
                    .Where(p => p.VerifiedPrice.HasValue)
                    .Select(p => p.VerifiedPrice.Value)
                    .ToListAsync();

                var pricingPatterns = new PricingPatternsDto
                {
                    AverageBasePrice = basePrices.Any() ? basePrices.Average() : 0,
                    AverageVerifiedPrice = verifiedPrices.Any() ? verifiedPrices.Average() : 0,
                    MinBasePrice = basePrices.Any() ? basePrices.Min() : 0,
                    MaxBasePrice = basePrices.Any() ? basePrices.Max() : 0,
                    MinVerifiedPrice = verifiedPrices.Any() ? verifiedPrices.Min() : 0,
                    MaxVerifiedPrice = verifiedPrices.Any() ? verifiedPrices.Max() : 0
                };

                return Ok(pricingPatterns);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error while fetching pricing patterns.");
                return StatusCode(500, ex.Message);
            }
        }
    }

    // DTO for Category Summary Report
    public class CategorySummaryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int ProductCount { get; set; }
        public decimal AverageBasePrice { get; set; }
        public decimal AverageVerifiedPrice { get; set; }
    }

    // DTO for Pricing Patterns Report
    public class PricingPatternsDto
    {
        public decimal AverageBasePrice { get; set; }
        public decimal AverageVerifiedPrice { get; set; }
        public decimal MinBasePrice { get; set; }
        public decimal MaxBasePrice { get; set; }
        public decimal MinVerifiedPrice { get; set; }
        public decimal MaxVerifiedPrice { get; set; }
    }
}