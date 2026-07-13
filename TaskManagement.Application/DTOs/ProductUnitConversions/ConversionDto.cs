using System;

namespace TaskManagement.Application.DTOs.ProductUnitConversions
{
    public class ConversionDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public Guid FromUnitId { get; set; }
        public string FromUnitName { get; set; } = string.Empty;
        public Guid ToUnitId { get; set; }
        public string ToUnitName { get; set; } = string.Empty;
        public decimal ConversionRate { get; set; }
    }
}
