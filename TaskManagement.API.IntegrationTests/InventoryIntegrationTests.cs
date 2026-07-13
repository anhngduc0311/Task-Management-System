using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Inventory;
using TaskManagement.Application.DTOs.Stock;
using TaskManagement.Application.DTOs.Warehouses;
using TaskManagement.Application.DTOs.Units;
using TaskManagement.Application.DTOs.ProductCategories;
using TaskManagement.Application.DTOs.Products;
using TaskManagement.Application.DTOs.AuditLogs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskManagement.API.IntegrationTests
{
    public class InventoryIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public InventoryIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        private HttpClient GetAuthenticatedClient(User user, string systemRole)
        {
            var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var scope = _factory.Services.CreateScope();
            var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var token = tokenService.GenerateAccessToken(user, new[] { systemRole });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<User> SeedUserAsync(string roleName)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"user_{Guid.NewGuid()}@test.com",
                FullName = $"{roleName} User",
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null)
            {
                throw new Exception($"Role '{roleName}' not found in database.");
            }

            db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await db.SaveChangesAsync();

            return user;
        }

        [Fact]
        public async Task Inventory_FullFlow_Success()
        {
            // 1. Arrange
            var adminUser = await SeedUserAsync("Admin");
            var adminClient = GetAuthenticatedClient(adminUser, "Admin");

            // Create Warehouse A
            var createWhA = new CreateWarehouseDto
            {
                Code = $"WHA_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Name = "Warehouse A",
                Address = "123 Street",
                Description = "Main warehouse",
                IsActive = true
            };
            var whARes = await adminClient.PostAsJsonAsync("/api/warehouses", createWhA);
            Assert.Equal(HttpStatusCode.Created, whARes.StatusCode);
            var whADto = await whARes.Content.ReadFromJsonAsync<WarehouseDto>();
            Assert.NotNull(whADto);

            // Create Warehouse B
            var createWhB = new CreateWarehouseDto
            {
                Code = $"WHB_{Guid.NewGuid().ToString().Substring(0, 8)}",
                Name = "Warehouse B",
                Address = "456 Street",
                Description = "Secondary warehouse",
                IsActive = true
            };
            var whBRes = await adminClient.PostAsJsonAsync("/api/warehouses", createWhB);
            Assert.Equal(HttpStatusCode.Created, whBRes.StatusCode);
            var whBDto = await whBRes.Content.ReadFromJsonAsync<WarehouseDto>();
            Assert.NotNull(whBDto);

            // Create Unit (Base Unit)
            var createUnit = new CreateUnitDto { Code = $"U_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Cái", IsActive = true };
            var unitRes = await adminClient.PostAsJsonAsync("/api/units", createUnit);
            var unitDto = await unitRes.Content.ReadFromJsonAsync<UnitDto>();
            Assert.NotNull(unitDto);

            // Create ProductCategory
            var createCategory = new CreateCategoryDto { Code = $"C_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Category test", IsActive = true };
            var catRes = await adminClient.PostAsJsonAsync("/api/product-categories", createCategory);
            var categoryDto = await catRes.Content.ReadFromJsonAsync<CategoryDto>();
            Assert.NotNull(categoryDto);

            // Create Product
            var createProduct = new CreateProductDto
            {
                ProductCode = $"P_{Guid.NewGuid().ToString().Substring(0,8)}",
                Name = "Inventory Test Product",
                Description = "<p>Invent Test</p>",
                DefaultPrice = 100,
                BaseUnitId = unitDto.Id,
                CategoryId = categoryDto.Id,
                Status = "Active"
            };
            var prodRes = await adminClient.PostAsJsonAsync("/api/products", createProduct);
            Assert.Equal(HttpStatusCode.Created, prodRes.StatusCode);
            var prodDto = await prodRes.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(prodDto);

            // 2. Import Receipt Flow
            var createImport = new CreateImportReceiptDto
            {
                WarehouseId = whADto.Id,
                Description = "Initial import",
                Lines = new List<CreateImportReceiptLineDto>
                {
                    new CreateImportReceiptLineDto
                    {
                        ProductId = prodDto.Id,
                        Quantity = 100,
                        UnitId = unitDto.Id,
                        UnitPrice = 80
                    }
                }
            };

            var importCreateRes = await adminClient.PostAsJsonAsync("/api/inventory/import-receipts", createImport);
            Assert.Equal(HttpStatusCode.Created, importCreateRes.StatusCode);
            var importSummary = await importCreateRes.Content.ReadFromJsonAsync<ImportReceiptDto>();
            Assert.NotNull(importSummary);

            // Confirm Import Receipt
            var confirmRes = await adminClient.PostAsync($"/api/inventory/import-receipts/{importSummary.Id}/confirm", null);
            Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

            // Check Stock Balance in Warehouse A
            var balanceRes = await adminClient.GetAsync($"/api/inventory/stock-balances?warehouseId={whADto.Id}&productId={prodDto.Id}");
            Assert.Equal(HttpStatusCode.OK, balanceRes.StatusCode);
            
            // Get JSON containing item array
            var balanceListObj = await balanceRes.Content.ReadFromJsonAsync<JsonElement>();
            var balanceItems = balanceListObj.GetProperty("items");
            Assert.Equal(1, balanceItems.GetArrayLength());
            var balanceQty = balanceItems[0].GetProperty("quantity").GetDecimal();
            Assert.Equal(100, balanceQty);

            // 3. Export Receipt Flow
            var createExport = new CreateExportReceiptDto
            {
                WarehouseId = whADto.Id,
                Description = "Sale export",
                Lines = new List<CreateExportReceiptLineDto>
                {
                    new CreateExportReceiptLineDto
                    {
                        ProductId = prodDto.Id,
                        Quantity = 40,
                        UnitId = unitDto.Id
                    }
                }
            };

            var exportCreateRes = await adminClient.PostAsJsonAsync("/api/inventory/export-receipts", createExport);
            Assert.Equal(HttpStatusCode.Created, exportCreateRes.StatusCode);
            var exportSummary = await exportCreateRes.Content.ReadFromJsonAsync<ExportReceiptDto>();
            Assert.NotNull(exportSummary);

            // Confirm Export Receipt
            var confirmExportRes = await adminClient.PostAsync($"/api/inventory/export-receipts/{exportSummary.Id}/confirm", null);
            Assert.Equal(HttpStatusCode.OK, confirmExportRes.StatusCode);

            // Check Stock Balance decreased
            var balanceRes2 = await adminClient.GetAsync($"/api/inventory/stock-balances?warehouseId={whADto.Id}&productId={prodDto.Id}");
            var balanceListObj2 = await balanceRes2.Content.ReadFromJsonAsync<JsonElement>();
            var balanceQty2 = balanceListObj2.GetProperty("items")[0].GetProperty("quantity").GetDecimal();
            Assert.Equal(60, balanceQty2); // 100 - 40 = 60

            // 4. Transfer Receipt Flow
            var createTransfer = new CreateTransferReceiptDto
            {
                FromWarehouseId = whADto.Id,
                ToWarehouseId = whBDto.Id,
                Description = "Transfer to Wh B",
                Lines = new List<CreateTransferReceiptLineDto>
                {
                    new CreateTransferReceiptLineDto
                    {
                        ProductId = prodDto.Id,
                        Quantity = 20,
                        UnitId = unitDto.Id
                    }
                }
            };

            var transferCreateRes = await adminClient.PostAsJsonAsync("/api/inventory/transfer-receipts", createTransfer);
            Assert.Equal(HttpStatusCode.Created, transferCreateRes.StatusCode);
            var transferSummary = await transferCreateRes.Content.ReadFromJsonAsync<TransferReceiptDto>();
            Assert.NotNull(transferSummary);

            // Confirm Transfer Receipt
            var confirmTransferRes = await adminClient.PostAsync($"/api/inventory/transfer-receipts/{transferSummary.Id}/confirm", null);
            Assert.Equal(HttpStatusCode.OK, confirmTransferRes.StatusCode);

            // Check stock in both warehouses
            // Warehouse A
            var balanceResA = await adminClient.GetAsync($"/api/inventory/stock-balances?warehouseId={whADto.Id}&productId={prodDto.Id}");
            var balanceObjA = await balanceResA.Content.ReadFromJsonAsync<JsonElement>();
            var qtyA = balanceObjA.GetProperty("items")[0].GetProperty("quantity").GetDecimal();
            Assert.Equal(40, qtyA); // 60 - 20 = 40

            // Warehouse B
            var balanceResB = await adminClient.GetAsync($"/api/inventory/stock-balances?warehouseId={whBDto.Id}&productId={prodDto.Id}");
            var balanceObjB = await balanceResB.Content.ReadFromJsonAsync<JsonElement>();
            var qtyB = balanceObjB.GetProperty("items")[0].GetProperty("quantity").GetDecimal();
            Assert.Equal(20, qtyB); // 0 + 20 = 20

            // 5. Cancel Receipt Flow
            var cancelTransferRes = await adminClient.PostAsync($"/api/inventory/transfer-receipts/{transferSummary.Id}/cancel", null);
            Assert.Equal(HttpStatusCode.OK, cancelTransferRes.StatusCode);

            // Verify stock is restored
            var balanceResA_2 = await adminClient.GetAsync($"/api/inventory/stock-balances?warehouseId={whADto.Id}&productId={prodDto.Id}");
            var balanceObjA_2 = await balanceResA_2.Content.ReadFromJsonAsync<JsonElement>();
            var qtyA_2 = balanceObjA_2.GetProperty("items")[0].GetProperty("quantity").GetDecimal();
            Assert.Equal(60, qtyA_2); // 40 + 20 = 60

            var balanceResB_2 = await adminClient.GetAsync($"/api/inventory/stock-balances?warehouseId={whBDto.Id}&productId={prodDto.Id}");
            var balanceObjB_2 = await balanceResB_2.Content.ReadFromJsonAsync<JsonElement>();
            var qtyB_2 = balanceObjB_2.GetProperty("items")[0].GetProperty("quantity").GetDecimal();
            Assert.Equal(0, qtyB_2); // 20 - 20 = 0

            // 6. Audit Log Check
            var auditRes = await adminClient.GetAsync("/api/inventory/audit-logs");
            Assert.Equal(HttpStatusCode.OK, auditRes.StatusCode);
            var logs = await auditRes.Content.ReadFromJsonAsync<List<AuditLogDto>>();
            Assert.NotNull(logs);
            Assert.NotEmpty(logs);
            Assert.Contains(logs, l => l.EntityType == "Warehouse" && l.Action == "Created");
            Assert.Contains(logs, l => l.EntityType == "ImportReceipt" && l.Action == "Created");
            Assert.Contains(logs, l => l.EntityType == "ImportReceipt" && l.Action == "Confirmed");
            Assert.Contains(logs, l => l.EntityType == "ExportReceipt" && l.Action == "Confirmed");
            Assert.Contains(logs, l => l.EntityType == "TransferReceipt" && l.Action == "Confirmed");
            Assert.Contains(logs, l => l.EntityType == "TransferReceipt" && l.Action == "Cancelled");
        }
    }
}
