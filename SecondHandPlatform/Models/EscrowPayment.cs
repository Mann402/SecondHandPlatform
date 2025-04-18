using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    public class EscrowPayment
    {
        [Key]
        public int paymentID { get; set; }

        [Required]
        public int orderID { get; set; }

        public decimal paymentAmount { get; set; }

        public string? paymentStatus { get; set; }

        public DateTime paymentDate { get; set; }

        public int userID { get; set; }

        public int verificationID { get; set; }

        public int productID { get; set; }
    }
}