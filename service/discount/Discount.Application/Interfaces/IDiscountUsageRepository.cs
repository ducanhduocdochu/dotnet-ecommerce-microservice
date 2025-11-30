using Discount.Domain.Entities;

namespace Discount.Application.Interfaces;

public interface IDiscountUsageRepository
{
    Task<DiscountUsage?> GetByOrderIdAsync(Guid orderId);
    Task<List<DiscountUsage>> GetByDiscountIdAsync(Guid discountId, int limit = 10);
    Task<int> GetUsageCountByUserAsync(Guid discountId, Guid userId);
    Task<(int TotalUsage, decimal TotalAmount, int UniqueUsers)> GetStatisticsAsync(Guid discountId);
    Task<List<(DateTime Date, int Count, decimal Amount)>> GetUsageByDateAsync(Guid discountId, DateTime startDate, DateTime endDate);
    Task AddAsync(DiscountUsage usage);
    Task DeleteAsync(DiscountUsage usage);
    Task SaveChangesAsync();
}

