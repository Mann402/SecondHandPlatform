using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecondHandPlatform.Models
{
    // Map the model to the shipping_addresses table
    [Table("shipping_addresses")]
    public class UserAddress
    {
        // Primary key maps to shipping_address_id
        [Key]
        [Column("shipping_address_id")]
        public int UserAddressId { get; set; }

        // Foreign key linking to Users (assumes Users table has user_id as PK)
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        // The actual address line
        [Required]
        [StringLength(255)]
        [Column("address")]
        public string Address { get; set; }

        // New property: city (added to match your database)
        [Required]
        [StringLength(100)]
        [Column("city")]
        public string City { get; set; }

        // Postal code
        [Required]
        [StringLength(20)]
        [Column("postcode")]
        public string Postcode { get; set; }

        // State/province
        [Required]
        [StringLength(100)]
        [Column("state")]
        public string State { get; set; }

        // New phone number field; adjusted string length to match database definition
        [StringLength(30)]
        [Column("phone_number")]
        public string PhoneNumber { get; set; }

        // Indicates if this is the default shipping address
        [Column("is_default")]
        public bool IsDefault { get; set; } = true;

        // Timestamps
        [Column("created_date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Column("modified_date")]
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        // Navigation property to the User entity
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
