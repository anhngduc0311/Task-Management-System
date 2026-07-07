using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class TaskDynamicFieldValueConfiguration : IEntityTypeConfiguration<TaskDynamicFieldValue>
    {
        public void Configure(EntityTypeBuilder<TaskDynamicFieldValue> builder)
        {
            builder.ToTable("TaskDynamicFieldValues");

            builder.HasKey(t => new { t.TaskId, t.DynamicFieldId });

            builder.Property(t => t.FieldValue)
                .HasMaxLength(4000);

            builder.HasOne(t => t.Task)
                .WithMany(t => t.DynamicFieldValues)
                .HasForeignKey(t => t.TaskId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(t => t.DynamicFieldDefinition)
                .WithMany(d => d.TaskDynamicFieldValues)
                .HasForeignKey(t => t.DynamicFieldId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
