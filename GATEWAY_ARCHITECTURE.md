# API Gateway Architecture - Future Enhancement

## Current Architecture (No Gateway)

```
Client → Auth Service (validate) → User Service (validate) → Product Service (validate)
```

- Mỗi service tự validate JWT
- Đơn giản, phù hợp development/small scale
- Không có single point of failure

## Future Architecture (With Gateway)

```
Client → API Gateway (validate JWT once) → Services (trust Gateway)
```

### Benefits:

1. **Centralized Authentication**: Validate một lần ở Gateway
2. **Performance**: Services không cần validate JWT
3. **Security**: Services không cần biết secret key
4. **Rate Limiting**: Centralized rate limiting
5. **Request Routing**: Smart routing và load balancing
6. **Monitoring**: Centralized logging và monitoring

### Implementation Options:

#### Option 1: Ocelot (Open Source .NET Gateway)

```csharp
// Gateway.Api/Program.cs
builder.Services.AddOcelot()
    .AddJwtBearer("JwtOptions", options => {
        // Validate JWT here
    });
```

#### Option 2: YARP (Yet Another Reverse Proxy) - Microsoft

```csharp
// Gateway.Api/Program.cs
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
```

#### Option 3: Kong / AWS API Gateway / Azure API Management

- Enterprise solutions
- More features but more complex

### Migration Path:

1. **Phase 1 (Current)**: Mỗi service tự validate ✅
2. **Phase 2**: Thêm Gateway, services vẫn validate (defense in depth)
3. **Phase 3**: Services trust Gateway, remove validation từ services

### Recommendation:

- **Now**: Giữ nguyên (mỗi service tự validate)
- **When to add Gateway**:
  - Khi có > 5 services
  - Cần rate limiting, monitoring centralized
  - Cần API versioning, routing phức tạp
  - Scale lên production lớn
