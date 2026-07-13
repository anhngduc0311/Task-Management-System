using System;

namespace TaskManagement.Application.DTOs.Products
{
    public class ProductSearchRequestDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Status { get; set; }
        public Guid? OriginId { get; set; }
        public Guid? SupplierId { get; set; }
        public Guid? LabelId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool IncludeChildCategories { get; set; } = true;
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
