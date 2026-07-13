using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TaskManagement.Application.DTOs.Products;
using TaskManagement.Application.DTOs.ProductLabels;
using TaskManagement.Application.DTOs.Suppliers;
using TaskManagement.Application.DTOs.ProductUnitConversions;
using TaskManagement.Application.DTOs.ProductVariants;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : BaseApiController
    {
        private readonly IAppDbContext _dbContext;
        private readonly IPermissionService _permissionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IAuditService _auditService;
        private readonly IHtmlSanitizer _htmlSanitizer;
        private readonly IConfiguration _configuration;

        public ProductsController(
            IAppDbContext dbContext,
            IPermissionService permissionService,
            IFileStorageService fileStorageService,
            IAuditService auditService,
            IHtmlSanitizer htmlSanitizer,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _permissionService = permissionService;
            _fileStorageService = fileStorageService;
            _auditService = auditService;
            _htmlSanitizer = htmlSanitizer;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] Guid? categoryId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var request = new ProductSearchRequestDto
            {
                Search = search,
                CategoryId = categoryId,
                Status = status,
                Page = page,
                PageSize = pageSize
            };

            return await ExecuteSearch(request);
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] ProductSearchRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            return await ExecuteSearch(request);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products
                .Include(p => p.BaseUnit)
                .Include(p => p.Category)
                .Include(p => p.Origin)
                .Include(p => p.Images)
                .Include(p => p.UnitConversions)
                    .ThenInclude(c => c.FromUnit)
                .Include(p => p.UnitConversions)
                    .ThenInclude(c => c.ToUnit)
                .Include(p => p.ProductSuppliers)
                    .ThenInclude(ps => ps.Supplier)
                .Include(p => p.ProductProductLabels)
                    .ThenInclude(pl => pl.ProductLabel)
                .Include(p => p.AttributeGroups)
                    .ThenInclude(g => g.AttributeValues)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantAttributeValues)
                        .ThenInclude(vav => vav.ProductAttributeValue)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            var dto = new ProductDetailDto
            {
                Id = product.Id,
                ProductCode = product.ProductCode,
                Name = product.Name,
                Description = product.Description,
                DefaultPrice = product.DefaultPrice,
                BaseUnitId = product.BaseUnitId,
                BaseUnitName = product.BaseUnit.Name,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Status = product.Status.ToString(),
                OriginId = product.OriginId,
                OriginName = product.Origin?.Name,
                IsDeleted = product.IsDeleted,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                RowVersion = Convert.ToBase64String(product.RowVersion),
                Images = product.Images.OrderBy(i => i.DisplayOrder).Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    FileName = i.FileName,
                    StorageKey = i.StorageKey,
                    Url = i.Url,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder,
                    CreatedAt = i.CreatedAt
                }).ToList(),
                UnitConversions = product.UnitConversions.Select(c => new ConversionDto
                {
                    Id = c.Id,
                    ProductId = c.ProductId,
                    FromUnitId = c.FromUnitId,
                    FromUnitName = c.FromUnit.Name,
                    ToUnitId = c.ToUnitId,
                    ToUnitName = c.ToUnit.Name,
                    ConversionRate = c.ConversionRate
                }).ToList(),
                Suppliers = product.ProductSuppliers.Select(ps => new SupplierDto
                {
                    Id = ps.Supplier.Id,
                    Code = ps.Supplier.Code,
                    Name = ps.Supplier.Name,
                    Phone = ps.Supplier.Phone,
                    Email = ps.Supplier.Email,
                    Address = ps.Supplier.Address,
                    TaxCode = ps.Supplier.TaxCode,
                    ContactPerson = ps.Supplier.ContactPerson,
                    IsActive = ps.Supplier.IsActive
                }).ToList(),
                Labels = product.ProductProductLabels.Select(pl => new LabelDto
                {
                    Id = pl.ProductLabel.Id,
                    Code = pl.ProductLabel.Code,
                    Name = pl.ProductLabel.Name,
                    Color = pl.ProductLabel.Color,
                    IsActive = pl.ProductLabel.IsActive
                }).ToList(),
                AttributeGroups = product.AttributeGroups.OrderBy(g => g.DisplayOrder).Select(g => new ProductAttributeGroupDto
                {
                    Id = g.Id,
                    ProductId = g.ProductId,
                    Name = g.Name,
                    DisplayOrder = g.DisplayOrder,
                    Values = g.AttributeValues.OrderBy(v => v.DisplayOrder).Select(v => new ProductAttributeValueDto
                    {
                        Id = v.Id,
                        AttributeGroupId = v.AttributeGroupId,
                        Value = v.Value,
                        DisplayOrder = v.DisplayOrder
                    }).ToList()
                }).ToList(),
                Variants = product.Variants.Where(v => !v.IsDeleted).Select(v => new ProductVariantDto
                {
                    Id = v.Id,
                    ProductId = v.ProductId,
                    SKU = v.SKU,
                    Price = v.Price,
                    ImageUrl = v.ImageUrl,
                    AttributeValueIds = v.VariantAttributeValues.Select(vav => vav.ProductAttributeValueId).ToList(),
                    AttributeValueCombinations = string.Join(" / ", v.VariantAttributeValues.Select(vav => vav.ProductAttributeValue.Value)),
                    RowVersion = Convert.ToBase64String(v.RowVersion)
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            // Check unique ProductCode
            var exists = await _dbContext.Products.AnyAsync(p => p.ProductCode.ToUpper() == dto.ProductCode.ToUpper() && !p.IsDeleted);
            if (exists)
            {
                return BadRequest(new { message = "Product code already exists." });
            }

            // Validate BaseUnit
            var baseUnitExists = await _dbContext.Units.AnyAsync(u => u.Id == dto.BaseUnitId && u.IsActive);
            if (!baseUnitExists)
            {
                return BadRequest(new { message = "Base unit not found or is inactive." });
            }

            // Validate Category
            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _dbContext.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId.Value && c.IsActive);
                if (!categoryExists)
                {
                    return BadRequest(new { message = "Category not found or is inactive." });
                }
            }

            // Validate Origin
            if (dto.OriginId.HasValue)
            {
                var originExists = await _dbContext.Origins.AnyAsync(o => o.Id == dto.OriginId.Value && o.IsActive);
                if (!originExists)
                {
                    return BadRequest(new { message = "Origin not found or is inactive." });
                }
            }

            if (!Enum.TryParse<ProductStatus>(dto.Status, out var status))
            {
                return BadRequest(new { message = "Invalid status value." });
            }

            // Sanitize description
            var sanitizedDescription = _htmlSanitizer.Sanitize(dto.Description);

            var product = new Product
            {
                ProductCode = dto.ProductCode.Trim(),
                Name = dto.Name.Trim(),
                Description = sanitizedDescription,
                DefaultPrice = dto.DefaultPrice,
                BaseUnitId = dto.BaseUnitId,
                CategoryId = dto.CategoryId,
                Status = status,
                OriginId = dto.OriginId,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Associate Suppliers
            if (dto.SupplierIds != null && dto.SupplierIds.Count > 0)
            {
                foreach (var supId in dto.SupplierIds.Distinct())
                {
                    var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == supId && s.IsActive);
                    if (supplierExists)
                    {
                        product.ProductSuppliers.Add(new ProductSupplier { SupplierId = supId });
                    }
                }
            }

            // Associate Labels
            if (dto.LabelIds != null && dto.LabelIds.Count > 0)
            {
                foreach (var labelId in dto.LabelIds.Distinct())
                {
                    var labelExists = await _dbContext.ProductLabels.AnyAsync(l => l.Id == labelId && l.IsActive);
                    if (labelExists)
                    {
                        product.ProductProductLabels.Add(new ProductProductLabel { ProductLabelId = labelId });
                    }
                }
            }

            // Associate Attribute Groups & Values
            if (dto.AttributeGroups != null && dto.AttributeGroups.Count > 0)
            {
                if (dto.AttributeGroups.Count > 2)
                {
                    return BadRequest(new { message = "A product can have a maximum of 2 attribute groups." });
                }

                for (int i = 0; i < dto.AttributeGroups.Count; i++)
                {
                    var groupDto = dto.AttributeGroups[i];
                    var group = new ProductAttributeGroup
                    {
                        Name = groupDto.Name.Trim(),
                        DisplayOrder = groupDto.DisplayOrder != 0 ? groupDto.DisplayOrder : i
                    };

                    if (groupDto.Values != null && groupDto.Values.Count > 0)
                    {
                        var uniqueVals = groupDto.Values.Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                        for (int j = 0; j < uniqueVals.Count; j++)
                        {
                            group.AttributeValues.Add(new ProductAttributeValue
                            {
                                Value = uniqueVals[j],
                                DisplayOrder = j
                            });
                        }
                    }

                    product.AddAttributeGroup(group);
                }
            }

            _dbContext.Products.Add(product);
            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Product",
                entityId: product.Id.ToString(),
                action: "ProductCreated",
                changedById: CurrentUserId,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            // Fetch names for result
            var baseUnitName = await _dbContext.Units.Where(u => u.Id == product.BaseUnitId).Select(u => u.Name).FirstOrDefaultAsync() ?? string.Empty;
            var categoryName = product.CategoryId.HasValue ? await _dbContext.ProductCategories.Where(c => c.Id == product.CategoryId.Value).Select(c => c.Name).FirstOrDefaultAsync() : null;
            var originName = product.OriginId.HasValue ? await _dbContext.Origins.Where(o => o.Id == product.OriginId.Value).Select(o => o.Name).FirstOrDefaultAsync() : null;

            var result = new ProductDto
            {
                Id = product.Id,
                ProductCode = product.ProductCode,
                Name = product.Name,
                Description = product.Description,
                DefaultPrice = product.DefaultPrice,
                BaseUnitId = product.BaseUnitId,
                BaseUnitName = baseUnitName,
                CategoryId = product.CategoryId,
                CategoryName = categoryName,
                Status = product.Status.ToString(),
                OriginId = product.OriginId,
                OriginName = originName,
                IsDeleted = product.IsDeleted,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                RowVersion = Convert.ToBase64String(product.RowVersion)
            };

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products
                .Include(p => p.ProductSuppliers)
                .Include(p => p.ProductProductLabels)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            // Concurrency check
            var clientRowVersion = Convert.FromBase64String(dto.RowVersion);
            if (!product.RowVersion.SequenceEqual(clientRowVersion))
            {
                return Conflict(new { message = "Concurrency conflict. The product has been modified by another process." });
            }

            // Check unique ProductCode
            var exists = await _dbContext.Products.AnyAsync(p => p.Id != id && p.ProductCode.ToUpper() == dto.ProductCode.ToUpper() && !p.IsDeleted);
            if (exists)
            {
                return BadRequest(new { message = "Product code already exists." });
            }

            // Validate BaseUnit
            var baseUnitExists = await _dbContext.Units.AnyAsync(u => u.Id == dto.BaseUnitId && u.IsActive);
            if (!baseUnitExists)
            {
                return BadRequest(new { message = "Base unit not found or is inactive." });
            }

            // Validate Category
            if (dto.CategoryId.HasValue)
            {
                var categoryExists = await _dbContext.ProductCategories.AnyAsync(c => c.Id == dto.CategoryId.Value && c.IsActive);
                if (!categoryExists)
                {
                    return BadRequest(new { message = "Category not found or is inactive." });
                }
            }

            // Validate Origin
            if (dto.OriginId.HasValue)
            {
                var originExists = await _dbContext.Origins.AnyAsync(o => o.Id == dto.OriginId.Value && o.IsActive);
                if (!originExists)
                {
                    return BadRequest(new { message = "Origin not found or is inactive." });
                }
            }

            if (!Enum.TryParse<ProductStatus>(dto.Status, out var status))
            {
                return BadRequest(new { message = "Invalid status value." });
            }

            var oldValue = JsonSerializer.Serialize(new 
            { 
                product.ProductCode, 
                product.Name, 
                product.Description, 
                product.DefaultPrice, 
                product.BaseUnitId, 
                product.CategoryId, 
                Status = product.Status.ToString(), 
                product.OriginId,
                SupplierIds = product.ProductSuppliers.Select(ps => ps.SupplierId).ToList(),
                LabelIds = product.ProductProductLabels.Select(pl => pl.ProductLabelId).ToList()
            });

            // Sanitize description
            var sanitizedDescription = _htmlSanitizer.Sanitize(dto.Description);

            product.ProductCode = dto.ProductCode.Trim();
            product.Name = dto.Name.Trim();
            product.Description = sanitizedDescription;
            product.DefaultPrice = dto.DefaultPrice;
            product.BaseUnitId = dto.BaseUnitId;
            product.CategoryId = dto.CategoryId;
            product.Status = status;
            product.OriginId = dto.OriginId;
            product.UpdatedAt = DateTime.UtcNow;

            // Update Suppliers
            _dbContext.ProductSuppliers.RemoveRange(product.ProductSuppliers);
            if (dto.SupplierIds != null && dto.SupplierIds.Count > 0)
            {
                foreach (var supId in dto.SupplierIds.Distinct())
                {
                    var supplierExists = await _dbContext.Suppliers.AnyAsync(s => s.Id == supId && s.IsActive);
                    if (supplierExists)
                    {
                        product.ProductSuppliers.Add(new ProductSupplier { SupplierId = supId });
                    }
                }
            }

            // Update Labels
            _dbContext.ProductProductLabels.RemoveRange(product.ProductProductLabels);
            if (dto.LabelIds != null && dto.LabelIds.Count > 0)
            {
                foreach (var labelId in dto.LabelIds.Distinct())
                {
                    var labelExists = await _dbContext.ProductLabels.AnyAsync(l => l.Id == labelId && l.IsActive);
                    if (labelExists)
                    {
                        product.ProductProductLabels.Add(new ProductProductLabel { ProductLabelId = labelId });
                    }
                }
            }

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Conflict(new { message = "Concurrency conflict." });
            }

            await _auditService.LogAsync(
                entityType: "Product",
                entityId: product.Id.ToString(),
                action: "ProductUpdated",
                changedById: CurrentUserId,
                oldValue: oldValue,
                newValue: JsonSerializer.Serialize(dto),
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products.FindAsync(id);
            if (product == null || product.IsDeleted)
            {
                return NotFound(new { message = "Product not found." });
            }

            // Restrict if product has stock balances or is referenced in warehouse transactions
            var inStocks = await _dbContext.StockBalances.AnyAsync(sb => sb.ProductId == id);
            var inMovements = await _dbContext.StockMovements.AnyAsync(sm => sm.ProductId == id);
            var inImports = await _dbContext.ImportReceiptLines.AnyAsync(l => l.ProductId == id);
            var inExports = await _dbContext.ExportReceiptLines.AnyAsync(l => l.ProductId == id);
            var inTransfers = await _dbContext.TransferReceiptLines.AnyAsync(l => l.ProductId == id);

            if (inStocks || inMovements || inImports || inExports || inTransfers)
            {
                return BadRequest(new { message = "Cannot delete product as it is referenced in stock balances or warehouse documents." });
            }

            var oldValue = JsonSerializer.Serialize(new { product.ProductCode, product.Name, product.DefaultPrice });

            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "Product",
                entityId: id.ToString(),
                action: "ProductDeleted",
                changedById: CurrentUserId,
                oldValue: oldValue,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        // Image APIs

        [HttpGet("images/{storageKey}")]
        [AllowAnonymous] // Allow viewing product image files publicly
        public async Task<IActionResult> GetImageFile(string storageKey)
        {
            try
            {
                var fileStream = await _fileStorageService.GetFileAsync(storageKey);
                // Determine mime type based on extension
                var ext = Path.GetExtension(storageKey).ToLower();
                var contentType = ext switch
                {
                    ".png" => "image/png",
                    ".webp" => "image/webp",
                    ".gif" => "image/gif",
                    _ => "image/jpeg"
                };
                return File(fileStream, contentType);
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = "Image file not found in storage." });
            }
        }

        [HttpGet("{id}/images")]
        public async Task<IActionResult> GetImages(Guid id)
        {
            if (!await _permissionService.CanViewProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var productExists = await _dbContext.Products.AnyAsync(p => p.Id == id && !p.IsDeleted);
            if (!productExists)
            {
                return NotFound(new { message = "Product not found." });
            }

            var images = await _dbContext.ProductImages
                .Where(i => i.ProductId == id)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto
                {
                    Id = i.Id,
                    FileName = i.FileName,
                    StorageKey = i.StorageKey,
                    Url = i.Url,
                    IsPrimary = i.IsPrimary,
                    DisplayOrder = i.DisplayOrder,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            return Ok(images);
        }

        [HttpPost("{id}/images")]
        public async Task<IActionResult> UploadImages(Guid id, [FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
            {
                return BadRequest(new { message = "No files were uploaded." });
            }

            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var product = await _dbContext.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (product == null)
            {
                return NotFound(new { message = "Product not found." });
            }

            // Size validation
            var maxSizeBytes = _configuration.GetValue<long>("FileStorage:MaxProductImageSizeInBytes", 5242880); // Default 5MB
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            // Total image limit per product is 10
            if (product.Images.Count + files.Count > 10)
            {
                return BadRequest(new { message = "A product cannot have more than 10 images." });
            }

            var uploadedImages = new List<ProductImageDto>();

            foreach (var file in files)
            {
                if (file.Length > maxSizeBytes)
                {
                    var maxSizeMb = maxSizeBytes / 1024 / 1024;
                    return BadRequest(new { message = $"File {file.FileName} exceeds the limit of {maxSizeMb}MB." });
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = $"Only image files are allowed ({string.Join(", ", allowedExtensions)})." });
                }
            }

            // Perform upload
            foreach (var file in files)
            {
                string storageKey;
                using (var stream = file.OpenReadStream())
                {
                    storageKey = await _fileStorageService.SaveFileAsync(stream, file.FileName);
                }

                // If it is the first image and product currently has no primary image, make it primary
                var isPrimary = product.Images.Count == 0 && uploadedImages.Count == 0;
                var displayOrder = product.Images.Count + uploadedImages.Count;

                var img = new ProductImage
                {
                    ProductId = id,
                    FileName = file.FileName,
                    StorageKey = storageKey,
                    Url = $"/api/products/images/{storageKey}",
                    IsPrimary = isPrimary,
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ProductImages.Add(img);
                await _dbContext.SaveChangesAsync(); // save to generate Id

                uploadedImages.Add(new ProductImageDto
                {
                    Id = img.Id,
                    FileName = img.FileName,
                    StorageKey = img.StorageKey,
                    Url = img.Url,
                    IsPrimary = img.IsPrimary,
                    DisplayOrder = img.DisplayOrder,
                    CreatedAt = img.CreatedAt
                });
            }

            await _auditService.LogAsync(
                entityType: "ProductImage",
                entityId: id.ToString(),
                action: "ProductImagesUploaded",
                changedById: CurrentUserId,
                newValue: $"Uploaded {files.Count} images.",
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return Ok(uploadedImages);
        }

        [HttpDelete("{id}/images/{imageId}")]
        public async Task<IActionResult> DeleteImage(Guid id, Guid imageId)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var image = await _dbContext.ProductImages.FindAsync(imageId);
            if (image == null || image.ProductId != id)
            {
                return NotFound(new { message = "Image not found." });
            }

            var isPrimary = image.IsPrimary;

            // Delete physically and from DB
            try
            {
                await _fileStorageService.DeleteFileAsync(image.StorageKey);
            }
            catch (Exception)
            {
                // Continue database cleanup even if physical deletion fails
            }

            _dbContext.ProductImages.Remove(image);
            await _dbContext.SaveChangesAsync();

            // If we deleted the primary image, make the first remaining image primary
            if (isPrimary)
            {
                var firstRemaining = await _dbContext.ProductImages
                    .Where(i => i.ProductId == id)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefaultAsync();

                if (firstRemaining != null)
                {
                    firstRemaining.IsPrimary = true;
                    await _dbContext.SaveChangesAsync();
                }
            }

            await _auditService.LogAsync(
                entityType: "ProductImage",
                entityId: imageId.ToString(),
                action: "ProductImageDeleted",
                changedById: CurrentUserId,
                oldValue: image.FileName,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        [HttpPut("{id}/images/{imageId}/primary")]
        public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId)
        {
            if (!await _permissionService.CanManageProductsAsync(CurrentUserId))
            {
                return Forbid();
            }

            var targetImage = await _dbContext.ProductImages.FindAsync(imageId);
            if (targetImage == null || targetImage.ProductId != id)
            {
                return NotFound(new { message = "Image not found." });
            }

            // Set all other images for this product as not primary
            var images = await _dbContext.ProductImages.Where(i => i.ProductId == id).ToListAsync();
            foreach (var img in images)
            {
                img.IsPrimary = (img.Id == imageId);
            }

            await _dbContext.SaveChangesAsync();

            await _auditService.LogAsync(
                entityType: "ProductImage",
                entityId: imageId.ToString(),
                action: "ProductImageSetPrimary",
                changedById: CurrentUserId,
                ipAddress: ClientIpAddress,
                userAgent: ClientUserAgent
            );

            return NoContent();
        }

        // Helper method for search
        private async Task<IActionResult> ExecuteSearch(ProductSearchRequestDto request)
        {
            var query = _dbContext.Products
                .Include(p => p.BaseUnit)
                .Include(p => p.Category)
                .Include(p => p.Origin)
                .Where(p => !p.IsDeleted);

            // Filtering
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim().ToUpper();
                query = query.Where(p => p.Name.ToUpper().Contains(term) || p.ProductCode.ToUpper().Contains(term));
            }

            if (request.CategoryId.HasValue)
            {
                if (request.IncludeChildCategories)
                {
                    var catIds = await GetDescendantCategoryIdsAsync(request.CategoryId.Value);
                    query = query.Where(p => p.CategoryId != null && catIds.Contains(p.CategoryId.Value));
                }
                else
                {
                    query = query.Where(p => p.CategoryId == request.CategoryId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<ProductStatus>(request.Status, true, out var statusEnum))
                {
                    query = query.Where(p => p.Status == statusEnum);
                }
            }

            if (request.OriginId.HasValue)
            {
                query = query.Where(p => p.OriginId == request.OriginId.Value);
            }

            if (request.SupplierId.HasValue)
            {
                query = query.Where(p => p.ProductSuppliers.Any(ps => ps.SupplierId == request.SupplierId.Value));
            }

            if (request.LabelId.HasValue)
            {
                query = query.Where(p => p.ProductProductLabels.Any(pl => pl.ProductLabelId == request.LabelId.Value));
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(p => p.DefaultPrice >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(p => p.DefaultPrice <= request.MaxPrice.Value);
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                var sortCol = request.SortBy.Trim().ToLower();
                var desc = request.SortDescending;

                query = sortCol switch
                {
                    "name" => desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                    "productcode" => desc ? query.OrderByDescending(p => p.ProductCode) : query.OrderBy(p => p.ProductCode),
                    "createdat" => desc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
                    "defaultprice" => desc ? query.OrderByDescending(p => p.DefaultPrice) : query.OrderBy(p => p.DefaultPrice),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(p => p.CreatedAt);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    ProductCode = p.ProductCode,
                    Name = p.Name,
                    Description = p.Description,
                    DefaultPrice = p.DefaultPrice,
                    BaseUnitId = p.BaseUnitId,
                    BaseUnitName = p.BaseUnit.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    Status = p.Status.ToString(),
                    OriginId = p.OriginId,
                    OriginName = p.Origin != null ? p.Origin.Name : null,
                    IsDeleted = p.IsDeleted,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    RowVersion = Convert.ToBase64String(p.RowVersion)
                })
                .ToListAsync();

            return Ok(new
            {
                items,
                totalCount,
                page = request.Page,
                pageSize = request.PageSize
            });
        }

        private async Task<List<Guid>> GetDescendantCategoryIdsAsync(Guid categoryId)
        {
            var categories = await _dbContext.ProductCategories
                .Select(c => new { c.Id, c.ParentId })
                .ToListAsync();

            var result = new List<Guid> { categoryId };
            var queue = new Queue<Guid>();
            queue.Enqueue(categoryId);

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                var children = categories.Where(c => c.ParentId == currentId).Select(c => c.Id);
                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child);
                }
            }

            return result;
        }
    }
}
