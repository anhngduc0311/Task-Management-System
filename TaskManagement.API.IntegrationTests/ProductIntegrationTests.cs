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
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.ProductCategories;
using TaskManagement.Application.DTOs.ProductLabels;
using TaskManagement.Application.DTOs.Products;
using TaskManagement.Application.DTOs.ProductUnitConversions;
using TaskManagement.Application.DTOs.ProductVariants;
using TaskManagement.Application.DTOs.Suppliers;
using TaskManagement.Application.DTOs.Units;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;
using TaskManagement.Infrastructure.Persistence;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace TaskManagement.API.IntegrationTests
{
    public class ProductIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly CustomWebApplicationFactory<Program> _factory;

        public ProductIntegrationTests(CustomWebApplicationFactory<Program> factory)
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
            
            // Ensure the in-memory database is created and model seeds (Roles) are populated
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
        public async Task ProductCatalog_FullFlow_Success()
        {
            // 1. Arrange
            var adminUser = await SeedUserAsync("Admin");
            var viewerUser = await SeedUserAsync("Viewer");

            var adminClient = GetAuthenticatedClient(adminUser, "Admin");
            var viewerClient = GetAuthenticatedClient(viewerUser, "Viewer");

            // 2. Create Unit
            var createUnit = new CreateUnitDto { Code = $"U_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Unit A", IsActive = true };
            var unitRes = await adminClient.PostAsJsonAsync("/api/units", createUnit);
            Assert.Equal(HttpStatusCode.Created, unitRes.StatusCode);
            var unitDto = await unitRes.Content.ReadFromJsonAsync<UnitDto>();
            Assert.NotNull(unitDto);
            Assert.Equal(createUnit.Code, unitDto.Code);

            // 3. Create ProductCategory
            var createCategory = new CreateCategoryDto { Code = $"C_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Category A", IsActive = true };
            var catRes = await adminClient.PostAsJsonAsync("/api/product-categories", createCategory);
            Assert.Equal(HttpStatusCode.Created, catRes.StatusCode);
            var categoryDto = await catRes.Content.ReadFromJsonAsync<CategoryDto>();
            Assert.NotNull(categoryDto);

            // 4. Create Product with newly created BaseUnit and Category
            var createProduct = new CreateProductDto
            {
                ProductCode = $"P_{Guid.NewGuid().ToString().Substring(0,8)}",
                Name = "Product Test",
                Description = "<p>Product description containing safe HTML</p><script>alert('xss')</script>",
                DefaultPrice = 150000,
                BaseUnitId = unitDto.Id,
                CategoryId = categoryDto.Id,
                Status = "Active"
            };

            // Non-authorized user should not be able to create product
            var forbidRes = await viewerClient.PostAsJsonAsync("/api/products", createProduct);
            Assert.Equal(HttpStatusCode.Forbidden, forbidRes.StatusCode);

            // Admin creates product
            var prodRes = await adminClient.PostAsJsonAsync("/api/products", createProduct);
            Assert.Equal(HttpStatusCode.Created, prodRes.StatusCode);
            var prodDto = await prodRes.Content.ReadFromJsonAsync<ProductDto>();
            Assert.NotNull(prodDto);
            Assert.Equal(createProduct.ProductCode, prodDto.ProductCode);
            // XSS sanitization check (script tag should be stripped)
            Assert.Contains("Product description containing safe HTML", prodDto.Description);
            Assert.DoesNotContain("<script>", prodDto.Description);

            // 5. Get Product Detail
            var getRes = await viewerClient.GetAsync($"/api/products/{prodDto.Id}");
            Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
            var detailDto = await getRes.Content.ReadFromJsonAsync<ProductDetailDto>();
            Assert.NotNull(detailDto);
            Assert.Equal(prodDto.Id, detailDto.Id);
            Assert.Equal(categoryDto.Name, detailDto.CategoryName);
            Assert.Equal(unitDto.Name, detailDto.BaseUnitName);

            // 6. Unit Conversion CRUD
            var createConversionUnit = new CreateUnitDto { Code = $"U_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Thùng", IsActive = true };
            var convUnitRes = await adminClient.PostAsJsonAsync("/api/units", createConversionUnit);
            var convUnitDto = await convUnitRes.Content.ReadFromJsonAsync<UnitDto>();
            Assert.NotNull(convUnitDto);

            var createConversion = new CreateConversionDto
            {
                FromUnitId = convUnitDto.Id,
                ToUnitId = unitDto.Id,
                ConversionRate = 24
            };

            var addConvRes = await adminClient.PostAsJsonAsync($"/api/products/{prodDto.Id}/unit-conversions", createConversion);
            Assert.Equal(HttpStatusCode.Created, addConvRes.StatusCode);
            var convDto = await addConvRes.Content.ReadFromJsonAsync<ConversionDto>();
            Assert.NotNull(convDto);
            Assert.Equal(24, convDto.ConversionRate);

            // 7. Attributes and Variants Generation
            var createGroup = new CreateProductAttributeGroupDto
            {
                Name = "Color",
                DisplayOrder = 1,
                Values = new List<string> { "Red", "Blue" }
            };

            var groupRes = await adminClient.PostAsJsonAsync($"/api/products/{prodDto.Id}/attribute-groups", createGroup);
            var groupResContent = await groupRes.Content.ReadAsStringAsync();
            Assert.True(groupRes.IsSuccessStatusCode, $"Error: {groupRes.StatusCode}. Response: {groupResContent}");

            // Auto-generate variants
            var generateRes = await adminClient.PostAsync($"/api/products/{prodDto.Id}/variants/generate", null);
            Assert.Equal(HttpStatusCode.OK, generateRes.StatusCode);

            // Get variants
            var getVariantsRes = await viewerClient.GetAsync($"/api/products/{prodDto.Id}/variants");
            var variantsList = await getVariantsRes.Content.ReadFromJsonAsync<List<ProductVariantDto>>();
            Assert.NotNull(variantsList);
            Assert.Equal(2, variantsList.Count); // Red and Blue
            Assert.Contains(variantsList, v => v.SKU.StartsWith(prodDto.ProductCode, StringComparison.OrdinalIgnoreCase) && v.AttributeValueCombinations.Contains("Red"));
            Assert.Contains(variantsList, v => v.SKU.StartsWith(prodDto.ProductCode, StringComparison.OrdinalIgnoreCase) && v.AttributeValueCombinations.Contains("Blue"));

            // 8. Delete operations check
            // Fetch latest product details to get current RowVersion (which changed when we added attribute group)
            var getLatestRes = await viewerClient.GetAsync($"/api/products/{prodDto.Id}");
            var latestProdDto = await getLatestRes.Content.ReadFromJsonAsync<ProductDetailDto>();
            Assert.NotNull(latestProdDto);

            // Admin updates product
            var updateProduct = new UpdateProductDto
            {
                ProductCode = prodDto.ProductCode,
                Name = "Updated Product Name",
                Description = prodDto.Description,
                DefaultPrice = 120000,
                BaseUnitId = prodDto.BaseUnitId,
                CategoryId = prodDto.CategoryId,
                Status = "Active",
                RowVersion = latestProdDto.RowVersion
            };
            var updateRes = await adminClient.PutAsJsonAsync($"/api/products/{prodDto.Id}", updateProduct);
            Assert.Equal(HttpStatusCode.NoContent, updateRes.StatusCode);

            // Delete product
            var deleteProdRes = await adminClient.DeleteAsync($"/api/products/{prodDto.Id}");
            Assert.Equal(HttpStatusCode.NoContent, deleteProdRes.StatusCode);
        }

        [Fact]
        public async Task SearchProducts_IncludingChildCategories_Success()
        {
            var adminUser = await SeedUserAsync("Admin");
            var adminClient = GetAuthenticatedClient(adminUser, "Admin");

            // Seed unit
            var unit = new Unit { Code = $"U_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Unit Test" };
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Units.Add(unit);
                await db.SaveChangesAsync();
            }

            // Seed categories: Parent -> Child -> Grandchild
            var catParent = new ProductCategory { Code = $"CAT_P_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Parent Cat" };
            var catChild = new ProductCategory { Code = $"CAT_C_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Child Cat", ParentId = catParent.Id };
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.ProductCategories.AddRange(catParent, catChild);
                await db.SaveChangesAsync();
            }

            // Seed products
            var prod1 = new Product { ProductCode = $"P1_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Alpha", CategoryId = catParent.Id, BaseUnitId = unit.Id, Status = ProductStatus.Active };
            var prod2 = new Product { ProductCode = $"P2_{Guid.NewGuid().ToString().Substring(0,8)}", Name = "Beta", CategoryId = catChild.Id, BaseUnitId = unit.Id, Status = ProductStatus.Active };
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Products.AddRange(prod1, prod2);
                await db.SaveChangesAsync();
            }

            // Search by parent category with IncludeChildCategories = true
            var searchRequest = new ProductSearchRequestDto
            {
                CategoryId = catParent.Id,
                IncludeChildCategories = true,
                PageSize = 10,
                Page = 1
            };

            var response = await adminClient.PostAsJsonAsync("/api/products/search", searchRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await response.Content.ReadFromJsonAsync<SearchResponseDto>();
            Assert.NotNull(result);
            Assert.True(result.TotalCount >= 2);
            var codes = result.Items.Select(i => i.ProductCode).ToList();
            Assert.Contains(prod1.ProductCode, codes);
            Assert.Contains(prod2.ProductCode, codes);
        }

        private class SearchResponseDto
        {
            public List<ProductDto> Items { get; set; } = new List<ProductDto>();
            public int TotalCount { get; set; }
        }
    }
}
