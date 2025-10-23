# Administrator Guide

## Overview

This guide covers advanced configuration, deployment strategies, security considerations, and maintenance procedures for Docker.Dotnet.UI administrators.

## Production Deployment

### System Requirements

#### Minimum Production Requirements
- **CPU**: 2 cores
- **RAM**: 2GB
- **Storage**: 10GB available space
- **Network**: Stable internet connection for image pulls
- **Docker**: Version 20.10+ recommended

#### Recommended Production Setup
- **CPU**: 4+ cores
- **RAM**: 8GB+
- **Storage**: 50GB+ SSD
- **Load Balancer**: Nginx/Apache for SSL termination
- **Monitoring**: Docker stats collection enabled

### Docker Compose Production Configuration

Create a production-ready `docker-compose.yml`:

```yaml
version: '3.8'

services:
  docker-dotnet-ui:
    image: ghcr.io/fengb3/docker-dotnet-ui:latest
    container_name: docker-dotnet-ui-prod
    restart: unless-stopped
    
    environment:
      # Application Configuration
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:443;http://+:80
      
      # Database Configuration
      - ConnectionStrings__DefaultConnection=Data Source=/data/app.db
      
      # Security Configuration
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/certs/app.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
      
      # Logging Configuration
      - Logging__LogLevel__Default=Information
      - Logging__LogLevel__Microsoft=Warning
      
    ports:
      - "443:443"
      - "80:80"
      
    volumes:
      # Docker Socket (Required)
      - /var/run/docker.sock:/var/run/docker.sock:ro
      
      # Persistent Data
      - docker-ui-data:/data
      - ./certs:/certs:ro
      
      # Logs
      - docker-ui-logs:/app/logs
    
    networks:
      - docker-ui-network
    
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

volumes:
  docker-ui-data:
    driver: local
  docker-ui-logs:
    driver: local

networks:
  docker-ui-network:
    driver: bridge
```

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Development` | No |
| `ASPNETCORE_URLS` | Listening URLs | `http://+:80` | No |
| `ConnectionStrings__DefaultConnection` | Database connection | SQLite | No |
| `DOCKER_HOST` | Docker daemon URL | Auto-detect | No |
| `Logging__LogLevel__Default` | Log level | `Information` | No |

### SSL/HTTPS Configuration

#### Option 1: Self-Signed Certificate
```bash
# Generate self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout app.key -out app.crt -days 365 -nodes
openssl pkcs12 -export -out app.pfx -inkey app.key -in app.crt
```

#### Option 2: Let's Encrypt with Nginx Proxy
```nginx
server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;
    
    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

## Security Configuration

### Access Control

#### Network Security
```yaml
# Restrict access to specific networks
networks:
  docker-ui-network:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
          ip_range: 172.20.240.0/20
```

#### Firewall Rules (iptables)
```bash
# Allow only specific IPs to access port 8080
iptables -A INPUT -p tcp --dport 8080 -s 192.168.1.0/24 -j ACCEPT
iptables -A INPUT -p tcp --dport 8080 -j DROP

# Allow HTTPS traffic
iptables -A INPUT -p tcp --dport 443 -j ACCEPT
```

### User Management

#### Default Administrator
- **Username**: `admin`
- **Password**: `Test123!`
- **⚠️ Change immediately after installation**

#### Password Policy (Future Enhancement)
Recommended password requirements:
- Minimum 12 characters
- Mix of uppercase, lowercase, numbers, symbols
- No dictionary words
- Regular rotation (90 days)

### Docker Socket Security

#### Read-Only Access (Recommended)
```yaml
volumes:
  - /var/run/docker.sock:/var/run/docker.sock:ro
```

#### Socket Proxy (Enhanced Security)
Use a socket proxy to limit Docker API access:

```yaml
services:
  docker-socket-proxy:
    image: tecnativa/docker-socket-proxy
    environment:
      - CONTAINERS=1
      - IMAGES=1
      - VOLUMES=1
      - NETWORKS=1
      - POST=1
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
    
  docker-dotnet-ui:
    depends_on:
      - docker-socket-proxy
    environment:
      - DOCKER_HOST=tcp://docker-socket-proxy:2375
```

## Database Management

### SQLite (Default)

#### Backup Strategy
```bash
# Automated backup script
#!/bin/bash
BACKUP_DIR="/backups/docker-ui"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup database
docker exec docker-dotnet-ui-prod cp /data/app.db /tmp/app_backup.db
docker cp docker-dotnet-ui-prod:/tmp/app_backup.db $BACKUP_DIR/app_$DATE.db

# Keep only last 30 backups
find $BACKUP_DIR -name "app_*.db" -mtime +30 -delete

echo "Backup completed: app_$DATE.db"
```

#### Database Migration
```bash
# Update database schema
docker exec docker-dotnet-ui-prod dotnet ef database update
```

### PostgreSQL (Advanced Setup)

For larger deployments, consider PostgreSQL:

```yaml
services:
  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=dockerui
      - POSTGRES_USER=dockerui
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres-data:/var/lib/postgresql/data
    restart: unless-stopped
  
  docker-dotnet-ui:
    depends_on:
      - postgres
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=dockerui;Username=dockerui;Password=${DB_PASSWORD}
```

## Monitoring and Logging

### Application Logs

#### Log Configuration
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Docker": "Information"
    },
    "File": {
      "Path": "/app/logs/app-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  }
}
```

#### Log Analysis
```bash
# Monitor real-time logs
docker logs -f docker-dotnet-ui-prod

# Search for errors
docker logs docker-dotnet-ui-prod 2>&1 | grep ERROR

# Export logs for analysis
docker logs docker-dotnet-ui-prod > app_logs_$(date +%Y%m%d).log
```

### Performance Monitoring

#### Docker Stats Integration
Monitor container resource usage:

```bash
# Create monitoring script
#!/bin/bash
docker stats docker-dotnet-ui-prod --no-stream >> /var/log/docker-ui-stats.log
```

#### Health Checks
```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:80/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

### External Monitoring

#### Prometheus Integration (Future)
```yaml
services:
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml
```

## Backup and Recovery

### Complete Backup Strategy

#### Full System Backup
```bash
#!/bin/bash
BACKUP_BASE="/backups/docker-ui"
DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_DIR="$BACKUP_BASE/full_$DATE"

mkdir -p $BACKUP_DIR

# 1. Stop application (optional)
docker-compose stop docker-dotnet-ui

# 2. Backup database
docker cp docker-dotnet-ui-prod:/data/app.db $BACKUP_DIR/

# 3. Backup configuration
cp docker-compose.yml $BACKUP_DIR/
cp -r certs $BACKUP_DIR/

# 4. Backup volumes
docker run --rm -v docker-ui-data:/data -v $BACKUP_DIR:/backup alpine tar czf /backup/volumes.tar.gz /data

# 5. Restart application
docker-compose start docker-dotnet-ui

# 6. Create manifest
echo "Backup created: $DATE" > $BACKUP_DIR/manifest.txt
echo "Database: app.db" >> $BACKUP_DIR/manifest.txt
echo "Configuration: docker-compose.yml" >> $BACKUP_DIR/manifest.txt
echo "Volumes: volumes.tar.gz" >> $BACKUP_DIR/manifest.txt
```

#### Recovery Procedure
```bash
#!/bin/bash
BACKUP_DIR="/backups/docker-ui/full_20251023_140000"

# 1. Stop current application
docker-compose down

# 2. Restore database
docker cp $BACKUP_DIR/app.db docker-dotnet-ui-prod:/data/

# 3. Restore configuration
cp $BACKUP_DIR/docker-compose.yml ./

# 4. Restore volumes
docker run --rm -v docker-ui-data:/data -v $BACKUP_DIR:/backup alpine tar xzf /backup/volumes.tar.gz -C /

# 5. Start application
docker-compose up -d
```

### Automated Backup Schedule

#### Cron Job Setup
```bash
# Add to crontab (crontab -e)
# Daily backup at 2 AM
0 2 * * * /opt/docker-ui/scripts/backup.sh

# Weekly full backup on Sunday at 1 AM
0 1 * * 0 /opt/docker-ui/scripts/full_backup.sh
```

## Scaling and High Availability

### Load Balancing

#### Nginx Configuration
```nginx
upstream docker_ui_backend {
    server docker-ui-1:80;
    server docker-ui-2:80;
    # Add more instances as needed
}

server {
    listen 443 ssl;
    server_name docker-ui.yourdomain.com;
    
    location / {
        proxy_pass http://docker_ui_backend;
        # Session affinity for SignalR
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header Host $host;
        
        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

### Multi-Instance Deployment
```yaml
# docker-compose.scale.yml
version: '3.8'

services:
  docker-dotnet-ui:
    image: ghcr.io/fengb3/docker-dotnet-ui:latest
    deploy:
      replicas: 3
    environment:
      - ASPNETCORE_URLS=http://+:80
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - shared-data:/data
    networks:
      - docker-ui-network

volumes:
  shared-data:
    driver: nfs
    driver_opts:
      type: nfs
      o: addr=nfs-server,rw
      device: ":/path/to/shared/storage"
```

## Troubleshooting

### Common Issues

#### Issue: Application Won't Start
```bash
# Check container status
docker ps -a | grep docker-dotnet-ui

# Check logs
docker logs docker-dotnet-ui-prod

# Check port conflicts
netstat -tulpn | grep :8080
```

#### Issue: Cannot Connect to Docker
```bash
# Check Docker daemon
systemctl status docker

# Check socket permissions
ls -la /var/run/docker.sock

# Test Docker API
curl --unix-socket /var/run/docker.sock http://localhost/version
```

#### Issue: Database Connection Errors
```bash
# Check database file permissions
docker exec docker-dotnet-ui-prod ls -la /data/

# Test database connection
docker exec docker-dotnet-ui-prod sqlite3 /data/app.db ".tables"
```

### Performance Optimization

#### Container Resource Limits
```yaml
services:
  docker-dotnet-ui:
    deploy:
      resources:
        limits:
          cpus: '2.0'
          memory: 2G
        reservations:
          cpus: '0.5'
          memory: 512M
```

#### Database Optimization
```bash
# SQLite optimization
docker exec docker-dotnet-ui-prod sqlite3 /data/app.db "VACUUM;"
docker exec docker-dotnet-ui-prod sqlite3 /data/app.db "ANALYZE;"
```

### Log Analysis

#### Common Log Patterns
```bash
# Database errors
docker logs docker-dotnet-ui-prod 2>&1 | grep -i "database\|sqlite"

# Docker API errors
docker logs docker-dotnet-ui-prod 2>&1 | grep -i "docker.*error"

# Authentication issues
docker logs docker-dotnet-ui-prod 2>&1 | grep -i "auth\|login"
```

## Maintenance Procedures

### Regular Maintenance Tasks

#### Weekly Tasks
- [ ] Review application logs for errors
- [ ] Check disk space usage
- [ ] Verify backup integrity
- [ ] Update system packages

#### Monthly Tasks
- [ ] Update Docker.Dotnet.UI to latest version
- [ ] Review user access logs
- [ ] Clean up old backups
- [ ] Performance review

#### Quarterly Tasks
- [ ] Security audit
- [ ] Disaster recovery test
- [ ] Capacity planning review
- [ ] Documentation updates

### Update Procedures

#### Application Updates
```bash
# 1. Backup current setup
./scripts/backup.sh

# 2. Pull latest image
docker pull ghcr.io/fengb3/docker-dotnet-ui:latest

# 3. Update with zero downtime
docker-compose up -d --no-deps docker-dotnet-ui

# 4. Verify update
curl -I http://localhost:8080/health
```

#### Database Migrations
```bash
# Run pending migrations
docker exec docker-dotnet-ui-prod dotnet ef database update

# Verify migration
docker exec docker-dotnet-ui-prod dotnet ef migrations list
```

## Security Hardening

### Container Security
```dockerfile
# Example security-hardened Dockerfile additions
USER 1001:1001
WORKDIR /app
COPY --chown=1001:1001 . .

# Remove unnecessary packages
RUN apt-get remove -y wget curl && \
    apt-get autoremove -y && \
    rm -rf /var/lib/apt/lists/*
```

### Network Security
```yaml
# Restrict container capabilities
security_opt:
  - no-new-privileges:true
cap_drop:
  - ALL
cap_add:
  - CHOWN
  - SETGID
  - SETUID
```

### Audit Logging
```bash
# Enable Docker daemon audit logging
echo 'DOCKER_OPTS="--log-level=warn --log-opt max-size=10m --log-opt max-file=3"' >> /etc/default/docker
```

## Support and Escalation

### Getting Help
1. **Documentation**: Check this guide and other docs
2. **Logs**: Collect relevant log files
3. **GitHub Issues**: Report bugs with details
4. **Community**: Ask questions in discussions

### Bug Report Template
When reporting issues, include:
- Application version
- Docker version
- Operating system
- Error messages
- Steps to reproduce
- Expected vs actual behavior

---

**Document Version**: 1.0  
**Last Updated**: October 23, 2025  
**Next Review**: January 23, 2026