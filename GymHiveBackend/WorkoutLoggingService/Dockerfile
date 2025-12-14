# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared library first
COPY ["GymHive.Messaging/GymHive.Messaging.csproj", "GymHive.Messaging/"]

# Copy project files
COPY ["WorkoutLoggingService/WorkoutLoggingService.csproj", "WorkoutLoggingService/"]
COPY ["WorkoutLoggingService/BLL/BLL.csproj", "WorkoutLoggingService/BLL/"]
COPY ["WorkoutLoggingService/DAL/DAL.csproj", "WorkoutLoggingService/DAL/"]

# Restore dependencies
RUN dotnet restore "WorkoutLoggingService/WorkoutLoggingService.csproj"

# Copy all source code
COPY ["GymHive.Messaging/", "GymHive.Messaging/"]
COPY ["WorkoutLoggingService/", "WorkoutLoggingService/"]

# Build
WORKDIR /src/WorkoutLoggingService
RUN dotnet build "WorkoutLoggingService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "WorkoutLoggingService.csproj" -c Release -o /app/publish /p:UseAppHost=false

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

ENTRYPOINT ["dotnet", "WorkoutLoggingService.dll"]
