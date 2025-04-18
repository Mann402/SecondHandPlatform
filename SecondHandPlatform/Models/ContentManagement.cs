using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    public class ContentManagement
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int announcementID { get; set; }

        public string? announcementTitle { get; set; }

        public string? announcementContent { get; set; }

        public DateOnly publishDate { get; set; }

        public DateOnly updateDate { get; set; }
    }
}