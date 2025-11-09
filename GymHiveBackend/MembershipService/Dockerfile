# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file
COPY MembershipService/MembershipService.csproj MembershipService/

# Restore dependencies
RUN dotnet restore "MembershipService/MembershipService.csproj"

# Copy all source code
COPY MembershipService/ MembershipService/

# Build
WORKDIR /src/MembershipService
RUN dotnet build "MembershipService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "MembershipService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "MembershipService.dll"]
