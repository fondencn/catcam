# Build stage - use Microsoft SDK for building
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

# Runtime stage - use Raspberry Pi OS base with .NET runtime
FROM debian:bookworm-slim AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Install .NET runtime dependencies and camera tools
RUN apt-get update && apt-get install -y \
    wget \
    ca-certificates \
    libicu-dev \
    libssl3 \
    libgssapi-krb5-2 \
    libkrb5-3 \
    zlib1g \
    v4l-utils \
    gnupg \
    && rm -rf /var/lib/apt/lists/*

# Install .NET 10 runtime for ARM64
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh \
    && chmod +x dotnet-install.sh \
    && ./dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet \
    && ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet \
    && rm dotnet-install.sh

# Install Raspberry Pi camera tools with all dependencies
RUN wget -qO - https://archive.raspberrypi.org/debian/raspberrypi.gpg.key | apt-key add - \
    && echo "deb https://archive.raspberrypi.org/debian/ bookworm main" > /etc/apt/sources.list.d/raspi.list \
    && apt-get update \
    && apt-get install -y \
        libcamera0.3 \
        libcamera-ipa \
        libcamera-tools \
        rpicam-apps-core \
        rpicam-apps-encoder \
        rpicam-apps-opencv-postprocess \
        rpicam-apps-preview \
        rpicam-apps \
    || apt-get install -y --fix-broken \
    && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create a non-root user
# Add user to video group for camera access
RUN useradd -m -s /bin/bash appuser && \
    mkdir -p /app/keys && \
    chown -R appuser:appuser /app && \
    usermod -a -G video appuser

# Create entrypoint script to fix volume permissions then switch to appuser
RUN echo '#!/bin/bash\n\
chown -R appuser:appuser /app/keys\n\
exec su appuser -c "dotnet /app/CatCam.Web.dll"' > /entrypoint.sh && \
    chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
