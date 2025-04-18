using Microsoft.EntityFrameworkCore;
using SecondHandPlatform.Models;

namespace SecondHandPlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Product> Product { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Cart> Cart { get; set; }
        public DbSet<Payment> Payment { get; set; }
        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<FaceRecognition> FaceRecognitions { get; set; }



        //Tables
        public DbSet<Admin> Admins { set; get; }
        public DbSet<ContentManagement> ContentManagements { get; set; }
        public DbSet<CustomerSupport> CustomerSupports { get; set; }
        public DbSet<EscrowPayment> EscrowPayments { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<UserAccountManagement> UserAccountManagements { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FaceRecognition>()
                .HasOne(f => f.User)
                .WithMany(u => u.FaceRecognitions)
                .HasForeignKey(f => f.UserId);

            // Configure the relationship between User and UserAddress
            modelBuilder.Entity<UserAddress>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public DbSet<FraudDetection> FraudDetection{ get; set; }



    }
}
