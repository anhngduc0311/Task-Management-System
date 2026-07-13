using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using TaskManagement.Domain.Entities;
using Task = TaskManagement.Domain.Entities.Task;

namespace TaskManagement.Application.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<User> Users { get; }
        DbSet<Role> Roles { get; }
        DbSet<UserRole> UserRoles { get; }
        DbSet<Project> Projects { get; }
        DbSet<ProjectMember> ProjectMembers { get; }
        DbSet<Task> Tasks { get; }
        DbSet<TaskComment> TaskComments { get; }
        DbSet<TaskAttachment> TaskAttachments { get; }
        DbSet<AuditLog> AuditLogs { get; }
        DbSet<DynamicFieldDefinition> DynamicFieldDefinitions { get; }
        DbSet<TaskDynamicFieldValue> TaskDynamicFieldValues { get; }

        // Product & Inventory Context
        DbSet<Unit> Units { get; }
        DbSet<ProductCategory> ProductCategories { get; }
        DbSet<Origin> Origins { get; }
        DbSet<Supplier> Suppliers { get; }
        DbSet<ProductLabel> ProductLabels { get; }
        DbSet<ProductProductLabel> ProductProductLabels { get; }
        DbSet<Product> Products { get; }
        DbSet<ProductImage> ProductImages { get; }
        DbSet<ProductUnitConversion> ProductUnitConversions { get; }
        DbSet<ProductSupplier> ProductSuppliers { get; }
        DbSet<ProductAttributeGroup> ProductAttributeGroups { get; }
        DbSet<ProductAttributeValue> ProductAttributeValues { get; }
        DbSet<ProductVariant> ProductVariants { get; }
        DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues { get; }
        DbSet<Warehouse> Warehouses { get; }
        DbSet<StockBalance> StockBalances { get; }
        DbSet<StockMovement> StockMovements { get; }
        DbSet<ImportReceipt> ImportReceipts { get; }
        DbSet<ImportReceiptLine> ImportReceiptLines { get; }
        DbSet<ExportReceipt> ExportReceipts { get; }
        DbSet<ExportReceiptLine> ExportReceiptLines { get; }
        DbSet<TransferReceipt> TransferReceipts { get; }
        DbSet<TransferReceiptLine> TransferReceiptLines { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
