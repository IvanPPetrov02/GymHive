# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared library first
COPY ["GymHive.Messaging/GymHive.Messaging.csproj", "GymHive.Messaging/"]

# Copy project files
COPY ["NotificationsService/NotificationsService.csproj", "NotificationsService/"]
COPY ["NotificationsService/BLL/BLL.csproj", "NotificationsService/BLL/"]
COPY ["NotificationsService/DAL/DAL.csproj", "NotificationsService/DAL/"]

# Restore dependencies
RUN dotnet restore "NotificationsService/NotificationsService.csproj"

# Copy all source code
COPY ["GymHive.Messaging/", "GymHive.Messaging/"]
COPY ["NotificationsService/", "NotificationsService/"]

# Build
WORKDIR /src/NotificationsService
RUN dotnet build "NotificationsService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "NotificationsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "NotificationsService.dll"]
