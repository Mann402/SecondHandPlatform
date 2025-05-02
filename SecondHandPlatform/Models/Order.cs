using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    public class Order
    {
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime OrderDate { get; set; }

    public string? OrderStatus { get; set; }
   
        [JsonIgnore]
        [JsonPropertyName("orderItems")]
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    = new List<OrderItem>();
        [JsonPropertyName("payment")]
        public virtual Payment? Payment { get; set; }

        public virtual User? User { get; set; }


        // ---- computed for JSON output ----

        [NotMapped]
        [JsonPropertyName("paymentMethod")]
        public string PaymentMethod
              => Payment?.PaymentMethod
                 ?? "Not specified";

        [NotMapped]
        [JsonPropertyName("paymentDate")]
        public DateTime? PaymentDate
        => Payment?.PaymentDate;
    }
}
