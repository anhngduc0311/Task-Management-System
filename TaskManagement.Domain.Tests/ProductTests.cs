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

        [Fact]
        public void VariantCombination_WithValidAttributes_ShouldSucceed()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "Phone" };
            var group = new ProductAttributeGroup { ProductId = product.Id, Name = "Color" };
            var val1 = new ProductAttributeValue { AttributeGroup = group, Value = "Red" };
            var val2 = new ProductAttributeValue { AttributeGroup = group, Value = "Blue" };

            var variant1 = new ProductVariant { ProductId = product.Id, SKU = "P-RED" };
            variant1.VariantAttributeValues.Add(new ProductVariantAttributeValue { ProductAttributeValue = val1 });

            var variant2 = new ProductVariant { ProductId = product.Id, SKU = "P-BLUE" };
            variant2.VariantAttributeValues.Add(new ProductVariantAttributeValue { ProductAttributeValue = val2 });

            product.Variants.Add(variant1);
            product.Variants.Add(variant2);

            // Assert
            Assert.Equal(2, product.Variants.Count);
            Assert.Contains(variant1, product.Variants);
            Assert.Contains(variant2, product.Variants);
        }
    }
}
