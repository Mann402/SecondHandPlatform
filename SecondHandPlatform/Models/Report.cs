using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    public class Report
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int reportID { get; set; }

        [Required]
        public string? reportType { get; set; }

        public DateTime generatedDate { get; set; }

        public string? dataSummary { get; set; }

        public string? reportDetails { get; set; }
    }
}