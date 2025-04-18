using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SecondHandPlatform.Models
{
    [Table("userAccountManagement")]
    public class UserAccountManagement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int userID { get; set; }

        public string? userName { get; set; }

        public string? userEmail { get; set; }

        public string? userAccountStatus { get; set; }

        public string? deactivateReason { get; set; }

    }
}