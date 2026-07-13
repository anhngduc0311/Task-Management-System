using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TaskManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductInventorySeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Origins",
                columns: new[] { "Id", "Code", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-0000-0000-000000000001"), "VN", true, "Việt Nam" },
                    { new Guid("d0000000-0000-0000-0000-000000000002"), "JP", true, "Nhật Bản" },
                    { new Guid("d0000000-0000-0000-0000-000000000003"), "US", true, "Hoa Kỳ" },
                    { new Guid("d0000000-0000-0000-0000-000000000004"), "CN", true, "Trung Quốc" }
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "Code", "Description", "DisplayOrder", "IsActive", "Name", "ParentId" },
                values: new object[,]
                {
                    { new Guid("c0000000-0000-0000-0000-000000000001"), "CAT_ELE", "Thiết bị điện tử, công nghệ", 1, true, "Điện tử", null },
                    { new Guid("c0000000-0000-0000-0000-000000000002"), "CAT_FAS", "Quần áo, phụ kiện thời trang", 2, true, "Thời trang", null },
                    { new Guid("c0000000-0000-0000-0000-000000000003"), "CAT_FNB", "Thức ăn, nước uống, sữa", 3, true, "Thực phẩm & Đồ uống", null },
                    { new Guid("c0000000-0000-0000-0000-000000000004"), "CAT_OFF", "Bút, tập, dụng cụ văn phòng", 4, true, "Văn phòng phẩm", null }
                });

            migrationBuilder.InsertData(
                table: "ProductLabels",
                columns: new[] { "Id", "Code", "Color", "IsActive", "Name" },
                values: new object[,]
                {
                    { new Guid("e0000000-0000-0000-0000-000000000001"), "LBL_HOT", "#ef4444", true, "Bán chạy" },
                    { new Guid("e0000000-0000-0000-0000-000000000002"), "LBL_NEW", "#10b981", true, "Sản phẩm mới" },
                    { new Guid("e0000000-0000-0000-0000-000000000003"), "LBL_SALE", "#f59e0b", true, "Khuyến mãi" },
                    { new Guid("e0000000-0000-0000-0000-000000000004"), "LBL_PREMIUM", "#8b5cf6", true, "Cao cấp" }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "Address", "Code", "ContactPerson", "CreatedAt", "Email", "IsActive", "Name", "Phone", "TaxCode", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000001"), "Tòa nhà FPT, Phố Duy Tân, Cầu Giấy, Hà Nội", "SUP001", "Nguyễn Văn A", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "info@synnexfpt.com.vn", true, "Công ty Cổ phần Synnex FPT", "02473007108", "0102613123", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000000-0000-0000-0000-000000000002"), "264 Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP. Hồ Chí Minh", "SUP002", "Trần Thị B", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "contact@phongvu.vn", true, "Công ty Cổ phần Thương mại Dịch vụ Phong Vũ", "18006867", "0304998765", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000000-0000-0000-0000-000000000003"), "10 Tân Trào, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh", "SUP003", "Phạm Minh C", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "vinamilk@vinamilk.com.vn", true, "Công ty Cổ phần Sữa Việt Nam (Vinamilk)", "02854155555", "0300588556", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("a0000000-0000-0000-0000-000000000004"), "Lô 6-8-10-12, Đường số 3, KCN Tân Tạo, Quận Bình Tân, TP. Hồ Chí Minh", "SUP004", "Lê Hoàng D", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), "info@thienlonggroup.com", true, "Tập đoàn Thiên Long", "02837505555", "0301464830", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ImportReceipts",
                columns: new[] { "Id", "CreatedAt", "CreatedById", "Description", "ReceiptNo", "Status", "SupplierId", "TotalAmount", "UpdatedAt", "WarehouseId" },
                values: new object[] { new Guid("db000000-0000-0000-0000-000000000001"), new DateTime(2026, 7, 13, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("8a4b4ef9-7ec7-4dbb-8fb6-82ff4b4ab456"), "Nhập kho lô hàng Laptop Dell XPS 13 & Áo thun Polo Uniqlo phục vụ kinh doanh.", "IMP202607130001", "Confirmed", new Guid("a0000000-0000-0000-0000-000000000001"), 2282000000m, new DateTime(2026, 7, 13, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("e11e11a1-1111-1111-1111-111111111111") });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BaseUnitId", "CategoryId", "CreatedAt", "DefaultPrice", "Description", "IsDeleted", "Name", "OriginId", "ProductCode", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("f11e11a1-1111-1111-1111-111111111111"), new Guid("c0000000-0000-0000-0000-000000000001"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), 45000000m, "Laptop cao cấp Dell XPS 13 Plus với chip Intel Core i7/i9 thế hệ 13, RAM 16GB/32GB, SSD 512GB/1TB.", false, "Laptop Dell XPS 13 9320", new Guid("d0000000-0000-0000-0000-000000000003"), "PROD_DELL_XPS13", "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("f11e11a1-2222-2222-2222-222222222222"), new Guid("c0000000-0000-0000-0000-000000000003"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), 32000m, "Sữa tươi tiệt trùng Vinamilk ít đường, thơm ngon bổ dưỡng.", false, "Sữa tươi Vinamilk ít đường 180ml", new Guid("d0000000-0000-0000-0000-000000000001"), "PROD_VNM_MILK_180", "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("f11e11a1-1111-1111-1111-111111111111"), new Guid("c0000000-0000-0000-0000-000000000004"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), 4000m, "Bút bi mực xanh Thiên Long TL-027, viết trơn, đều mực, được ưa chuộng nhất.", false, "Bút bi Thiên Long TL-027", new Guid("d0000000-0000-0000-0000-000000000001"), "PROD_TL_027", "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("f11e11a1-1111-1111-1111-111111111111"), new Guid("c0000000-0000-0000-0000-000000000002"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), 490000m, "Áo Polo nam Uniqlo chất liệu thun cotton thoáng mát, thấm hút mồ hôi tốt.", false, "Áo thun Polo Nam Uniqlo", new Guid("d0000000-0000-0000-0000-000000000002"), "PROD_UNIQLO_POLO", "Active", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProductProductLabels",
                columns: new[] { "ProductId", "ProductLabelId" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("e0000000-0000-0000-0000-000000000004") },
                    { new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("e0000000-0000-0000-0000-000000000002") },
                    { new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("e0000000-0000-0000-0000-000000000001") },
                    { new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("e0000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.InsertData(
                table: "ProductSuppliers",
                columns: new[] { "ProductId", "SupplierId" },
                values: new object[,]
                {
                    { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("a0000000-0000-0000-0000-000000000001") },
                    { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("a0000000-0000-0000-0000-000000000002") },
                    { new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("a0000000-0000-0000-0000-000000000003") },
                    { new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("a0000000-0000-0000-0000-000000000004") },
                    { new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("a0000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "ProductUnitConversions",
                columns: new[] { "Id", "ConversionRate", "CreatedAt", "FromUnitId", "ProductId", "ToUnitId", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("fb000000-0000-0000-0000-000000000021"), 48m, new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f11e11a1-3333-3333-3333-333333333333"), new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("f11e11a1-2222-2222-2222-222222222222"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fb000000-0000-0000-0000-000000000031"), 20m, new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f11e11a1-2222-2222-2222-222222222222"), new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("f11e11a1-1111-1111-1111-111111111111"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ProductVariants",
                columns: new[] { "Id", "CreatedAt", "ImageUrl", "IsDeleted", "Price", "ProductId", "SKU", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("fa000000-0000-0000-0000-000000000011"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 45000000m, new Guid("f0000000-0000-0000-0000-000000000001"), "DELL-XPS13-I7", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa000000-0000-0000-0000-000000000012"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 55000000m, new Guid("f0000000-0000-0000-0000-000000000001"), "DELL-XPS13-I9", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa000000-0000-0000-0000-000000000021"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 32000m, new Guid("f0000000-0000-0000-0000-000000000002"), "VNM-MILK-180", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa000000-0000-0000-0000-000000000031"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 4000m, new Guid("f0000000-0000-0000-0000-000000000003"), "TL-027-BLUE", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa000000-0000-0000-0000-000000000041"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 490000m, new Guid("f0000000-0000-0000-0000-000000000004"), "UQ-POLO-M", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa000000-0000-0000-0000-000000000042"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 490000m, new Guid("f0000000-0000-0000-0000-000000000004"), "UQ-POLO-L", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("fa000000-0000-0000-0000-000000000043"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), null, false, 520000m, new Guid("f0000000-0000-0000-0000-000000000004"), "UQ-POLO-XL", new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "ImportReceiptLines",
                columns: new[] { "Id", "Amount", "BaseQuantity", "ConversionRate", "CreatedAt", "ImportReceiptId", "ProductId", "ProductVariantId", "Quantity", "UnitId", "UnitPrice" },
                values: new object[,]
                {
                    { new Guid("db000000-0000-0000-0000-000000000011"), 2250000000m, 50m, 1m, new DateTime(2026, 7, 13, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("db000000-0000-0000-0000-000000000001"), new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("fa000000-0000-0000-0000-000000000011"), 50m, new Guid("f11e11a1-1111-1111-1111-111111111111"), 45000000m },
                    { new Guid("db000000-0000-0000-0000-000000000012"), 32000000m, 65m, 1m, new DateTime(2026, 7, 13, 10, 0, 0, 0, DateTimeKind.Utc), new Guid("db000000-0000-0000-0000-000000000001"), new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("fa000000-0000-0000-0000-000000000041"), 65m, new Guid("f11e11a1-1111-1111-1111-111111111111"), 492307.6923m }
                });

            migrationBuilder.InsertData(
                table: "StockBalances",
                columns: new[] { "Id", "LastUpdatedAt", "ProductId", "ProductVariantId", "Quantity", "WarehouseId" },
                values: new object[,]
                {
                    { new Guid("b0000000-0000-0000-0000-000000000011"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("fa000000-0000-0000-0000-000000000011"), 50m, new Guid("e11e11a1-1111-1111-1111-111111111111") },
                    { new Guid("b0000000-0000-0000-0000-000000000012"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("fa000000-0000-0000-0000-000000000012"), 30m, new Guid("e11e11a1-1111-1111-1111-111111111111") },
                    { new Guid("b0000000-0000-0000-0000-000000000021"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("fa000000-0000-0000-0000-000000000021"), 1200m, new Guid("e11e11a1-1111-1111-1111-111111111111") },
                    { new Guid("b0000000-0000-0000-0000-000000000031"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("fa000000-0000-0000-0000-000000000031"), 500m, new Guid("e11e11a1-1111-1111-1111-111111111111") },
                    { new Guid("b0000000-0000-0000-0000-000000000041"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("fa000000-0000-0000-0000-000000000041"), 150m, new Guid("e11e11a1-1111-1111-1111-111111111111") },
                    { new Guid("b0000000-0000-0000-0000-000000000042"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("fa000000-0000-0000-0000-000000000042"), 200m, new Guid("e11e11a1-1111-1111-1111-111111111111") },
                    { new Guid("b0000000-0000-0000-0000-000000000043"), new DateTime(2026, 7, 7, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("fa000000-0000-0000-0000-000000000043"), 100m, new Guid("e11e11a1-1111-1111-1111-111111111111") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ImportReceiptLines",
                keyColumn: "Id",
                keyValue: new Guid("db000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "ImportReceiptLines",
                keyColumn: "Id",
                keyValue: new Guid("db000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "ProductProductLabels",
                keyColumns: new[] { "ProductId", "ProductLabelId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("e0000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                table: "ProductProductLabels",
                keyColumns: new[] { "ProductId", "ProductLabelId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("e0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "ProductProductLabels",
                keyColumns: new[] { "ProductId", "ProductLabelId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("e0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "ProductProductLabels",
                keyColumns: new[] { "ProductId", "ProductLabelId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("e0000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "ProductSuppliers",
                keyColumns: new[] { "ProductId", "SupplierId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "ProductSuppliers",
                keyColumns: new[] { "ProductId", "SupplierId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000001"), new Guid("a0000000-0000-0000-0000-000000000002") });

            migrationBuilder.DeleteData(
                table: "ProductSuppliers",
                keyColumns: new[] { "ProductId", "SupplierId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000002"), new Guid("a0000000-0000-0000-0000-000000000003") });

            migrationBuilder.DeleteData(
                table: "ProductSuppliers",
                keyColumns: new[] { "ProductId", "SupplierId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000003"), new Guid("a0000000-0000-0000-0000-000000000004") });

            migrationBuilder.DeleteData(
                table: "ProductSuppliers",
                keyColumns: new[] { "ProductId", "SupplierId" },
                keyValues: new object[] { new Guid("f0000000-0000-0000-0000-000000000004"), new Guid("a0000000-0000-0000-0000-000000000001") });

            migrationBuilder.DeleteData(
                table: "ProductUnitConversions",
                keyColumn: "Id",
                keyValue: new Guid("fb000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "ProductUnitConversions",
                keyColumn: "Id",
                keyValue: new Guid("fb000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                table: "StockBalances",
                keyColumn: "Id",
                keyValue: new Guid("b0000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                table: "ImportReceipts",
                keyColumn: "Id",
                keyValue: new Guid("db000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ProductLabels",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ProductLabels",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ProductLabels",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "ProductLabels",
                keyColumn: "Id",
                keyValue: new Guid("e0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000041"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                table: "ProductVariants",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("f0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: new Guid("a0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Origins",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "ProductCategories",
                keyColumn: "Id",
                keyValue: new Guid("c0000000-0000-0000-0000-000000000004"));
        }
    }
}
