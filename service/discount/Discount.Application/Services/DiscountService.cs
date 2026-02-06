using Discount.Application.DTOs;
using Discount.Application.Interfaces;
using Discount.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Discount.Application.Services;

public class DiscountService
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IDiscountProductRepository _discountProductRepository;
    private readonly IDiscountCategoryRepository _discountCategoryRepository;
    private readonly IDiscountUserRepository _discountUserRepository;
    private readonly IDiscountUsageRepository _discountUsageRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IPromotionDiscountRepository _promotionDiscountRepository;
    private readonly IFlashSaleRepository _flashSaleRepository;
    private readonly IFlashSaleItemRepository _flashSaleItemRepository;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(
        IDiscountRepository discountRepository,
        IDiscountProductRepository discountProductRepository,
        IDiscountCategoryRepository discountCategoryRepository,
        IDiscountUserRepository discountUserRepository,
        IDiscountUsageRepository discountUsageRepository,
        IPromotionRepository promotionRepository,
        IPromotionDiscountRepository promotionDiscountRepository,
        IFlashSaleRepository flashSaleRepository,
        IFlashSaleItemRepository flashSaleItemRepository,
        ILogger<DiscountService> logger)
    {
        _discountRepository = discountRepository;
        _discountProductRepository = discountProductRepository;
        _discountCategoryRepository = discountCategoryRepository;
        _discountUserRepository = discountUserRepository;
        _discountUsageRepository = discountUsageRepository;
        _promotionRepository = promotionRepository;
        _promotionDiscountRepository = promotionDiscountRepository;
        _flashSaleRepository = flashSaleRepository;
        _flashSaleItemRepository = flashSaleItemRepository;
        _logger = logger;
    }

    // ============================================
    // DISCOUNT - PUBLIC
    // ============================================

    public async Task<PagedResult<DiscountResponse>> GetPublicDiscountsAsync(int page, int pageSize)
    {
        var discounts = await _discountRepository.GetActivePublicDiscountsAsync(page, pageSize);
        var total = await _discountRepository.GetActivePublicDiscountsCountAsync();

        var items = discounts.Select(d => new DiscountResponse(
            d.Id, d.Code, d.Name, d.Description, d.Type, d.Value,
            d.MaxDiscountAmount, d.MinOrderAmount, d.StartDate, d.EndDate
        )).ToList();

        return new PagedResult<DiscountResponse>(items, total, page, pageSize);
    }

    public async Task<ValidateDiscountResponse> ValidateDiscountAsync(ValidateDiscountRequest request, Guid userId)
    {
        var discount = await _discountRepository.GetByCodeAsync(request.Code);
        
        if (discount == null)
            return new ValidateDiscountResponse(false, null, 0, "Mã giảm giá không tồn tại");

        if (!discount.IsActive)
            return new ValidateDiscountResponse(false, null, 0, "Mã giảm giá đã hết hiệu lực");

        var now = DateTime.UtcNow;
        if (discount.StartDate > now)
            return new ValidateDiscountResponse(false, null, 0, "Mã giảm giá chưa được áp dụng");

        if (discount.EndDate < now)
            return new ValidateDiscountResponse(false, null, 0, "Mã giảm giá đã hết hạn");

        if (discount.UsageLimit.HasValue && discount.UsageCount >= discount.UsageLimit.Value)
            return new ValidateDiscountResponse(false, null, 0, "Mã giảm giá đã hết lượt sử dụng");

        // Check user usage limit
        var userUsageCount = await _discountUsageRepository.GetUsageCountByUserAsync(discount.Id, userId);
        if (userUsageCount >= discount.UsageLimitPerUser)
            return new ValidateDiscountResponse(false, null, 0, "Bạn đã sử dụng hết lượt của mã này");

        // Check minimum order amount
        if (request.OrderAmount < discount.MinOrderAmount)
            return new ValidateDiscountResponse(false, null, 0, $"Đơn hàng tối thiểu {discount.MinOrderAmount:N0}đ");

        // Check minimum quantity
        var totalQuantity = request.Items.Sum(i => i.Quantity);
        if (totalQuantity < discount.MinQuantity)
            return new ValidateDiscountResponse(false, null, 0, $"Cần tối thiểu {discount.MinQuantity} sản phẩm");

        // Check scope
        var validationResult = await ValidateScopeAsync(discount, request.Items, userId);
        if (!validationResult.IsValid)
            return new ValidateDiscountResponse(false, null, 0, validationResult.Message);

        // Calculate discount
        var discountAmount = discount.CalculateDiscount(request.OrderAmount, totalQuantity);

        var discountResponse = new DiscountResponse(
            discount.Id, discount.Code, discount.Name, discount.Description,
            discount.Type, discount.Value, discount.MaxDiscountAmount,
            discount.MinOrderAmount, discount.StartDate, discount.EndDate
        );

        return new ValidateDiscountResponse(true, discountResponse, discountAmount, "Áp dụng mã giảm giá thành công");
    }

    public async Task<ApplyDiscountResponse> ApplyDiscountAsync(ApplyDiscountRequest request, Guid userId)
    {
        var validateResult = await ValidateDiscountAsync(
            new ValidateDiscountRequest(request.Code, request.OrderAmount, request.Items),
            userId
        );

        if (!validateResult.Valid)
            return new ApplyDiscountResponse(false, null, 0, validateResult.Message);

        var discount = await _discountRepository.GetByCodeAsync(request.Code);
        if (discount == null)
            return new ApplyDiscountResponse(false, null, 0, "Mã giảm giá không tồn tại");

        // Record usage
        var usage = new DiscountUsage(
            discount.Id, userId, request.OrderId, request.OrderNumber,
            request.OrderAmount, validateResult.DiscountAmount
        );
        await _discountUsageRepository.AddAsync(usage);

        // Increment usage count
        discount.IncrementUsageCount();
        await _discountRepository.UpdateAsync(discount);
        await _discountRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Applied discount {Code} for order {OrderId}, amount: {Amount}",
            request.Code, request.OrderId, validateResult.DiscountAmount);

        return new ApplyDiscountResponse(true, discount.Id, validateResult.DiscountAmount, "Áp dụng mã giảm giá thành công");
    }

    public async Task<List<UserDiscountResponse>> GetUserDiscountsAsync(Guid userId)
    {
        // Get discounts available for all users
        var publicDiscounts = await _discountRepository.GetActivePublicDiscountsAsync(1, 100);
        
        // Get discounts specific to this user
        var userDiscounts = await _discountRepository.GetDiscountsForUserAsync(userId);

        var allDiscounts = publicDiscounts.Union(userDiscounts).DistinctBy(d => d.Id).ToList();

        var result = new List<UserDiscountResponse>();
        foreach (var discount in allDiscounts)
        {
            var userUsageCount = await _discountUsageRepository.GetUsageCountByUserAsync(discount.Id, userId);
            var usageRemaining = discount.UsageLimitPerUser - userUsageCount;
            
            if (usageRemaining > 0 && discount.IsValid())
            {
                result.Add(new UserDiscountResponse(
                    discount.Id, discount.Code, discount.Name, discount.Description,
                    discount.Type, discount.Value, discount.MinOrderAmount,
                    discount.EndDate, usageRemaining
                ));
            }
        }

        return result;
    }

    // ============================================
    // DISCOUNT - ADMIN
    // ============================================

    public async Task<PagedResult<DiscountResponse>> GetAllDiscountsAsync(int page, int pageSize, string? type, bool? isActive, string? search)
    {
        var discounts = await _discountRepository.GetAllAsync(page, pageSize, type, isActive, search);
        var total = await _discountRepository.GetTotalCountAsync(type, isActive, search);

        var items = discounts.Select(d => new DiscountResponse(
            d.Id, d.Code, d.Name, d.Description, d.Type, d.Value,
            d.MaxDiscountAmount, d.MinOrderAmount, d.StartDate, d.EndDate
        )).ToList();

        return new PagedResult<DiscountResponse>(items, total, page, pageSize);
    }

    public async Task<DiscountDetailResponse?> GetDiscountDetailAsync(Guid id)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        if (discount == null) return null;

        var products = await _discountProductRepository.GetByDiscountIdAsync(id);
        var categories = await _discountCategoryRepository.GetByDiscountIdAsync(id);
        var users = await _discountUserRepository.GetByDiscountIdAsync(id);
        var recentUsages = await _discountUsageRepository.GetByDiscountIdAsync(id, 10);

        return new DiscountDetailResponse(
            discount.Id, discount.Code, discount.Name, discount.Description,
            discount.Type, discount.Value, discount.MaxDiscountAmount,
            discount.MinOrderAmount, discount.MinQuantity,
            discount.BuyQuantity, discount.GetQuantity, discount.GetDiscountPercent,
            discount.UsageLimit, discount.UsageLimitPerUser, discount.UsageCount,
            discount.StartDate, discount.EndDate, discount.Scope,
            discount.IsActive, discount.IsPublic, discount.IsStackable, discount.Priority,
            products.Select(p => p.ProductId).ToList(),
            categories.Select(c => c.CategoryId).ToList(),
            users.Select(u => u.UserId).ToList(),
            recentUsages.Select(u => new DiscountUsageResponse(
                u.Id, u.UserId, u.OrderId, u.OrderNumber, u.OrderAmount, u.DiscountAmount, u.CreatedAt
            )).ToList(),
            discount.CreatedAt, discount.UpdatedAt
        );
    }

    public async Task<Guid> CreateDiscountAsync(CreateDiscountRequest request, Guid? createdBy, string? createdByName)
    {
        // Check if code exists
        var existing = await _discountRepository.GetByCodeAsync(request.Code);
        if (existing != null)
            throw new InvalidOperationException($"Mã giảm giá '{request.Code}' đã tồn tại");

        var discount = new DiscountEntity(
            request.Code, request.Name, request.Description, request.Type, request.Value,
            request.MaxDiscountAmount, request.MinOrderAmount, request.MinQuantity,
            request.BuyQuantity, request.GetQuantity, request.GetDiscountPercent,
            request.UsageLimit, request.UsageLimitPerUser, request.StartDate, request.EndDate,
            request.Scope, request.IsPublic, request.IsStackable, request.Priority,
            createdBy, createdByName
        );

        await _discountRepository.AddAsync(discount);
        await _discountRepository.SaveChangesAsync();

        // Add products
        if (request.ProductIds?.Any() == true)
        {
            var discountProducts = request.ProductIds.Select(pid => new DiscountProduct(discount.Id, pid));
            await _discountProductRepository.AddRangeAsync(discountProducts);
            await _discountProductRepository.SaveChangesAsync();
        }

        // Add categories
        if (request.CategoryIds?.Any() == true)
        {
            var discountCategories = request.CategoryIds.Select(cid => new DiscountCategory(discount.Id, cid));
            await _discountCategoryRepository.AddRangeAsync(discountCategories);
            await _discountCategoryRepository.SaveChangesAsync();
        }

        // Add users
        if (request.UserIds?.Any() == true)
        {
            var discountUsers = request.UserIds.Select(uid => new DiscountUser(discount.Id, uid));
            await _discountUserRepository.AddRangeAsync(discountUsers);
            await _discountUserRepository.SaveChangesAsync();
        }

        _logger.LogInformation("✅ Created discount: {Code}", discount.Code);
        return discount.Id;
    }

    public async Task UpdateDiscountAsync(Guid id, UpdateDiscountRequest request)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        if (discount == null)
            throw new InvalidOperationException("Không tìm thấy mã giảm giá");

        discount.Update(
            request.Name, request.Description, request.Value, request.MaxDiscountAmount,
            request.MinOrderAmount, request.MinQuantity, request.UsageLimit, request.UsageLimitPerUser,
            request.StartDate, request.EndDate, request.Scope, request.IsPublic, request.IsStackable, request.Priority
        );

        await _discountRepository.UpdateAsync(discount);
        await _discountRepository.SaveChangesAsync();

        // Update products
        await _discountProductRepository.DeleteByDiscountIdAsync(id);
        if (request.ProductIds?.Any() == true)
        {
            var discountProducts = request.ProductIds.Select(pid => new DiscountProduct(id, pid));
            await _discountProductRepository.AddRangeAsync(discountProducts);
        }
        await _discountProductRepository.SaveChangesAsync();

        // Update categories
        await _discountCategoryRepository.DeleteByDiscountIdAsync(id);
        if (request.CategoryIds?.Any() == true)
        {
            var discountCategories = request.CategoryIds.Select(cid => new DiscountCategory(id, cid));
            await _discountCategoryRepository.AddRangeAsync(discountCategories);
        }
        await _discountCategoryRepository.SaveChangesAsync();

        // Update users
        await _discountUserRepository.DeleteByDiscountIdAsync(id);
        if (request.UserIds?.Any() == true)
        {
            var discountUsers = request.UserIds.Select(uid => new DiscountUser(id, uid));
            await _discountUserRepository.AddRangeAsync(discountUsers);
        }
        await _discountUserRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Updated discount: {Id}", id);
    }

    public async Task DeleteDiscountAsync(Guid id)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        if (discount == null)
            throw new InvalidOperationException("Không tìm thấy mã giảm giá");

        await _discountRepository.DeleteAsync(discount);
        await _discountRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Deleted discount: {Id}", id);
    }

    public async Task ToggleDiscountStatusAsync(Guid id)
    {
        var discount = await _discountRepository.GetByIdAsync(id);
        if (discount == null)
            throw new InvalidOperationException("Không tìm thấy mã giảm giá");

        discount.SetActive(!discount.IsActive);
        await _discountRepository.UpdateAsync(discount);
        await _discountRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Toggled discount status: {Id} -> {IsActive}", id, discount.IsActive);
    }

    public async Task<DiscountStatisticsResponse> GetDiscountStatisticsAsync(Guid id)
    {
        var (totalUsage, totalAmount, uniqueUsers) = await _discountUsageRepository.GetStatisticsAsync(id);
        var usageByDate = await _discountUsageRepository.GetUsageByDateAsync(id, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);

        return new DiscountStatisticsResponse(
            totalUsage, totalAmount, uniqueUsers,
            usageByDate.Select(u => new UsageByDateResponse(u.Date, u.Count, u.Amount)).ToList()
        );
    }

    // ============================================
    // PROMOTION
    // ============================================

    public async Task<List<PromotionResponse>> GetActivePromotionsAsync()
    {
        var promotions = await _promotionRepository.GetActivePromotionsAsync();
        var result = new List<PromotionResponse>();

        foreach (var promo in promotions)
        {
            var promoDiscounts = await _promotionDiscountRepository.GetByPromotionIdAsync(promo.Id);
            var discountResponses = new List<DiscountResponse>();

            foreach (var pd in promoDiscounts.OrderBy(p => p.DisplayOrder))
            {
                var discount = await _discountRepository.GetByIdAsync(pd.DiscountId);
                if (discount != null && discount.IsValid())
                {
                    discountResponses.Add(new DiscountResponse(
                        discount.Id, discount.Code, discount.Name, discount.Description,
                        discount.Type, discount.Value, discount.MaxDiscountAmount,
                        discount.MinOrderAmount, discount.StartDate, discount.EndDate
                    ));
                }
            }

            result.Add(new PromotionResponse(
                promo.Id, promo.Code, promo.Name, promo.Description,
                promo.BannerUrl, promo.ThumbnailUrl, promo.StartDate, promo.EndDate,
                promo.IsFeatured, discountResponses
            ));
        }

        return result;
    }

    public async Task<PromotionDetailResponse?> GetPromotionDetailAsync(Guid id)
    {
        var promo = await _promotionRepository.GetByIdWithDiscountsAsync(id);
        if (promo == null) return null;

        var promoDiscounts = await _promotionDiscountRepository.GetByPromotionIdAsync(id);
        var discountResponses = new List<DiscountResponse>();

        foreach (var pd in promoDiscounts.OrderBy(p => p.DisplayOrder))
        {
            var discount = await _discountRepository.GetByIdAsync(pd.DiscountId);
            if (discount != null)
            {
                discountResponses.Add(new DiscountResponse(
                    discount.Id, discount.Code, discount.Name, discount.Description,
                    discount.Type, discount.Value, discount.MaxDiscountAmount,
                    discount.MinOrderAmount, discount.StartDate, discount.EndDate
                ));
            }
        }

        return new PromotionDetailResponse(
            promo.Id, promo.Code, promo.Name, promo.Description,
            promo.BannerUrl, promo.ThumbnailUrl, promo.StartDate, promo.EndDate,
            promo.IsActive, promo.IsFeatured, promo.DisplayOrder,
            discountResponses, promo.CreatedAt, promo.UpdatedAt
        );
    }

    public async Task<PagedResult<PromotionResponse>> GetAllPromotionsAsync(int page, int pageSize, bool? isActive)
    {
        var promotions = await _promotionRepository.GetAllAsync(page, pageSize, isActive);
        var total = await _promotionRepository.GetTotalCountAsync(isActive);

        var items = new List<PromotionResponse>();
        foreach (var promo in promotions)
        {
            var promoDiscounts = await _promotionDiscountRepository.GetByPromotionIdAsync(promo.Id);
            var discountResponses = new List<DiscountResponse>();

            foreach (var pd in promoDiscounts.Take(5))
            {
                var discount = await _discountRepository.GetByIdAsync(pd.DiscountId);
                if (discount != null)
                {
                    discountResponses.Add(new DiscountResponse(
                        discount.Id, discount.Code, discount.Name, discount.Description,
                        discount.Type, discount.Value, discount.MaxDiscountAmount,
                        discount.MinOrderAmount, discount.StartDate, discount.EndDate
                    ));
                }
            }

            items.Add(new PromotionResponse(
                promo.Id, promo.Code, promo.Name, promo.Description,
                promo.BannerUrl, promo.ThumbnailUrl, promo.StartDate, promo.EndDate,
                promo.IsFeatured, discountResponses
            ));
        }

        return new PagedResult<PromotionResponse>(items, total, page, pageSize);
    }

    public async Task<Guid> CreatePromotionAsync(CreatePromotionRequest request, Guid? createdBy, string? createdByName)
    {
        var promotion = new Promotion(
            request.Code, request.Name, request.Description,
            request.BannerUrl, request.ThumbnailUrl,
            request.StartDate, request.EndDate, request.IsFeatured, request.DisplayOrder,
            createdBy, createdByName
        );

        await _promotionRepository.AddAsync(promotion);
        await _promotionRepository.SaveChangesAsync();

        // Add discounts
        if (request.DiscountIds?.Any() == true)
        {
            var promoDiscounts = request.DiscountIds.Select((did, idx) => new PromotionDiscount(promotion.Id, did, idx));
            await _promotionDiscountRepository.AddRangeAsync(promoDiscounts);
            await _promotionDiscountRepository.SaveChangesAsync();
        }

        _logger.LogInformation("✅ Created promotion: {Code}", promotion.Code);
        return promotion.Id;
    }

    public async Task UpdatePromotionAsync(Guid id, UpdatePromotionRequest request)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id);
        if (promotion == null)
            throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi");

        promotion.Update(
            request.Name, request.Description, request.BannerUrl, request.ThumbnailUrl,
            request.StartDate, request.EndDate, request.IsFeatured, request.DisplayOrder
        );

        await _promotionRepository.UpdateAsync(promotion);
        await _promotionRepository.SaveChangesAsync();

        // Update discounts
        await _promotionDiscountRepository.DeleteByPromotionIdAsync(id);
        if (request.DiscountIds?.Any() == true)
        {
            var promoDiscounts = request.DiscountIds.Select((did, idx) => new PromotionDiscount(id, did, idx));
            await _promotionDiscountRepository.AddRangeAsync(promoDiscounts);
        }
        await _promotionDiscountRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Updated promotion: {Id}", id);
    }

    public async Task DeletePromotionAsync(Guid id)
    {
        var promotion = await _promotionRepository.GetByIdAsync(id);
        if (promotion == null)
            throw new InvalidOperationException("Không tìm thấy chương trình khuyến mãi");

        await _promotionRepository.DeleteAsync(promotion);
        await _promotionRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Deleted promotion: {Id}", id);
    }

    // ============================================
    // FLASH SALE
    // ============================================

    public async Task<List<FlashSaleResponse>> GetActiveFlashSalesAsync()
    {
        var flashSales = await _flashSaleRepository.GetActiveFlashSalesAsync();
        var result = new List<FlashSaleResponse>();

        foreach (var fs in flashSales)
        {
            var items = await _flashSaleItemRepository.GetByFlashSaleIdAsync(fs.Id);
            result.Add(new FlashSaleResponse(
                fs.Id, fs.Name, fs.StartTime, fs.EndTime, fs.IsActive,
                items.Select(i => new FlashSaleItemResponse(
                    i.Id, i.ProductId, i.VariantId, i.OriginalPrice, i.SalePrice,
                    i.DiscountPercent, i.QuantityLimit, i.QuantitySold, i.GetQuantityRemaining(), i.LimitPerUser
                )).ToList()
            ));
        }

        return result;
    }

    public async Task<FlashSaleResponse?> GetFlashSaleDetailAsync(Guid id)
    {
        var flashSale = await _flashSaleRepository.GetByIdWithItemsAsync(id);
        if (flashSale == null) return null;

        var items = await _flashSaleItemRepository.GetByFlashSaleIdAsync(id);
        return new FlashSaleResponse(
            flashSale.Id, flashSale.Name, flashSale.StartTime, flashSale.EndTime, flashSale.IsActive,
            items.Select(i => new FlashSaleItemResponse(
                i.Id, i.ProductId, i.VariantId, i.OriginalPrice, i.SalePrice,
                i.DiscountPercent, i.QuantityLimit, i.QuantitySold, i.GetQuantityRemaining(), i.LimitPerUser
            )).ToList()
        );
    }

    public async Task<FlashSaleAvailabilityResponse?> CheckFlashSaleItemAvailabilityAsync(Guid flashSaleId, Guid productId, Guid? variantId)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(flashSaleId);
        if (flashSale == null || !flashSale.IsValid()) return null;

        var item = await _flashSaleItemRepository.GetByProductAsync(flashSaleId, productId, variantId);
        if (item == null) return null;

        return new FlashSaleAvailabilityResponse(
            item.IsAvailable(), item.GetQuantityRemaining(), item.SalePrice, item.LimitPerUser
        );
    }

    public async Task<PagedResult<FlashSaleResponse>> GetAllFlashSalesAsync(int page, int pageSize)
    {
        var flashSales = await _flashSaleRepository.GetAllAsync(page, pageSize);
        var total = await _flashSaleRepository.GetTotalCountAsync();

        var items = new List<FlashSaleResponse>();
        foreach (var fs in flashSales)
        {
            var fsItems = await _flashSaleItemRepository.GetByFlashSaleIdAsync(fs.Id);
            items.Add(new FlashSaleResponse(
                fs.Id, fs.Name, fs.StartTime, fs.EndTime, fs.IsActive,
                fsItems.Select(i => new FlashSaleItemResponse(
                    i.Id, i.ProductId, i.VariantId, i.OriginalPrice, i.SalePrice,
                    i.DiscountPercent, i.QuantityLimit, i.QuantitySold, i.GetQuantityRemaining(), i.LimitPerUser
                )).ToList()
            ));
        }

        return new PagedResult<FlashSaleResponse>(items, total, page, pageSize);
    }

    public async Task<Guid> CreateFlashSaleAsync(CreateFlashSaleRequest request)
    {
        var flashSale = new FlashSale(request.Name, request.StartTime, request.EndTime);
        await _flashSaleRepository.AddAsync(flashSale);
        await _flashSaleRepository.SaveChangesAsync();

        if (request.Items?.Any() == true)
        {
            var items = request.Items.Select(i => new FlashSaleItem(
                flashSale.Id, i.ProductId, i.VariantId, i.OriginalPrice, i.SalePrice, i.QuantityLimit, i.LimitPerUser
            ));
            await _flashSaleItemRepository.AddRangeAsync(items);
            await _flashSaleItemRepository.SaveChangesAsync();
        }

        _logger.LogInformation("✅ Created flash sale: {Name}", flashSale.Name);
        return flashSale.Id;
    }

    public async Task UpdateFlashSaleAsync(Guid id, UpdateFlashSaleRequest request)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(id);
        if (flashSale == null)
            throw new InvalidOperationException("Không tìm thấy Flash Sale");

        flashSale.Update(request.Name, request.StartTime, request.EndTime, request.IsActive);
        await _flashSaleRepository.UpdateAsync(flashSale);
        await _flashSaleRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Updated flash sale: {Id}", id);
    }

    public async Task<Guid> AddFlashSaleItemAsync(Guid flashSaleId, CreateFlashSaleItemRequest request)
    {
        var flashSale = await _flashSaleRepository.GetByIdAsync(flashSaleId);
        if (flashSale == null)
            throw new InvalidOperationException("Không tìm thấy Flash Sale");

        var item = new FlashSaleItem(
            flashSaleId, request.ProductId, request.VariantId,
            request.OriginalPrice, request.SalePrice, request.QuantityLimit, request.LimitPerUser
        );

        await _flashSaleItemRepository.AddAsync(item);
        await _flashSaleItemRepository.SaveChangesAsync();

        return item.Id;
    }

    public async Task UpdateFlashSaleItemAsync(Guid itemId, UpdateFlashSaleItemRequest request)
    {
        var item = await _flashSaleItemRepository.GetByIdAsync(itemId);
        if (item == null)
            throw new InvalidOperationException("Không tìm thấy item");

        item.Update(request.SalePrice, request.QuantityLimit, request.LimitPerUser);
        await _flashSaleItemRepository.UpdateAsync(item);
        await _flashSaleItemRepository.SaveChangesAsync();
    }

    public async Task DeleteFlashSaleItemAsync(Guid itemId)
    {
        var item = await _flashSaleItemRepository.GetByIdAsync(itemId);
        if (item == null)
            throw new InvalidOperationException("Không tìm thấy item");

        await _flashSaleItemRepository.DeleteAsync(item);
        await _flashSaleItemRepository.SaveChangesAsync();
    }

    // ============================================
    // INTERNAL
    // ============================================

    public async Task RecordUsageAsync(RecordUsageRequest request)
    {
        var discount = await _discountRepository.GetByIdAsync(request.DiscountId);
        if (discount == null) return;

        var usage = new DiscountUsage(
            request.DiscountId, request.UserId, request.OrderId,
            request.OrderNumber, request.OrderAmount, request.DiscountAmount
        );

        await _discountUsageRepository.AddAsync(usage);
        discount.IncrementUsageCount();
        await _discountRepository.UpdateAsync(discount);
        await _discountRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Recorded usage for discount {DiscountId}, order {OrderId}", request.DiscountId, request.OrderId);
    }

    public async Task RollbackUsageAsync(Guid orderId)
    {
        var usage = await _discountUsageRepository.GetByOrderIdAsync(orderId);
        if (usage == null) return;

        var discount = await _discountRepository.GetByIdAsync(usage.DiscountId);
        if (discount != null)
        {
            discount.DecrementUsageCount();
            await _discountRepository.UpdateAsync(discount);
        }

        await _discountUsageRepository.DeleteAsync(usage);
        await _discountUsageRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Rolled back usage for order {OrderId}", orderId);
    }

    public async Task<List<ProductDiscountResponse>> GetDiscountsForProductsAsync(List<Guid> productIds)
    {
        var discounts = await _discountRepository.GetDiscountsForProductsAsync(productIds);
        
        return productIds.Select(pid => new ProductDiscountResponse(
            pid,
            discounts.Where(d => d.Scope == DiscountScopes.All || 
                                 d.DiscountProducts.Any(dp => dp.ProductId == pid))
                     .Select(d => new DiscountResponse(
                         d.Id, d.Code, d.Name, d.Description, d.Type, d.Value,
                         d.MaxDiscountAmount, d.MinOrderAmount, d.StartDate, d.EndDate
                     )).ToList()
        )).ToList();
    }

    // ============================================
    // PRIVATE HELPERS
    // ============================================

    private async Task<(bool IsValid, string Message)> ValidateScopeAsync(DiscountEntity discount, List<ValidateDiscountItem> items, Guid userId)
    {
        switch (discount.Scope)
        {
            case DiscountScopes.SpecificProducts:
                var discountProducts = await _discountProductRepository.GetByDiscountIdAsync(discount.Id);
                var productIds = discountProducts.Select(p => p.ProductId).ToHashSet();
                if (!items.Any(i => productIds.Contains(i.ProductId)))
                    return (false, "Mã giảm giá không áp dụng cho sản phẩm này");
                break;

            case DiscountScopes.SpecificCategories:
                var discountCategories = await _discountCategoryRepository.GetByDiscountIdAsync(discount.Id);
                var categoryIds = discountCategories.Select(c => c.CategoryId).ToHashSet();
                if (!items.Any(i => i.CategoryId.HasValue && categoryIds.Contains(i.CategoryId.Value)))
                    return (false, "Mã giảm giá không áp dụng cho danh mục này");
                break;

            case DiscountScopes.SpecificUsers:
                var discountUsers = await _discountUserRepository.GetByDiscountIdAsync(discount.Id);
                if (!discountUsers.Any(u => u.UserId == userId))
                    return (false, "Bạn không đủ điều kiện sử dụng mã này");
                break;
        }

        return (true, string.Empty);
    }
}

