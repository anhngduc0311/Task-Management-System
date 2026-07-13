using System;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.EntityFrameworkCore;
using Xunit;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Domain.Tests
{
    public class UnitConversionServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        [Fact]
        public async Task ConvertToBaseUnitAsync_WithBaseUnit_ReturnsSameQuantity()
        {
            // Arrange
            var db = CreateDbContext();
            var unitId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            db.Units.Add(new Unit { Id = unitId, Code = "CAI", Name = "Cái" });
            db.Products.Add(new Product
            {
                Id = productId,
                ProductCode = "SP001",
                Name = "Sản phẩm A",
                BaseUnitId = unitId,
                DefaultPrice = 100
            });
            await db.SaveChangesAsync();

            var service = new UnitConversionService(db);

            // Act
            var result = await service.ConvertToBaseUnitAsync(productId, unitId, 5);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task ConvertToBaseUnitAsync_WithConversionRate_ReturnsConvertedQuantity()
        {
            // Arrange
            var db = CreateDbContext();
            var baseUnitId = Guid.NewGuid();
            var secondaryUnitId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            db.Units.Add(new Unit { Id = baseUnitId, Code = "CAI", Name = "Cái" });
            db.Units.Add(new Unit { Id = secondaryUnitId, Code = "HOP", Name = "Hộp" });
            db.Products.Add(new Product
            {
                Id = productId,
                ProductCode = "SP001",
                Name = "Sản phẩm A",
                BaseUnitId = baseUnitId,
                DefaultPrice = 100
            });
            db.ProductUnitConversions.Add(new ProductUnitConversion
            {
                ProductId = productId,
                FromUnitId = secondaryUnitId,
                ToUnitId = baseUnitId,
                ConversionRate = 10
            });
            await db.SaveChangesAsync();

            var service = new UnitConversionService(db);

            // Act
            var result = await service.ConvertToBaseUnitAsync(productId, secondaryUnitId, 5);

            // Assert
            Assert.Equal(50, result);
        }

        [Fact]
        public async Task ConvertToBaseUnitAsync_WithInvalidProduct_ThrowsArgumentException()
        {
            // Arrange
            var db = CreateDbContext();
            var service = new UnitConversionService(db);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ConvertToBaseUnitAsync(Guid.NewGuid(), Guid.NewGuid(), 5));
        }

        [Fact]
        public async Task ConvertToBaseUnitAsync_WithMissingConversionPath_ThrowsInvalidOperationException()
        {
            // Arrange
            var db = CreateDbContext();
            var baseUnitId = Guid.NewGuid();
            var secondaryUnitId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            db.Units.Add(new Unit { Id = baseUnitId, Code = "CAI", Name = "Cái" });
            db.Units.Add(new Unit { Id = secondaryUnitId, Code = "HOP", Name = "Hộp" });
            db.Products.Add(new Product
            {
                Id = productId,
                ProductCode = "SP001",
                Name = "Sản phẩm A",
                BaseUnitId = baseUnitId,
                DefaultPrice = 100
            });
            await db.SaveChangesAsync();

            var service = new UnitConversionService(db);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ConvertToBaseUnitAsync(productId, secondaryUnitId, 5));
        }

        [Fact]
        public async Task ConvertFromBaseUnitAsync_WithConversionRate_ReturnsConvertedQuantity()
        {
            // Arrange
            var db = CreateDbContext();
            var baseUnitId = Guid.NewGuid();
            var secondaryUnitId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            db.Units.Add(new Unit { Id = baseUnitId, Code = "CAI", Name = "Cái" });
            db.Units.Add(new Unit { Id = secondaryUnitId, Code = "HOP", Name = "Hộp" });
            db.Products.Add(new Product
            {
                Id = productId,
                ProductCode = "SP001",
                Name = "Sản phẩm A",
                BaseUnitId = baseUnitId,
                DefaultPrice = 100
            });
            db.ProductUnitConversions.Add(new ProductUnitConversion
            {
                ProductId = productId,
                FromUnitId = secondaryUnitId,
                ToUnitId = baseUnitId,
                ConversionRate = 10
            });
            await db.SaveChangesAsync();

            var service = new UnitConversionService(db);

            // Act
            var result = await service.ConvertFromBaseUnitAsync(productId, secondaryUnitId, 50);

            // Assert
            Assert.Equal(5, result);
        }

        [Fact]
        public async Task ConvertPriceToUnitAsync_WithConversionRate_ReturnsConvertedPrice()
        {
            // Arrange
            var db = CreateDbContext();
            var baseUnitId = Guid.NewGuid();
            var secondaryUnitId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            db.Units.Add(new Unit { Id = baseUnitId, Code = "CAI", Name = "Cái" });
            db.Units.Add(new Unit { Id = secondaryUnitId, Code = "HOP", Name = "Hộp" });
            db.Products.Add(new Product
            {
                Id = productId,
                ProductCode = "SP001",
                Name = "Sản phẩm A",
                BaseUnitId = baseUnitId,
                DefaultPrice = 100
            });
            db.ProductUnitConversions.Add(new ProductUnitConversion
            {
                ProductId = productId,
                FromUnitId = secondaryUnitId,
                ToUnitId = baseUnitId,
                ConversionRate = 10
            });
            await db.SaveChangesAsync();

            var service = new UnitConversionService(db);

            // Act
            var result = await service.ConvertPriceToUnitAsync(productId, 10, secondaryUnitId);

            // Assert
            Assert.Equal(100, result);
        }
    }
}
