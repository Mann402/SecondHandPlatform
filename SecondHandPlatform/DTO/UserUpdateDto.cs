using System.ComponentModel.DataAnnotations;

namespace SecondHandPlatform.DTOs
{
    public class UserUpdateDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        // Optional password fields
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}