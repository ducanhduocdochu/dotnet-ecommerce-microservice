using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
    private readonly IOrderRefundRepository _refundRepository;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        ICartRepository cartRepository,
        IOrderStatusHistoryRepository statusHistoryRepository,
        IOrderRefundRepository refundRepository)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _cartRepository = cartRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _refundRepository = refundRepository;
    }

    // ============ Cart Methods ============
    public async Task<CartResponse> GetCartAsync(Guid userId)
    {
        var items = await _cartRepository.GetByUserIdAsync(userId);
        return new CartResponse(
            items.Select(i => new CartItemResponse(
                i.Id, i.ProductId, i.ProductName, i.ProductImage, i.ProductPrice,
                i.VariantId, i.VariantName, i.VariantPrice,
                i.SellerId, i.SellerName, i.Quantity, i.GetTotalPrice()
            )).ToList(),
            items.Sum(i => i.Quantity),
            items.Sum(i => i.GetTotalPrice())
        );
    }

    public async Task<CartItemResponse> AddToCartAsync(Guid userId, AddToCartRequest request)
    {
        var existing = await _cartRepository.GetByUserAndProductAsync(userId, request.ProductId, request.VariantId);
        
        if (existing != null)
        {
            existing.IncrementQuantity(request.Quantity);
            await _cartRepository.UpdateAsync(existing);
            await _cartRepository.SaveChangesAsync();
            return new CartItemResponse(
                existing.Id, existing.ProductId, existing.ProductName, existing.ProductImage, existing.ProductPrice,
                existing.VariantId, existing.VariantName, existing.VariantPrice,
                existing.SellerId, existing.SellerName, existing.Quantity, existing.GetTotalPrice()
            );
        }

        var item = new CartItem(
            userId, request.ProductId, request.ProductName, request.ProductPrice, request.SellerId,
            request.Quantity, request.ProductImage, request.VariantId, request.VariantName,
            request.VariantPrice, request.SellerName
        );
        await _cartRepository.AddAsync(item);
        await _cartRepository.SaveChangesAsync();

        return new CartItemResponse(
            item.Id, item.ProductId, item.ProductName, item.ProductImage, item.ProductPrice,
            item.VariantId, item.VariantName, item.VariantPrice,
            item.SellerId, item.SellerName, item.Quantity, item.GetTotalPrice()
        );
    }

    public async Task<CartItemResponse?> UpdateCartItemAsync(Guid userId, Guid itemId, int quantity)
    {
        var item = await _cartRepository.GetByIdAsync(itemId);
        if (item == null || item.UserId != userId) return null;

        if (quantity <= 0)
        {
            await _cartRepository.RemoveAsync(item);
            await _cartRepository.SaveChangesAsync();
            return null;
        }

        item.UpdateQuantity(quantity);
        await _cartRepository.UpdateAsync(item);
        await _cartRepository.SaveChangesAsync();

        return new CartItemResponse(
            item.Id, item.ProductId, item.ProductName, item.ProductImage, item.ProductPrice,
            item.VariantId, item.VariantName, item.VariantPrice,
            item.SellerId, item.SellerName, item.Quantity, item.GetTotalPrice()
        );
    }

    public async Task<bool> RemoveCartItemAsync(Guid userId, Guid itemId)
    {
        var item = await _cartRepository.GetByIdAsync(itemId);
        if (item == null || item.UserId != userId) return false;

        await _cartRepository.RemoveAsync(item);
        await _cartRepository.SaveChangesAsync();
        return true;
    }

    public async Task ClearCartAsync(Guid userId)
    {
        await _cartRepository.ClearByUserIdAsync(userId);
        await _cartRepository.SaveChangesAsync();
    }

    // ============ Order Methods ============
    public async Task<OrderCreatedResponse> CreateOrderAsync(Guid userId, CreateOrderRequest request)
    {
        var order = new OrderEntity(
            userId,
            request.Shipping.Name,
            request.Shipping.Phone,
            request.Shipping.Address,
            request.PaymentMethod,
            request.ShippingMethod,
            request.CustomerNote,
            request.UserName,
            request.UserEmail,
            request.UserPhone
        );

        order.SetShippingAddress(
            request.Shipping.Ward,
            request.Shipping.District,
            request.Shipping.City,
            request.Shipping.Country,
            request.Shipping.PostalCode
        );

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        // Add items
        decimal subtotal = 0;
        var orderItems = new List<OrderItem>();
        foreach (var itemReq in request.Items)
        {
            var item = new OrderItem(
                order.Id,
                itemReq.ProductId,
                itemReq.ProductName,
                itemReq.SellerId,
                itemReq.UnitPrice,
                itemReq.Quantity,
                itemReq.ProductSlug,
                itemReq.ProductImage,
                itemReq.ProductSku,
                itemReq.VariantId,
                itemReq.VariantName,
                itemReq.VariantOptions,
                itemReq.SellerName,
                itemReq.SalePrice
            );
            orderItems.Add(item);
            subtotal += item.TotalPrice;
        }
        await _orderItemRepository.AddRangeAsync(orderItems);

        // Calculate totals
        decimal shippingFee = CalculateShippingFee(request.ShippingMethod);
        decimal discountAmount = 0; // TODO: Apply discount from Discount service
        order.CalculateTotals(subtotal, shippingFee, discountAmount, 0);

        // Add initial status history
        var history = new OrderStatusHistory(order.Id, "PENDING", null, "Order created");
        await _statusHistoryRepository.AddAsync(history);

        await _orderRepository.SaveChangesAsync();

        // Clear cart after successful order
        await _cartRepository.ClearByUserIdAsync(userId);
        await _cartRepository.SaveChangesAsync();

        return new OrderCreatedResponse(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.PaymentStatus,
            order.Subtotal,
            order.ShippingFee,
            order.DiscountAmount,
            order.TotalAmount,
            orderItems.Select(i => new OrderItemResponse(
                i.Id, i.ProductId, i.ProductName, i.ProductImage, i.VariantName,
                i.UnitPrice, i.SalePrice, i.Quantity, i.TotalPrice, i.Status
            )).ToList(),
            new ShippingInfoResponse(
                order.ShippingName, order.ShippingPhone, order.ShippingAddress,
                order.ShippingWard, order.ShippingDistrict, order.ShippingCity,
                order.ShippingCountry, order.ShippingPostalCode
            ),
            null // PaymentUrl - TODO: Generate from Payment service
        );
    }

    public async Task<PagedResponse<OrderListResponse>> GetUserOrdersAsync(Guid userId, int page, int pageSize, string? status = null)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId, page, pageSize, status);
        var total = await _orderRepository.GetCountByUserIdAsync(userId, status);
        return new PagedResponse<OrderListResponse>(
            orders.Select(o => new OrderListResponse(
                o.Id, o.OrderNumber, o.Status, o.PaymentStatus, o.TotalAmount, o.Items.Count, o.CreatedAt
            )).ToList(),
            total, page, pageSize
        );
    }

    public async Task<PagedResponse<OrderListResponse>> GetSellerOrdersAsync(Guid sellerId, int page, int pageSize, string? status = null)
    {
        var orders = await _orderRepository.GetBySellerIdAsync(sellerId, page, pageSize, status);
        var total = await _orderRepository.GetCountBySellerIdAsync(sellerId, status);
        return new PagedResponse<OrderListResponse>(
            orders.Select(o => new OrderListResponse(
                o.Id, o.OrderNumber, o.Status, o.PaymentStatus, o.TotalAmount, o.Items.Count, o.CreatedAt
            )).ToList(),
            total, page, pageSize
        );
    }

    public async Task<OrderDetailResponse?> GetOrderByIdAsync(Guid orderId, Guid? userId = null)
    {
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        if (order == null) return null;
        if (userId.HasValue && order.UserId != userId.Value) return null;
        return MapToDetailResponse(order);
    }

    public async Task<OrderDetailResponse?> GetOrderByNumberAsync(string orderNumber, Guid? userId = null)
    {
        var order = await _orderRepository.GetByOrderNumberAsync(orderNumber);
        if (order == null) return null;
        if (userId.HasValue && order.UserId != userId.Value) return null;
        return MapToDetailResponse(order);
    }

    public async Task<bool> CancelOrderAsync(Guid orderId, Guid userId, string reason)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId || !order.CanCancel()) return false;

        var previousStatus = order.Status;
        order.Cancel(reason);
        
        var history = new OrderStatusHistory(orderId, "CANCELLED", previousStatus, reason, userId);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();
        return true;
    }

    // Seller actions
    public async Task<bool> ConfirmOrderAsync(Guid orderId, Guid? changedBy = null, string? changedByName = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || !order.CanConfirm()) return false;

        var previousStatus = order.Status;
        order.Confirm();
        
        var history = new OrderStatusHistory(orderId, "CONFIRMED", previousStatus, "Order confirmed", changedBy, changedByName);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ProcessOrderAsync(Guid orderId, Guid? changedBy = null, string? changedByName = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || !order.CanProcess()) return false;

        var previousStatus = order.Status;
        order.Process();
        
        var history = new OrderStatusHistory(orderId, "PROCESSING", previousStatus, "Order processing", changedBy, changedByName);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ShipOrderAsync(Guid orderId, ShipOrderRequest request, Guid? changedBy = null, string? changedByName = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || !order.CanShip()) return false;

        var previousStatus = order.Status;
        order.Ship(request.ShippingCarrier, request.TrackingNumber, request.EstimatedDelivery);
        
        var history = new OrderStatusHistory(orderId, "SHIPPED", previousStatus, $"Shipped via {request.ShippingCarrier}, tracking: {request.TrackingNumber}", changedBy, changedByName);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeliverOrderAsync(Guid orderId, Guid? changedBy = null, string? changedByName = null)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || !order.CanDeliver()) return false;

        var previousStatus = order.Status;
        order.Deliver();
        
        var history = new OrderStatusHistory(orderId, "DELIVERED", previousStatus, "Order delivered", changedBy, changedByName);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();
        return true;
    }

    // Admin
    public async Task<PagedResponse<OrderListResponse>> GetAllOrdersAsync(int page, int pageSize, string? status = null, Guid? userId = null, DateTime? from = null, DateTime? to = null)
    {
        var orders = await _orderRepository.GetAllAsync(page, pageSize, status, userId, from, to);
        var total = await _orderRepository.GetCountAsync(status, userId, from, to);
        return new PagedResponse<OrderListResponse>(
            orders.Select(o => new OrderListResponse(
                o.Id, o.OrderNumber, o.Status, o.PaymentStatus, o.TotalAmount, o.Items.Count, o.CreatedAt
            )).ToList(),
            total, page, pageSize
        );
    }

    // Refund
    public async Task<RefundResponse?> CreateRefundAsync(Guid orderId, Guid userId, CreateRefundRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        if (order == null || order.UserId != userId) return null;

        var refund = new OrderRefund(orderId, request.Amount, request.Reason, request.RefundMethod, request.OrderItemId);
        if (request.RefundMethod == "BANK_TRANSFER")
        {
            refund.SetBankInfo(request.BankName, request.BankAccount, request.BankAccountName);
        }

        await _refundRepository.AddAsync(refund);
        await _refundRepository.SaveChangesAsync();

        return new RefundResponse(
            refund.Id, refund.OrderId, order.OrderNumber, refund.OrderItemId,
            refund.Amount, refund.Reason, refund.Status, refund.RefundMethod,
            refund.BankName, refund.BankAccount, refund.ProcessedByName,
            refund.ProcessedAt, refund.AdminNote, refund.CreatedAt
        );
    }

    public async Task<PagedResponse<RefundResponse>> GetRefundsAsync(int page, int pageSize, string? status = null)
    {
        var refunds = await _refundRepository.GetAllAsync(page, pageSize, status);
        var total = await _refundRepository.GetCountAsync(status);
        
        // Get order numbers for each refund
        var responses = new List<RefundResponse>();
        foreach (var r in refunds)
        {
            var order = await _orderRepository.GetByIdAsync(r.OrderId);
            responses.Add(new RefundResponse(
                r.Id, r.OrderId, order?.OrderNumber ?? "", r.OrderItemId,
                r.Amount, r.Reason, r.Status, r.RefundMethod,
                r.BankName, r.BankAccount, r.ProcessedByName,
                r.ProcessedAt, r.AdminNote, r.CreatedAt
            ));
        }

        return new PagedResponse<RefundResponse>(responses, total, page, pageSize);
    }

    public async Task<bool> ProcessRefundAsync(Guid refundId, ProcessRefundRequest request, Guid processedBy, string? processedByName)
    {
        var refund = await _refundRepository.GetByIdAsync(refundId);
        if (refund == null || refund.Status != "PENDING") return false;

        if (request.Status == "APPROVED")
            refund.Approve(processedBy, processedByName, request.AdminNote);
        else
            refund.Reject(processedBy, processedByName, request.AdminNote);

        await _refundRepository.SaveChangesAsync();
        return true;
    }

    // Sync user info
    public async Task SyncUserInfoAsync(Guid userId, string? fullName, string? email, string? phone)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId, 1, int.MaxValue, null);
        foreach (var order in orders)
        {
            order.UpdateUserInfo(fullName, email, phone);
        }
        await _orderRepository.SaveChangesAsync();
    }

    // Helpers
    private static decimal CalculateShippingFee(string? method) => method switch
    {
        "EXPRESS" => 50000,
        "SAME_DAY" => 100000,
        _ => 25000 // STANDARD
    };

    private static OrderDetailResponse MapToDetailResponse(OrderEntity o) => new(
        o.Id, o.OrderNumber, o.Status, o.PaymentStatus,
        o.UserId, o.UserName, o.UserEmail, o.UserPhone,
        o.Subtotal, o.ShippingFee, o.DiscountAmount, o.TaxAmount, o.TotalAmount,
        o.DiscountCode,
        o.Items.Select(i => new OrderItemResponse(
            i.Id, i.ProductId, i.ProductName, i.ProductImage, i.VariantName,
            i.UnitPrice, i.SalePrice, i.Quantity, i.TotalPrice, i.Status
        )).ToList(),
        new ShippingInfoResponse(
            o.ShippingName, o.ShippingPhone, o.ShippingAddress,
            o.ShippingWard, o.ShippingDistrict, o.ShippingCity,
            o.ShippingCountry, o.ShippingPostalCode
        ),
        o.ShippingMethod, o.ShippingCarrier, o.TrackingNumber, o.EstimatedDelivery,
        o.StatusHistory.OrderByDescending(h => h.CreatedAt).Select(h => new OrderStatusHistoryResponse(
            h.Status, h.PreviousStatus, h.Note, h.ChangedByName, h.CreatedAt
        )).ToList(),
        o.CustomerNote, o.CreatedAt, o.ConfirmedAt, o.ShippedAt, o.DeliveredAt
    );
}

