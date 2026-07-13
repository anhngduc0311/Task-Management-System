using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using Task = TaskManagement.Domain.Entities.Task;

using TaskManagement.Application.Interfaces;

namespace TaskManagement.Infrastructure.Persistence
{
    public class AppDbContext : DbContext, IAppDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Project> Projects => Set<Project>();
        public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
        public DbSet<Task> Tasks => Set<Task>();
        public DbSet<TaskComment> TaskComments => Set<TaskComment>();
        public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<DynamicFieldDefinition> DynamicFieldDefinitions => Set<DynamicFieldDefinition>();
        public DbSet<TaskDynamicFieldValue> TaskDynamicFieldValues => Set<TaskDynamicFieldValue>();

        // Product & Inventory Context
        public DbSet<Unit> Units => Set<Unit>();
        public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
        public DbSet<Origin> Origins => Set<Origin>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<ProductLabel> ProductLabels => Set<ProductLabel>();
        public DbSet<ProductProductLabel> ProductProductLabels => Set<ProductProductLabel>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductImage> ProductImages => Set<ProductImage>();
        public DbSet<ProductUnitConversion> ProductUnitConversions => Set<ProductUnitConversion>();
        public DbSet<ProductSupplier> ProductSuppliers => Set<ProductSupplier>();
        public DbSet<ProductAttributeGroup> ProductAttributeGroups => Set<ProductAttributeGroup>();
        public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues => Set<ProductVariantAttributeValue>();
        public DbSet<Warehouse> Warehouses => Set<Warehouse>();
        public DbSet<StockBalance> StockBalances => Set<StockBalance>();
        public DbSet<StockMovement> StockMovements => Set<StockMovement>();
        public DbSet<ImportReceipt> ImportReceipts => Set<ImportReceipt>();
        public DbSet<ImportReceiptLine> ImportReceiptLines => Set<ImportReceiptLine>();
        public DbSet<ExportReceipt> ExportReceipts => Set<ExportReceipt>();
        public DbSet<ExportReceiptLine> ExportReceiptLines => Set<ExportReceiptLine>();
        public DbSet<TransferReceipt> TransferReceipts => Set<TransferReceipt>();
        public DbSet<TransferReceiptLine> TransferReceiptLines => Set<TransferReceiptLine>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all entity configurations in the assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }

        public override System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is TaskManagement.Domain.Entities.Task task)
                {
                    task.RowVersion = Guid.NewGuid().ToByteArray();
                }
                else if (entry.Entity is Product product)
                {
                    product.RowVersion = Guid.NewGuid().ToByteArray();
                }
                else if (entry.Entity is ProductVariant variant)
                {
                    variant.RowVersion = Guid.NewGuid().ToByteArray();
                }
                else if (entry.Entity is ImportReceipt importReceipt)
                {
                    importReceipt.RowVersion = Guid.NewGuid().ToByteArray();
                }
                else if (entry.Entity is ExportReceipt exportReceipt)
                {
                    exportReceipt.RowVersion = Guid.NewGuid().ToByteArray();
                }
                else if (entry.Entity is TransferReceipt transferReceipt)
                {
                    transferReceipt.RowVersion = Guid.NewGuid().ToByteArray();
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
