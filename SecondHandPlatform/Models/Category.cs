using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    [Table("categories")]
    public class Category
    {
        [Column("category_id")] 
        public int CategoryId { get; set; }
        [Column("category_name")]
        public string Name { get; set; } = null!;
        [Column("slug")] 
        public string Slug { get; set; } = null!;
        [Column("date_created")] 
        public DateTime DateCreated { get; set; }

        public virtual ICollection<Product> Products { get; set; }
          = new List<Product>();
    }
}
