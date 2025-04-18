using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SecondHandPlatform.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        // foreign key to Order
        public int OrderId { get; set; }

        // foreign key to Product
        public int ProductId { get; set; }

        public int Quantity { get; set; } = 1;

        // navigation back to Order
        [JsonIgnore]
        public virtual Order Order { get; set; } = null!;

        // navigation to Product
        [JsonPropertyName("product")]
        public virtual Product Product { get; set; } = null!;
    }
}
