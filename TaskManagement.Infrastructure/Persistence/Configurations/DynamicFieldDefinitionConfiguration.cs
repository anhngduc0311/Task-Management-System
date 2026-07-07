using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;
using TaskManagement.Domain.Entities;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class DynamicFieldDefinitionConfiguration : IEntityTypeConfiguration<DynamicFieldDefinition>
    {
        public void Configure(EntityTypeBuilder<DynamicFieldDefinition> builder)
        {
            builder.ToTable("DynamicFieldDefinitions");

            builder.HasKey(d => d.Id);

            builder.Property(d => d.FieldName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(d => d.FieldKey)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(d => d.FieldType)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(d => d.IsRequired)
                .IsRequired();

            var comparer = new ValueComparer<List<string>>(
                (c1, c2) => c1 != null && c2 != null ? c1.SequenceEqual(c2) : c1 == c2,
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );

            builder.Property(d => d.Options)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                )
                .Metadata.SetValueComparer(comparer);

            builder.Property(d => d.DefaultValue)
                .HasMaxLength(4000);

            builder.Property(d => d.DisplayOrder)
                .IsRequired();

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(d => d.CreatedAt)
                .IsRequired();

            builder.Property(d => d.UpdatedAt)
                .IsRequired();

            builder.HasOne(d => d.Project)
                .WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(d => new { d.ProjectId, d.FieldKey })
                .IsUnique();
        }
    }
}
