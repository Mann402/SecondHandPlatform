using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SecondHandPlatform.Models;

public partial class SecondhandplatformContext : DbContext
{
  

    public SecondhandplatformContext(DbContextOptions<SecondhandplatformContext> options)
        : base(options)
    {
    }

public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<FaceRecognition> FaceRecognitions { get; set; }

    public virtual DbSet<Feedback> Feedback { get; set; }

    public virtual DbSet<FraudDetection> FraudDetections { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    public virtual DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseMySql("server=localhost;database=secondhandplatform;user=Puimann;password=Puimann", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.41-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        // Prevent Order Deletion When Cart, Product, or User is Removed
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Cart)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CartId)
            .OnDelete(DeleteBehavior.SetNull);  //  Prevent order deletion when cart is deleted

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Product)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.ProductId)
            .OnDelete(DeleteBehavior.Restrict);  //  Prevent order deletion when product is deleted

        modelBuilder.Entity<Order>()
            .HasOne(o => o.User)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);  //  Prevent order deletion when user is deleted

        //  Ensure IsSold Default Value is False
        modelBuilder.Entity<Product>()
            .Property(p => p.IsSold)
            .HasDefaultValue(false)
            .HasColumnName("IsSold");

        //  Ensure Default Order Status is 'Pending Verification'
        modelBuilder.Entity<Order>()
            .Property(o => o.OrderStatus)
            .HasDefaultValue("Unverified");


        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PRIMARY");

            entity.ToTable("cart");

            entity.HasIndex(e => e.ProductId, "product_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TotalPrice)
                .HasPrecision(10, 2)
                .HasColumnName("total_price");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("cart_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("cart_ibfk_1");
        });

        modelBuilder.Entity<FaceRecognition>(entity =>
        {
            entity.HasKey(e => e.FaceId).HasName("PRIMARY");

            entity.ToTable("face_recognition");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.FaceId).HasColumnName("face_id");
            entity.Property(e => e.PhotoPath)
                .HasMaxLength(255)
                .HasColumnName("photo_path");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerificationStatus)
                .HasMaxLength(20)
                .HasColumnName("verification_status");

            entity.HasOne(d => d.User).WithMany(p => p.FaceRecognitions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("face_recognition_ibfk_1");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PRIMARY");

            entity.ToTable("feedback");

            entity.HasIndex(e => e.ProductId, "product_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.Comment)
                .HasColumnType("text")
                .HasColumnName("comment");
            entity.Property(e => e.DateSubmitted)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("date_submitted");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Product).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("feedback_ibfk_1");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("feedback_ibfk_2");
        });

        modelBuilder.Entity<FraudDetection>(entity =>
        {
            entity.HasKey(e => e.FraudDetectionId).HasName("PRIMARY");

            entity.ToTable("fraud_detection");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.FraudDetectionId).HasColumnName("fraud_detection_id");
            entity.Property(e => e.SuspiciousFlag).HasColumnName("suspicious_flag");
            entity.Property(e => e.TypingRhythm)
                .HasMaxLength(100)
                .HasColumnName("typing_rhythm");
            entity.Property(e => e.TypingSpeed).HasColumnName("typing_speed");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.FraudDetections)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("fraud_detection_ibfk_1");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PRIMARY");

            entity.ToTable("orders");

            entity.HasIndex(e => e.CartId, "cart_id");

            entity.HasIndex(e => e.ProductId, "product_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("order_date");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Pending Verification")
                .HasColumnName("order_status");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.TotalAmount)
                .HasPrecision(10, 2)
                .HasColumnName("total_amount");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Cart)
                .WithMany(p => p.Orders)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("orders_ibfk_2")
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Product).WithMany(p => p.Orders)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("orders_ibfk_3");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("orders_ibfk_1");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PRIMARY");

            entity.ToTable("payments");

            entity.HasIndex(e => e.OrderId, "order_id");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasPrecision(10, 2)
                .HasColumnName("amount");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValueSql("'Pending'")
                .HasColumnName("payment_status");

            entity.HasOne(d => d.Order).WithMany(p => p.Payments)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("payments_ibfk_1");
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            // point EF at your lowercase “orderitems” table in MySQL
            entity.ToTable("orderitems");

            // your PK
            entity.HasKey(e => e.OrderItemId)
                  .HasName("PRIMARY");

            // map each property to its snake_case column
            entity.Property(e => e.OrderItemId)
                  .HasColumnName("order_item_id");
            entity.Property(e => e.OrderId)
                  .HasColumnName("order_id");
            entity.Property(e => e.ProductId)
                  .HasColumnName("product_id");
            entity.Property(e => e.Quantity)
                  .HasColumnName("quantity");

            // relationships
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                  .WithMany()    // or .WithMany(p => p.OrderItems) if you added that nav
                  .HasForeignKey(oi => oi.ProductId);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PRIMARY");

            entity.ToTable("products");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.DatePosted)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("date_posted");
            entity.Property(e => e.IsSold)
                .HasDefaultValue(false)
                .HasColumnName("IsSold");
            entity.Property(e => e.ProductCategory)
                .HasMaxLength(50)
                .HasColumnName("product_category");
            entity.Property(e => e.ProductCondition)
                .HasMaxLength(20)
                .HasColumnName("product_condition");
            entity.Property(e => e.ProductDescription)
                .HasColumnType("text")
                .HasColumnName("product_description");
            entity.Property(e => e.ProductImage)
                .HasMaxLength(255)
                .HasColumnName("product_image");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasColumnName("product_name");
            entity.Property(e => e.ProductPrice)
                .HasPrecision(10, 2)
                .HasColumnName("product_price");
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(20)
                .HasDefaultValueSql("Pending Verification")
                .HasColumnName("product_status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Products)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("products_ibfk_1");
        });


        // —— map your UserAddress entity to shipping_addresses table —— 
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.ToTable("shipping_addresses");
            entity.HasKey(e => e.UserAddressId).HasName("PRIMARY");
            entity.Property(e => e.UserAddressId)
                  .HasColumnName("shipping_address_id");
            entity.Property(e => e.UserId)
                  .HasColumnName("user_id");
            entity.Property(e => e.Address)
                  .HasColumnName("address");
            entity.Property(e => e.City)
                  .HasColumnName("city");
            entity.Property(e => e.Postcode)
                  .HasColumnName("postcode");
            entity.Property(e => e.State)
                  .HasColumnName("state");
            entity.Property(e => e.PhoneNumber)
                  .HasColumnName("phone_number");
            entity.Property(e => e.IsDefault)
                  .HasColumnName("is_default");
            entity.Property(e => e.CreatedDate)
                  .HasColumnName("created_date");
            entity.Property(e => e.ModifiedDate)
                  .HasColumnName("modified_date");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.UserAddresses)   // make sure User has `ICollection<UserAddress> UserAddresses`
                  .HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.StudentCardPicture)
                .HasMaxLength(255)
                .HasColumnName("student_card_picture");
            entity.Property(e => e.UserStatus)
                .HasMaxLength(20)
                .HasColumnName("user_status");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
