# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared + service csproj files and restore
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["service/discount/Discount.Domain/Discount.Domain.csproj", "service/discount/Discount.Domain/"]
COPY ["service/discount/Discount.Application/Discount.Application.csproj", "service/discount/Discount.Application/"]
COPY ["service/discount/Discount.Infrastructure/Discount.Infrastructure.csproj", "service/discount/Discount.Infrastructure/"]
COPY ["service/discount/Discount.Api/Discount.Api.csproj", "service/discount/Discount.Api/"]
RUN dotnet restore "service/discount/Discount.Api/Discount.Api.csproj"

# Copy source code (shared + service)
COPY shared/ shared/
COPY service/discount/ service/discount/

# Build
WORKDIR "/src/service/discount/Discount.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Discount.Api.dll"]

