using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Application.Services
{
    public class UnitConversionService : IUnitConversionService
    {
        private readonly IAppDbContext _dbContext;

        public UnitConversionService(IAppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<decimal> ConvertToBaseUnitAsync(Guid productId, Guid fromUnitId, decimal quantity)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null) throw new ArgumentException("Product not found.");

            if (fromUnitId == product.BaseUnitId) return quantity;

            var conversion = await _dbContext.ProductUnitConversions
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.FromUnitId == fromUnitId && c.ToUnitId == product.BaseUnitId);

            if (conversion == null)
            {
                throw new InvalidOperationException($"No unit conversion path found from unit {fromUnitId} to base unit {product.BaseUnitId} for product {productId}.");
            }

            return quantity * conversion.ConversionRate;
        }

        public async Task<decimal> ConvertFromBaseUnitAsync(Guid productId, Guid toUnitId, decimal baseQuantity)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null) throw new ArgumentException("Product not found.");

            if (toUnitId == product.BaseUnitId) return baseQuantity;

            var conversion = await _dbContext.ProductUnitConversions
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.FromUnitId == toUnitId && c.ToUnitId == product.BaseUnitId);

            if (conversion == null)
            {
                throw new InvalidOperationException($"No unit conversion path found from base unit {product.BaseUnitId} to unit {toUnitId} for product {productId}.");
            }

            return baseQuantity / conversion.ConversionRate;
        }

        public async Task<decimal> ConvertPriceToUnitAsync(Guid productId, decimal basePrice, Guid targetUnitId)
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null) throw new ArgumentException("Product not found.");

            if (targetUnitId == product.BaseUnitId) return basePrice;

            var conversion = await _dbContext.ProductUnitConversions
                .FirstOrDefaultAsync(c => c.ProductId == productId && c.FromUnitId == targetUnitId && c.ToUnitId == product.BaseUnitId);

            if (conversion == null)
            {
                throw new InvalidOperationException($"No unit conversion path found from unit {targetUnitId} to base unit {product.BaseUnitId} for product {productId}.");
            }

            return basePrice * conversion.ConversionRate;
        }
    }
}
