using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace SecondHandPlatform.DTOs
{
    public class RegisterUserDto
    {
        [Required(ErrorMessage = "First Name is required.")]
        [FromForm(Name = "first_name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [FromForm(Name = "last_name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [FromForm(Name = "email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        [FromForm(Name = "password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Student card file is required.")]
        [FromForm(Name = "student_card")]
        public IFormFile StudentCardFile { get; set; }
    }

  
}
