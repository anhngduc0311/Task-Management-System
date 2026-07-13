using System;
using System.Collections.Generic;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Domain.Tests
{
    public class ProductCategoryTests
    {
        [Fact]
        public void UpdateParent_WithSelfAsParent_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var category = new ProductCategory { Id = Guid.NewGuid(), Name = "Category A" };
            var allCategories = new List<ProductCategory> { category };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                category.UpdateParent(category.Id, allCategories));
            Assert.Equal("A category cannot be its own parent.", exception.Message);
        }

        [Fact]
        public void UpdateParent_WithCircularReference_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var catA = new ProductCategory { Id = Guid.NewGuid(), Name = "Cat A" };
            var catB = new ProductCategory { Id = Guid.NewGuid(), Name = "Cat B", ParentId = catA.Id };
            var catC = new ProductCategory { Id = Guid.NewGuid(), Name = "Cat C", ParentId = catB.Id };

            var allCategories = new List<ProductCategory> { catA, catB, catC };

            // Act & Assert: try to make catC parent of catA
            var exception = Assert.Throws<InvalidOperationException>(() =>
                catA.UpdateParent(catC.Id, allCategories));
            Assert.Equal("Circular reference detected in category hierarchy.", exception.Message);
        }

        [Fact]
        public void UpdateParent_WithValidParent_ShouldUpdateParentId()
        {
            // Arrange
            var catA = new ProductCategory { Id = Guid.NewGuid(), Name = "Cat A" };
            var catB = new ProductCategory { Id = Guid.NewGuid(), Name = "Cat B" };
            var allCategories = new List<ProductCategory> { catA, catB };

            // Act
            catB.UpdateParent(catA.Id, allCategories);

            // Assert
            Assert.Equal(catA.Id, catB.ParentId);
        }
    }
}
