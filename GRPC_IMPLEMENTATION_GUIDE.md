# 🛠️ gRPC Implementation Guide

## 📋 Overview

Hướng dẫn chi tiết triển khai gRPC cho Order → Inventory và Order → Discount.

---

## 🗂️ Project Structure

```
dotnet-ecommerce-microservice/
├── shared/
│   ├── Shared.Protos/                    # Proto definitions
│   │   ├── Shared.Protos.csproj
│   │   ├── common/
│   │   │   └── types.proto
│   │   ├── inventory/
│   │   │   └── v1/
│   │   │       └── inventory.proto
│   │   └── discount/
│   │       └── v1/
│   │           └── discount.proto
│   │
│   ├── Shared.Messaging/                 # RabbitMQ (existing)
│   └── Shared.Caching/                   # Redis (existing)
│
├── service/
│   ├── inventory/
│   │   └── Inventory.Api/
│   │       ├── Services/
│   │       │   └── InventoryGrpcService.cs    # gRPC implementation
│   │       └── Program.cs                      # Register gRPC
│   │
│   ├── discount/
│   │   └── Discount.Api/
│   │       ├── Services/
│   │       │   └── DiscountGrpcService.cs     # gRPC implementation
│   │       └── Program.cs                      # Register gRPC
│   │
│   └── order/
│       └── Order.Api/
│           ├── Clients/
│           │   ├── InventoryGrpcClient.cs     # gRPC client
│           │   └── DiscountGrpcClient.cs      # gRPC client
│           └── Program.cs                      # Register gRPC clients
```

---

## 📦 Phase 1: Setup Shared.Protos Project

### **Step 1: Create Shared.Protos Project**

```bash
cd shared
mkdir Shared.Protos
cd Shared.Protos
dotnet new classlib -f net8.0
```

### **Step 2: Update Shared.Protos.csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- gRPC and Protobuf packages -->
    <PackageReference Include="Grpc.Tools" Version="2.60.0" PrivateAssets="All" />
    <PackageReference Include="Grpc.Core" Version="2.60.0" />
    <PackageReference Include="Grpc.Net.Client" Version="2.60.0" />
    <PackageReference Include="Google.Protobuf" Version="3.25.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Proto files -->
    <Protobuf Include="common\types.proto" />
    <Protobuf Include="inventory\v1\inventory.proto" GrpcServices="Both" />
    <Protobuf Include="discount\v1\discount.proto" GrpcServices="Both" />
  </ItemGroup>

</Project>
```

### **Step 3: Create Proto Files**

See `GRPC_PROTOBUF_CONTRACTS.md` for complete proto definitions.

```bash
# Create directory structure
mkdir -p common inventory/v1 discount/v1

# Copy proto files from documentation
# common/types.proto
# inventory/v1/inventory.proto
# discount/v1/discount.proto
```

### **Step 4: Build to Generate Code**

```bash
dotnet build
```

Generated files will be in: `obj/Debug/net8.0/`

---

## 🔧 Phase 2: Implement Inventory gRPC Service

### **Step 1: Add Reference to Shared.Protos**

```bash
cd service/inventory/Inventory.Api
dotnet add reference ../../../shared/Shared.Protos/Shared.Protos.csproj
```

### **Step 2: Add gRPC Packages**

```bash
dotnet add package Grpc.AspNetCore
dotnet add package Grpc.AspNetCore.Server.Reflection
```

### **Step 3: Create InventoryGrpcService.cs**

**Location:** `service/inventory/Inventory.Api/Services/InventoryGrpcService.cs`

```csharp
using Grpc.Core;
using Shared.Protos.Inventory.V1;
using Inventory.Application.Interfaces;

namespace Inventory.Api.Services;

public class InventoryGrpcService : InventoryService.InventoryServiceBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryGrpcService> _logger;

    public InventoryGrpcService(
        IInventoryService inventoryService,
        ILogger<InventoryGrpcService> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    // CheckStock
    public override async Task<CheckStockResponse> CheckStock(
        CheckStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "CheckStock called for ProductId: {ProductId}, Quantity: {Quantity}",
            request.ProductId, request.Quantity);

        try
        {
            var productId = Guid.Parse(request.ProductId);
            var variantId = string.IsNullOrEmpty(request.VariantId) 
                ? null 
                : Guid.Parse(request.VariantId);

            var result = await _inventoryService.CheckStockAsync(
                productId, 
                variantId, 
                request.Quantity);

            return new CheckStockResponse
            {
                Available = result.Available,
                AvailableQuantity = result.AvailableQuantity,
                WarehouseId = result.WarehouseId ?? "",
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    // CheckStockBatch
    public override async Task<CheckStockBatchResponse> CheckStockBatch(
        CheckStockBatchRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("CheckStockBatch called for {Count} items", request.Items.Count);

        try
        {
            var items = request.Items.Select(i => new
            {
                ProductId = Guid.Parse(i.ProductId),
                VariantId = string.IsNullOrEmpty(i.VariantId) ? null : Guid.Parse(i.VariantId),
                Quantity = i.Quantity
            }).ToList();

            var results = await _inventoryService.CheckStockBatchAsync(items);

            var response = new CheckStockBatchResponse
            {
                AllAvailable = results.All(r => r.IsAvailable)
            };

            foreach (var result in results)
            {
                response.Items.Add(new StockItemAvailability
                {
                    ProductId = result.ProductId.ToString(),
                    VariantId = result.VariantId?.ToString() ?? "",
                    RequestedQuantity = result.RequestedQuantity,
                    AvailableQuantity = result.AvailableQuantity,
                    IsAvailable = result.IsAvailable,
                    WarehouseId = result.WarehouseId ?? ""
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking stock batch");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    // ReserveStock
    public override async Task<ReserveStockResponse> ReserveStock(
        ReserveStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation(
            "ReserveStock called for OrderId: {OrderId}", 
            request.OrderId);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            var items = request.Items.Select(i => new
            {
                ProductId = Guid.Parse(i.ProductId),
                VariantId = string.IsNullOrEmpty(i.VariantId) ? null : Guid.Parse(i.VariantId),
                Quantity = i.Quantity
            }).ToList();

            var result = await _inventoryService.ReserveStockAsync(
                orderId,
                request.OrderNumber,
                items,
                request.ExpirationMinutes);

            return new ReserveStockResponse
            {
                Success = result.Success,
                ReservationIds = { result.ReservationIds },
                ExpiresAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    result.ExpiresAt.ToUniversalTime()),
                Message = result.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    // CommitStock
    public override async Task<CommitStockResponse> CommitStock(
        CommitStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("CommitStock called for OrderId: {OrderId}", request.OrderId);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            var reservationIds = request.ReservationIds.Select(Guid.Parse).ToList();

            var result = await _inventoryService.CommitStockAsync(orderId, reservationIds);

            return new CommitStockResponse
            {
                Success = result.Success,
                Message = result.Message,
                ItemsCommitted = result.ItemsCommitted
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error committing stock");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    // ReleaseStock
    public override async Task<ReleaseStockResponse> ReleaseStock(
        ReleaseStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("ReleaseStock called for OrderId: {OrderId}", request.OrderId);

        try
        {
            var orderId = Guid.Parse(request.OrderId);
            var reservationIds = request.ReservationIds.Select(Guid.Parse).ToList();

            var result = await _inventoryService.ReleaseStockAsync(
                orderId, 
                reservationIds, 
                request.Reason);

            return new ReleaseStockResponse
            {
                Success = result.Success,
                Message = result.Message,
                ItemsReleased = result.ItemsReleased
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing stock");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    // GetStock
    public override async Task<GetStockResponse> GetStock(
        GetStockRequest request,
        ServerCallContext context)
    {
        _logger.LogInformation("GetStock called for ProductId: {ProductId}", request.ProductId);

        try
        {
            var productId = Guid.Parse(request.ProductId);
            var variantId = string.IsNullOrEmpty(request.VariantId) 
                ? null 
                : Guid.Parse(request.VariantId);

            var result = await _inventoryService.GetStockAsync(productId, variantId);

            var response = new GetStockResponse
            {
                ProductId = result.ProductId.ToString(),
                VariantId = result.VariantId?.ToString() ?? "",
                TotalQuantity = result.TotalQuantity,
                ReservedQuantity = result.ReservedQuantity,
                AvailableQuantity = result.AvailableQuantity,
                InStock = result.InStock,
                LowStock = result.LowStock,
                LowStockThreshold = result.LowStockThreshold
            };

            foreach (var warehouse in result.Warehouses)
            {
                response.Warehouses.Add(new WarehouseStock
                {
                    WarehouseId = warehouse.WarehouseId,
                    WarehouseName = warehouse.WarehouseName,
                    Quantity = warehouse.Quantity,
                    Available = warehouse.Available,
                    Location = warehouse.Location ?? ""
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock");
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }
}
```

### **Step 4: Register gRPC in Program.cs**

**Location:** `service/inventory/Inventory.Api/Program.cs`

```csharp
using Inventory.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add gRPC
builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true; // For development
    options.MaxReceiveMessageSize = 2 * 1024 * 1024; // 2 MB
    options.MaxSendMessageSize = 5 * 1024 * 1024;    // 5 MB
});

// Add gRPC reflection (for development/testing)
builder.Services.AddGrpcReflection();

// Existing services...
builder.Services.AddControllers();
// ... other configurations

var app = builder.Build();

// Map gRPC services
app.MapGrpcService<InventoryGrpcService>();

// Add gRPC reflection endpoint (development only)
if (app.Environment.IsDevelopment())
{
    app.MapGrpcReflectionService();
}

// Existing middleware...
app.MapControllers();

app.Run();
```

### **Step 5: Update appsettings.json**

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5005",
        "Protocols": "Http1"
      },
      "Grpc": {
        "Url": "http://localhost:5015",
        "Protocols": "Http2"
      }
    }
  }
}
```

---

## 🎁 Phase 3: Implement Discount gRPC Service

### **Similar steps as Inventory Service:**

1. Add reference to Shared.Protos
2. Add gRPC packages
3. Create `DiscountGrpcService.cs`
4. Register in Program.cs
5. Update appsettings.json

**Key methods to implement:**
- ValidateDiscount
- ApplyDiscount
- RecordUsage
- RollbackUsage
- GetActiveDiscounts

---

## 🔌 Phase 4: Implement gRPC Clients in Order Service

### **Step 1: Add References**

```bash
cd service/order/Order.Api
dotnet add reference ../../../shared/Shared.Protos/Shared.Protos.csproj
dotnet add package Grpc.Net.Client
dotnet add package Grpc.Net.ClientFactory
```

### **Step 2: Create InventoryGrpcClient.cs**

**Location:** `service/order/Order.Api/Clients/InventoryGrpcClient.cs`

```csharp
using Grpc.Net.Client;
using Shared.Protos.Inventory.V1;
using Order.Application.Interfaces;

namespace Order.Api.Clients;

public class InventoryGrpcClient : IInventoryClient
{
    private readonly InventoryService.InventoryServiceClient _client;
    private readonly ILogger<InventoryGrpcClient> _logger;

    public InventoryGrpcClient(
        InventoryService.InventoryServiceClient client,
        ILogger<InventoryGrpcClient> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> CheckStockAsync(Guid productId, int quantity)
    {
        _logger.LogInformation("Calling Inventory.CheckStock via gRPC");

        try
        {
            var request = new CheckStockRequest
            {
                ProductId = productId.ToString(),
                Quantity = quantity
            };

            var response = await _client.CheckStockAsync(request);

            _logger.LogInformation(
                "Stock check result: Available={Available}, Quantity={Quantity}",
                response.Available, response.AvailableQuantity);

            return response.Available;
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC call failed: {Status}", ex.Status);
            throw;
        }
    }

    public async Task<List<string>> ReserveStockAsync(
        Guid orderId,
        string orderNumber,
        List<OrderItemDto> items)
    {
        _logger.LogInformation("Calling Inventory.ReserveStock via gRPC");

        try
        {
            var request = new ReserveStockRequest
            {
                OrderId = orderId.ToString(),
                OrderNumber = orderNumber,
                ExpirationMinutes = 30
            };

            foreach (var item in items)
            {
                request.Items.Add(new StockItem
                {
                    ProductId = item.ProductId.ToString(),
                    VariantId = item.VariantId?.ToString() ?? "",
                    Quantity = item.Quantity
                });
            }

            var response = await _client.ReserveStockAsync(request);

            if (!response.Success)
            {
                throw new InvalidOperationException(response.Message);
            }

            return response.ReservationIds.ToList();
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC call failed: {Status}", ex.Status);
            throw;
        }
    }

    public async Task CommitStockAsync(Guid orderId, List<string> reservationIds)
    {
        _logger.LogInformation("Calling Inventory.CommitStock via gRPC");

        try
        {
            var request = new CommitStockRequest
            {
                OrderId = orderId.ToString()
            };
            request.ReservationIds.AddRange(reservationIds);

            var response = await _client.CommitStockAsync(request);

            if (!response.Success)
            {
                throw new InvalidOperationException(response.Message);
            }
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC call failed: {Status}", ex.Status);
            throw;
        }
    }

    public async Task ReleaseStockAsync(
        Guid orderId, 
        List<string> reservationIds, 
        string reason)
    {
        _logger.LogInformation("Calling Inventory.ReleaseStock via gRPC");

        try
        {
            var request = new ReleaseStockRequest
            {
                OrderId = orderId.ToString(),
                Reason = reason
            };
            request.ReservationIds.AddRange(reservationIds);

            var response = await _client.ReleaseStockAsync(request);

            if (!response.Success)
            {
                _logger.LogWarning("Failed to release stock: {Message}", response.Message);
            }
        }
        catch (Grpc.Core.RpcException ex)
        {
            _logger.LogError(ex, "gRPC call failed: {Status}", ex.Status);
            // Don't throw - release is best effort
        }
    }
}
```

### **Step 3: Create DiscountGrpcClient.cs**

Similar structure for Discount Service client.

### **Step 4: Register gRPC Clients in Program.cs**

```csharp
using Grpc.Net.Client;
using Shared.Protos.Inventory.V1;
using Shared.Protos.Discount.V1;
using Order.Api.Clients;

var builder = WebApplication.CreateBuilder(args);

// Register gRPC clients
builder.Services.AddGrpcClient<InventoryService.InventoryServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["GrpcServices:Inventory"] 
        ?? "http://localhost:5015");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator // Dev only!
    };
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

builder.Services.AddGrpcClient<DiscountService.DiscountServiceClient>(options =>
{
    options.Address = new Uri(builder.Configuration["GrpcServices:Discount"] 
        ?? "http://localhost:5016");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetTimeoutPolicy());

// Register wrapper clients
builder.Services.AddScoped<IInventoryClient, InventoryGrpcClient>();
builder.Services.AddScoped<IDiscountClient, DiscountGrpcClient>();

// Retry policy
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

// Timeout policy
static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
}
```

### **Step 5: Update appsettings.json**

```json
{
  "GrpcServices": {
    "Inventory": "http://localhost:5015",
    "Discount": "http://localhost:5016"
  }
}
```

---

## 🧪 Phase 5: Testing

### **Test with grpcurl**

```bash
# Install grpcurl
go install github.com/fullstorydev/grpcurl/cmd/grpcurl@latest

# List services
grpcurl -plaintext localhost:5015 list

# Describe service
grpcurl -plaintext localhost:5015 describe inventory.v1.InventoryService

# Call CheckStock
grpcurl -plaintext -d '{
  "product_id": "550e8400-e29b-41d4-a716-446655440000",
  "quantity": 10
}' localhost:5015 inventory.v1.InventoryService/CheckStock
```

### **Test with Postman**

Postman now supports gRPC!
1. New → gRPC Request
2. Enter URL: `localhost:5015`
3. Import proto files
4. Select method and test

### **Integration Test**

```csharp
[Fact]
public async Task CheckoutFlow_WithGrpc_ShouldSucceed()
{
    // Arrange
    var orderId = Guid.NewGuid();
    var items = new List<OrderItemDto>
    {
        new() { ProductId = Guid.NewGuid(), Quantity = 2 }
    };

    // Act - Call via gRPC
    var discountValid = await _discountClient.ValidateDiscountAsync("SALE10", userId, 500000);
    var stockAvailable = await _inventoryClient.CheckStockAsync(items[0].ProductId, 2);
    var reservationIds = await _inventoryClient.ReserveStockAsync(orderId, "ORD-001", items);

    // Assert
    Assert.True(discountValid);
    Assert.True(stockAvailable);
    Assert.NotEmpty(reservationIds);
}
```

---

## 📊 Phase 6: Monitoring

### **Add Metrics**

```csharp
using System.Diagnostics;
using System.Diagnostics.Metrics;

public class GrpcMetrics
{
    private static readonly Meter _meter = new("Order.Api.Grpc");
    private static readonly Counter<long> _callsCounter = 
        _meter.CreateCounter<long>("grpc.client.calls");
    private static readonly Histogram<double> _durationHistogram = 
        _meter.CreateHistogram<double>("grpc.client.duration");

    public static void RecordCall(string method, string status, double duration)
    {
        _callsCounter.Add(1, 
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status", status));
        
        _durationHistogram.Record(duration,
            new KeyValuePair<string, object?>("method", method));
    }
}
```

### **Add Logging Interceptor**

```csharp
using Grpc.Core;
using Grpc.Core.Interceptors;

public class LoggingInterceptor : Interceptor
{
    private readonly ILogger<LoggingInterceptor> _logger;

    public LoggingInterceptor(ILogger<LoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        
        _logger.LogInformation("Starting gRPC call: {Method}", context.Method.Name);

        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponse(call.ResponseAsync, stopwatch, context.Method.Name),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponse<TResponse>(
        Task<TResponse> responseTask,
        Stopwatch stopwatch,
        string methodName)
    {
        try
        {
            var response = await responseTask;
            stopwatch.Stop();
            
            _logger.LogInformation(
                "gRPC call completed: {Method} in {Duration}ms",
                methodName, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (RpcException ex)
        {
            stopwatch.Stop();
            
            _logger.LogError(
                "gRPC call failed: {Method} in {Duration}ms - {Status}: {Message}",
                methodName, stopwatch.ElapsedMilliseconds, ex.Status.StatusCode, ex.Message);
            
            throw;
        }
    }
}
```

---

## 🚀 Phase 7: Deployment

### **Docker Support**

**Dockerfile for Inventory Service:**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5005
EXPOSE 5015

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["shared/Shared.Protos/", "shared/Shared.Protos/"]
COPY ["service/inventory/Inventory.Api/", "service/inventory/Inventory.Api/"]
RUN dotnet restore "service/inventory/Inventory.Api/Inventory.Api.csproj"
RUN dotnet build "service/inventory/Inventory.Api/Inventory.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "service/inventory/Inventory.Api/Inventory.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
```

### **Docker Compose**

```yaml
version: '3.8'

services:
  inventory-api:
    build:
      context: .
      dockerfile: service/inventory/Inventory.Api/Dockerfile
    ports:
      - "5005:5005"  # REST
      - "5015:5015"  # gRPC
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DBConnectParam=Host=postgres;Database=inventory_db;...

  discount-api:
    build:
      context: .
      dockerfile: service/discount/Discount.Api/Dockerfile
    ports:
      - "5006:5006"  # REST
      - "5016:5016"  # gRPC

  order-api:
    build:
      context: .
      dockerfile: service/order/Order.Api/Dockerfile
    ports:
      - "5003:5003"
    environment:
      - GrpcServices__Inventory=http://inventory-api:5015
      - GrpcServices__Discount=http://discount-api:5016
    depends_on:
      - inventory-api
      - discount-api
```

---

## ⚠️ Troubleshooting

### **Issue 1: "Call failed with gRPC error status. Status code: Unavailable"**

**Solution:**
- Check if gRPC service is running
- Verify port and URL
- Check firewall rules

### **Issue 2: "Call failed with gRPC error status. Status code: Unimplemented"**

**Solution:**
- Method not implemented on server
- Check proto file versions match
- Rebuild projects

### **Issue 3: HTTP/2 not working**

**Solution:**
```csharp
// Kestrel configuration
"Kestrel": {
  "EndpointDefaults": {
    "Protocols": "Http2"
  }
}
```

---

## 📚 Next Steps

1. ✅ Setup Shared.Protos project
2. ✅ Implement Inventory gRPC service
3. ✅ Implement Discount gRPC service
4. ✅ Implement gRPC clients in Order service
5. ✅ Test thoroughly
6. ✅ Add monitoring
7. ✅ Deploy and measure performance

---

**Performance Target:**
- Checkout flow: **500ms → 216ms** (2.3x faster)
- Throughput: **100 → 300 orders/sec** (3x improvement)

Good luck! 🚀

