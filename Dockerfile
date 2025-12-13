# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY CatCam.Web/CatCam.Web.csproj CatCam.Web/
RUN dotnet restore CatCam.Web/CatCam.Web.csproj

# Copy everything else and build
COPY CatCam.Web/ CatCam.Web/
WORKDIR /src/CatCam.Web
RUN dotnet build CatCam.Web.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish CatCam.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published application
COPY --from=publish /app/publish .

# Create a non-root user
RUN useradd -m -s /bin/bash appuser && chown -R appuser:appuser /app
USER appuser

ENTRYPOINT ["dotnet", "CatCam.Web.dll"]
