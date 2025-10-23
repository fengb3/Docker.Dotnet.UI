# Installation Guide

## Prerequisites

### System Requirements
- **Operating System**: Windows 10/11, macOS 10.15+, or Linux (Ubuntu 20.04+)
- **Docker**: Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- **Memory**: Minimum 4GB RAM, 8GB recommended
- **Storage**: 2GB free space for application and Docker images

### Software Dependencies
- **.NET 9.0 SDK** (for development) - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker**: 
  - Windows/Mac: [Docker Desktop](https://www.docker.com/products/docker-desktop)
  - Linux: [Docker Engine](https://docs.docker.com/engine/install/)

## Installation Methods

### Method 1: Docker Compose (Recommended for Production)

1. **Download the docker-compose.yml file**:
   ```bash
   curl -O https://raw.githubusercontent.com/fengb3/Docker.Dotnet.UI/master/docker-compose.yml
   ```

2. **Start the application**:
   ```bash
   docker-compose up -d
   ```

3. **Access the application**:
   - Open your browser and navigate to: http://localhost:8080
   - Default credentials: `admin` / `Test123!`

4. **View logs** (if needed):
   ```bash
   docker-compose logs -f
   ```

5. **Stop the application**:
   ```bash
   docker-compose down
   ```

### Method 2: Direct Docker Run

```bash
# Pull the latest image
docker pull ghcr.io/fengb3/docker-dotnet-ui:latest

# Run the container
docker run -d \
  --name docker-dotnet-ui \
  -p 8080:8080 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  ghcr.io/fengb3/docker-dotnet-ui:latest
```

### Method 3: Development Setup (Source Code)

1. **Clone the repository**:
   ```bash
   git clone https://github.com/fengb3/Docker.Dotnet.UI.git
   cd Docker.Dotnet.UI
   ```

2. **Install .NET 9.0 SDK** (if not already installed)

3. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

4. **Run the application**:
   ```bash
   cd Docker.Dotnet.UI
   dotnet run
   ```

5. **Access the application**:
   - HTTPS: https://localhost:7150
   - HTTP: http://localhost:5149

## Post-Installation Configuration

### 1. Change Default Password

⚠️ **Important**: Change the default admin password immediately after first login:

1. Login with `admin` / `Test123!`
2. Navigate to User Management (if available) or contact your administrator
3. Change the password to a strong, unique password

### 2. Configure HTTPS (Production)

For production deployments, configure HTTPS:

```yaml
# docker-compose.yml modifications
services:
  docker-dotnet-ui:
    environment:
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=yourpassword
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    volumes:
      - ./https:/https:ro
    ports:
      - "443:443"
      - "80:80"
```

### 3. Database Backup (Optional)

The application uses SQLite by default. To backup user data:

```bash
# Copy the database file from the container
docker cp docker-dotnet-ui:/app/app.db ./app.db.backup
```

## Verification

### Check Application Status

1. **Web Interface**: Navigate to http://localhost:8080 and verify the dashboard loads
2. **Docker Connection**: Ensure the dashboard shows your Docker system information
3. **Authentication**: Verify you can login with admin credentials
4. **Container Management**: Try listing your containers in the Containers page

### Troubleshooting Common Issues

| Issue | Solution |
|-------|----------|
| "Cannot connect to Docker" | Ensure Docker daemon is running and socket is accessible |
| "Application won't start" | Check port 8080 is not in use: `netstat -an \| grep 8080` |
| "Login page won't load" | Verify container is running: `docker ps` |
| "Permission denied on Docker socket" | Add user to docker group: `sudo usermod -aG docker $USER` |

## Security Considerations

⚠️ **Production Security Checklist**:

- [ ] Change default admin password
- [ ] Enable HTTPS with valid certificates
- [ ] Restrict access to Docker socket
- [ ] Configure firewall rules
- [ ] Set up regular database backups
- [ ] Monitor application logs
- [ ] Keep Docker and application updated

## Next Steps

- Read the [User Manual](USER_MANUAL.md) to learn how to use the application
- See [Administrator Guide](ADMIN_GUIDE.md) for advanced configuration
- Check [Troubleshooting Guide](TROUBLESHOOTING.md) if you encounter issues

## Uninstallation

### Docker Compose
```bash
docker-compose down
docker-compose down --volumes  # Also remove data volumes
```

### Direct Docker
```bash
docker stop docker-dotnet-ui
docker rm docker-dotnet-ui
docker rmi ghcr.io/fengb3/docker-dotnet-ui:latest
```

## Support

- **Documentation**: [Project Wiki](https://github.com/fengb3/Docker.Dotnet.UI/wiki)
- **Issues**: [GitHub Issues](https://github.com/fengb3/Docker.Dotnet.UI/issues)
- **Discussions**: [GitHub Discussions](https://github.com/fengb3/Docker.Dotnet.UI/discussions)