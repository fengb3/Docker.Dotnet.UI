# Docker.Dotnet.UI

A modern Blazor Server web UI for managing Docker: containers, images, volumes and networks. It provides a full-featured web interface with multi-language support and live monitoring.

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)]()
[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)]()
[![Docker](https://img.shields.io/badge/docker-ready-blue.svg)]()
[![.NET](https://img.shields.io/badge/.NET-9.0-purple.svg)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)]()

## 📋 Contents

- [Quick Start](#quick-start)
- [Features](#features)
- [Installation](#installation)
- [Documentation](#documentation)
- [Development](#development)
- [Roadmap](#roadmap)

## Requirements

- Docker Desktop (Windows/macOS) or Docker Engine (Linux)
- .NET 9.0 SDK (for local development)

## Quick Start

### Run locally (simplest)

```bash
cd Docker.Dotnet.UI
dotnet run
```

Then open your browser at: https://localhost:7150 or http://localhost:5149

### Run with Docker Compose

```bash
# Build and start
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

Access the app at: http://localhost:8080

## Features

### Dashboard 📊
- System overview (Docker version, OS, architecture, memory, CPU)
- Resource counts (containers, images, volumes, networks)
- Container status distribution (running / stopped / paused)
- Quick navigation to resource pages

### Container Management 📦
- List all containers (running/stopped)
- Start, stop, restart containers
- Pause / unpause containers
- View container logs
- Inspect container details
- Remove containers

### Image Management 🖼️
- List images
- Pull images from registries
- Load images from tar files
- Export images to tar
- Inspect image details
- Remove images

### Volume Management 💾
- List Docker volumes
- Create volumes (multiple drivers supported)
- Inspect volume details
- Prune unused volumes
- Bulk delete volumes
- Search/filter by name or driver

### Network Management 🌐
- List Docker networks
- Create networks (bridge, host, overlay, macvlan, none)
- Inspect network details
- Delete user-created networks (system networks are protected)
- Bulk delete networks
- Search/filter by name or driver

### Advanced
- Real-time monitoring: container CPU/memory metrics
- Global search and filtering across resource pages
- Bulk operations: multi-select start/stop/delete
- Enhanced image workflows: pull from registry and load from tar
- Modern UI built with MudBlazor
- Multi-language localization (6 languages)
- Authentication via ASP.NET Core Identity
- Event-driven UI updates for responsive UX

## Installation

### Prerequisites
- Docker Desktop (Windows/macOS) or Docker Engine (Linux)
- .NET 9.0 SDK (for local development)

See the full installation instructions: **[Installation Guide](docs/INSTALLATION.md)**

## Documentation

### User Docs
- **[Installation Guide](docs/INSTALLATION.md)** - Deployment and installation
- **[User Manual](docs/USER_MANUAL.md)** - How to use the web UI
- **[Admin Guide](docs/ADMIN_GUIDE.md)** - Configuration and administration

### Technical Docs
- **[Architecture Overview](docs/ARCHITECTURE.md)** - System design and MVVM pattern
- **[Authentication](docs/AUTHENTICATION.md)** - Login and security details
- **[Source Generator README](docs/SOURCEGENERATOR_README.md)** - CSV-to-code generation

### Project Info
- **[Acceptance Status](docs/ACCEPTANCE_STATUS.md)** - v0.1.0 acceptance report
- **[v0.2.0 Roadmap](docs/ROADMAP_v0.2.0.md)** - Next-version plan
- **[Enhancements](docs/ENHANCEMENTS.md)** - Development history
- **[API](docs/API.md)** - Future API plans (currently Web UI only)

## Development

### Run locally
```bash
# Clone
git clone https://github.com/fengb3/Docker.Dotnet.UI.git
cd Docker.Dotnet.UI

# Run
cd Docker.Dotnet.UI
dotnet run
```

Open: https://localhost:7150

### Contributing
Pull requests welcome. See architecture docs for development guidelines.

## Roadmap

### Current version: v0.1.0 ✅
- Full Docker resource management (containers/images/volumes/networks)
- Real-time monitoring and bulk operations
- Multi-language localization
- Production-ready

### Next version: v0.2.0 🚀 (planned Q1 2026)
- Docker Compose management
- Container terminal access (exec)
- REST API for programmatic access
- Image build from Dockerfile
- Enhanced monitoring with historical charts and alerts

See details: **[v0.2.0 Roadmap](docs/ROADMAP_v0.2.0.md)**

## License

MIT License — see [LICENSE](LICENSE)

---

If this project is helpful, please give it a star! ⭐
