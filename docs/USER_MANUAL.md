# User Manual

## Getting Started

Welcome to Docker.Dotnet.UI - a modern web-based interface for managing Docker containers, images, volumes, and networks. This guide will help you navigate and use all the features effectively.

## Login and Authentication

### First Time Login
1. Open your browser and navigate to the application URL (typically http://localhost:8080)
2. Use the default credentials:
   - **Username**: `admin`
   - **Password**: `Test123!`
3. **Important**: Change your password immediately after first login

### Changing Your Password
*Note: User management features may be added in future versions*

## Dashboard Overview

The Dashboard is your home base, providing a quick overview of your Docker environment.

### System Information
- **Docker Version**: Shows your Docker engine version
- **System Details**: Operating system, architecture, CPU count, and memory
- **Resource Counts**: Total numbers of containers, images, volumes, and networks

### Container Status Distribution
- Visual progress bars showing:
  - Running containers (green)
  - Stopped containers (red)
  - Paused containers (yellow)

### Quick Navigation
Click on any resource card to jump directly to the management page for that resource type.

## Container Management

Navigate to **Containers** from the main menu to manage your Docker containers.

### Viewing Containers
- **List View**: See all containers with status, image, ports, and creation time
- **Status Icons**: Visual indicators for running (green), stopped (red), paused (yellow)
- **Real-time Updates**: Lists refresh automatically after operations

### Container Operations

#### Starting and Stopping
- **Start**: Click the play button to start a stopped container
- **Stop**: Click the stop button to gracefully stop a running container
- **Restart**: Use the restart button to stop and start a container

#### Advanced Operations
- **Pause/Unpause**: Temporarily freeze/unfreeze a running container
- **View Logs**: Click "Logs" to see the last 500 lines of container output
- **Terminal**: Open an interactive shell inside a running container
- **Inspect**: View complete container details in JSON format
- **Delete**: Remove containers (stopped containers only)

#### Container Terminal (Exec)
The terminal feature allows you to access an interactive shell inside running containers directly from your browser.

**Features:**
- **Dialog Mode**: Quick terminal access from the container list
- **Full-Screen Mode**: Dedicated page for extended sessions
- **Auto-Detection**: Automatically selects the appropriate shell (sh, bash, or powershell)
- **Real-Time I/O**: Bidirectional streaming for command execution and output

**How to Use:**
1. Ensure the container is in "running" state (terminal button is enabled only for running containers)
2. Click the **Terminal** button (terminal icon) in the container actions
3. A dialog opens showing:
   - Connection status (Connecting/Connected/Disconnected)
   - Terminal output area with monospace font on dark background
   - Command input field at the bottom
4. Type commands and press Enter to execute
5. Use the **Clear** button to clear terminal output
6. Use the **Disconnect** button to close the exec session
7. Click **Open in New Page** link for full-screen terminal mode

**Full-Screen Mode:**
- Access via the link in the dialog or directly at `/containers/{id}/exec`
- Larger terminal display for extended work
- Back button returns to containers list
- Same functionality as dialog mode

**Supported Shells:**
- Linux containers: `/bin/sh` (default), `/bin/bash` (if available)
- Windows containers: `powershell.exe`, `cmd.exe`

**Notes:**
- Terminal is only available for running containers
- If the container stops during a session, you'll see an error message
- Click **Reconnect** to start a new session if disconnected
- Maximum output buffer: 5,000 lines (older output auto-removed)

#### Container Logs
- Shows the last 500 lines of container output
- Useful for debugging and monitoring
- Scroll through logs in the dialog window

#### Container Inspection
- Complete technical details about the container
- JSON format with all Docker metadata
- Includes network settings, volumes, environment variables

### Batch Operations
- Select multiple containers using checkboxes
- Perform bulk operations like start, stop, or delete
- Operations are performed sequentially with individual feedback

### Search and Filtering
- **Search Bar**: Filter containers by name, image, or ID
- **Status Filter**: Show only running, stopped, or paused containers
- **Real-time Filtering**: Results update as you type

## Image Management

Navigate to **Images** to manage your Docker images.

### Viewing Images
- **List View**: All images with repository, tag, size, and creation date
- **Multi-tag Support**: Images with multiple tags are clearly displayed
- **Size Information**: Human-readable file sizes

### Image Operations

#### Pulling Images
- **Pull from Registry**: Download images from Docker Hub or other registries
- **Tag Specification**: Specify exact tags or use 'latest'
- **Progress Feedback**: Visual indication during download

#### Image Export/Import
- **Export**: Download images as tar files to your computer
- **Import**: Upload tar files to create new images
- **Stream Processing**: Efficient handling of large image files

#### Image Management
- **Delete**: Remove unused images to free up space
- **Inspect**: View detailed image information and layers
- **Refresh**: Update the image list manually

### Batch Operations
- Select multiple images for bulk deletion
- Confirm operations before execution
- Individual operation status feedback

## Volume Management

Navigate to **Volumes** to manage Docker volumes.

### Viewing Volumes
- **List View**: All volumes with driver, mount point, and creation date
- **Usage Status**: See which volumes are currently in use

### Volume Operations

#### Creating Volumes
- **Name**: Specify a unique volume name
- **Driver**: Choose storage driver (local is default)
- **Advanced Options**: Configure driver-specific settings

#### Volume Management
- **Inspect**: View volume details and configuration
- **Delete**: Remove unused volumes
- **Prune**: Bulk remove all unused volumes (with confirmation)

#### Prune Operations
- **Safety Check**: Confirmation dialog before deletion
- **Batch Cleanup**: Remove all unused volumes at once
- **Preserve Active**: Only unused volumes are removed

## Network Management

Navigate to **Networks** to manage Docker networks.

### Viewing Networks
- **List View**: All networks with name, driver, and scope
- **System Protection**: System networks are clearly marked

### Network Operations

#### Creating Networks
- **Name**: Specify network name
- **Driver Types**:
  - `bridge`: Standard isolated network
  - `host`: Use host networking
  - `overlay`: Multi-host networking
  - `macvlan`: MAC address assignment
  - `none`: No networking

#### Network Management
- **Inspect**: View network configuration and connected containers
- **Delete**: Remove user-created networks
- **System Protection**: Built-in networks (bridge, host, none) cannot be deleted

### Safety Features
- System networks are protected from accidental deletion
- Clear visual indicators for system vs. user networks
- Confirmation dialogs for destructive operations

## Language Support

Docker.Dotnet.UI supports multiple languages:

### Available Languages
- **English** (en-us)
- **中文简体** (zh-cn)
- **Français** (fr-fr)
- **日本語** (ja)
- **한국어** (ko-kr)
- **Español** (es)

### Changing Language
1. Look for the language selector in the navigation bar
2. Select your preferred language
3. The interface updates immediately
4. Your preference is saved for future sessions

## Search and Filtering

### Global Search Features
All list pages support powerful search and filtering:

#### Search Capabilities
- **Name Search**: Find resources by name
- **ID Search**: Search by Docker ID (partial matches work)
- **Status Filtering**: Filter by operational status
- **Real-time Results**: Instant filtering as you type

#### Advanced Filters
- **Container Status**: Running, stopped, paused
- **Image Tags**: Filter by repository or tag patterns
- **Volume Drivers**: Filter by storage driver type
- **Network Drivers**: Filter by network type

## Keyboard Shortcuts

### General Navigation
- **Ctrl+1**: Dashboard
- **Ctrl+2**: Containers
- **Ctrl+3**: Images
- **Ctrl+4**: Volumes
- **Ctrl+5**: Networks

### Page Actions
- **F5**: Refresh current page
- **Ctrl+F**: Focus search box (where available)
- **Esc**: Close dialog boxes

## Tips and Best Practices

### Container Management
- ✅ Regularly check container logs for issues
- ✅ Use meaningful container names
- ✅ Stop containers before deleting
- ⚠️ Be careful with force operations

### Image Management
- ✅ Remove unused images regularly to save space
- ✅ Tag images meaningfully
- ✅ Use specific tags instead of 'latest' for production
- ⚠️ Large image operations may take time

### Volume Management
- ✅ Name volumes descriptively
- ✅ Regular cleanup of unused volumes
- ⚠️ Deleting volumes removes all data permanently
- ⚠️ Always backup important data

### Network Management
- ✅ Use custom networks for container isolation
- ✅ Document network purposes
- ⚠️ Don't delete system networks
- ⚠️ Check container connections before deleting networks

### Performance Tips
- 🚀 Use search/filter to manage large lists
- 🚀 Refresh pages after bulk operations
- 🚀 Monitor system resources on the dashboard
- 🚀 Close unused dialog windows

### Security Reminders
- 🔒 Change default passwords immediately
- 🔒 Review container configurations regularly
- 🔒 Limit access to sensitive containers
- 🔒 Monitor logs for suspicious activity

## Troubleshooting Common Issues

### Dashboard Shows "Cannot Connect to Docker"
- Ensure Docker daemon is running
- Check Docker socket permissions
- Restart the application if needed

### Operations Fail with Permission Errors
- Verify Docker socket access
- Check user permissions
- Ensure containers are in correct state for operation

### Slow Performance
- Too many containers/images - use search/filtering
- Check system resources on dashboard
- Consider cleanup of unused resources

### Language Not Displaying Correctly
- Try refreshing the page
- Check browser language settings
- Contact support if text appears as keys (e.g., "CONTAINER_NAME")

## What's New in This Version

### Recent Features (v0.1.0)
- ✨ **Real-time Monitoring**: Live container statistics
- ✨ **Enhanced Search**: Powerful filtering across all resources
- ✨ **Batch Operations**: Multi-select and bulk actions
- ✨ **Improved UI**: Modern MudBlazor components
- ✨ **Better Performance**: Stream processing for large operations

### Upcoming Features (v0.2.0)
- 🔄 **Docker Compose Support**: Manage multi-container applications
- 🖥️ **Container Terminal**: Execute commands in containers
- 🏗️ **Image Building**: Build images from Dockerfiles
- 👥 **User Management**: Advanced user roles and permissions

## Getting Help

### Built-in Help
- Hover over buttons for tooltips
- Look for info icons next to complex features
- Status messages provide operation feedback

### Documentation
- **Installation Guide**: [INSTALLATION.md](INSTALLATION.md)
- **Administrator Guide**: [ADMIN_GUIDE.md](ADMIN_GUIDE.md)
- **Troubleshooting**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

### Community Support
- **GitHub Issues**: Report bugs or request features
- **Discussions**: Ask questions and share experiences
- **Wiki**: Community-contributed guides and tips

---

**Version**: 0.1.0  
**Last Updated**: October 23, 2025  
**Documentation**: [https://github.com/fengb3/Docker.Dotnet.UI/docs](https://github.com/fengb3/Docker.Dotnet.UI/tree/master/docs)