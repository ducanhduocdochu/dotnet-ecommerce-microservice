# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries first
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["shared/Shared.Protos/Shared.Protos.csproj", "shared/Shared.Protos/"]
COPY ["shared/Shared.Caching/Shared.Caching.csproj", "shared/Shared.Caching/"]

# Copy service csproj files
COPY ["service/discount/Discount.Domain/Discount.Domain.csproj", "service/discount/Discount.Domain/"]
COPY ["service/discount/Discount.Application/Discount.Application.csproj", "service/discount/Discount.Application/"]
COPY ["service/discount/Discount.Infrastructure/Discount.Infrastructure.csproj", "service/discount/Discount.Infrastructure/"]
COPY ["service/discount/Discount.Api/Discount.Api.csproj", "service/discount/Discount.Api/"]

# Restore dependencies
RUN dotnet restore "service/discount/Discount.Api/Discount.Api.csproj"

# Copy shared libraries source code
COPY shared/Shared.Messaging/ shared/Shared.Messaging/
COPY shared/Shared.Protos/ shared/Shared.Protos/
COPY shared/Shared.Caching/ shared/Shared.Caching/

# Copy service source code
COPY service/discount/ service/discount/

# Build and publish
WORKDIR "/src/service/discount/Discount.Api"
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

ENTRYPOINT ["dotnet", "Discount.Api.dll"]
