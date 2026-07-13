using System;
using System.Threading.Tasks;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Application.Interfaces
{
    public interface IStockService
    {
        Task AdjustStockAsync(
            Guid warehouseId,
            Guid productId,
            Guid? productVariantId,
            decimal quantityChange,
            MovementType movementType,
            Guid referenceId,
            string referenceNo,
            Guid changedById,
            string? note = null);

        Task ConfirmImportReceiptAsync(Guid receiptId, Guid userId);
        Task CancelImportReceiptAsync(Guid receiptId, Guid userId);

        Task ConfirmExportReceiptAsync(Guid receiptId, Guid userId);
        Task CancelExportReceiptAsync(Guid receiptId, Guid userId);

        Task ConfirmTransferReceiptAsync(Guid receiptId, Guid userId);
        Task CancelTransferReceiptAsync(Guid receiptId, Guid userId);
    }
}
