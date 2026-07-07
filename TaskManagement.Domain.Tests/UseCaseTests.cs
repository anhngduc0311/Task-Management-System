using System;
using Xunit;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using DomainTask = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Tests
{
    public class UseCaseTests
    {
        [Fact]
        public void CreateTask_WithValidAttributes_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var projectId = Guid.NewGuid();
            var creatorId = Guid.NewGuid();
            var assigneeId = Guid.NewGuid();
            var dueDate = DateTime.UtcNow.AddDays(7);

            var task = new DomainTask
            {
                ProjectId = projectId,
                Title = "Test Task",
                Description = "Test Description",
                Status = TaskStatus.Todo,
                Priority = TaskPriority.High,
                AssigneeId = assigneeId,
                CreatedById = creatorId,
                DueDate = dueDate
            };

            // Assert
            Assert.Equal(projectId, task.ProjectId);
            Assert.Equal("Test Task", task.Title);
            Assert.Equal(TaskStatus.Todo, task.Status);
            Assert.Equal(TaskPriority.High, task.Priority);
            Assert.Equal(assigneeId, task.AssigneeId);
            Assert.Equal(creatorId, task.CreatedById);
            Assert.Equal(dueDate, task.DueDate);
            Assert.False(task.IsDeleted);
        }

        [Fact]
        public void TransitionTo_ValidTransitions_ShouldSucceed()
        {
            // Todo -> InProgress
            var task = new DomainTask { Status = TaskStatus.Todo };
            task.TransitionTo(TaskStatus.InProgress);
            Assert.Equal(TaskStatus.InProgress, task.Status);

            // InProgress -> InReview
            task.TransitionTo(TaskStatus.InReview);
            Assert.Equal(TaskStatus.InReview, task.Status);

            // InReview -> Done
            task.TransitionTo(TaskStatus.Done);
            Assert.Equal(TaskStatus.Done, task.Status);

            // Done -> Todo (Reopen)
            task.TransitionTo(TaskStatus.Todo);
            Assert.Equal(TaskStatus.Todo, task.Status);
        }

        [Fact]
        public void TransitionTo_InvalidTransitions_ShouldThrowInvalidOperationException()
        {
            // Cancelled -> Done should throw
            var task = new DomainTask { Status = TaskStatus.Cancelled };
            Assert.Throws<InvalidOperationException>(() => task.TransitionTo(TaskStatus.Done));

            // Cancelled -> InProgress should throw
            Assert.Throws<InvalidOperationException>(() => task.TransitionTo(TaskStatus.InProgress));
        }

        [Fact]
        public void UpdateTaskAttributes_ShouldApplyChanges()
        {
            // Arrange
            var task = new DomainTask
            {
                Title = "Old Title",
                Description = "Old Description",
                Priority = TaskPriority.Low,
                DueDate = DateTime.UtcNow.AddDays(1)
            };

            // Act
            task.UpdateTitle("New Title");
            task.Description = "New Description";
            task.Priority = TaskPriority.Critical;
            var newDueDate = DateTime.UtcNow.AddDays(5);
            task.DueDate = newDueDate;

            // Assert
            Assert.Equal("New Title", task.Title);
            Assert.Equal("New Description", task.Description);
            Assert.Equal(TaskPriority.Critical, task.Priority);
            Assert.Equal(newDueDate, task.DueDate);
        }
    }
}
