# Gateway Security Best Practices

## Vấn đề bảo mật

```
Client → Gateway (validate JWT) → Service (không có gì bảo mật?)
```

Nếu không có biện pháp bảo mật, ai đó truy cập được internal network có thể gọi trực tiếp services.

## Giải pháp thực tế

### 1. Network Segmentation (Quan trọng nhất)

**Kiến trúc:**

```
Internet
  ↓
[Public Network] - Gateway (public IP)
  ↓ (firewall rules)
[Private Network] - Services (private IP, không expose ra internet)
```

**Implementation:**

- Services chỉ listen trên private IP (192.168.x.x, 10.x.x.x)
- Gateway có thể access private network
- Firewall: chỉ Gateway IP mới được access services
- Services không có public IP

**Ví dụ Docker/Kubernetes:**

```yaml
# Services chỉ expose trong internal network
services:
  user-service:
    networks:
      - internal # Chỉ internal network
    ports:
      - "127.0.0.1:5001:5001" # Chỉ localhost
```

### 2. Service-to-Service Authentication

**Cách 1: Internal API Key**

```csharp
// Gateway forward request với internal key
headers.Add("X-Internal-Key", "internal-secret-key-12345");

// Service validate
if (request.Headers["X-Internal-Key"] != "internal-secret-key-12345")
    return Unauthorized();
```

**Cách 2: Internal JWT Token**

```csharp
// Gateway tạo internal token
var internalToken = GenerateInternalToken();
headers.Add("Authorization", $"Internal {internalToken}");

// Service validate internal token
```

**Cách 3: mTLS (Mutual TLS)**

```csharp
// Gateway và Services dùng certificates
// Chỉ services có valid certificate mới giao tiếp được
```

### 3. Trust Header từ Gateway

**Implementation:**

```csharp
// Gateway thêm signed header
var signature = HMAC_SHA256(secret, request_body);
headers.Add("X-Gateway-Signature", signature);
headers.Add("X-Gateway-Timestamp", timestamp);

// Service validate
var expectedSignature = HMAC_SHA256(secret, request_body);
if (signature != expectedSignature) return Unauthorized();
```

### 4. IP Whitelist

```csharp
// Service chỉ accept requests từ Gateway IP
var allowedIPs = new[] { "10.0.0.100" }; // Gateway IP
if (!allowedIPs.Contains(clientIP)) return Forbidden();
```

## Kiến trúc đề xuất (Multi-layer Security)

### Layer 1: Network Security

- Services trong private network
- Firewall rules
- VPN cho admin access

### Layer 2: Service Authentication

- Internal API key hoặc mTLS
- Trust header từ Gateway

### Layer 3: Request Validation

- Validate Gateway signature
- Rate limiting per service
- Request logging

## Ví dụ Implementation

### Gateway Service

```csharp
// Gateway.Api/Program.cs
app.Use(async (context, next) =>
{
    // Validate JWT từ client
    var jwtValid = ValidateJwt(context.Request.Headers["Authorization"]);
    if (!jwtValid) return Unauthorized();

    // Thêm internal authentication
    context.Request.Headers.Add("X-Internal-Key", _internalSecret);
    context.Request.Headers.Add("X-Gateway-Request", "true");

    await next();
});
```

### User Service

```csharp
// User.Api/Program.cs
app.Use(async (context, next) =>
{
    // Chỉ accept requests từ Gateway
    var internalKey = context.Request.Headers["X-Internal-Key"];
    var isGatewayRequest = context.Request.Headers["X-Gateway-Request"];

    if (internalKey != _expectedInternalKey || isGatewayRequest != "true")
    {
        return Results.Forbid();
    }

    await next();
});
```

## So sánh với cách hiện tại

### Cách hiện tại (Mỗi service validate JWT)

✅ **Ưu điểm:**

- Mỗi service tự bảo vệ
- Không có single point of failure
- Đơn giản, dễ hiểu

❌ **Nhược điểm:**

- Validate JWT nhiều lần (performance)
- Phải chia sẻ secret key

### Cách Gateway (Gateway validate, services trust)

✅ **Ưu điểm:**

- Validate JWT một lần (performance tốt)
- Centralized authentication
- Services không cần biết JWT secret

❌ **Nhược điểm:**

- Cần thêm bảo mật giữa Gateway và Services
- Single point of failure (Gateway)
- Phức tạp hơn

## Recommendation

**Cho development/small scale:**

- Giữ nguyên cách hiện tại (mỗi service validate JWT)
- Đơn giản, an toàn

**Cho production lớn:**

- Dùng Gateway + Network Security + Internal Authentication
- Multi-layer security
- Best practice cho enterprise
