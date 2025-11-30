# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared + service csproj files and restore
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["service/product/Product.Domain/Product.Domain.csproj", "service/product/Product.Domain/"]
COPY ["service/product/Product.Application/Product.Application.csproj", "service/product/Product.Application/"]
COPY ["service/product/Product.Infrastructure/Product.Infrastructure.csproj", "service/product/Product.Infrastructure/"]
COPY ["service/product/Product.Api/Product.Api.csproj", "service/product/Product.Api/"]
RUN dotnet restore "service/product/Product.Api/Product.Api.csproj"

# Copy source code (shared + service)
COPY shared/ shared/
COPY service/product/ service/product/

# Build
WORKDIR "/src/service/product/Product.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Product.Api.dll"]

