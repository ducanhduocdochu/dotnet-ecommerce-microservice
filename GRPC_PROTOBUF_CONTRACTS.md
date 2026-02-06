# 📦 gRPC Protobuf Contracts

## 📋 Overview

Protobuf contracts cho service-to-service communication.

**Location:** `shared/Shared.Protos/`

---

## 🗂️ Directory Structure

```
shared/
  Shared.Protos/
    ├── common/
    │   └── types.proto          # Common types
    ├── inventory/
    │   └── v1/
    │       └── inventory.proto  # Inventory service
    └── discount/
        └── v1/
            └── discount.proto   # Discount service
```

---

## 📄 Common Types

### **common/types.proto**

```protobuf
syntax = "proto3";

package common;

option csharp_namespace = "Shared.Protos.Common";

// Common UUID type
message Uuid {
  string value = 1;
}

// Timestamp
message Timestamp {
  int64 seconds = 1;
  int32 nanos = 2;
}

// Money type
message Money {
  int64 amount = 1;      // Amount in smallest unit (cents, đồng)
  string currency = 2;   // ISO 4217 currency code (VND, USD)
}

// Pagination request
message PaginationRequest {
  int32 page = 1;
  int32 page_size = 2;
}

// Pagination response
message PaginationResponse {
  int32 total = 1;
  int32 page = 2;
  int32 page_size = 3;
  int32 total_pages = 4;
}

// Generic success response
message SuccessResponse {
  bool success = 1;
  string message = 2;
}

// Error details
message ErrorDetails {
  string code = 1;
  string message = 2;
  map<string, string> metadata = 3;
}
```

---

## 📦 Inventory Service

### **inventory/v1/inventory.proto**

```protobuf
syntax = "proto3";

package inventory.v1;

import "google/protobuf/timestamp.proto";
import "common/types.proto";

option csharp_namespace = "Shared.Protos.Inventory.V1";

// ============================================
// Inventory Service
// ============================================
service InventoryService {
  // Check if stock is available for a single product
  rpc CheckStock(CheckStockRequest) returns (CheckStockResponse);

  // Check stock for multiple products (batch)
  rpc CheckStockBatch(CheckStockBatchRequest) returns (CheckStockBatchResponse);

  // Reserve stock for an order (creates reservation)
  rpc ReserveStock(ReserveStockRequest) returns (ReserveStockResponse);

  // Commit reserved stock (after payment success)
  rpc CommitStock(CommitStockRequest) returns (CommitStockResponse);

  // Release reserved stock (after order cancelled/payment failed)
  rpc ReleaseStock(ReleaseStockRequest) returns (ReleaseStockResponse);

  // Get stock information for display
  rpc GetStock(GetStockRequest) returns (GetStockResponse);
}

// ============================================
// Messages
// ============================================

// CheckStock - Single product
message CheckStockRequest {
  string product_id = 1;
  string variant_id = 2;      // Optional
  int32 quantity = 3;
  string warehouse_id = 4;    // Optional, default warehouse if empty
}

message CheckStockResponse {
  bool available = 1;
  int32 available_quantity = 2;
  string warehouse_id = 3;
  string message = 4;         // e.g., "In stock", "Out of stock", "Low stock"
}

// CheckStockBatch - Multiple products
message CheckStockBatchRequest {
  repeated StockItem items = 1;
}

message CheckStockBatchResponse {
  bool all_available = 1;
  repeated StockItemAvailability items = 2;
}

message StockItem {
  string product_id = 1;
  string variant_id = 2;
  int32 quantity = 3;
}

message StockItemAvailability {
  string product_id = 1;
  string variant_id = 2;
  int32 requested_quantity = 3;
  int32 available_quantity = 4;
  bool is_available = 5;
  string warehouse_id = 6;
}

// ReserveStock - Create reservation
message ReserveStockRequest {
  string order_id = 1;
  string order_number = 2;
  repeated StockItem items = 3;
  int32 expiration_minutes = 4;  // Default: 30 minutes
  string note = 5;
}

message ReserveStockResponse {
  bool success = 1;
  repeated string reservation_ids = 2;
  google.protobuf.Timestamp expires_at = 3;
  string message = 4;
}

// CommitStock - Confirm reservation (deduct from inventory)
message CommitStockRequest {
  string order_id = 1;
  repeated string reservation_ids = 2;
}

message CommitStockResponse {
  bool success = 1;
  string message = 2;
  int32 items_committed = 3;
}

// ReleaseStock - Cancel reservation
message ReleaseStockRequest {
  string order_id = 1;
  repeated string reservation_ids = 2;
  string reason = 3;  // ORDER_CANCELLED, PAYMENT_FAILED, EXPIRED
}

message ReleaseStockResponse {
  bool success = 1;
  string message = 2;
  int32 items_released = 3;
}

// GetStock - Get stock info for display
message GetStockRequest {
  string product_id = 1;
  string variant_id = 2;
  string warehouse_id = 3;  // Optional
}

message GetStockResponse {
  string product_id = 1;
  string variant_id = 2;
  int32 total_quantity = 3;
  int32 reserved_quantity = 4;
  int32 available_quantity = 5;
  bool in_stock = 6;
  bool low_stock = 7;
  int32 low_stock_threshold = 8;
  repeated WarehouseStock warehouses = 9;
}

message WarehouseStock {
  string warehouse_id = 1;
  string warehouse_name = 2;
  int32 quantity = 3;
  int32 available = 4;
  string location = 5;
}
```

---

## 🎁 Discount Service

### **discount/v1/discount.proto**

```protobuf
syntax = "proto3";

package discount.v1;

import "google/protobuf/timestamp.proto";
import "common/types.proto";

option csharp_namespace = "Shared.Protos.Discount.V1";

// ============================================
// Discount Service
// ============================================
service DiscountService {
  // Validate discount code
  rpc ValidateDiscount(ValidateDiscountRequest) returns (ValidateDiscountResponse);

  // Apply discount to order
  rpc ApplyDiscount(ApplyDiscountRequest) returns (ApplyDiscountResponse);

  // Record discount usage (internal)
  rpc RecordUsage(RecordUsageRequest) returns (RecordUsageResponse);

  // Rollback discount usage (when order cancelled)
  rpc RollbackUsage(RollbackUsageRequest) returns (RollbackUsageResponse);

  // Get active discounts
  rpc GetActiveDiscounts(GetActiveDiscountsRequest) returns (GetActiveDiscountsResponse);

  // Get discounts for specific products
  rpc GetProductDiscounts(GetProductDiscountsRequest) returns (GetProductDiscountsResponse);
}

// ============================================
// Messages
// ============================================

// ValidateDiscount
message ValidateDiscountRequest {
  string code = 1;
  string user_id = 2;
  common.Money order_amount = 3;
  repeated OrderItem items = 4;
}

message ValidateDiscountResponse {
  bool valid = 1;
  string message = 2;
  DiscountInfo discount = 3;
  common.Money discount_amount = 4;
  repeated ValidationError errors = 5;
}

message OrderItem {
  string product_id = 1;
  string category_id = 2;
  int32 quantity = 3;
  common.Money unit_price = 4;
}

message DiscountInfo {
  string id = 1;
  string code = 2;
  string name = 3;
  string type = 4;  // PERCENTAGE, FIXED_AMOUNT, FREE_SHIPPING, BUY_X_GET_Y
  double value = 5;
  common.Money max_discount_amount = 6;
  common.Money min_order_amount = 7;
  int32 min_quantity = 8;
  google.protobuf.Timestamp start_date = 9;
  google.protobuf.Timestamp end_date = 10;
}

message ValidationError {
  string code = 1;
  string message = 2;
}

// ApplyDiscount
message ApplyDiscountRequest {
  string code = 1;
  string user_id = 2;
  string order_id = 3;
  string order_number = 4;
  common.Money order_amount = 5;
  repeated OrderItem items = 6;
}

message ApplyDiscountResponse {
  bool success = 1;
  string message = 2;
  string discount_id = 3;
  common.Money discount_amount = 4;
  string application_id = 5;  // Unique ID for this application
}

// RecordUsage
message RecordUsageRequest {
  string discount_id = 1;
  string user_id = 2;
  string order_id = 3;
  string order_number = 4;
  common.Money order_amount = 5;
  common.Money discount_amount = 6;
}

message RecordUsageResponse {
  bool success = 1;
  string message = 2;
  string usage_id = 3;
}

// RollbackUsage
message RollbackUsageRequest {
  string order_id = 1;
  string discount_id = 2;
  string reason = 3;
}

message RollbackUsageResponse {
  bool success = 1;
  string message = 2;
}

// GetActiveDiscounts
message GetActiveDiscountsRequest {
  string user_id = 1;
  common.PaginationRequest pagination = 2;
  DiscountFilter filter = 3;
}

message GetActiveDiscountsResponse {
  repeated DiscountInfo discounts = 1;
  common.PaginationResponse pagination = 2;
}

message DiscountFilter {
  string type = 1;
  common.Money min_order = 2;
  bool public_only = 3;
}

// GetProductDiscounts
message GetProductDiscountsRequest {
  repeated string product_ids = 1;
}

message GetProductDiscountsResponse {
  map<string, ProductDiscounts> discounts = 1;  // productId -> discounts
}

message ProductDiscounts {
  string product_id = 1;
  repeated DiscountInfo discounts = 2;
}
```

---

## 🎯 Usage Examples

### **Inventory Service - CheckStock**

**Request:**

```protobuf
CheckStockRequest {
  product_id: "550e8400-e29b-41d4-a716-446655440000"
  variant_id: "660e8400-e29b-41d4-a716-446655440000"
  quantity: 10
}
```

**Response:**

```protobuf
CheckStockResponse {
  available: true
  available_quantity: 50
  warehouse_id: "WH-HCM-01"
  message: "In stock"
}
```

---

### **Inventory Service - ReserveStock**

**Request:**

```protobuf
ReserveStockRequest {
  order_id: "ORDER-001"
  order_number: "ORD-20241129-001"
  items: [
    {
      product_id: "550e8400-e29b-41d4-a716-446655440000"
      quantity: 2
    },
    {
      product_id: "660e8400-e29b-41d4-a716-446655440000"
      quantity: 1
    }
  ]
  expiration_minutes: 30
}
```

**Response:**

```protobuf
ReserveStockResponse {
  success: true
  reservation_ids: ["RSV-001", "RSV-002"]
  expires_at: { seconds: 1701234567 }
  message: "Stock reserved successfully"
}
```

---

### **Discount Service - ValidateDiscount**

**Request:**

```protobuf
ValidateDiscountRequest {
  code: "SALE10"
  user_id: "user-123"
  order_amount: {
    amount: 50000000  // 500,000 VND (in smallest unit)
    currency: "VND"
  }
  items: [
    {
      product_id: "prod-001"
      category_id: "cat-electronics"
      quantity: 2
      unit_price: { amount: 25000000, currency: "VND" }
    }
  ]
}
```

**Response:**

```protobuf
ValidateDiscountResponse {
  valid: true
  message: "Discount code is valid"
  discount: {
    id: "disc-123"
    code: "SALE10"
    name: "Sale 10%"
    type: "PERCENTAGE"
    value: 10.0
    max_discount_amount: { amount: 5000000, currency: "VND" }
  }
  discount_amount: { amount: 5000000, currency: "VND" }
}
```

---

## 🔧 Code Generation

### **Generate C# Code**

```bash
# Install tools
dotnet tool install -g Grpc.Tools

# Generate from .proto files
cd shared/Shared.Protos

# Inventory
protoc --csharp_out=./Generated --grpc_out=./Generated \
  --plugin=protoc-gen-grpc=`which grpc_csharp_plugin` \
  inventory/v1/inventory.proto

# Discount
protoc --csharp_out=./Generated --grpc_out=./Generated \
  --plugin=protoc-gen-grpc=`which grpc_csharp_plugin` \
  discount/v1/discount.proto
```

### **.csproj Configuration**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Grpc.Tools" Version="2.60.0" PrivateAssets="All" />
    <PackageReference Include="Grpc.Core" Version="2.60.0" />
    <PackageReference Include="Grpc.Net.Client" Version="2.60.0" />
    <PackageReference Include="Google.Protobuf" Version="3.25.0" />
  </ItemGroup>

  <ItemGroup>
    <Protobuf Include="inventory/v1/inventory.proto" GrpcServices="Both" />
    <Protobuf Include="discount/v1/discount.proto" GrpcServices="Both" />
    <Protobuf Include="common/types.proto" />
  </ItemGroup>
</Project>
```

---

## 📋 Versioning Strategy

### **v1 → v2 Migration**

```
inventory/
  v1/
    inventory.proto    # Current version
  v2/
    inventory.proto    # Future version with breaking changes
```

### **Backward Compatibility Rules**

✅ **Can do:**

- Add new fields
- Add new RPC methods
- Add new message types
- Deprecate fields (mark as deprecated)

❌ **Cannot do:**

- Remove fields
- Change field types
- Change field numbers
- Rename fields

### **Example: Adding new field**

```protobuf
// v1 - Old
message CheckStockRequest {
  string product_id = 1;
  int32 quantity = 2;
}

// v1 - Updated (backward compatible)
message CheckStockRequest {
  string product_id = 1;
  int32 quantity = 2;
  string warehouse_id = 3;  // NEW FIELD - OK!
}
```

---

## 🎯 Best Practices

### **1. Field Numbers**

```protobuf
// Reserve ranges for different purposes
message Product {
  // 1-15: Single-byte encoding (most common fields)
  string id = 1;
  string name = 2;
  int32 quantity = 3;

  // 16-2047: Two-byte encoding (less common)
  string description = 16;
  repeated string tags = 17;

  // 19000-19999: Reserved for implementation
  reserved 19000 to 19999;
}
```

### **2. Required vs Optional**

```protobuf
// proto3: All fields are optional by default
message Request {
  string id = 1;              // Optional
  int32 quantity = 2;         // Optional (0 if not set)

  // Use wrapper types for explicit null
  google.protobuf.Int32Value count = 3;
}
```

### **3. Error Handling**

```protobuf
message Response {
  oneof result {
    Success success = 1;
    Error error = 2;
  }
}

message Success {
  string message = 1;
  // success data
}

message Error {
  string code = 1;
  string message = 2;
  map<string, string> details = 3;
}
```

### **4. Naming Conventions**

```protobuf
// PascalCase for messages and services
service InventoryService { }
message CheckStockRequest { }

// snake_case for fields
message Product {
  string product_id = 1;
  int32 available_quantity = 2;
}

// SCREAMING_SNAKE_CASE for enums
enum OrderStatus {
  ORDER_STATUS_UNSPECIFIED = 0;
  ORDER_STATUS_PENDING = 1;
  ORDER_STATUS_CONFIRMED = 2;
}
```

---

## 📊 Message Size Optimization

### **Before Optimization:**

```protobuf
message OrderItem {
  string product_id = 1;            // 36 bytes (UUID string)
  string variant_id = 2;            // 36 bytes
  int32 quantity = 3;               // 4 bytes
  string product_name = 4;          // Variable
  string description = 5;           // Large!
}
Total: ~100-500 bytes per item
```

### **After Optimization:**

```protobuf
message OrderItem {
  string product_id = 1;      // Only necessary fields
  string variant_id = 2;
  int32 quantity = 3;
  // Remove product_name, description (can get from Product Service)
}
Total: ~50 bytes per item
```

**Rule:** Only include fields necessary for the operation!

---

## 🔍 Testing

### **grpcurl Examples**

```bash
# List services
grpcurl -plaintext localhost:5015 list

# Describe service
grpcurl -plaintext localhost:5015 describe inventory.v1.InventoryService

# Call CheckStock
grpcurl -plaintext -d '{
  "product_id": "550e8400-e29b-41d4-a716-446655440000",
  "quantity": 10
}' localhost:5015 inventory.v1.InventoryService/CheckStock

# Call ValidateDiscount
grpcurl -plaintext -d '{
  "code": "SALE10",
  "user_id": "user-123",
  "order_amount": {
    "amount": 50000000,
    "currency": "VND"
  }
}' localhost:5016 discount.v1.DiscountService/ValidateDiscount
```

---

## 📚 References

- Protobuf Documentation: https://protobuf.dev/
- gRPC C# Guide: https://grpc.io/docs/languages/csharp/
- Style Guide: https://protobuf.dev/programming-guides/style/

---

**Next:** See `GRPC_IMPLEMENTATION_GUIDE.md` for implementation details!
