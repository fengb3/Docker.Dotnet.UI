# Docker.Dotnet.UI - AI Coding Agent Instructions

## Architecture Overview

This is a **Blazor Server** application using **MudBlazor** for UI and **Docker.DotNet** to manage Docker containers/images/volumes. The app follows an **MVVM pattern** where Razor pages contain minimal logic—all business logic lives in ViewModels.

### Key Components
- **Razor Pages** (`Components/Pages/`) - UI only, delegates to ViewModels
- **ViewModels** (`ViewModels/`) - Business logic, Docker API calls, state management
- **Base Component** (`MyComponentBase<T>`) - All pages inherit this; provides ViewModel injection and `Localizer`
- **Custom Localizer** (`Services/MyLocalizer.cs`) - CSV-based i18n using generated immutable tables

## Critical Patterns

### 1. MVVM Architecture (`.cursor/rules/mvvm-rule.mdc`)
**Rule**: Razor files contain minimal logic; data and logic belong in ViewModels.

```csharp
// ✅ Correct: ViewModel with business logic
[RegisterScoped(typeof(ContainersPageViewModel))]
public class ContainersPageViewModel(DockerClient dockerClient) : ViewModel
{
    public IList<ContainerListItemViewModel>? Containers { get; set; }
    public override async Task InitializeAsync() => await RefreshContainersAsync();
    public async Task StartContainerAsync(string id) { /* ... */ }
}

// ✅ Razor page delegates to ViewModel
@inherits MyComponentBase<ContainersPageViewModel>
<MudButton OnClick="@(() => Vm!.StartContainerAsync(container.ID))">Start</MudButton>
```

### Razor UI requirements (strict)
1. All UI Razor pages MUST inherit from `MyComponentBase<TViewModel>` (or a concrete subtype) so the `Vm` property, lifecycle hooks and `Localizer` are available and consistent across the app.
2. Razor pages MUST NOT contain "wrapper" methods that merely call into the ViewModel. In other words, do not add small helper methods in the `.razor` file whose only job is to forward calls to `Vm` methods. This breaks the MVVM separation and makes testing and reasoning harder.

Incorrect (wrapper method in Razor):
```razor
@inherits MyComponentBase<ContainersPageViewModel>
@code {
    // ❌ Avoid creating forwarder/wrapper methods in Razor
    private async Task OnStart(string id) => await Vm!.StartContainerAsync(id);
}
<MudButton OnClick="@(() => OnStart(container.ID))">Start</MudButton>
```

Correct (delegate directly to ViewModel):
```razor
@inherits MyComponentBase<ContainersPageViewModel>
<MudButton OnClick="@(() => Vm!.StartContainerAsync(container.ID))">Start</MudButton>
```

Why: keeping the Razor page as a light UI surface (no passthrough wrappers) preserves a single place for business logic (the ViewModel), avoids duplication, and ensures the source generators and test harnesses can rely on the same lifecycle and wiring.

### 2. Dependency Injection via Attributes
Use `[RegisterScoped]`, `[RegisterTransient]`, or `[RegisterSingleton]` attributes on classes. Call `services.AutoRegister()` in `Program.cs` to auto-discover and register.

```csharp
[RegisterScoped(typeof(ImagesPageViewModel))]  // ← Marks for DI
public class ImagesPageViewModel : ViewModel { }
```

### 3. Localization (i18n)
**All user-facing text** uses `@Localizer["KEY"]`. Translations are in `ImmutableTables/Localization.table.csv` (9 columns: key, en-us, zh-cn, fr-fr, ja, ko-kr, es, descriptions).

```razor
<MudText>@Localizer["DOCKER_CONTAINERS"]</MudText>
<MudButton title='@Localizer["STOP"]'>Stop</MudButton>
```

**CSV Format Rules**:
- Fields with commas MUST be quoted: `"e.g.: nginx, mysql, ubuntu"`
- Internal quotes escaped as `""`: `"He said ""hello"""`
- Comment lines start with `#`

### 4. Source Generators
**CSV to Code** (`Docker.Dotnet.UI.SourceGenerator`):
- Files ending in `.table.csv` in `ImmutableTables/` are auto-generated to C# classes
- Access via `ImmutableTables.Localization.Items["KEY"]`
- First column marked `[Key]resource-key:string` is the primary key
- Format: `[Key?]column-name:type`

**Mapperly** (`ViewModelMapper.cs`):
- Uses `[Mapper]` attribute for auto-generating DTO mappings
- Partial methods like `ToViewModel(this ContainerListResponse response)` are implemented by source generator

### 5. Docker Client Configuration
Auto-detects OS and uses appropriate Docker socket (`DependencyInjection.cs`):
- **Linux/Mac**: `unix:///var/run/docker.sock`
- **Windows**: `npipe://./pipe/docker_engine`
- Override with `DOCKER_HOST` environment variable

### 6. Authentication
- **ASP.NET Core Identity** with SQLite (`ApplicationDbContext`)
- Default admin: `admin` / `Test123!` (created on first run via `DbInitializer.SeedDefaultUserAsync`)
- All pages except login/logout require `[Authorize]` attribute
- Cookie-based auth configured in `Program.cs` with 7-day expiration

## Developer Workflows

### Build & Run
```powershell
cd Docker.Dotnet.UI
dotnet run   # Listens on https://localhost:7150; http://localhost:5149
```

### Docker Deployment
```bash
docker-compose up -d  # Exposes port 8080
# OR manually:
docker build -t docker-dotnet-ui:latest -f Docker.Dotnet.UI/Dockerfile .
docker run -d -p 8080:8080 -v /var/run/docker.sock:/var/run/docker.sock docker-dotnet-ui:latest
```
**Critical**: Must mount Docker socket (`-v /var/run/docker.sock:/var/run/docker.sock`) for container management.

### Database Migrations
```bash
cd Docker.Dotnet.UI
dotnet ef migrations add MigrationName
dotnet ef database update
```
Migrations auto-apply on startup (`context.Database.Migrate()` in `Program.cs`).

### Adding Localization Keys
1. Add row to `ImmutableTables/Localization.table.csv` with 9 columns
2. Ensure commas in text are quoted: `"text, with, commas"`
3. Rebuild to regenerate `Localization.g.cs`
4. Use in Razor: `@Localizer["NEW_KEY"]`

## Project-Specific Conventions

### UI Components (`.cursor/rules/mudblazor-rule.mdc`)
**Always use MudBlazor** components unless explicitly instructed otherwise:
- `<MudButton>`, `<MudTextField>`, `<MudDialog>`, `<MudAlert>`, etc.
- Color scheme: `Color.Primary`, `Color.Success`, `Color.Error`, etc.

### ViewModel Lifecycle
1. Page inherits `MyComponentBase<TViewModel>`
2. `Vm` property auto-injected
3. `Vm.InitializeAsync()` called on first render (`OnAfterRenderAsync`)
4. Razor accesses via `Vm!.PropertyOrMethod()`

### File Structure
```
Docker.Dotnet.UI/
├── Components/
│   ├── Pages/
│   │   ├── AccountPages/     # Login, Logout, AccessDenied
│   │   └── DockerPages/       # Containers, Images, Volumes
│   ├── Layout/                # MainLayout, NavMenu
│   └── MyComponentBase.cs     # Base for all pages
├── ViewModels/                # Business logic + DTO mappings
├── Services/                  # MyLocalizer, etc.
├── Database/                  # EF Core DbContext, Models, Migrations
├── ImmutableTables/           # *.table.csv → auto-generated code
└── Controllers/               # AccountController (login/logout endpoints)
```

## Common Pitfalls

1. **CSV Escaping**: Forgetting to quote fields with commas breaks column alignment
2. **Localizer Access**: Must inject `IStringLocalizer Localizer` in pages not inheriting `MyComponentBase<T>`
3. **Docker Socket**: Container needs privileged access; development mode uses host Docker
4. **ViewModel Registration**: Forgetting `[RegisterScoped]` attribute means DI won't find it
5. **Async Initialization**: Always call `await Vm.InitializeAsync()` indirectly via base class, not manually

## External Dependencies

- **Docker.DotNet** - Docker Engine API client
- **MudBlazor** - Material Design component library
- **Riok.Mapperly** - Compile-time object mapping (source generator)
- **ASP.NET Core Identity** - Authentication/authorization
- **Entity Framework Core** - ORM (SQLite provider)

## Quick Reference

| Task            | Command/Pattern                                                    |
| --------------- | ------------------------------------------------------------------ |
| Add new page    | Create `.razor` in `Pages/`, inherit `MyComponentBase<TViewModel>` |
| Add ViewModel   | Create class with `[RegisterScoped]`, inherit `ViewModel`          |
| Add translation | Edit `Localization.table.csv`, use `@Localizer["KEY"]`             |
| Access Docker   | Inject `DockerClient dockerClient` in ViewModel constructor        |
| Run migrations  | `dotnet ef migrations add Name` then restart app                   |
| Check errors    | Build project; CSV generator errors show in build output           |
