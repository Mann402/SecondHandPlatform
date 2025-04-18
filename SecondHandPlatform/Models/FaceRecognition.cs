using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SecondHandPlatform.Models;

public partial class FaceRecognition
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

    public int FaceId { get; set; }

    public int UserId { get; set; }
    //[NotMapped] // Tells EF Core to ignore this entity

    public string PhotoPath { get; set; } = null!;

    public string VerificationStatus { get; set; } = null!;
    
    public virtual User User { get; set; } = null!;
}
