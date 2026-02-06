using Inventory.Application.DTOs;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;

namespace Inventory.Application.Services;

public class InventoryService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly IStockReservationRepository _reservationRepository;

    public InventoryService(
        IWarehouseRepository warehouseRepository,
        IInventoryRepository inventoryRepository,
        IInventoryTransactionRepository transactionRepository,
        IStockReservationRepository reservationRepository)
    {
        _warehouseRepository = warehouseRepository;
        _inventoryRepository = inventoryRepository;
        _transactionRepository = transactionRepository;
        _reservationRepository = reservationRepository;
    }

    // ============ Warehouse Methods ============
    public async Task<List<WarehouseResponse>> GetWarehousesAsync()
    {
        var warehouses = await _warehouseRepository.GetAllAsync();
        return warehouses.Select(w => new WarehouseResponse(
            w.Id, w.Code, w.Name, w.Address, w.City, w.Phone, w.ManagerName, w.IsActive, w.IsDefault, w.CreatedAt
        )).ToList();
    }

    public async Task<WarehouseResponse?> GetWarehouseByIdAsync(Guid id)
    {
        var w = await _warehouseRepository.GetByIdAsync(id);
        if (w == null) return null;
        return new WarehouseResponse(w.Id, w.Code, w.Name, w.Address, w.City, w.Phone, w.ManagerName, w.IsActive, w.IsDefault, w.CreatedAt);
    }

    public async Task<WarehouseResponse> CreateWarehouseAsync(CreateWarehouseRequest request)
    {
        var warehouse = new Warehouse(request.Code, request.Name, request.City, request.IsDefault);
        await _warehouseRepository.AddAsync(warehouse);
        await _warehouseRepository.SaveChangesAsync();
        return new WarehouseResponse(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.City, warehouse.Phone, warehouse.ManagerName, warehouse.IsActive, warehouse.IsDefault, warehouse.CreatedAt);
    }

    public async Task<WarehouseResponse?> UpdateWarehouseAsync(Guid id, UpdateWarehouseRequest request)
    {
        var warehouse = await _warehouseRepository.GetByIdAsync(id);
        if (warehouse == null) return null;
        warehouse.Update(request.Name, request.Address, request.City, request.Phone, request.ManagerName, request.IsActive, request.IsDefault);
        await _warehouseRepository.SaveChangesAsync();
        return new WarehouseResponse(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.Address, warehouse.City, warehouse.Phone, warehouse.ManagerName, warehouse.IsActive, warehouse.IsDefault, warehouse.CreatedAt);
    }

    // ============ Inventory Methods ============
    public async Task<PagedResponse<InventoryResponse>> GetInventoryAsync(Guid? warehouseId, bool? lowStock, int page, int pageSize)
    {
        var items = await _inventoryRepository.GetAllAsync(warehouseId, lowStock, page, pageSize);
        var total = await _inventoryRepository.GetCountAsync(warehouseId, lowStock);
        return new PagedResponse<InventoryResponse>(
            items.Select(i => MapToResponse(i)).ToList(),
            total, page, pageSize
        );
    }

    public async Task<InventoryDetailResponse?> GetInventoryByIdAsync(Guid id)
    {
        var item = await _inventoryRepository.GetByIdWithDetailsAsync(id);
        if (item == null) return null;
        
        var transactions = await _transactionRepository.GetByInventoryIdAsync(id, 1, 10);
        return new InventoryDetailResponse(
            item.Id, item.ProductId, item.VariantId, item.Sku,
            new WarehouseResponse(item.Warehouse!.Id, item.Warehouse.Code, item.Warehouse.Name, item.Warehouse.Address, item.Warehouse.City, item.Warehouse.Phone, item.Warehouse.ManagerName, item.Warehouse.IsActive, item.Warehouse.IsDefault, item.Warehouse.CreatedAt),
            item.Quantity, item.ReservedQuantity, item.AvailableQuantity,
            item.LowStockThreshold, item.ReorderPoint, item.ReorderQuantity, item.IsLowStock, item.Location,
            transactions.Select(t => new TransactionResponse(t.Id, t.Type, t.QuantityChange, t.QuantityBefore, t.QuantityAfter, t.ReferenceType, t.ReferenceCode, t.Reason, t.CreatedByName, t.CreatedAt)).ToList()
        );
    }

    public async Task<InventoryResponse> CreateInventoryAsync(CreateInventoryRequest request)
    {
        var existing = await _inventoryRepository.GetByProductAndWarehouseAsync(request.ProductId, request.VariantId, request.WarehouseId);
        if (existing != null)
            throw new InvalidOperationException("Inventory already exists for this product in this warehouse");

        var item = new InventoryItem(request.ProductId, request.WarehouseId, request.Quantity, request.VariantId, request.Sku, request.Location);
        item.UpdateThresholds(request.LowStockThreshold, null, null);
        await _inventoryRepository.AddAsync(item);
        await _inventoryRepository.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task<InventoryResponse?> UpdateInventoryAsync(Guid id, UpdateInventoryRequest request, Guid? userId = null, string? userName = null)
    {
        var item = await _inventoryRepository.GetByIdAsync(id);
        if (item == null) return null;

        int quantityBefore = item.Quantity;
        
        if (request.Quantity.HasValue && request.Quantity.Value != item.Quantity)
        {
            item.SetQuantity(request.Quantity.Value);
            await CreateTransaction(item.Id, "ADJUST", request.Quantity.Value - quantityBefore, quantityBefore, request.Quantity.Value, "ADJUSTMENT", null, null, request.Reason, null, userId, userName);
        }

        item.UpdateThresholds(request.LowStockThreshold, request.ReorderPoint, request.ReorderQuantity);
        if (request.Location != null) item.UpdateLocation(request.Location);
        
        await _inventoryRepository.SaveChangesAsync();
        return MapToResponse(item);
    }

    public async Task<InventoryResponse?> AdjustInventoryAsync(Guid id, AdjustInventoryRequest request, Guid? userId = null, string? userName = null)
    {
        var item = await _inventoryRepository.GetByIdAsync(id);
        if (item == null) return null;

        int quantityBefore = item.Quantity;
        item.AddQuantity(request.Adjustment);
        
        await CreateTransaction(item.Id, "ADJUST", request.Adjustment, quantityBefore, item.Quantity, "ADJUSTMENT", null, null, request.Reason, null, userId, userName);
        await _inventoryRepository.SaveChangesAsync();
        return MapToResponse(item);
    }

    // ============ Stock Check Methods ============
    public async Task<StockCheckResponse> CheckStockAsync(CheckStockRequest request)
    {
        var results = new List<StockCheckItemResponse>();
        bool allAvailable = true;

        foreach (var item in request.Items)
        {
            var inventories = await _inventoryRepository.GetByProductIdAsync(item.ProductId, item.VariantId);
            int totalAvailable = inventories.Sum(i => i.AvailableQuantity);
            bool isAvailable = totalAvailable >= item.Quantity;
            if (!isAvailable) allAvailable = false;
            results.Add(new StockCheckItemResponse(item.ProductId, item.VariantId, item.Quantity, totalAvailable, isAvailable));
        }

        return new StockCheckResponse(allAvailable, results);
    }

    public async Task<ProductStockResponse?> GetProductStockAsync(Guid productId, Guid? variantId = null)
    {
        var inventories = await _inventoryRepository.GetByProductIdAsync(productId, variantId);
        if (!inventories.Any()) return null;

        return new ProductStockResponse(
            productId, variantId,
            inventories.Sum(i => i.Quantity),
            inventories.Sum(i => i.ReservedQuantity),
            inventories.Sum(i => i.AvailableQuantity),
            inventories.Select(i => new WarehouseStockResponse(i.WarehouseId, i.Warehouse?.Name ?? "", i.Quantity, i.AvailableQuantity)).ToList()
        );
    }

    // ============ Reservation Methods ============
    public async Task<ReserveStockResponse> ReserveStockAsync(ReserveStockRequest request)
    {
        var reservationIds = new List<Guid>();
        var defaultWarehouse = await _warehouseRepository.GetDefaultAsync();

        foreach (var item in request.Items)
        {
            // Find inventory with available stock
            var inventories = await _inventoryRepository.GetByProductIdAsync(item.ProductId, item.VariantId);
            var inventory = inventories.FirstOrDefault(i => i.AvailableQuantity >= item.Quantity);
            
            if (inventory == null)
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

            int quantityBefore = inventory.ReservedQuantity;
            inventory.Reserve(item.Quantity);

            var reservation = new StockReservation(inventory.Id, request.OrderId, item.OrderItemId, item.Quantity, request.OrderNumber, request.ExpirationMinutes);
            await _reservationRepository.AddAsync(reservation);
            reservationIds.Add(reservation.Id);

            await CreateTransaction(inventory.Id, "RESERVE", item.Quantity, quantityBefore, inventory.ReservedQuantity, "ORDER", request.OrderId, request.OrderNumber, "Order reservation", null, null, null);
        }

        await _inventoryRepository.SaveChangesAsync();
        await _reservationRepository.SaveChangesAsync();

        return new ReserveStockResponse(true, reservationIds, DateTime.UtcNow.AddMinutes(request.ExpirationMinutes));
    }

    public async Task<bool> CommitStockAsync(Guid orderId)
    {
        var reservations = await _reservationRepository.GetByOrderIdAsync(orderId);
        if (!reservations.Any()) return false;

        foreach (var reservation in reservations.Where(r => r.Status == "RESERVED"))
        {
            reservation.Commit();
        }

        await _reservationRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReleaseStockAsync(ReleaseStockRequest request)
    {
        var reservations = await _reservationRepository.GetByOrderIdAsync(request.OrderId);
        if (!reservations.Any()) return false;

        foreach (var reservation in reservations.Where(r => r.Status == "RESERVED"))
        {
            var inventory = await _inventoryRepository.GetByIdAsync(reservation.InventoryId);
            if (inventory != null)
            {
                int quantityBefore = inventory.ReservedQuantity;
                inventory.Release(reservation.Quantity);
                await CreateTransaction(inventory.Id, "RELEASE", -reservation.Quantity, quantityBefore, inventory.ReservedQuantity, "ORDER", request.OrderId, reservation.OrderNumber, request.Reason ?? "Order released", null, null, null);
            }
            reservation.Release();
        }

        await _inventoryRepository.SaveChangesAsync();
        await _reservationRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeductStockAsync(DeductStockRequest request)
    {
        foreach (var item in request.Items)
        {
            var inventories = await _inventoryRepository.GetByProductIdAsync(item.ProductId, item.VariantId);
            var inventory = inventories.FirstOrDefault(i => i.Quantity >= item.Quantity);
            
            if (inventory == null)
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");

            int quantityBefore = inventory.Quantity;
            inventory.Deduct(item.Quantity);
            await CreateTransaction(inventory.Id, "EXPORT", -item.Quantity, quantityBefore, inventory.Quantity, "ORDER", request.OrderId, request.OrderNumber, "Order shipped", null, null, null);
        }

        await _inventoryRepository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReturnStockAsync(ReturnStockRequest request)
    {
        var defaultWarehouse = await _warehouseRepository.GetDefaultAsync();
        if (defaultWarehouse == null) return false;

        foreach (var item in request.Items)
        {
            var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(item.ProductId, item.VariantId, defaultWarehouse.Id);
            if (inventory == null) continue;

            int quantityBefore = inventory.Quantity;
            inventory.AddQuantity(item.Quantity);
            await CreateTransaction(inventory.Id, "RETURN", item.Quantity, quantityBefore, inventory.Quantity, "ORDER", request.OrderId, request.OrderNumber, item.Reason ?? "Order returned", null, null, null);
        }

        await _inventoryRepository.SaveChangesAsync();
        return true;
    }

    public async Task<int> ReleaseExpiredReservationsAsync()
    {
        var expired = await _reservationRepository.GetExpiredAsync();
        int count = 0;

        foreach (var reservation in expired)
        {
            var inventory = await _inventoryRepository.GetByIdAsync(reservation.InventoryId);
            if (inventory != null)
            {
                inventory.Release(reservation.Quantity);
                await CreateTransaction(inventory.Id, "RELEASE", -reservation.Quantity, inventory.ReservedQuantity + reservation.Quantity, inventory.ReservedQuantity, "ORDER", reservation.OrderId, reservation.OrderNumber, "Reservation expired", null, null, null);
            }
            reservation.Expire();
            count++;
        }

        await _inventoryRepository.SaveChangesAsync();
        await _reservationRepository.SaveChangesAsync();
        return count;
    }

    // ============ Low Stock ============
    public async Task<List<LowStockAlertResponse>> GetLowStockAlertsAsync(Guid? warehouseId = null)
    {
        var items = await _inventoryRepository.GetLowStockAsync(warehouseId);
        return items.Select(i => new LowStockAlertResponse(
            i.ProductId, i.Sku, i.Warehouse?.Name,
            i.AvailableQuantity, i.LowStockThreshold, i.ReorderPoint, i.ReorderQuantity
        )).ToList();
    }

    // ============ Transfer ============
    public async Task<bool> TransferStockAsync(TransferStockRequest request, Guid? userId = null, string? userName = null)
    {
        foreach (var item in request.Items)
        {
            var fromInventory = await _inventoryRepository.GetByProductAndWarehouseAsync(item.ProductId, item.VariantId, request.FromWarehouseId);
            if (fromInventory == null || fromInventory.AvailableQuantity < item.Quantity)
                throw new InvalidOperationException($"Insufficient stock in source warehouse for product {item.ProductId}");

            var toInventory = await _inventoryRepository.GetByProductAndWarehouseAsync(item.ProductId, item.VariantId, request.ToWarehouseId);
            if (toInventory == null)
            {
                toInventory = new InventoryItem(item.ProductId, request.ToWarehouseId, 0, item.VariantId, fromInventory.Sku);
                await _inventoryRepository.AddAsync(toInventory);
                await _inventoryRepository.SaveChangesAsync();
            }

            int fromBefore = fromInventory.Quantity;
            int toBefore = toInventory.Quantity;
            
            fromInventory.SubtractQuantity(item.Quantity);
            toInventory.AddQuantity(item.Quantity);

            var fromTx = new InventoryTransaction(fromInventory.Id, "TRANSFER", -item.Quantity, fromBefore, fromInventory.Quantity, "TRANSFER", null, null, "Transfer out", request.Note, userId, userName);
            fromTx.SetTransferWarehouses(request.FromWarehouseId, request.ToWarehouseId);
            await _transactionRepository.AddAsync(fromTx);

            var toTx = new InventoryTransaction(toInventory.Id, "TRANSFER", item.Quantity, toBefore, toInventory.Quantity, "TRANSFER", null, null, "Transfer in", request.Note, userId, userName);
            toTx.SetTransferWarehouses(request.FromWarehouseId, request.ToWarehouseId);
            await _transactionRepository.AddAsync(toTx);
        }

        await _inventoryRepository.SaveChangesAsync();
        await _transactionRepository.SaveChangesAsync();
        return true;
    }

    // ============ Helpers ============
    private async Task CreateTransaction(Guid inventoryId, string type, int change, int before, int after, string? refType, Guid? refId, string? refCode, string? reason, string? note, Guid? userId, string? userName)
    {
        var tx = new InventoryTransaction(inventoryId, type, change, before, after, refType, refId, refCode, reason, note, userId, userName);
        await _transactionRepository.AddAsync(tx);
    }

    private InventoryResponse MapToResponse(InventoryItem i) => new(
        i.Id, i.ProductId, i.VariantId, i.Sku, i.WarehouseId, i.Warehouse?.Name,
        i.Quantity, i.ReservedQuantity, i.AvailableQuantity, i.LowStockThreshold, i.IsLowStock, i.Location, i.UpdatedAt
    );
}

