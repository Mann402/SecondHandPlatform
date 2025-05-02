using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SecondHandPlatform.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public int Rating { get; set; }

    public string Comment { get; set; }

    public DateTime DateSubmitted { get; set; } = DateTime.UtcNow;
    public virtual Product ? Product { get; set; }

    public virtual User?  User { get; set; }


}
