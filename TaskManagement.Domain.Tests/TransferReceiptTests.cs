using System;
using TaskManagement.Domain.Entities;
using Xunit;

namespace TaskManagement.Domain.Tests
{
    public class TransferReceiptTests
    {
        [Fact]
        public void ValidateReceipt_WithSameFromAndToWarehouse_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var receipt = new TransferReceipt
            {
                FromWarehouseId = warehouseId,
                ToWarehouseId = warehouseId
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => receipt.ValidateReceipt());
            Assert.Equal("Source and destination warehouses cannot be the same.", exception.Message);
        }

        [Fact]
        public void ValidateReceipt_WithDifferentFromAndToWarehouse_ShouldSucceed()
        {
            // Arrange
            var receipt = new TransferReceipt
            {
                FromWarehouseId = Guid.NewGuid(),
                ToWarehouseId = Guid.NewGuid()
            };

            // Act & Assert (should not throw)
            receipt.ValidateReceipt();
        }
    }
}
