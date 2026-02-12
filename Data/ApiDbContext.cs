using Microsoft.EntityFrameworkCore;
using GelirGiderTakip.API.Models;

namespace GelirGiderTakip.API.Data
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Account> Accounts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.UserId);
            });

            // Seed admin user
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasData(
                // Admin kullanıcı (şifre: Admin123!)
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@gelir-gider.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    FirstName = "System",
                    LastName = "Administrator",
                    Role = UserRole.Admin,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Emin kullanıcı (şifre: Emin123!)
                new User
                {
                    Id = 2,
                    Username = "emin",
                    Email = "emin@gelir-gider.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Emin123!"),
                    FirstName = "Emin",
                    LastName = "Kullanıcı",
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Nihat kullanıcı (şifre: Nihat123!)
                new User
                {
                    Id = 3,
                    Username = "nihat",
                    Email = "nihat@gelir-gider.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Nihat123!"),
                    FirstName = "Nihat",
                    LastName = "Kullanıcı",
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                // Arif kullanıcı (şifre: Arif123!)
                new User
                {
                    Id = 4,
                    Username = "arif",
                    Email = "arif@gelir-gider.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Arif123!"),
                    FirstName = "Arif",
                    LastName = "Kullanıcı",
                    Role = UserRole.User,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
