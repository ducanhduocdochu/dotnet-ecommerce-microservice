# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["service/auth/Auth.Domain/Auth.Domain.csproj", "service/auth/Auth.Domain/"]
COPY ["service/auth/Auth.Application/Auth.Application.csproj", "service/auth/Auth.Application/"]
COPY ["service/auth/Auth.Infrastructure/Auth.Infrastructure.csproj", "service/auth/Auth.Infrastructure/"]
COPY ["service/auth/Auth.Api/Auth.Api.csproj", "service/auth/Auth.Api/"]
RUN dotnet restore "service/auth/Auth.Api/Auth.Api.csproj"

# Copy source code
COPY service/auth/ service/auth/

# Build
WORKDIR "/src/service/auth/Auth.Api"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Auth.Api.dll"]

