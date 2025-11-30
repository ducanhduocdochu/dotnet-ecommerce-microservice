# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared + service csproj files and restore
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["service/payment/Payment.Domain/Payment.Domain.csproj", "service/payment/Payment.Domain/"]
COPY ["service/payment/Payment.Application/Payment.Application.csproj", "service/payment/Payment.Application/"]
COPY ["service/payment/Payment.Infrastructure/Payment.Infrastructure.csproj", "service/payment/Payment.Infrastructure/"]
COPY ["service/payment/Payment.Api/Payment.Api.csproj", "service/payment/Payment.Api/"]
RUN dotnet restore "service/payment/Payment.Api/Payment.Api.csproj"

# Copy source code (shared + service)
COPY shared/ shared/
COPY service/payment/ service/payment/

# Build
WORKDIR "/src/service/payment/Payment.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Payment.Api.dll"]

