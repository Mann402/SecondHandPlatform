using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    [Table("Admin")]
    public class Admin
    {
        [Key]
        public string? AdminID { get; set; }

        [Required]
        public string? AdminName { get; set; }

        [Required]
        public string? AdminEmail { get; set; }

        [Required]
        public string? AdminPassword { get; set; }
    }
}