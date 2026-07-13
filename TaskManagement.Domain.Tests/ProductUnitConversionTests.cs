using System;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Domain.Tests
{
    public class ProductUnitConversionTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-0.5)]
        public void ConversionRate_SetZeroOrNegative_ShouldThrowArgumentException(decimal invalidRate)
        {
            // Arrange
            var conversion = new ProductUnitConversion();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => conversion.ConversionRate = invalidRate);
            Assert.Equal("Conversion rate must be greater than zero.", exception.Message);
        }

        [Fact]
        public void ConversionRate_SetPositive_ShouldSucceed()
        {
            // Arrange
            var conversion = new ProductUnitConversion();
            decimal validRate = 12.5m;

            // Act
            conversion.ConversionRate = validRate;

            // Assert
            Assert.Equal(validRate, conversion.ConversionRate);
        }
    }
}
