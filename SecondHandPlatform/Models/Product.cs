using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
[Table("products")]
public partial class Product
{
        [Column("product_id")]
        public int ProductId { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("product_name")]
        public string ProductName { get; set; } = null!;
        [Column("product_description")]
        public string ProductDescription { get; set; } = null!;

        [Column("category_id")] public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        [Column("product_price")]
        public decimal ProductPrice { get; set; }
        [Column("verified_price")]
        public decimal? VerifiedPrice { get; set; }
        [Column("product_condition")]
        public string ProductCondition { get; set; }
        [Column("product_status")]
        public string ProductStatus { get; set; }
        [Column("product_image")]
        public byte[]? ProductImage { get; set; }
        [Column("date_posted")]
        public DateTime DatePosted { get; set; }

        public bool IsVerificationRequested { get; set; }
        public DateTime? VerificationRequestedDate { get; set; }

        public bool IsSold { get; set; }

    public virtual ICollection<Cart> Carts { get; } = new List<Cart>();

    public virtual ICollection<Feedback> Feedbacks { get; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    public virtual User? User { get; set; } // Make it nullable

}
}