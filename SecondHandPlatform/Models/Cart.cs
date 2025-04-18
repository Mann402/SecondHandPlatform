using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SecondHandPlatform.Models;

[Table("cart")]
public partial class Cart
{
    [Key]
    [Column("cart_id")]
    public int CartId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("product_id")]
    public int ProductId { get; set; }

    [Column("total_price", TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }
    
    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    [ForeignKey(nameof(ProductId))]
    public virtual Product ? Product { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }
}
