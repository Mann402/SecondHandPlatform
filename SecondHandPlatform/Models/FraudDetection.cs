using System;
using System.Collections.Generic;

namespace SecondHandPlatform.Models;

public partial class FraudDetection
{
    public int FraudDetectionId { get; set; }

    public int UserId { get; set; }

    public float TypingSpeed { get; set; }

    public string TypingRhythm { get; set; } = null!;

    public bool SuspiciousFlag { get; set; }

    public virtual User User { get; set; } = null!;
}
