# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["service/gateway/Gateway.Api/Gateway.Api.csproj", "service/gateway/Gateway.Api/"]
RUN dotnet restore "service/gateway/Gateway.Api/Gateway.Api.csproj"

# Copy source code
COPY service/gateway/ service/gateway/

# Build
WORKDIR "/src/service/gateway/Gateway.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]

