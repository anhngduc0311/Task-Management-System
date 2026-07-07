using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.FullName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(u => u.Email)
                .HasMaxLength(256)
                .IsRequired();

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.PasswordHash)
                .HasMaxLength(512);

            builder.Property(u => u.ExternalAuthId)
                .HasMaxLength(256);

            builder.Property(u => u.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.AvatarUrl)
                .HasMaxLength(512);

            builder.Property(u => u.RefreshToken)
                .HasMaxLength(512);

            builder.Property(u => u.RefreshTokenExpiryTime);

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.UpdatedAt)
                .IsRequired();

            // Seed default Admin user
            var adminUser = new User
            {
                Id = Guid.Parse("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"),
                FullName = "Administrator",
                Email = "admin@taskmanagement.com",
                // Hashed value of "Admin@12345"
                PasswordHash = "$2a$11$qRz3vYmK3e4e9Fh6z82sNu98c5C6z7b6O8y/eW5G42e9X2aO1q9i.",
                Status = UserStatus.Active,
                CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
            };

            builder.HasData(adminUser);
        }
    }
}
