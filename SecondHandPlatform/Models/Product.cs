using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
[Table("products")]
public partial class Product
{
    public int ProductId { get; set; }

    public int UserId { get; set; }

    public string ProductName { get; set; } = null!;

    public string ProductDescription { get; set; } = null!;

    public string ProductCategory { get; set; } = null!;

    public decimal ProductPrice { get; set; }
        [Column("verified_price")]
        public decimal? VerifiedPrice { get; set; }

    public string ProductCondition { get; set; } 

    public string ProductStatus { get; set; } 

    public byte[]? ProductImage { get; set; } 

    public DateTime DatePosted { get; set; }

    public bool IsSold { get; set; }

    public virtual ICollection<Cart> Carts { get; } = new List<Cart>();

    public virtual ICollection<Feedback> Feedbacks { get; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    public virtual User? User { get; set; } // Make it nullable

}
}