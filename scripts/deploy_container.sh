# dotnet publish -c Release -r linux-arm64 --self-contained true 
# scp -r ./publish/arm64/* pi@192.168.178.57:/home/pi/catcam


#!/usr/bin/env bash
set -euo pipefail

IMAGE="catcam:latest"
CONFIG_FILE=".deploy-config"

echo "🚀 CatCam Deployment Script"
echo "=========================="
echo ""

# Load configuration from file if it exists
if [ -f "${CONFIG_FILE}" ]; then
    echo "📂 Loading saved configuration..."
    source "${CONFIG_FILE}"
    echo "✅ Configuration loaded!"
    echo ""
fi

# Prompt for SSH configuration
if [ -z "${PI_USER:-}" ]; then
    read -p "📝 Enter SSH username [pi]: " PI_USER
    PI_USER="${PI_USER:-pi}"
fi

if [ -z "${PI_IP:-}" ]; then
    read -p "🌐 Enter Pi IP address [192.168.178.57]: " PI_IP
    PI_IP="${PI_IP:-192.168.178.57}"
fi

PI_HOST="${PI_USER}@${PI_IP}"

echo ""
echo "💡 Note: SSH will use key-based authentication or prompt for password if needed."
echo ""

# Prompt for required environment variables if not set
if [ -z "${LETSENCRYPT_EMAIL:-}" ]; then
    read -p "📧 Enter Let's Encrypt email address: " LETSENCRYPT_EMAIL
fi

if [ -z "${LETSENCRYPT_HOST:-}" ]; then
    read -p "🌍 Enter Let's Encrypt host: " LETSENCRYPT_HOST
fi

if [ -z "${CATCAM_PASSWORD:-}" ]; then
    read -sp "🔐 Enter CatCam password: " CATCAM_PASSWORD
    echo ""
fi

PASSWORD="${CATCAM_PASSWORD}"

# Check if required variables are still empty and exit if so
if [ -z "${LETSENCRYPT_EMAIL}" ] || [ -z "${LETSENCRYPT_HOST}" ] || [ -z "${PASSWORD}" ]; then
    echo ""
    echo "❌ Error: Required environment variables are not set."
    echo "  📧 LETSENCRYPT_EMAIL: ${LETSENCRYPT_EMAIL:-<not set>}"
    echo "  🌍 LETSENCRYPT_HOST: ${LETSENCRYPT_HOST:-<not set>}"
    echo "  🔐 PASSWORD: ${PASSWORD:+<set>}${PASSWORD:-<not set>}"
    exit 1
fi

# Save configuration to file
cat > "${CONFIG_FILE}" << EOF
PI_USER="${PI_USER}"
PI_IP="${PI_IP}"
LETSENCRYPT_EMAIL="${LETSENCRYPT_EMAIL}"
LETSENCRYPT_HOST="${LETSENCRYPT_HOST}"
CATCAM_PASSWORD="${PASSWORD}"
EOF

echo ""
echo "💾 Configuration saved to ${CONFIG_FILE}"
echo ""
echo "✅ Configuration complete!"
echo "  👤 SSH User: ${PI_USER}"
echo "  🌐 Pi IP: ${PI_IP}"
echo "  📧 Let's Encrypt Email: ${LETSENCRYPT_EMAIL}"
echo "  🌍 Let's Encrypt Host: ${LETSENCRYPT_HOST}"
echo ""

echo "🔨 Building Docker image for ARM64..."
docker buildx build --platform linux/arm64 -t "${IMAGE}" --load .

echo ""
echo "📦 Transferring image to Pi..."
docker save "${IMAGE}" | ssh "${PI_HOST}" "docker load"
scp docker-compose.yml "${PI_HOST}":~/catcam/docker-compose.yml

echo ""
echo "🚢 Deploying on Pi..."
ssh "${PI_HOST}" "cd ~/catcam && docker compose down && CATCAM_PASSWORD='${PASSWORD}' LETSENCRYPT_EMAIL='${LETSENCRYPT_EMAIL}' LETSENCRYPT_HOST='${LETSENCRYPT_HOST}' VIRTUAL_HOST='${LETSENCRYPT_HOST}' docker compose up -d"

echo ""
echo "✨ Deployment complete!"