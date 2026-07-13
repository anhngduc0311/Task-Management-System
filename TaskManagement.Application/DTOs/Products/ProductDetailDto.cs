using System.Collections.Generic;
using TaskManagement.Application.DTOs.ProductLabels;
using TaskManagement.Application.DTOs.Suppliers;
using TaskManagement.Application.DTOs.ProductUnitConversions;
using TaskManagement.Application.DTOs.ProductVariants;

namespace TaskManagement.Application.DTOs.Products
{
    public class ProductDetailDto : ProductDto
    {
        public List<ProductImageDto> Images { get; set; } = new List<ProductImageDto>();
        public List<ConversionDto> UnitConversions { get; set; } = new List<ConversionDto>();
        public List<SupplierDto> Suppliers { get; set; } = new List<SupplierDto>();
        public List<LabelDto> Labels { get; set; } = new List<LabelDto>();
        public List<ProductAttributeGroupDto> AttributeGroups { get; set; } = new List<ProductAttributeGroupDto>();
        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
    }

    public class ProductImageDto
    {
        public System.Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StorageKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
        public System.DateTime CreatedAt { get; set; }
    }
}
