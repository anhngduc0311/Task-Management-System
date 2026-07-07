using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");

            builder.HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed assignments
            builder.HasData(
                new UserRole { UserId = Guid.Parse("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"), RoleId = 1 }, // Admin
                new UserRole { UserId = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), RoleId = 2 }, // PM
                new UserRole { UserId = Guid.Parse("2a98e29a-2454-4fbb-91bc-341aefba6222"), RoleId = 3 }, // Member
                new UserRole { UserId = Guid.Parse("3f78e7aa-2e45-424a-81a1-f3b17789a333"), RoleId = 4 }  // Guest
            );
        }
    }
}
