using System;

namespace TaskManagement.Domain.Entities
{
    public class ProductUnitConversion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid FromUnitId { get; set; }
        public Unit FromUnit { get; set; } = null!;

        public Guid ToUnitId { get; set; }
        public Unit ToUnit { get; set; } = null!;

        private decimal _conversionRate;
        public decimal ConversionRate
        {
            get => _conversionRate;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentException("Conversion rate must be greater than zero.");
                }
                _conversionRate = value;
            }
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
