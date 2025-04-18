using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;


namespace SecondHandPlatform.DTOs
{
    // DTO for user registration face verification
    public class UserRegistrationFaceVerifyDto
    {
        [FromForm(Name = "webcam_image")] // Correct name to match React FormData
        public IFormFile LiveImage { get; set; }
        [FromForm(Name = "email")] // Correct name to match React FormData
        public string Email { get; set; }
        // Added TempId property
        [FromForm(Name = "temp_id")]
        public string TempId { get; set; }
    }

    // DTO for general face verification
    public class FaceVerificationDto
    {
        [FromForm(Name = "LiveImage")] // Correct name to match React FormData

        public IFormFile LiveImage { get; set; }
        [FromForm(Name = "Email")] // Correct name to match React FormData

        public string Email { get; set; }
        // Added TempId property
        [FromForm(Name = "temp_id")]
        public string TempId { get; set; }
    }

    // Existing DTO for face registration
    public class FaceRegisterDto
    {
        public IFormFile FaceImage { get; set; }
    }
}
