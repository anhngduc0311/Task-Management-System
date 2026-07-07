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

            // Seed default users
            var adminUser = new User
            {
                Id = Guid.Parse("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"),
                FullName = "Administrator",
                Email = "admin@taskmanagement.com",
                PasswordHash = "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq", // Admin@12345
                Status = UserStatus.Active,
                CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
            };

            var pmUser = new User
            {
                Id = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                FullName = "Project Manager User",
                Email = "pm@taskmanagement.com",
                PasswordHash = "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq", // Admin@12345
                Status = UserStatus.Active,
                CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
            };

            var memberUser = new User
            {
                Id = Guid.Parse("2a98e29a-2454-4fbb-91bc-341aefba6222"),
                FullName = "Regular Member User",
                Email = "member@taskmanagement.com",
                PasswordHash = "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq", // Admin@12345
                Status = UserStatus.Active,
                CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
            };

            var guestUser = new User
            {
                Id = Guid.Parse("3f78e7aa-2e45-424a-81a1-f3b17789a333"),
                FullName = "Guest User",
                Email = "guest@taskmanagement.com",
                PasswordHash = "$2a$11$roDRW6Ytx41flf36/FevM.l02hymIFkzhoEw8XvK/vnbS28GF6Bnq", // Admin@12345
                Status = UserStatus.Active,
                CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
            };

            builder.HasData(adminUser, pmUser, memberUser, guestUser);
        }
    }
}
