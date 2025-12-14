# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared library first
COPY ["GymHive.Messaging/GymHive.Messaging.csproj", "GymHive.Messaging/"]

# Copy project files
COPY ["AuthenticationService/AuthenticationService.csproj", "AuthenticationService/"]
COPY ["AuthenticationService/BLL/BLL.csproj", "AuthenticationService/BLL/"]
COPY ["AuthenticationService/DAL/DAL.csproj", "AuthenticationService/DAL/"]

# Restore dependencies
RUN dotnet restore "AuthenticationService/AuthenticationService.csproj"

# Copy source code
COPY ["GymHive.Messaging/", "GymHive.Messaging/"]
COPY ["AuthenticationService/", "AuthenticationService/"]

# Build the application
WORKDIR /src/AuthenticationService
RUN dotnet build "AuthenticationService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "AuthenticationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published app
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

# Run the application
ENTRYPOINT ["dotnet", "AuthenticationService.dll"]
