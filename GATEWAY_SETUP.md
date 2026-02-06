# Gateway Setup Guide

## Kiến trúc Gateway

```
Client → Gateway (validate JWT) → Services (validate internal key)
```

## Các thành phần đã tạo

### 1. Gateway.Api
- **Location**: `service/gateway/Gateway.Api/`
- **Port**: 8080
- **Technology**: YARP (Yet Another Reverse Proxy) - Microsoft
- **Features**:
  - JWT validation
  - Request routing
  - Internal authentication
  - Load balancing support

### 2. Internal Authentication
- Gateway thêm headers khi forward request:
  - `X-Internal-Key`: Secret key để services validate
  - `X-Gateway-Request`: Flag để services biết request từ Gateway
  - `X-Gateway-Timestamp`: Timestamp

### 3. Services Update
- User Service đã được cập nhật với `InternalAuthMiddleware`
- Validate internal key nếu request từ Gateway
- Vẫn hỗ trợ direct access (backward compatible)

## Cách hoạt động

### Luồng request qua Gateway:

1. **Client gửi request** với JWT token
   ```
   GET /api/users/me/profile
   Authorization: Bearer {jwt_token}
   ```

2. **Gateway validate JWT**
   - Check token hợp lệ
   - Extract user claims
   - Nếu invalid → return 401

3. **Gateway forward request** đến User Service
   - Thêm headers:
     - `X-Internal-Key`: internal-gateway-secret-key-2024
     - `X-Gateway-Request`: true
     - `X-Gateway-Timestamp`: timestamp
   - Forward JWT token (để service extract user info)

4. **User Service validate**
   - Check `X-Gateway-Request` header
   - Validate `X-Internal-Key`
   - Nếu valid → process request
   - Nếu invalid → return 403

## Cấu hình

### Gateway Configuration (`appsettings.json`)

```json
{
  "Jwt": {
    "Secret": "ducanhdeptrai123_ducanhdeptrai123"  // Same as Auth Service
  },
  "Internal": {
    "Secret": "internal-gateway-secret-key-2024"  // Shared với services
  },
  "Services": {
    "Auth": "http://localhost:5000",
    "User": "http://localhost:5001",
    // ... other services
  }
}
```

### Service Configuration

Mỗi service cần thêm:
```json
{
  "Internal": {
    "Secret": "internal-gateway-secret-key-2024"  // Same as Gateway
  }
}
```

## Routes Configuration

Gateway routes các requests:
- `/api/auth/*` → Auth Service (port 5000)
- `/api/users/*` → User Service (port 5001)
- `/api/products/*` → Product Service (port 5002)
- `/api/orders/*` → Order Service (port 5003)
- `/api/payments/*` → Payment Service (port 5004)
- `/api/inventory/*` → Inventory Service (port 5005)
- `/api/discounts/*` → Discount Service (port 5006)
- `/api/notifications/*` → Notification Service (port 5007)

## Security Layers

### Layer 1: Network Security (Recommended)
- Services chỉ expose trong private network
- Gateway ở public network
- Firewall rules: chỉ Gateway IP access services

### Layer 2: Internal Authentication
- Gateway thêm internal key
- Services validate internal key
- Chỉ Gateway có key này

### Layer 3: JWT Validation
- Gateway validate JWT một lần
- Services không cần validate JWT (nếu có Gateway)
- Services vẫn có thể validate JWT nếu direct access

## Testing

### Test Gateway:
```bash
# Health check
curl http://localhost:8080/health

# Login qua Gateway
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'

# Get profile qua Gateway
curl http://localhost:8080/api/users/me/profile \
  -H "Authorization: Bearer {token}"
```

### Test Direct Access (backward compatible):
```bash
# Direct access vẫn hoạt động
curl http://localhost:5001/api/users/me/profile \
  -H "Authorization: Bearer {token}"
```

## Migration Path

### Phase 1: Current (Hybrid)
- Gateway available nhưng không bắt buộc
- Services hỗ trợ cả Gateway và direct access
- Clients có thể dùng Gateway hoặc direct

### Phase 2: Gateway Required
- Services chỉ accept requests từ Gateway
- Remove direct access
- Tất cả traffic đi qua Gateway

## Benefits

1. **Centralized Authentication**: Validate JWT một lần
2. **Performance**: Services không cần validate JWT
3. **Security**: Services không cần biết JWT secret
4. **Monitoring**: Centralized logging
5. **Rate Limiting**: Có thể thêm ở Gateway
6. **Load Balancing**: YARP hỗ trợ multiple destinations

## Next Steps

1. ✅ Gateway service created
2. ✅ JWT validation configured
3. ✅ Internal authentication implemented
4. ✅ User Service updated
5. ⏳ Update other services (Product, Order, etc.)
6. ⏳ Add rate limiting
7. ⏳ Add request logging
8. ⏳ Add health checks

