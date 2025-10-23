# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["GymService/GymService.csproj", "GymService/"]
COPY ["GymService/BLL/BLL.csproj", "GymService/BLL/"]
COPY ["GymService/DAL/DAL.csproj", "GymService/DAL/"]
RUN dotnet restore "GymService/GymService.csproj"

# Copy all source files
COPY ["GymService/", "GymService/"]

# Build the application
WORKDIR "/src/GymService"
RUN dotnet build "GymService.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GymService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published files
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "GymService.dll"]
