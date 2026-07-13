using System;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Domain.Tests
{
    public class ProductTests
    {
        [Fact]
        public void AddAttributeGroup_WithLessThanTwoGroups_ShouldSucceed()
        {
            // Arrange
            var product = new Product { Name = "Shirt" };
            var group1 = new ProductAttributeGroup { Name = "Color" };
            var group2 = new ProductAttributeGroup { Name = "Size" };

            // Act
            product.AddAttributeGroup(group1);
            product.AddAttributeGroup(group2);

            // Assert
            Assert.Equal(2, product.AttributeGroups.Count);
        }

        [Fact]
        public void AddAttributeGroup_WithMoreThanTwoGroups_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var product = new Product { Name = "Shirt" };
            var group1 = new ProductAttributeGroup { Name = "Color" };
            var group2 = new ProductAttributeGroup { Name = "Size" };
            var group3 = new ProductAttributeGroup { Name = "Material" };

            product.AddAttributeGroup(group1);
            product.AddAttributeGroup(group2);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                product.AddAttributeGroup(group3));
            Assert.Equal("A product can have a maximum of 2 attribute groups.", exception.Message);
        }
    }
}
