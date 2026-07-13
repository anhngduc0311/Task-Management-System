using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations
{
    public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
    {
        public void Configure(EntityTypeBuilder<Supplier> builder)
        {
            builder.ToTable("Suppliers");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(s => s.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(s => s.Phone)
                .HasMaxLength(20);

            builder.Property(s => s.Email)
                .HasMaxLength(150);

            builder.Property(s => s.Address)
                .HasMaxLength(500);

            builder.Property(s => s.TaxCode)
                .HasMaxLength(50);

            builder.Property(s => s.ContactPerson)
                .HasMaxLength(150);

            builder.Property(s => s.IsActive)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .IsRequired();

            builder.Property(s => s.UpdatedAt)
                .IsRequired();

            builder.HasIndex(s => s.Code)
                .IsUnique();

            builder.HasData(
                new Supplier
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                    Code = "SUP001",
                    Name = "Công ty Cổ phần Synnex FPT",
                    Phone = "02473007108",
                    Email = "info@synnexfpt.com.vn",
                    Address = "Tòa nhà FPT, Phố Duy Tân, Cầu Giấy, Hà Nội",
                    TaxCode = "0102613123",
                    ContactPerson = "Nguyễn Văn A",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Supplier
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                    Code = "SUP002",
                    Name = "Công ty Cổ phần Thương mại Dịch vụ Phong Vũ",
                    Phone = "18006867",
                    Email = "contact@phongvu.vn",
                    Address = "264 Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP. Hồ Chí Minh",
                    TaxCode = "0304998765",
                    ContactPerson = "Trần Thị B",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Supplier
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"),
                    Code = "SUP003",
                    Name = "Công ty Cổ phần Sữa Việt Nam (Vinamilk)",
                    Phone = "02854155555",
                    Email = "vinamilk@vinamilk.com.vn",
                    Address = "10 Tân Trào, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh",
                    TaxCode = "0300588556",
                    ContactPerson = "Phạm Minh C",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                },
                new Supplier
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"),
                    Code = "SUP004",
                    Name = "Tập đoàn Thiên Long",
                    Phone = "02837505555",
                    Email = "info@thienlonggroup.com",
                    Address = "Lô 6-8-10-12, Đường số 3, KCN Tân Tạo, Quận Bình Tân, TP. Hồ Chí Minh",
                    TaxCode = "0301464830",
                    ContactPerson = "Lê Hoàng D",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 7, 7, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
