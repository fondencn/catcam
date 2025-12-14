#!/bin/bash

# Native deployment script for CatCam - publishes and deploys directly to Raspberry Pi
set -e

# Color codes for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Icons
ROCKET="🚀"
PACKAGE="📦"
UPLOAD="📤"
CHECK="✅"
CROSS="❌"
CONFIG="⚙️"

# Configuration file
CONFIG_FILE=".deploy-config"

echo -e "${BLUE}${ROCKET} CatCam Native Deployment Script${NC}"
echo ""

# Load configuration if exists
if [ -f "$CONFIG_FILE" ]; then
    echo -e "${CONFIG}${BLUE} Loading configuration from $CONFIG_FILE${NC}"
    source "$CONFIG_FILE"
fi

# Prompt for SSH configuration
echo -e "${CONFIG}${BLUE} SSH Configuration${NC}"
read -p "Enter Pi username (default: ${PI_USER:-pi}): " input_user
PI_USER=${input_user:-${PI_USER:-pi}}

read -p "Enter Pi IP address (default: ${PI_IP:-192.168.178.57}): " input_ip
PI_IP=${input_ip:-${PI_IP:-192.168.178.57}}

read -p "Enter deployment path on Pi (default: /home/$PI_USER/catcam): " input_path
DEPLOY_PATH=${input_path:-/home/$PI_USER/catcam}

# Prompt for password if not set
echo ""
echo -e "${CONFIG}${BLUE} Application Configuration${NC}"

if [ -z "$CATCAM_PASSWORD" ]; then
    read -sp "Enter CatCam password: " CATCAM_PASSWORD
    echo ""
fi

# Validate required variables
if [ -z "$CATCAM_PASSWORD" ]; then
    echo -e "${CROSS}${RED} Error: Missing required password${NC}"
    exit 1
fi

# Save configuration
echo -e "${CONFIG}${BLUE} Saving configuration to $CONFIG_FILE${NC}"
cat > "$CONFIG_FILE" << EOF
PI_USER="$PI_USER"
PI_IP="$PI_IP"
DEPLOY_PATH="$DEPLOY_PATH"
CATCAM_PASSWORD="$CATCAM_PASSWORD"
EOF

echo ""
echo -e "${PACKAGE}${BLUE} Publishing application for ARM64 (self-contained)${NC}"

# Navigate to solution directory
cd "$(dirname "$0")/.."

# Clean previous publish
rm -rf publish/native-arm64

# Publish the application
dotnet publish CatCam.Web/CatCam.Web.csproj \
    --configuration Release \
    --runtime linux-arm64 \
    --self-contained true \
    -p:PublishSingleFile=false \
    --output publish/native-arm64

if [ $? -ne 0 ]; then
    echo -e "${CROSS}${RED} Build failed${NC}"
    exit 1
fi

echo -e "${CHECK}${GREEN} Build successful${NC}"
echo ""

# Create appsettings.Production.json with environment variables
echo -e "${CONFIG}${BLUE} Creating production configuration${NC}"
cat > publish/native-arm64/appsettings.Production.json << EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Authentication": {
    "Password": "$CATCAM_PASSWORD"
  }
}
EOF

# Copy systemd service file to publish directory
cp scripts/catcam.service publish/native-arm64/

echo ""
echo -e "${UPLOAD}${BLUE} Deploying to $PI_USER@$PI_IP:$DEPLOY_PATH${NC}"

# Stop service if running
echo -e "${BLUE} Stopping service (if running)${NC}"
ssh "$PI_USER@$PI_IP" "sudo systemctl stop catcam 2>/dev/null || true"

# Create deployment directory on Pi
ssh "$PI_USER@$PI_IP" "mkdir -p $DEPLOY_PATH"

# Copy files to Pi
echo -e "${BLUE} Copying files...${NC}"
scp -r publish/native-arm64/* "$PI_USER@$PI_IP:$DEPLOY_PATH/"

# Set executable permissions
echo -e "${BLUE} Setting permissions${NC}"
ssh "$PI_USER@$PI_IP" "chmod +x $DEPLOY_PATH/CatCam.Web"

# Install and start systemd service
echo -e "${BLUE} Installing systemd service${NC}"
ssh "$PI_USER@$PI_IP" "sudo cp $DEPLOY_PATH/catcam.service /etc/systemd/system/"
ssh "$PI_USER@$PI_IP" "sudo systemctl daemon-reload"
ssh "$PI_USER@$PI_IP" "sudo systemctl enable catcam"
ssh "$PI_USER@$PI_IP" "sudo systemctl start catcam"

echo ""
echo -e "${CHECK}${GREEN} Deployment complete!${NC}"
echo ""
echo -e "${BLUE} Application is running at:${NC}"
echo -e "  Local: http://$PI_IP:8080"
echo ""
echo -e "${BLUE} Useful commands:${NC}"
echo -e "  View logs:    ssh $PI_USER@$PI_IP 'sudo journalctl -u catcam -f'"
echo -e "  Restart:      ssh $PI_USER@$PI_IP 'sudo systemctl restart catcam'"
echo -e "  Stop:         ssh $PI_USER@$PI_IP 'sudo systemctl stop catcam'"
echo -e "  Status:       ssh $PI_USER@$PI_IP 'sudo systemctl status catcam'"

