# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj file
COPY ["service/gateway/Gateway.Api/Gateway.Api.csproj", "service/gateway/Gateway.Api/"]

# Restore dependencies
RUN dotnet restore "service/gateway/Gateway.Api/Gateway.Api.csproj"

# Copy service source code
COPY service/gateway/ service/gateway/

# Build and publish
WORKDIR "/src/service/gateway/Gateway.Api"
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

ENTRYPOINT ["dotnet", "Gateway.Api.dll"]
