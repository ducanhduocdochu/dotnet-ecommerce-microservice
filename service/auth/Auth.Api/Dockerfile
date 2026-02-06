# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries first
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["shared/Shared.Protos/Shared.Protos.csproj", "shared/Shared.Protos/"]
COPY ["shared/Shared.Caching/Shared.Caching.csproj", "shared/Shared.Caching/"]

# Copy service csproj files
COPY ["service/auth/Auth.Domain/Auth.Domain.csproj", "service/auth/Auth.Domain/"]
COPY ["service/auth/Auth.Application/Auth.Application.csproj", "service/auth/Auth.Application/"]
COPY ["service/auth/Auth.Infrastructure/Auth.Infrastructure.csproj", "service/auth/Auth.Infrastructure/"]
COPY ["service/auth/Auth.Api/Auth.Api.csproj", "service/auth/Auth.Api/"]

# Restore dependencies
RUN dotnet restore "service/auth/Auth.Api/Auth.Api.csproj"

# Copy shared libraries source code
COPY shared/Shared.Messaging/ shared/Shared.Messaging/
COPY shared/Shared.Protos/ shared/Shared.Protos/
COPY shared/Shared.Caching/ shared/Shared.Caching/

# Copy service source code
COPY service/auth/ service/auth/

# Build and publish
WORKDIR "/src/service/auth/Auth.Api"
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

ENTRYPOINT ["dotnet", "Auth.Api.dll"]
