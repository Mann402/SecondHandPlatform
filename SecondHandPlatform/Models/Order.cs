using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    public partial class Order
{
    public int OrderId { get; set; }

    public int? CartId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime OrderDate { get; set; }

    public string? OrderStatus { get; set; }
    [JsonIgnore]
    public virtual Cart? Cart { get; set; }

        [JsonPropertyName("payments")]
        public virtual ICollection<Payment> Payments { get; } = new List<Payment>();

        [JsonPropertyName("product")]
        public virtual Product? Product { get; set; }

        public virtual User? User { get; set; }

         // navigation for the new items
    [JsonPropertyName("orderItems")]
   public virtual ICollection<OrderItem> OrderItems { get; } = new List<OrderItem>();

        // ---- computed for JSON output ----

        [NotMapped]
        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod
            => Payments.FirstOrDefault()?.PaymentMethod
               ?? "Not specified";

        [NotMapped]
        [JsonPropertyName("paymentDate")]
        public DateTime? PaymentDate
            => Payments.FirstOrDefault()?.PaymentDate;
    }
}
