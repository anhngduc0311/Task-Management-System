using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
    {
        public void Configure(EntityTypeBuilder<ProjectMember> builder)
        {
            builder.ToTable("ProjectMembers");

            builder.HasKey(pm => new { pm.ProjectId, pm.UserId });

            builder.Property(pm => pm.RoleInProject)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(pm => pm.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(pm => pm.JoinedAt)
                .IsRequired();

            builder.HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed project memberships
            builder.HasData(
                new ProjectMember
                {
                    ProjectId = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                    UserId = Guid.Parse("1d5952f4-bb85-451f-bfbd-ef1b11a5e111"),
                    RoleInProject = ProjectMemberRole.ProjectManager,
                    Status = ProjectMemberStatus.Active,
                    JoinedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProjectMember
                {
                    ProjectId = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                    UserId = Guid.Parse("2a98e29a-2454-4fbb-91bc-341aefba6222"),
                    RoleInProject = ProjectMemberRole.Member,
                    Status = ProjectMemberStatus.Active,
                    JoinedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new ProjectMember
                {
                    ProjectId = Guid.Parse("c7a52f44-8842-45e6-bd51-24ff43521234"),
                    UserId = Guid.Parse("3f78e7aa-2e45-424a-81a1-f3b17789a333"),
                    RoleInProject = ProjectMemberRole.Guest,
                    Status = ProjectMemberStatus.Active,
                    JoinedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
