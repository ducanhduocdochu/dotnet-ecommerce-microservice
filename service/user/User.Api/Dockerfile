# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared + service csproj files and restore
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["service/user/User.Domain/User.Domain.csproj", "service/user/User.Domain/"]
COPY ["service/user/User.Application/User.Application.csproj", "service/user/User.Application/"]
COPY ["service/user/User.Infrastructure/User.Infrastructure.csproj", "service/user/User.Infrastructure/"]
COPY ["service/user/User.Api/User.Api.csproj", "service/user/User.Api/"]
RUN dotnet restore "service/user/User.Api/User.Api.csproj"

# Copy source code (shared + service)
COPY shared/ shared/
COPY service/user/ service/user/

# Build
WORKDIR "/src/service/user/User.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "User.Api.dll"]

