# Architecture Overview

## System Architecture

Docker.Dotnet.UI is a **Blazor Server** application that provides a web-based interface for managing Docker containers, images, volumes, and networks. The application follows a strict **MVVM (Model-View-ViewModel)** pattern with dependency injection and event-driven updates.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Docker.Dotnet.UI                         │
│                   (Blazor Server App)                       │
├─────────────────────────────────────────────────────────────┤
│  Web Browser (Client)                                       │
│  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────┐  │
│  │   Razor Pages   │ │   MudBlazor     │ │   SignalR     │  │
│  │   (.razor)      │ │   Components    │ │   (Future)    │  │
│  └─────────────────┘ └─────────────────┘ └───────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  Server-Side Application                                    │
│  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────┐  │
│  │   ViewModels    │ │   Services      │ │  Controllers  │  │
│  │   (Business     │ │   (Localization,│ │  (Account)    │  │
│  │    Logic)       │ │   User Prefs)   │ │               │  │
│  └─────────────────┘ └─────────────────┘ └───────────────┘  │
│  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────┐  │
│  │  Source Gen     │ │   Database      │ │  Middleware   │  │
│  │  (CSV Tables)   │ │  (SQLite/EF)    │ │  (Auth, etc.) │  │
│  └─────────────────┘ └─────────────────┘ └───────────────┘  │
├─────────────────────────────────────────────────────────────┤
│  External Dependencies                                      │
│  ┌─────────────────┐ ┌─────────────────┐ ┌───────────────┐  │
│  │  Docker.DotNet  │ │   Docker Engine │ │   File System │  │
│  │     SDK         │ │   (via Socket)  │ │   (Uploads)   │  │
│  └─────────────────┘ └─────────────────┘ └───────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Presentation Layer (Razor Components)

**Location**: `Components/Pages/`

- **MyComponentBase<T>**: Base class for all pages, provides ViewModel injection and lifecycle
- **Dashboard**: System overview and quick navigation
- **DockerPages/**: Container, Image, Volume, Network management pages
- **AccountPages/**: Authentication and user management

**Key Patterns**:
- All pages inherit from `MyComponentBase<TViewModel>`
- Minimal logic in Razor files - delegates to ViewModels
- Uses `@Localizer["KEY"]` for all user-facing text
- MudBlazor components for consistent UI

### 2. Business Logic Layer (ViewModels)

**Location**: `ViewModels/`

**Base Classes**:
- `IViewModel`: Interface defining lifecycle and state change events
- `ViewModel`: Base implementation with `NotifyStateChanged()`

**Key ViewModels**:
- `DashboardViewModel`: System information and resource counts
- `ContainersPageViewModel`: Container management and monitoring
- `ImagesPageViewModel`: Image operations and registry interaction
- `VolumesPageViewModel`: Volume creation and management
- `NetworksPageViewModel`: Network creation and management

**Features**:
- Dependency injection via `[RegisterScoped]` attributes
- Async initialization pattern
- Event-driven UI updates
- Direct Docker.DotNet SDK usage
- Error handling and user feedback

### 3. Data Access Layer

#### Docker Integration
- **Docker.DotNet SDK**: Direct communication with Docker Engine
- **Socket Detection**: Auto-detects Unix socket (Linux/Mac) vs Named Pipe (Windows)
- **ViewModelMapper**: Mapperly-generated mappings between Docker DTOs and ViewModels

#### Database (ASP.NET Core Identity)
- **SQLite**: Default database for user accounts and preferences
- **Entity Framework Core**: ORM with automatic migrations
- **ApplicationDbContext**: Custom context with User and Role entities

### 4. Services Layer

**Location**: `Services/`

- **MyLocalizer**: CSV-based localization system
- **UserPreferencesService**: User settings and preferences
- **DbInitializer**: Database seeding and default user creation

### 5. Source Generation

**Location**: `Docker.Dotnet.UI.SourceGenerator/`

- **CSVImmutableDataSourceGenerator**: Converts `*.table.csv` files to C# classes
- **Localization Tables**: Immutable data structures for i18n
- **Build-time Generation**: Integrated into MSBuild process

## MVVM Pattern Implementation

### Strict Separation of Concerns

```csharp
// ✅ Correct: Razor delegates to ViewModel
@inherits MyComponentBase<ContainersPageViewModel>

<MudButton OnClick="@(() => Vm!.StartContainerAsync(container.ID))">
    @Localizer["START"]
</MudButton>

// ❌ Incorrect: Business logic in Razor
@code {
    private async Task StartContainer(string id) 
    {
        // This belongs in ViewModel!
        await dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters());
    }
}
```

### ViewModel Lifecycle

1. **Injection**: ViewModels auto-registered via `[RegisterScoped]` attribute
2. **Initialization**: `InitializeAsync()` called by `MyComponentBase` on first render
3. **State Changes**: `NotifyStateChanged()` triggers UI refresh
4. **Disposal**: Automatic cleanup via DI container

### Event-Driven Updates

```csharp
[RegisterScoped(typeof(ContainersPageViewModel))]
public class ContainersPageViewModel : ViewModel
{
    public async Task StartContainerAsync(string id)
    {
        // Business logic
        await dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters());
        
        // Trigger UI refresh
        await RefreshContainersAsync();
        NotifyStateChanged();
    }
}
```

## Dependency Injection Architecture

### Auto-Registration System

```csharp
// Attribute-based registration
[RegisterScoped(typeof(ContainersPageViewModel))]
public class ContainersPageViewModel : ViewModel { }

// Auto-discovery in Program.cs
services.AutoRegister();
```

### Service Lifetimes
- **Singleton**: `DockerClient`, configuration services
- **Scoped**: ViewModels, localization services (per-user session)
- **Transient**: Utility services, short-lived operations

## Localization System

### CSV-Based Source Generation

```csv
[Key]resource-key:string,en-us:string,zh-cn:string,fr-fr:string,ja:string,ko-kr:string,es:string,description:string
DOCKER_CONTAINERS,Docker Containers,Docker 容器,Conteneurs Docker,Docker コンテナ,Docker 컨테이너,Contenedores Docker,Page title for container management
```

### Generated Code Access

```csharp
// Generated at build time
var localizedText = ImmutableTables.Localization.Items["DOCKER_CONTAINERS"];

// Runtime access via IStringLocalizer
@Localizer["DOCKER_CONTAINERS"]
```

## Security Architecture

### Authentication Flow

1. **ASP.NET Core Identity**: Cookie-based authentication
2. **Authorization Attributes**: `[Authorize]` on all pages except login
3. **Default User**: `admin` / `Test123!` created on first run
4. **Session Management**: 7-day cookie expiration

### Docker Security

- **Socket Permissions**: Requires Docker socket access
- **Container Isolation**: No direct host access from containers
- **Force Parameters**: Explicit `Force=true` for destructive operations

## Performance Considerations

### Blazor Server Optimizations
- **Event-Driven Updates**: Minimal re-rendering via `NotifyStateChanged()`
- **Scoped ViewModels**: Efficient memory usage per user session
- **Async Operations**: Non-blocking Docker API calls

### Docker API Efficiency
- **Connection Reuse**: Single `DockerClient` instance
- **Streaming Operations**: Large data (logs, exports) handled as streams
- **Bulk Operations**: Batch processing for multiple containers/images

## Error Handling Strategy

### Layered Error Handling

1. **Docker API Errors**: Caught in ViewModels, user-friendly messages
2. **Validation Errors**: Client-side validation with server backup
3. **System Errors**: Global exception handling with logging
4. **User Feedback**: Consistent error display via MudBlazor alerts

### Example Error Flow

```csharp
public async Task StartContainerAsync(string id)
{
    try
    {
        await dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters());
        SuccessMessage = Localizer["CONTAINER_STARTED_SUCCESSFULLY"];
    }
    catch (DockerApiException ex)
    {
        ErrorMessage = Localizer["FAILED_TO_START_CONTAINER", ex.Message];
    }
    finally
    {
        NotifyStateChanged();
    }
}
```

## Extensibility Points

### Adding New Docker Resource Types

1. Create ViewModel with `[RegisterScoped]` attribute
2. Add Razor page inheriting `MyComponentBase<TViewModel>`
3. Add navigation menu item
4. Add localization keys to CSV

### Adding New Features

1. **Services**: Add to `Services/` with appropriate DI registration
2. **API Endpoints**: Add controllers for REST API (v0.2.0+)
3. **Background Tasks**: Use hosted services for monitoring/cleanup
4. **UI Components**: Create reusable MudBlazor components

## Development Workflow

### Build Process

1. **Source Generation**: CSV files → C# classes
2. **Mapperly Generation**: DTO mappings
3. **Blazor Compilation**: Razor → C#
4. **Standard .NET Build**: IL generation

### Testing Strategy

- **Unit Tests**: ViewModel business logic
- **Integration Tests**: Docker API interactions
- **UI Tests**: Blazor component testing
- **E2E Tests**: Full user workflows

---

**Document Version**: 1.0  
**Last Updated**: October 23, 2025  
**Architecture Review**: Quarterly