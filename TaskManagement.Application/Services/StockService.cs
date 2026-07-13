using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Services
{
    public class StockService : IStockService
    {
        private readonly IAppDbContext _dbContext;
        private readonly IConfiguration _configuration;

        public StockService(IAppDbContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        private bool AllowNegativeStock
        {
            get
            {
                var val = _configuration["Inventory:AllowNegativeStock"];
                return !string.IsNullOrEmpty(val) && bool.TryParse(val, out var res) && res;
            }
        }

        public async Task AdjustStockAsync(
            Guid warehouseId,
            Guid productId,
            Guid? productVariantId,
            decimal quantityChange,
            MovementType movementType,
            Guid referenceId,
            string referenceNo,
            Guid changedById,
            string? note = null)
        {
            // Find or create StockBalance
            var balance = await _dbContext.StockBalances
                .FirstOrDefaultAsync(sb => sb.WarehouseId == warehouseId &&
                                           sb.ProductId == productId &&
                                           sb.ProductVariantId == productVariantId);

            if (balance == null)
            {
                if (quantityChange < 0 && !AllowNegativeStock)
                {
                    throw new InvalidOperationException("Insufficient stock in warehouse.");
                }

                balance = new StockBalance
                {
                    WarehouseId = warehouseId,
                    ProductId = productId,
                    ProductVariantId = productVariantId,
                    Quantity = 0,
                    LastUpdatedAt = DateTime.UtcNow
                };
                _dbContext.StockBalances.Add(balance);
            }

            var newQuantity = balance.Quantity + quantityChange;
            if (newQuantity < 0 && !AllowNegativeStock)
            {
                throw new InvalidOperationException("Insufficient stock in warehouse.");
            }

            balance.Quantity = newQuantity;
            balance.LastUpdatedAt = DateTime.UtcNow;

            // Create StockMovement
            var movement = new StockMovement
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                ProductVariantId = productVariantId,
                Quantity = Math.Abs(quantityChange), // movement stores absolute quantity
                MovementType = movementType,
                ReferenceId = referenceId,
                ReferenceNo = referenceNo,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.StockMovements.Add(movement);
        }

        public async Task ConfirmImportReceiptAsync(Guid receiptId, Guid userId)
        {
            var db = _dbContext as DbContext;
            var useTransaction = db != null && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            using var transaction = useTransaction ? await db!.Database.BeginTransactionAsync() : null;

            try
            {
                var receipt = await _dbContext.ImportReceipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) throw new ArgumentException("Receipt not found.");
                if (receipt.Status != ReceiptStatus.Draft)
                    throw new InvalidOperationException("Only draft receipts can be confirmed.");

                var warehouse = await _dbContext.Warehouses.FindAsync(receipt.WarehouseId);
                if (warehouse == null || !warehouse.IsActive)
                    throw new InvalidOperationException("Warehouse is inactive or not found.");

                foreach (var line in receipt.Lines)
                {
                    await AdjustStockAsync(
                        receipt.WarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        line.BaseQuantity,
                        MovementType.Import,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId
                    );
                }

                receipt.Status = ReceiptStatus.Confirmed;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelImportReceiptAsync(Guid receiptId, Guid userId)
        {
            var db = _dbContext as DbContext;
            var useTransaction = db != null && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            using var transaction = useTransaction ? await db!.Database.BeginTransactionAsync() : null;

            try
            {
                var receipt = await _dbContext.ImportReceipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) throw new ArgumentException("Receipt not found.");
                if (receipt.Status != ReceiptStatus.Confirmed)
                    throw new InvalidOperationException("Only confirmed receipts can be cancelled.");

                foreach (var line in receipt.Lines)
                {
                    await AdjustStockAsync(
                        receipt.WarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        -line.BaseQuantity, // reverse: subtract stock
                        MovementType.AdjustmentOut,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId,
                        "Reverse Import Receipt Cancellation"
                    );
                }

                receipt.Status = ReceiptStatus.Cancelled;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ConfirmExportReceiptAsync(Guid receiptId, Guid userId)
        {
            var db = _dbContext as DbContext;
            var useTransaction = db != null && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            using var transaction = useTransaction ? await db!.Database.BeginTransactionAsync() : null;

            try
            {
                var receipt = await _dbContext.ExportReceipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) throw new ArgumentException("Receipt not found.");
                if (receipt.Status != ReceiptStatus.Draft)
                    throw new InvalidOperationException("Only draft receipts can be confirmed.");

                var warehouse = await _dbContext.Warehouses.FindAsync(receipt.WarehouseId);
                if (warehouse == null || !warehouse.IsActive)
                    throw new InvalidOperationException("Warehouse is inactive or not found.");

                foreach (var line in receipt.Lines)
                {
                    await AdjustStockAsync(
                        receipt.WarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        -line.BaseQuantity, // subtract stock
                        MovementType.Export,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId
                    );
                }

                receipt.Status = ReceiptStatus.Confirmed;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelExportReceiptAsync(Guid receiptId, Guid userId)
        {
            var db = _dbContext as DbContext;
            var useTransaction = db != null && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            using var transaction = useTransaction ? await db!.Database.BeginTransactionAsync() : null;

            try
            {
                var receipt = await _dbContext.ExportReceipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) throw new ArgumentException("Receipt not found.");
                if (receipt.Status != ReceiptStatus.Confirmed)
                    throw new InvalidOperationException("Only confirmed receipts can be cancelled.");

                foreach (var line in receipt.Lines)
                {
                    await AdjustStockAsync(
                        receipt.WarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        line.BaseQuantity, // reverse: add stock back
                        MovementType.AdjustmentIn,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId,
                        "Reverse Export Receipt Cancellation"
                    );
                }

                receipt.Status = ReceiptStatus.Cancelled;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ConfirmTransferReceiptAsync(Guid receiptId, Guid userId)
        {
            var db = _dbContext as DbContext;
            var useTransaction = db != null && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            using var transaction = useTransaction ? await db!.Database.BeginTransactionAsync() : null;

            try
            {
                var receipt = await _dbContext.TransferReceipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) throw new ArgumentException("Receipt not found.");
                if (receipt.Status != ReceiptStatus.Draft)
                    throw new InvalidOperationException("Only draft receipts can be confirmed.");

                receipt.ValidateReceipt();

                var fromWarehouse = await _dbContext.Warehouses.FindAsync(receipt.FromWarehouseId);
                if (fromWarehouse == null || !fromWarehouse.IsActive)
                    throw new InvalidOperationException("Source warehouse is inactive or not found.");

                var toWarehouse = await _dbContext.Warehouses.FindAsync(receipt.ToWarehouseId);
                if (toWarehouse == null || !toWarehouse.IsActive)
                    throw new InvalidOperationException("Destination warehouse is inactive or not found.");

                foreach (var line in receipt.Lines)
                {
                    // Decrease from source warehouse
                    await AdjustStockAsync(
                        receipt.FromWarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        -line.BaseQuantity,
                        MovementType.TransferOut,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId
                    );

                    // Increase to destination warehouse
                    await AdjustStockAsync(
                        receipt.ToWarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        line.BaseQuantity,
                        MovementType.TransferIn,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId
                    );
                }

                receipt.Status = ReceiptStatus.Confirmed;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task CancelTransferReceiptAsync(Guid receiptId, Guid userId)
        {
            var db = _dbContext as DbContext;
            var useTransaction = db != null && db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";
            using var transaction = useTransaction ? await db!.Database.BeginTransactionAsync() : null;

            try
            {
                var receipt = await _dbContext.TransferReceipts
                    .Include(r => r.Lines)
                    .FirstOrDefaultAsync(r => r.Id == receiptId);

                if (receipt == null) throw new ArgumentException("Receipt not found.");
                if (receipt.Status != ReceiptStatus.Confirmed)
                    throw new InvalidOperationException("Only confirmed receipts can be cancelled.");

                foreach (var line in receipt.Lines)
                {
                    // Reverse destination: subtract from destination warehouse
                    await AdjustStockAsync(
                        receipt.ToWarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        -line.BaseQuantity,
                        MovementType.AdjustmentOut,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId,
                        "Reverse Transfer Destination Cancellation"
                    );

                    // Reverse source: add back to source warehouse
                    await AdjustStockAsync(
                        receipt.FromWarehouseId,
                        line.ProductId,
                        line.ProductVariantId,
                        line.BaseQuantity,
                        MovementType.AdjustmentIn,
                        receipt.Id,
                        receipt.ReceiptNo,
                        userId,
                        "Reverse Transfer Source Cancellation"
                    );
                }

                receipt.Status = ReceiptStatus.Cancelled;
                receipt.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                if (transaction != null) await transaction.CommitAsync();
            }
            catch
            {
                if (transaction != null) await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
