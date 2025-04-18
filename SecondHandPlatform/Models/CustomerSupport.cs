using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    public class CustomerSupport
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ticketID { set; get; }

        public int userID { set; get; }

        public string? userName { get; set; }

        public string? userEmail { set; get; }

        public string? issueDetails { set; get; }

        public string? customerMessage { get; set; }

        public string? response { set; get; }

        public DateTime? createdDate { set; get; }

        public DateTime? resolveDate { set; get; }

        [ForeignKey("userID")]
        public UserAccountManagement UserAccountManagement { get; set; }

    }
}