# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy shared libraries first
COPY ["shared/Shared.Messaging/Shared.Messaging.csproj", "shared/Shared.Messaging/"]
COPY ["shared/Shared.Protos/Shared.Protos.csproj", "shared/Shared.Protos/"]
COPY ["shared/Shared.Caching/Shared.Caching.csproj", "shared/Shared.Caching/"]

# Copy service csproj files
COPY ["service/user/User.Domain/User.Domain.csproj", "service/user/User.Domain/"]
COPY ["service/user/User.Application/User.Application.csproj", "service/user/User.Application/"]
COPY ["service/user/User.Infrastructure/User.Infrastructure.csproj", "service/user/User.Infrastructure/"]
COPY ["service/user/User.Api/User.Api.csproj", "service/user/User.Api/"]

# Restore dependencies
RUN dotnet restore "service/user/User.Api/User.Api.csproj"

# Copy shared libraries source code
COPY shared/Shared.Messaging/ shared/Shared.Messaging/
COPY shared/Shared.Protos/ shared/Shared.Protos/
COPY shared/Shared.Caching/ shared/Shared.Caching/

# Copy service source code
COPY service/user/ service/user/

# Build and publish
WORKDIR "/src/service/user/User.Api"
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

ENTRYPOINT ["dotnet", "User.Api.dll"]
