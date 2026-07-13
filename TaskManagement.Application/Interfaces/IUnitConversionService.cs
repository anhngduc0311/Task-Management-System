using System;
using System.Threading.Tasks;

namespace TaskManagement.Application.Interfaces
{
    public interface IUnitConversionService
    {
        Task<decimal> ConvertToBaseUnitAsync(Guid productId, Guid fromUnitId, decimal quantity);
        Task<decimal> ConvertFromBaseUnitAsync(Guid productId, Guid toUnitId, decimal baseQuantity);
        Task<decimal> ConvertPriceToUnitAsync(Guid productId, decimal basePrice, Guid targetUnitId);
    }
}
