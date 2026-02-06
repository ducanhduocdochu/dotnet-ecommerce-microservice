using Microsoft.Extensions.Logging;
using Order.Application.Clients;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Shared.Messaging.Events;
using Shared.Messaging.RabbitMQ;

namespace Order.Application.Services;

public class OrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IOrderStatusHistoryRepository _statusHistoryRepository;
    private readonly IOrderRefundRepository _refundRepository;
    private readonly IDiscountClient _discountClient;
    private readonly IInventoryClient _inventoryClient;
    private readonly IPaymentClient _paymentClient;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        ICartRepository cartRepository,
        IOrderStatusHistoryRepository statusHistoryRepository,
        IOrderRefundRepository refundRepository,
        IDiscountClient discountClient,
        IInventoryClient inventoryClient,
        IPaymentClient paymentClient,
        IEventPublisher eventPublisher,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _cartRepository = cartRepository;
        _statusHistoryRepository = statusHistoryRepository;
        _refundRepository = refundRepository;
        _discountClient = discountClient;
        _inventoryClient = inventoryClient;
        _paymentClient = paymentClient;
        _eventPublisher = eventPublisher;
        _logger = logger;
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

    // ============ Checkout Flow (HTTP Sync) ============
    public async Task<CheckoutResult> CheckoutAsync(Guid userId, CheckoutRequest request)
    {
        _logger.LogInformation("🛒 Starting checkout for user {UserId}", userId);

        // Step 1: Validate discount (if provided)
        decimal discountAmount = 0;
        Guid? discountId = null;
        string? discountCode = null;

        if (!string.IsNullOrEmpty(request.DiscountCode))
        {
            Console.WriteLine($"request.DiscountCode: {request.DiscountCode}");
            var discountItems = request.Items.Select(i => new ValidateDiscountItem(
                i.ProductId, i.CategoryId, i.Quantity, i.UnitPrice
            )).ToList();
            Console.WriteLine($"discountItems: {discountItems}");

            var validateResult = await _discountClient.ValidateAsync(new ValidateDiscountRequest(
                request.DiscountCode,
                request.Items.Sum(i => i.Quantity * i.UnitPrice),
                discountItems
            ));
            Console.WriteLine($"validateResult: {validateResult}");

            if (!validateResult.Valid)
            {
                _logger.LogWarning("❌ Discount validation failed: {Message}", validateResult.Message);
                return new CheckoutResult(false, null, null, validateResult.Message);
            }
            Console.WriteLine($"validateResult.Valid: {validateResult.Valid}");

            discountAmount = validateResult.DiscountAmount;
            discountCode = request.DiscountCode;
            _logger.LogInformation("✅ Discount validated: {Code}, amount: {Amount}", request.DiscountCode, discountAmount);
        }

        // Step 2: Check stock availability
        var stockCheckItems = request.Items.Select(i => new CheckStockItem(
            i.ProductId, i.VariantId, i.Quantity, null
        )).ToList();

        var stockResult = await _inventoryClient.CheckStockAsync(new CheckStockRequest(stockCheckItems));
        if (!stockResult.AllAvailable)
        {
            _logger.LogWarning("❌ Stock check failed: {Message}", stockResult.Message);
            return new CheckoutResult(false, null, null, stockResult.Message ?? "Một số sản phẩm đã hết hàng");
        }

        // Step 3: Create Order (PENDING status)
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

        if (!string.IsNullOrEmpty(discountCode))
        {
            order.SetDiscount(discountId, discountCode, discountAmount);
        }

        await _orderRepository.AddAsync(order);
        await _orderRepository.SaveChangesAsync();

        // Step 4: Reserve inventory
        var reserveItems = request.Items.Select(i => new ReserveStockItem(
            i.ProductId, i.VariantId, i.Quantity, null
        )).ToList();

        var reserveResult = await _inventoryClient.ReserveAsync(new ReserveStockRequest(
            order.Id,
            order.OrderNumber,
            userId,
            reserveItems,
            15 // 15 minutes expiration
        ));

        if (!reserveResult.Success)
        {
            _logger.LogWarning("❌ Stock reservation failed: {Message}", reserveResult.Message);
            // Rollback: Delete order
            await _orderRepository.DeleteAsync(order);
            await _orderRepository.SaveChangesAsync();
            return new CheckoutResult(false, null, null, reserveResult.Message ?? "Không thể đặt hàng, vui lòng thử lại");
        }

        // Step 5: Add order items with reservation info
        decimal subtotal = 0;
        var orderItems = new List<OrderItem>();
        for (int i = 0; i < request.Items.Count; i++)
        {
            var itemReq = request.Items[i];
            var reservation = reserveResult.Reservations.FirstOrDefault(r => 
                r.ProductId == itemReq.ProductId && r.VariantId == itemReq.VariantId);

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
            
            // Store reservation info
            item.SetReservationInfo(reservation?.ReservationId, reservation?.WarehouseId);
            
            orderItems.Add(item);
            subtotal += item.TotalPrice;
        }
        await _orderItemRepository.AddRangeAsync(orderItems);

        // Step 6: Apply discount (lock usage)
        if (!string.IsNullOrEmpty(discountCode))
        {
            var discountApplyItems = request.Items.Select(i => new ValidateDiscountItem(
                i.ProductId, i.CategoryId, i.Quantity, i.UnitPrice
            )).ToList();

            var applyResult = await _discountClient.ApplyAsync(new ApplyDiscountRequest(
                discountCode,
                order.Id,
                order.OrderNumber,
                subtotal,
                discountApplyItems
            ));

            if (applyResult.Success)
            {
                discountId = applyResult.DiscountId;
                order.SetDiscount(discountId, discountCode, applyResult.DiscountAmount);
            }
        }

        // Step 7: Calculate totals
        decimal shippingFee = CalculateShippingFee(request.ShippingMethod);
        order.CalculateTotals(subtotal, shippingFee, discountAmount, 0);

        // Add initial status history
        var history = new OrderStatusHistory(order.Id, "PENDING", null, "Order created, awaiting payment");
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();

        // Step 8: Create payment
        var paymentResult = await _paymentClient.CreatePaymentAsync(new CreatePaymentRequest(
            order.Id,
            order.OrderNumber,
            userId,
            order.TotalAmount,
            "VND",
            request.PaymentGateway ?? "VNPAY",
            request.PaymentMethod,
            $"Thanh toán đơn hàng {order.OrderNumber}",
            request.ReturnUrl ?? "",
            request.CancelUrl ?? "",
            new PaymentOrderInfo(
                request.Shipping.Name,
                request.UserEmail,
                request.UserPhone,
                orderItems.Select(i => new PaymentOrderItem(i.ProductName, i.Quantity, i.UnitPrice)).ToList()
            )
        ));

        if (!paymentResult.Success)
        {
            _logger.LogWarning("⚠️ Payment creation failed, but order created: {OrderId}", order.Id);
            // Order is still valid, just return without payment URL
        }
        else
        {
            order.SetPaymentTransaction(paymentResult.TransactionId);
            await _orderRepository.SaveChangesAsync();
        }

        // Clear cart after successful order
        await _cartRepository.ClearByUserIdAsync(userId);
        await _cartRepository.SaveChangesAsync();

        _logger.LogInformation("✅ Checkout completed for order: {OrderNumber}", order.OrderNumber);

        return new CheckoutResult(
            true,
            new OrderCreatedResponse(
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
                paymentResult.PaymentUrl
            ),
            paymentResult.PaymentUrl,
            "Đặt hàng thành công"
        );
    }

    // ============ Payment Callback Handler ============
    public async Task<bool> HandlePaymentSuccessAsync(Guid orderId, Guid transactionId)
    {
        _logger.LogInformation("💳 Handling payment success for order: {OrderId}", orderId);

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        if (order == null) return false;

        var previousStatus = order.Status;
        order.ConfirmPayment();

        var history = new OrderStatusHistory(orderId, "CONFIRMED", previousStatus, "Payment successful");
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();

        // Publish OrderConfirmedEvent
        await PublishOrderConfirmedEventAsync(order);

        _logger.LogInformation("✅ Order confirmed: {OrderNumber}", order.OrderNumber);
        return true;
    }

    public async Task<bool> HandlePaymentFailedAsync(Guid orderId, string reason)
    {
        _logger.LogInformation("❌ Handling payment failure for order: {OrderId}", orderId);

        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        if (order == null) return false;

        var previousStatus = order.Status;
        order.FailPayment();

        var history = new OrderStatusHistory(orderId, "PAYMENT_FAILED", previousStatus, reason);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();

        // Release inventory reservations
        var reservationIds = order.Items
            .Where(i => i.ReservationId.HasValue)
            .Select(i => i.ReservationId!.Value)
            .ToList();

        if (reservationIds.Any())
        {
            await _inventoryClient.ReleaseReservationAsync(new ReleaseReservationRequest(orderId, reservationIds));
        }

        _logger.LogInformation("⚠️ Order payment failed: {OrderNumber}", order.OrderNumber);
        return true;
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
        decimal discountAmount = 0;
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
            null
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
        var order = await _orderRepository.GetByIdWithDetailsAsync(orderId);
        if (order == null || order.UserId != userId || !order.CanCancel()) return false;

        var previousStatus = order.Status;
        var stockCommitted = order.Status == "CONFIRMED" || order.Status == "PROCESSING";
        
        order.Cancel(reason);
        
        var history = new OrderStatusHistory(orderId, "CANCELLED", previousStatus, reason, userId);
        await _statusHistoryRepository.AddAsync(history);
        await _orderRepository.SaveChangesAsync();

        // Publish OrderCancelledEvent
        await PublishOrderCancelledEventAsync(order, reason, "Customer", stockCommitted);

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

    // Sync user info (for RabbitMQ consumer)
    public async Task SyncUserInfoAsync(Guid userId, string? fullName, string? email, string? phone)
    {
        var orders = await _orderRepository.GetByUserIdAsync(userId, 1, int.MaxValue, null);
        foreach (var order in orders)
        {
            order.UpdateUserInfo(fullName, email, phone);
        }
        await _orderRepository.SaveChangesAsync();

        var orderItems = await _orderItemRepository.GetBySellerIdAsync(userId);
        foreach (var item in orderItems)
        {
            item.UpdateSellerInfo(fullName);
        }
        await _orderItemRepository.SaveChangesAsync();
    }

    // ============ Event Publishing ============
    private async Task PublishOrderConfirmedEventAsync(OrderEntity order)
    {
        var orderEvent = new OrderConfirmedEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            UserName = order.UserName,
            UserEmail = order.UserEmail,
            UserPhone = order.UserPhone,
            Items = order.Items.Select(i => new OrderConfirmedItem
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                ProductName = i.ProductName,
                Sku = i.ProductSku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                SellerId = i.SellerId,
                SellerName = i.SellerName,
                ReservationId = i.ReservationId,
                WarehouseId = i.WarehouseId
            }).ToList(),
            DiscountId = order.DiscountId,
            DiscountCode = order.DiscountCode,
            DiscountAmount = order.DiscountAmount,
            SubTotal = order.Subtotal,
            ShippingFee = order.ShippingFee,
            TotalAmount = order.TotalAmount,
            ConfirmedAt = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(
            EventConstants.OrderExchange,
            EventConstants.OrderConfirmed,
            orderEvent
        );

        _logger.LogInformation("📤 Published OrderConfirmedEvent for order: {OrderNumber}", order.OrderNumber);
    }

    private async Task PublishOrderCancelledEventAsync(OrderEntity order, string reason, string cancelledBy, bool stockCommitted)
    {
        var orderEvent = new OrderCancelledEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            UserName = order.UserName,
            UserEmail = order.UserEmail,
            Reason = reason,
            CancelledBy = cancelledBy,
            Items = order.Items.Select(i => new OrderCancelledItem
            {
                ProductId = i.ProductId,
                VariantId = i.VariantId,
                Quantity = i.Quantity,
                ReservationId = i.ReservationId,
                WarehouseId = i.WarehouseId,
                StockCommitted = stockCommitted
            }).ToList(),
            DiscountId = order.DiscountId,
            PaymentTransactionId = order.PaymentTransactionId,
            RefundAmount = order.TotalAmount,
            RequiresRefund = order.PaymentStatus == "PAID",
            CancelledAt = DateTime.UtcNow
        };

        await _eventPublisher.PublishAsync(
            EventConstants.OrderExchange,
            EventConstants.OrderCancelled,
            orderEvent
        );

        _logger.LogInformation("📤 Published OrderCancelledEvent for order: {OrderNumber}", order.OrderNumber);
    }

    // Helpers
    private static decimal CalculateShippingFee(string? method) => method switch
    {
        "EXPRESS" => 50000,
        "SAME_DAY" => 100000,
        _ => 25000
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
