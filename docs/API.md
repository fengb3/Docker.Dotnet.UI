# API Documentation

## Overview

**Note**: Docker.Dotnet.UI is currently a Blazor Server application and does **not** provide REST APIs. This document is a placeholder for future API development.

The current version (v0.1.0) is a web-based UI that communicates directly with the Docker Engine through the Docker.DotNet SDK. All functionality is accessed through the web interface.

## Future API Plans

REST APIs may be added in future versions to support:
- Programmatic access to Docker operations
- Integration with external tools
- Mobile applications
- Automation and CI/CD workflows

For now, please use the web interface at your deployment URL (typically `http://localhost:8080`).

## Current Architecture

The application uses:
- **Blazor Server** for real-time web UI
- **ASP.NET Core Identity** for authentication (web forms only)
- **Docker.DotNet SDK** for Docker Engine communication
- **SignalR** for real-time updates (future enhancement)

## Web Endpoints (Current)

The following web endpoints are available:

### Authentication
- `GET /Account/Login` - Login page
- `POST /Account/Login` - Process login
- `POST /Account/Logout` - Logout
- `GET /Account/AccessDenied` - Access denied page

### Application Pages
- `GET /` - Dashboard (requires authentication)
- `GET /containers` - Container management
- `GET /images` - Image management  
- `GET /volumes` - Volume management
- `GET /networks` - Network management

## Version History

- **v0.1.0** (Current): Blazor Server web application only
- **v0.2.0** (Planned): May include basic API endpoints
- **Future**: Full REST API with OpenAPI documentation

---

**Last Updated**: October 23, 2025  
**Status**: No APIs available - Web UI only