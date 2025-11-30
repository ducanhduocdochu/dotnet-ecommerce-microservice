# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared + service csproj files and restore
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["service/inventory/Inventory.Domain/Inventory.Domain.csproj", "service/inventory/Inventory.Domain/"]
COPY ["service/inventory/Inventory.Application/Inventory.Application.csproj", "service/inventory/Inventory.Application/"]
COPY ["service/inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "service/inventory/Inventory.Infrastructure/"]
COPY ["service/inventory/Inventory.Api/Inventory.Api.csproj", "service/inventory/Inventory.Api/"]
RUN dotnet restore "service/inventory/Inventory.Api/Inventory.Api.csproj"

# Copy source code (shared + service)
COPY shared/ shared/
COPY service/inventory/ service/inventory/

# Build
WORKDIR "/src/service/inventory/Inventory.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Inventory.Api.dll"]

