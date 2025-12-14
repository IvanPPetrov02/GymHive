# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy shared library first
COPY ["GymHive.Messaging/GymHive.Messaging.csproj", "GymHive.Messaging/"]

# Copy project files
COPY ["MembershipService/MembershipService.csproj", "MembershipService/"]
COPY ["MembershipService/BLL/BLL.csproj", "MembershipService/BLL/"]
COPY ["MembershipService/DAL/DAL.csproj", "MembershipService/DAL/"]

# Restore dependencies
RUN dotnet restore "MembershipService/MembershipService.csproj"

# Copy all source code
COPY ["GymHive.Messaging/", "GymHive.Messaging/"]
COPY ["MembershipService/", "MembershipService/"]

# Build
WORKDIR /src/MembershipService
RUN dotnet build "MembershipService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "MembershipService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "MembershipService.dll"]
