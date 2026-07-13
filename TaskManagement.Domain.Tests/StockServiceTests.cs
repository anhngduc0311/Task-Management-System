#pragma warning disable CS8603, CS8625, CS8767, CS8602, CS0162

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using TaskManagement.Application.Services;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskManagement.Domain.Tests
{
    public class StockServiceTests
    {
        private AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private IConfiguration CreateConfiguration(bool allowNegativeStock)
        {
            return new MockConfiguration(allowNegativeStock);
        }

        private class MockConfiguration : IConfiguration
        {
            private readonly string _allowNegativeStock;

            public MockConfiguration(bool allowNegativeStock)
            {
                _allowNegativeStock = allowNegativeStock.ToString().ToLower();
            }

            public string this[string key]
            {
                get => key == "Inventory:AllowNegativeStock" ? _allowNegativeStock : null;
                set => throw new NotImplementedException();
            }

            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public IChangeToken GetReloadToken() => new MockChangeToken();
            public IConfigurationSection GetSection(string key) => null;
        }

        private class MockChangeToken : IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object> callback, object state) => null;
        }

        [Fact]
        public async Task AdjustStockAsync_PositiveQuantity_Succeeds()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(false);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var service = new StockService(db, config);

            // Act
            await service.AdjustStockAsync(
                warehouseId,
                productId,
                null,
                10,
                MovementType.Import,
                Guid.NewGuid(),
                "NK-001",
                userId
            );
            await db.SaveChangesAsync();

            // Assert
            var balance = await db.StockBalances.FirstOrDefaultAsync();
            Assert.NotNull(balance);
            Assert.Equal(10, balance.Quantity);

            var movement = await db.StockMovements.FirstOrDefaultAsync();
            Assert.NotNull(movement);
            Assert.Equal(10, movement.Quantity);
            Assert.Equal(MovementType.Import, movement.MovementType);
        }

        [Fact]
        public async Task AdjustStockAsync_NegativeQuantity_AllowNegativeFalse_ThrowsException()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(false);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var service = new StockService(db, config);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AdjustStockAsync(
                    warehouseId,
                    productId,
                    null,
                    -5,
                    MovementType.Export,
                    Guid.NewGuid(),
                    "XK-001",
                    userId
                )
            );
        }

        [Fact]
        public async Task AdjustStockAsync_NegativeQuantity_AllowNegativeTrue_Succeeds()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(true);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();

            var service = new StockService(db, config);

            // Act
            await service.AdjustStockAsync(
                warehouseId,
                productId,
                null,
                -5,
                MovementType.Export,
                Guid.NewGuid(),
                "XK-001",
                userId
            );
            await db.SaveChangesAsync();

            // Assert
            var balance = await db.StockBalances.FirstOrDefaultAsync();
            Assert.NotNull(balance);
            Assert.Equal(-5, balance.Quantity);
        }

        [Fact]
        public async Task ConfirmImportReceiptAsync_ValidDraft_UpdatesStockAndStatus()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(false);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var receiptId = Guid.NewGuid();

            db.Warehouses.Add(new Warehouse { Id = warehouseId, Code = "WH1", Name = "Warehouse 1", IsActive = true });
            var receipt = new ImportReceipt
            {
                Id = receiptId,
                ReceiptNo = "NK-01",
                WarehouseId = warehouseId,
                Status = ReceiptStatus.Draft,
                Lines = new List<ImportReceiptLine>
                {
                    new ImportReceiptLine
                    {
                        ProductId = productId,
                        Quantity = 10,
                        BaseQuantity = 10,
                        UnitPrice = 100,
                        Amount = 1000
                    }
                }
            };
            db.ImportReceipts.Add(receipt);
            await db.SaveChangesAsync();

            var service = new StockService(db, config);

            // Act
            await service.ConfirmImportReceiptAsync(receiptId, userId);

            // Assert
            var updatedReceipt = await db.ImportReceipts.FindAsync(receiptId);
            Assert.NotNull(updatedReceipt);
            Assert.Equal(ReceiptStatus.Confirmed, updatedReceipt.Status);

            var balance = await db.StockBalances.FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId && sb.ProductId == productId);
            Assert.NotNull(balance);
            Assert.Equal(10, balance.Quantity);
        }

        [Fact]
        public async Task CancelImportReceiptAsync_ConfirmedImport_RevertsStockAndStatus()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(false);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var receiptId = Guid.NewGuid();

            var receipt = new ImportReceipt
            {
                Id = receiptId,
                ReceiptNo = "NK-01",
                WarehouseId = warehouseId,
                Status = ReceiptStatus.Confirmed,
                Lines = new List<ImportReceiptLine>
                {
                    new ImportReceiptLine
                    {
                        ProductId = productId,
                        Quantity = 10,
                        BaseQuantity = 10,
                        UnitPrice = 100,
                        Amount = 1000
                    }
                }
            };
            db.ImportReceipts.Add(receipt);
            db.StockBalances.Add(new StockBalance
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = 15,
                LastUpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = new StockService(db, config);

            // Act
            await service.CancelImportReceiptAsync(receiptId, userId);

            // Assert
            var updatedReceipt = await db.ImportReceipts.FindAsync(receiptId);
            Assert.NotNull(updatedReceipt);
            Assert.Equal(ReceiptStatus.Cancelled, updatedReceipt.Status);

            var balance = await db.StockBalances.FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId && sb.ProductId == productId);
            Assert.NotNull(balance);
            Assert.Equal(5, balance.Quantity); // 15 - 10 = 5
        }

        [Fact]
        public async Task ConfirmExportReceiptAsync_ValidDraft_UpdatesStockAndStatus()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(false);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var receiptId = Guid.NewGuid();

            db.Warehouses.Add(new Warehouse { Id = warehouseId, Code = "WH1", Name = "Warehouse 1", IsActive = true });
            var receipt = new ExportReceipt
            {
                Id = receiptId,
                ReceiptNo = "XK-01",
                WarehouseId = warehouseId,
                Status = ReceiptStatus.Draft,
                Lines = new List<ExportReceiptLine>
                {
                    new ExportReceiptLine
                    {
                        ProductId = productId,
                        Quantity = 5,
                        BaseQuantity = 5
                    }
                }
            };
            db.ExportReceipts.Add(receipt);
            db.StockBalances.Add(new StockBalance
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = 20,
                LastUpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = new StockService(db, config);

            // Act
            await service.ConfirmExportReceiptAsync(receiptId, userId);

            // Assert
            var updatedReceipt = await db.ExportReceipts.FindAsync(receiptId);
            Assert.NotNull(updatedReceipt);
            Assert.Equal(ReceiptStatus.Confirmed, updatedReceipt.Status);

            var balance = await db.StockBalances.FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId && sb.ProductId == productId);
            Assert.NotNull(balance);
            Assert.Equal(15, balance.Quantity); // 20 - 5 = 15
        }

        [Fact]
        public async Task ConfirmExportReceiptAsync_InsufficientStock_ThrowsException()
        {
            // Arrange
            var db = CreateDbContext();
            var config = CreateConfiguration(false);
            var warehouseId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var receiptId = Guid.NewGuid();

            db.Warehouses.Add(new Warehouse { Id = warehouseId, Code = "WH1", Name = "Warehouse 1", IsActive = true });
            var receipt = new ExportReceipt
            {
                Id = receiptId,
                ReceiptNo = "XK-01",
                WarehouseId = warehouseId,
                Status = ReceiptStatus.Draft,
                Lines = new List<ExportReceiptLine>
                {
                    new ExportReceiptLine
                    {
                        ProductId = productId,
                        Quantity = 15,
                        BaseQuantity = 15
                    }
                }
            };
            db.ExportReceipts.Add(receipt);
            db.StockBalances.Add(new StockBalance
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = 5,
                LastUpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = new StockService(db, config);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ConfirmExportReceiptAsync(receiptId, userId)
            );
        }
    }
}
