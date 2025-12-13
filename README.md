# CatCam

Watch your Cats via Raspberry Pi Cam

A secure ASP.NET Core Razor Pages application for viewing a Raspberry Pi camera stream with a beautiful Fluent 2 design interface.

## Features

- 🔐 **Password-protected access** - Configurable via environment variables or appsettings.json
- 📹 **Live webcam streaming** - Designed to work with Raspberry Pi camera
- 🎨 **Fluent 2 Design System** - Modern, beautiful UI using Microsoft's Fluent UI
- 🐳 **Docker support** - Easy deployment with Docker and Docker Compose
- 🔒 **HTTPS in production** - Automated SSL/TLS certificates with Let's Encrypt
- 🚀 **Simple development setup** - HTTP for local development

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (for local development)
- [Docker](https://www.docker.com/get-started) and [Docker Compose](https://docs.docker.com/compose/) (for containerized deployment)
- Raspberry Pi with camera module (for actual video streaming)

## Quick Start

### Development (HTTP)

1. **Clone the repository:**
   ```bash
   git clone https://github.com/fondencn/catcam.git
   cd catcam
   ```

2. **Run with .NET CLI:**
   ```bash
   cd CatCam.Web
   dotnet run
   ```
   
   The application will be available at `http://localhost:5000`

3. **Or run with Docker Compose:**
   ```bash
   docker-compose -f docker-compose.dev.yml up --build
   ```
   
   The application will be available at `http://localhost:5000`

4. **Login:**
   - Default password: `catcam123`
   - Configure in `appsettings.json` or via environment variable `CATCAM_PASSWORD`

### Production (HTTPS with Let's Encrypt)

1. **Configure environment variables:**
   ```bash
   cp .env.example .env
   # Edit .env with your domain and email
   ```

2. **Update .env file:**
   ```env
   CATCAM_PASSWORD=your-secure-password
   VIRTUAL_HOST=catcam.yourdomain.com
   LETSENCRYPT_HOST=catcam.yourdomain.com
   LETSENCRYPT_EMAIL=your-email@example.com
   ```

3. **Ensure DNS is configured:**
   - Point your domain to your server's IP address
   - Wait for DNS propagation

4. **Deploy with Docker Compose:**
   ```bash
   docker-compose up -d
   ```

5. **Access your application:**
   - Visit `https://catcam.yourdomain.com`
   - Let's Encrypt will automatically obtain and renew SSL certificates

## Configuration

### Password Authentication

Configure the password in one of these ways:

1. **Environment variable (recommended for production):**
   ```bash
   export CATCAM_PASSWORD="your-secure-password"
   ```

2. **appsettings.json (development):**
   ```json
   {
     "Authentication": {
       "Password": "catcam123"
     }
   }
   ```

3. **Docker Compose:**
   ```yaml
   environment:
     - Authentication__Password=your-secure-password
   ```

### Camera Setup

The application expects a camera stream at `/api/camera/stream`. You'll need to implement the camera streaming endpoint or configure your Raspberry Pi to provide the stream.

**Example using Raspberry Pi:**

1. Install `rpicam-apps` or `motion` on your Raspberry Pi
2. Configure the camera to stream to a network endpoint
3. Update the webcam page to point to your camera stream URL

## Docker Commands

### Development

```bash
# Build and run development environment
docker-compose -f docker-compose.dev.yml up --build

# Stop development environment
docker-compose -f docker-compose.dev.yml down

# View logs
docker-compose -f docker-compose.dev.yml logs -f
```

### Production

```bash
# Start production services
docker-compose up -d

# Stop production services
docker-compose down

# View logs
docker-compose logs -f catcam-web

# Rebuild and restart
docker-compose up -d --build

# Check SSL certificate status
docker-compose logs letsencrypt-companion
```

## Project Structure

```
catcam/
├── CatCam.sln                      # Visual Studio solution
├── CatCam.Web/                     # ASP.NET Core Razor Pages project
│   ├── Pages/
│   │   ├── Login.cshtml            # Login page with Fluent 2 design
│   │   ├── Webcam.cshtml           # Webcam stream page
│   │   └── ...
│   ├── Program.cs                  # Application configuration
│   ├── appsettings.json            # Application settings
│   └── CatCam.Web.csproj
├── Dockerfile                      # Multi-stage Docker build
├── docker-compose.yml              # Production deployment (HTTPS)
├── docker-compose.dev.yml          # Development deployment (HTTP)
├── .env.example                    # Environment variables template
└── README.md
```

## Security Notes

- Always use strong passwords in production
- The default password `catcam123` should be changed
- HTTPS is automatically enabled in production via Let's Encrypt
- Cookies are set to secure mode when using HTTPS
- The application runs as a non-root user in Docker

## Technology Stack

- **Backend:** ASP.NET Core 10.0 (Razor Pages)
- **UI Framework:** Fluent UI Web Components (Fluent 2 Design System)
- **Authentication:** Cookie-based authentication
- **Containerization:** Docker
- **Reverse Proxy:** nginx-proxy
- **SSL/TLS:** Let's Encrypt (via acme-companion)

## Troubleshooting

### Camera not accessible in Docker

Make sure the camera device is available and accessible:
```bash
ls -l /dev/video0
```

Grant necessary permissions or adjust the Docker Compose configuration.

### Let's Encrypt certificate issues

1. Check DNS configuration:
   ```bash
   nslookup catcam.yourdomain.com
   ```

2. Check letsencrypt-companion logs:
   ```bash
   docker-compose logs letsencrypt-companion
   ```

3. Ensure ports 80 and 443 are accessible from the internet

### Connection refused

1. Check if services are running:
   ```bash
   docker-compose ps
   ```

2. Check application logs:
   ```bash
   docker-compose logs catcam-web
   ```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

See [LICENSE](LICENSE) file for details.

