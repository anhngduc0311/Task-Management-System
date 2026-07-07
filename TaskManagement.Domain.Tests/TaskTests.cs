using System;
using Xunit;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using Task = TaskManagement.Domain.Entities.Task;
using TaskStatus = TaskManagement.Domain.Enums.TaskStatus;

namespace TaskManagement.Domain.Tests
{
    public class TaskTests
    {
        [Fact]
        public void UpdateTitle_WithValidTitle_ShouldUpdateTitle()
        {
            // Arrange
            var task = new Task { Title = "Old Title" };
            var newTitle = "New Valid Title";

            // Act
            task.UpdateTitle(newTitle);

            // Assert
            Assert.Equal(newTitle, task.Title);
        }

        [Fact]
        public void UpdateTitle_WithEmptyTitle_ShouldThrowArgumentException()
        {
            // Arrange
            var task = new Task { Title = "Valid Title" };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => task.UpdateTitle(""));
            Assert.Throws<ArgumentException>(() => task.UpdateTitle("   "));
        }

        [Fact]
        public void UpdateTitle_WithTooLongTitle_ShouldThrowArgumentException()
        {
            // Arrange
            var task = new Task { Title = "Valid Title" };
            var longTitle = new string('A', 201);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => task.UpdateTitle(longTitle));
        }

        [Fact]
        public void TransitionTo_FromCancelledToInProgress_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var task = new Task { Status = TaskStatus.Cancelled };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => task.TransitionTo(TaskStatus.InProgress));
            Assert.Equal("A cancelled task can only be reopened to Todo status.", exception.Message);
        }

        [Fact]
        public void TransitionTo_FromCancelledToTodo_ShouldSucceed()
        {
            // Arrange
            var task = new Task { Status = TaskStatus.Cancelled };

            // Act
            task.TransitionTo(TaskStatus.Todo);

            // Assert
            Assert.Equal(TaskStatus.Todo, task.Status);
        }

        [Fact]
        public void TransitionTo_FromTodoToInProgress_ShouldSucceed()
        {
            // Arrange
            var task = new Task { Status = TaskStatus.Todo };

            // Act
            task.TransitionTo(TaskStatus.InProgress);

            // Assert
            Assert.Equal(TaskStatus.InProgress, task.Status);
        }
    }
}
