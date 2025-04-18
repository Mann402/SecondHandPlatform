using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models;

public partial class User
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public int UserId { get; set; }
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string LastName { get; set; } = null!;
    [Required,EmailAddress]
    public string Email { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;

    public byte[]? StudentCardPicture { get; set; }

    public string? UserStatus { get; set; } = "Active";

    [NotMapped]
    public IFormFile? StudentCardFile { get; set; }

    public virtual ICollection<Cart> Carts { get; } = new List<Cart>();
    public virtual ICollection<FaceRecognition> FaceRecognitions { get; set; } = new List<FaceRecognition>();

    // public virtual ICollection<FaceRecognition> FaceRecognitions { get; } = new List<FaceRecognition>();

    public virtual ICollection<Feedback> Feedbacks { get; } = new List<Feedback>();

    public virtual ICollection<FraudDetection> FraudDetections { get; } = new List<FraudDetection>();

    public virtual ICollection<Order> Orders { get; } = new List<Order>();

    public virtual ICollection<Product> Products { get; } = new List<Product>();

    public virtual ICollection<UserAddress> UserAddresses { get; set; }
    = new List<UserAddress>();
}
