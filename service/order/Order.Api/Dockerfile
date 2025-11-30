# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared + service csproj files and restore
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["service/order/Order.Domain/Order.Domain.csproj", "service/order/Order.Domain/"]
COPY ["service/order/Order.Application/Order.Application.csproj", "service/order/Order.Application/"]
COPY ["service/order/Order.Infrastructure/Order.Infrastructure.csproj", "service/order/Order.Infrastructure/"]
COPY ["service/order/Order.Api/Order.Api.csproj", "service/order/Order.Api/"]
RUN dotnet restore "service/order/Order.Api/Order.Api.csproj"

# Copy source code (shared + service)
COPY shared/ shared/
COPY service/order/ service/order/

# Build
WORKDIR "/src/service/order/Order.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Order.Api.dll"]

