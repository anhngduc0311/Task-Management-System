using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("Projects");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasMaxLength(2000);

            builder.Property(p => p.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .IsRequired();

            builder.HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed default project
            builder.HasData(new Project
            {
                Id = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                Name = "Core Platform Development",
                Description = "Phát triển nền tảng ứng dụng Quản lý công việc cốt lõi của doanh nghiệp.",
                OwnerId = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"), // PM
                Status = ProjectStatus.Active,
                CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
