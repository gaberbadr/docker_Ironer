using CoreLayer.Entities;
using CoreLayer.Entities.Identity;
using CoreLayer.Entities.Orders;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RepositoryLayer.Data.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        // Add DbSets for all entities
        public DbSet<Address> Addresses { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<TypeOfService> TypeOfServices { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<DeliveryType> DeliveryTypes { get; set; }
        public DbSet<ItemOrder> ItemOrders { get; set; }
        public DbSet<OrderProduct> OrderProducts { get; set; }
        public DbSet<OrderService> OrderServices { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey<ApplicationUser>(u => u.AddressId)
                .OnDelete(DeleteBehavior.SetNull);

            // Make phone number unique
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique();

            // Order -> OrderAddress (Owned)
            builder.Entity<Order>()
                .OwnsOne(o => o.Address, a =>
                {
                    a.Property(p => p.Street).HasMaxLength(200);
                    a.Property(p => p.City).HasMaxLength(100);
                    a.Property(p => p.Government).HasMaxLength(100);
                });

            // Order - User (One to Many)
            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order - Coupon (Optional One to Many)
            builder.Entity<Order>()
                .HasOne(o => o.Coupon)
                .WithMany()
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            // Order - DeliveryType (One to Many)
            builder.Entity<Order>()
                .HasOne(o => o.DeliveryType)
                .WithMany()
                .HasForeignKey(o => o.DeliveryTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ItemOrder - Order (One to Many)
            builder.Entity<ItemOrder>()
                .HasOne(io => io.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(io => io.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderProduct - ItemOrder (One to Many)
            builder.Entity<OrderProduct>()
                .HasOne(op => op.ItemOrder)
                .WithMany(io => io.Products)
                .HasForeignKey(op => op.ItemOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderProduct - Product (One to Many)
            builder.Entity<OrderProduct>()
                .HasOne(op => op.Product)
                .WithMany()
                .HasForeignKey(op => op.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // OrderService - ItemOrder (One to Many)
            builder.Entity<OrderService>()
                .HasOne(os => os.ItemOrder)
                .WithMany(io => io.Services)
                .HasForeignKey(os => os.ItemOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrderService - TypeOfService (One to Many)
            builder.Entity<OrderService>()
                .HasOne(os => os.TypeOfService)
                .WithMany()
                .HasForeignKey(os => os.TypeOfServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - Sender
            builder.Entity<Notification>()
                .HasOne(n => n.Sender)
                .WithMany()
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification - Receiver
            builder.Entity<Notification>()
                .HasOne(n => n.Receiver)
                .WithMany()
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Make coupon name unique
            builder.Entity<Coupon>()
            .HasIndex(c => c.Name)
            .IsUnique();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Apply default precision for all decimal properties
            configurationBuilder
                .Properties<decimal>()
                .HavePrecision(18, 2);
        }
    }
}
