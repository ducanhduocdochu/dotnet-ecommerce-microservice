# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries first
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["shared/Shared.Protos/Shared.Protos.csproj", "shared/Shared.Protos/"]
COPY ["shared/Shared.Caching/Shared.Caching.csproj", "shared/Shared.Caching/"]

# Copy service csproj files
COPY ["service/inventory/Inventory.Domain/Inventory.Domain.csproj", "service/inventory/Inventory.Domain/"]
COPY ["service/inventory/Inventory.Application/Inventory.Application.csproj", "service/inventory/Inventory.Application/"]
COPY ["service/inventory/Inventory.Infrastructure/Inventory.Infrastructure.csproj", "service/inventory/Inventory.Infrastructure/"]
COPY ["service/inventory/Inventory.Api/Inventory.Api.csproj", "service/inventory/Inventory.Api/"]

# Restore dependencies
RUN dotnet restore "service/inventory/Inventory.Api/Inventory.Api.csproj"

# Copy shared libraries source code
COPY shared/Shared.Messaging/ shared/Shared.Messaging/
COPY shared/Shared.Protos/ shared/Shared.Protos/
COPY shared/Shared.Caching/ shared/Shared.Caching/

# Copy service source code
COPY service/inventory/ service/inventory/

# Build and publish
WORKDIR "/src/service/inventory/Inventory.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Create non-root user for security
RUN addgroup --system --gid 1000 dotnet && \
    adduser --system --uid 1000 --ingroup dotnet --shell /bin/sh dotnet

# Copy published files
COPY --from=build /app/publish .

# Set ownership
RUN chown -R dotnet:dotnet /app

# Switch to non-root user
USER dotnet

ENTRYPOINT ["dotnet", "Inventory.Api.dll"]
