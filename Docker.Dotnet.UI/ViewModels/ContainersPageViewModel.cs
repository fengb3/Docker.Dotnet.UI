using Docker.DotNet;
using Docker.DotNet.Models;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(ContainersPageViewModel))]
public class ContainersPageViewModel(DockerClient dockerClient) : ViewModel
{

    public override async Task InitializeAsync()
    {
        await RefreshContainersAsync();
    }

    private IList<ContainerListItemViewModel>? _allContainers;
    public IList<ContainerListItemViewModel>? Containers { get; set; }
    
    // Search/Filter state
    public string SearchText { get; set; } = string.Empty;
    public string StatusFilter { get; set; } = "all";
    
    // Batch operations state
    public HashSet<string> SelectedContainerIds { get; } = new();
    public bool SelectAll { get; set; }
    
    // Error state
    public string? ErrorMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Dialog states
    public bool ShowLogsDialog { get; set; }
    public bool ShowInspectDialog { get; set; }
    public bool ShowStatsDialog { get; set; }
    public string? SelectedContainerId { get; set; }
    public string? SelectedContainerName { get; set; }
    public List<string> ContainerLogs { get; } = new();
    public bool IsLoadingLogs { get; set; }
    public string? InspectJson { get; set; }
    public ContainerStatsResponse? CurrentStats { get; set; }
    
    // Real-time stats monitoring
    public bool IsMonitoringStats { get; set; }
    private System.Threading.CancellationTokenSource? _statsCancellationTokenSource;
    public double CpuPercent { get; set; }
    public double MemoryPercent { get; set; }
    public string MemoryUsage { get; set; } = "0 B";
    public string MemoryLimit { get; set; } = "0 B";
    public string NetworkRx { get; set; } = "0 B";
    public string NetworkTx { get; set; } = "0 B";

    public DialogOptions DialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };

    public async Task RefreshContainersAsync()
    {
        try
        {
            ErrorMessage = null;
            
            var containers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters() { All = true }
            );

            _allContainers = containers.ToViewModel();
            ApplyFilters();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running and accessible.";
            _allContainers = null;
            Containers = null;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to connect to Docker: {ex.Message}";
            _allContainers = null;
            Containers = null;
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void ApplyFilters()
    {
        if (_allContainers == null)
        {
            Containers = null;
            return;
        }

        var filtered = _allContainers.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.ContainerName.ToLowerInvariant().Contains(searchLower) ||
                c.Image.ToLowerInvariant().Contains(searchLower) ||
                c.ShortId.ToLowerInvariant().Contains(searchLower)
            );
        }

        // Apply status filter
        if (StatusFilter != "all")
        {
            filtered = filtered.Where(c => c.State.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        Containers = filtered.ToList();
        NotifyStateChanged();
    }

    public async Task StartContainerAsync(string containerId)
    {
        try
        {
            ErrorMessage = null;
            await dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters()
            );
            await RefreshContainersAsync();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start container: {ex.Message}";
            NotifyStateChanged();
        }
    }

    public async Task StopContainerAsync(string containerId)
    {
        try
        {
            ErrorMessage = null;
            await dockerClient.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters()
            );
            await RefreshContainersAsync();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to stop container: {ex.Message}";
            NotifyStateChanged();
        }
    }

    public async Task RestartContainerAsync(string containerId)
    {
        try
        {
            ErrorMessage = null;
            await dockerClient.Containers.RestartContainerAsync(
                containerId,
                new ContainerRestartParameters()
            );
            await RefreshContainersAsync();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to restart container: {ex.Message}";
            NotifyStateChanged();
        }
    }

    public async Task PauseContainerAsync(string containerId)
    {
        try
        {
            ErrorMessage = null;
            await dockerClient.Containers.PauseContainerAsync(containerId);
            await RefreshContainersAsync();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to pause container: {ex.Message}";
            NotifyStateChanged();
        }
    }

    public async Task UnpauseContainerAsync(string containerId)
    {
        try
        {
            ErrorMessage = null;
            await dockerClient.Containers.UnpauseContainerAsync(containerId);
            await RefreshContainersAsync();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to unpause container: {ex.Message}";
            NotifyStateChanged();
        }
    }

    public async Task DeleteContainerAsync(string containerId)
    {
        try
        {
            ErrorMessage = null;
            await dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters() { Force = true }
            );
            await RefreshContainersAsync();
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to delete container: {ex.Message}";
            NotifyStateChanged();
        }
    }

    public async Task ShowLogsAsync(string containerId, string containerName)
    {
        SelectedContainerId = containerId;
        SelectedContainerName = containerName;
        ShowLogsDialog = true;
        IsLoadingLogs = true;
        ContainerLogs.Clear();
        NotifyStateChanged();

        try
        {
            var multiplexedStream = await dockerClient.Containers.GetContainerLogsAsync(
                containerId,
                tty: false,
                new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Tail = "500",
                }
            );

            var buffer = new byte[4096];
            while (true)
            {
                var result = await multiplexedStream.ReadOutputAsync(buffer, 0, buffer.Length, default);
                if (result.EOF)
                    break;

                if (result.Count > 0)
                {
                    var line = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        ContainerLogs.Add(line.Trim());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ContainerLogs.Add($"Error loading logs: {ex.Message}");
        }
        finally
        {
            IsLoadingLogs = false;
            NotifyStateChanged();
        }
    }

    public async Task ShowInspectAsync(string containerId, string containerName)
    {
        SelectedContainerId = containerId;
        SelectedContainerName = containerName;
        ShowInspectDialog = true;
        NotifyStateChanged();

        try
        {
            var inspect = await dockerClient.Containers.InspectContainerAsync(containerId);
            InspectJson = System.Text.Json.JsonSerializer.Serialize(
                inspect,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            InspectJson = $"Error inspecting container: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void CloseLogsDialog()
    {
        ShowLogsDialog = false;
        ContainerLogs.Clear();
        NotifyStateChanged();
    }

    public void CloseInspectDialog()
    {
        ShowInspectDialog = false;
        InspectJson = null;
        NotifyStateChanged();
    }

    public async Task ShowStatsAsync(string containerId, string containerName)
    {
        SelectedContainerId = containerId;
        SelectedContainerName = containerName;
        ShowStatsDialog = true;
        IsMonitoringStats = true;
        NotifyStateChanged();

        // Cancel any existing monitoring
        _statsCancellationTokenSource?.Cancel();
        _statsCancellationTokenSource = new System.Threading.CancellationTokenSource();

        try
        {
            await MonitorStatsAsync(containerId, _statsCancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Normal cancellation
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error monitoring stats: {ex.Message}";
            NotifyStateChanged();
        }
    }

    private async Task MonitorStatsAsync(string containerId, System.Threading.CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && ShowStatsDialog)
        {
            try
            {
                var progress = new Progress<ContainerStatsResponse>(stats =>
                {
                    CurrentStats = stats;
                    CalculateStats(stats);
                    NotifyStateChanged();
                });

                await dockerClient.Containers.GetContainerStatsAsync(
                    containerId,
                    new ContainerStatsParameters { Stream = false },
                    progress,
                    cancellationToken
                );

                // Wait before next update (every 2 seconds)
                await Task.Delay(2000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Ignore errors and continue monitoring
                await Task.Delay(2000, cancellationToken);
            }
        }
    }

    private void CalculateStats(ContainerStatsResponse stats)
    {
        if (stats == null) return;

        // Calculate CPU percentage
        var cpuDelta = stats.CPUStats.CPUUsage.TotalUsage - stats.PreCPUStats.CPUUsage.TotalUsage;
        var systemDelta = stats.CPUStats.SystemUsage - stats.PreCPUStats.SystemUsage;
        var cpuCount = stats.CPUStats.OnlineCPUs > 0 ? (int)stats.CPUStats.OnlineCPUs : (stats.CPUStats.CPUUsage.PercpuUsage?.Count ?? 1);
        
        if (systemDelta > 0 && cpuDelta > 0)
        {
            CpuPercent = (double)cpuDelta / systemDelta * cpuCount * 100.0;
        }

        // Calculate memory percentage and usage
        if (stats.MemoryStats.Limit > 0)
        {
            var usedMemory = stats.MemoryStats.Usage - (stats.MemoryStats.Stats?.TryGetValue("cache", out var cache) == true ? cache : 0);
            MemoryPercent = (double)usedMemory / stats.MemoryStats.Limit * 100.0;
            MemoryUsage = FormatBytes((long)usedMemory);
            MemoryLimit = FormatBytes((long)stats.MemoryStats.Limit);
        }

        // Calculate network I/O
        if (stats.Networks != null)
        {
            ulong totalRx = 0;
            ulong totalTx = 0;
            foreach (var network in stats.Networks.Values)
            {
                totalRx += network.RxBytes;
                totalTx += network.TxBytes;
            }
            NetworkRx = FormatBytes((long)totalRx);
            NetworkTx = FormatBytes((long)totalTx);
        }
    }

    public void CloseStatsDialog()
    {
        ShowStatsDialog = false;
        IsMonitoringStats = false;
        _statsCancellationTokenSource?.Cancel();
        _statsCancellationTokenSource = null;
        CurrentStats = null;
        CpuPercent = 0;
        MemoryPercent = 0;
        MemoryUsage = "0 B";
        MemoryLimit = "0 B";
        NetworkRx = "0 B";
        NetworkTx = "0 B";
        NotifyStateChanged();
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public void ToggleSelectAll()
    {
        SelectAll = !SelectAll;
        SelectedContainerIds.Clear();
        
        if (SelectAll && Containers != null)
        {
            foreach (var container in Containers)
            {
                SelectedContainerIds.Add(container.ID);
            }
        }
        
        NotifyStateChanged();
    }

    public void ToggleContainerSelection(string containerId)
    {
        if (SelectedContainerIds.Contains(containerId))
        {
            SelectedContainerIds.Remove(containerId);
            SelectAll = false;
        }
        else
        {
            SelectedContainerIds.Add(containerId);
            
            // Check if all containers are now selected
            if (Containers != null && SelectedContainerIds.Count == Containers.Count)
            {
                SelectAll = true;
            }
        }
        
        NotifyStateChanged();
    }

    public async Task StartSelectedContainersAsync()
    {
        if (SelectedContainerIds.Count == 0) return;

        var tasks = SelectedContainerIds
            .Select(id => dockerClient.Containers.StartContainerAsync(id, new ContainerStartParameters()))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start some containers: {ex.Message}";
        }
        finally
        {
            SelectedContainerIds.Clear();
            SelectAll = false;
            await RefreshContainersAsync();
        }
    }

    public async Task StopSelectedContainersAsync()
    {
        if (SelectedContainerIds.Count == 0) return;

        var tasks = SelectedContainerIds
            .Select(id => dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters()))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to stop some containers: {ex.Message}";
        }
        finally
        {
            SelectedContainerIds.Clear();
            SelectAll = false;
            await RefreshContainersAsync();
        }
    }

    public async Task RemoveSelectedContainersAsync()
    {
        if (SelectedContainerIds.Count == 0) return;

        var tasks = SelectedContainerIds
            .Select(id => dockerClient.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters { Force = true }))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to remove some containers: {ex.Message}";
        }
        finally
        {
            SelectedContainerIds.Clear();
            SelectAll = false;
            await RefreshContainersAsync();
        }
    }

    // Create container state
    public bool ShowCreateDialog { get; set; }
    public bool IsCreating { get; set; }
    public CreateContainerModel CreateModel { get; set; } = new();
    public List<NetworkListItemViewModel> AvailableNetworks { get; set; } = new();
    public string? CreateError { get; set; }
    public string? CreateProgress { get; set; }

    // Docker Run Parser state
    public string DockerRunCommand { get; set; } = string.Empty;
    public bool IsParsing { get; set; }
    public string? ParseError { get; set; }
    public string? ParseSuccess { get; set; }

    // Text parsing properties for Entrypoint and Command
    public string EntrypointText
    {
        get => string.Join("\n", CreateModel.Entrypoint);
        set
        {
            CreateModel.Entrypoint = value
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToList();
        }
    }

    public string CommandText
    {
        get => string.Join("\n", CreateModel.Command);
        set
        {
            CreateModel.Command = value
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToList();
        }
    }

    public async Task OpenCreateDialog()
    {
        CreateModel = new CreateContainerModel();
        CreateError = null;
        CreateProgress = null;
        ShowCreateDialog = true;
        
        // Load available networks
        try
        {
            var networks = await dockerClient.Networks.ListNetworksAsync();
            AvailableNetworks = networks.ToViewModel().ToList();
        }
        catch (Exception ex)
        {
            CreateError = $"Failed to load networks: {ex.Message}";
        }
        
        NotifyStateChanged();
    }

    public void CloseCreateDialog()
    {
        ShowCreateDialog = false;
        CreateModel = new CreateContainerModel();
        CreateError = null;
        CreateProgress = null;
        DockerRunCommand = string.Empty;
        ParseError = null;
        ParseSuccess = null;
        NotifyStateChanged();
    }

    public async Task CreateContainerAsync()
    {
        IsCreating = true;
        CreateError = null;
        NotifyStateChanged();

        try
        {
            // Validate
            var errors = ValidateCreateModel(CreateModel).ToList();
            if (errors.Any())
            {
                CreateError = string.Join("; ", errors);
                return;
            }

            // Parse image name and tag
            var imageParts = CreateModel.Image.Split(':');
            var imageName = imageParts[0];
            var imageTag = imageParts.Length > 1 ? imageParts[1] : "latest";
            var fullImage = $"{imageName}:{imageTag}";

            // Ensure image exists
            if (CreateModel.PullIfNotExists)
            {
                await EnsureImageExistsAsync(imageName, imageTag);
            }

            // Build create parameters
            var createParams = BuildCreateContainerParameters(CreateModel, fullImage);

            // Create container
            CreateProgress = "Creating container...";
            NotifyStateChanged();

            var response = await dockerClient.Containers.CreateContainerAsync(createParams);

            // Optionally start the container
            if (CreateModel.AutoStartAfterCreate)
            {
                CreateProgress = "Starting container...";
                NotifyStateChanged();
                
                try
                {
                    await dockerClient.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
                }
                catch (Exception ex)
                {
                    CreateError = $"Container created but failed to start: {ex.Message}";
                    await RefreshContainersAsync();
                    return;
                }
            }

            // Success
            await RefreshContainersAsync();
            CloseCreateDialog();
        }
        catch (Exception ex)
        {
            CreateError = $"Failed to create container: {ex.Message}";
        }
        finally
        {
            IsCreating = false;
            CreateProgress = null;
            NotifyStateChanged();
        }
    }

    private async Task EnsureImageExistsAsync(string imageName, string imageTag)
    {
        try
        {
            await dockerClient.Images.InspectImageAsync($"{imageName}:{imageTag}");
        }
        catch (DockerImageNotFoundException)
        {
            // Image not found, pull it
            CreateProgress = $"Pulling image {imageName}:{imageTag}...";
            NotifyStateChanged();

            var progress = new Progress<JSONMessage>(message =>
            {
                if (!string.IsNullOrEmpty(message.Status))
                {
                    var progressText = message.Status;
                    if (message.ProgressMessage != null)
                    {
                        progressText += $" {message.ProgressMessage}";
                    }
                    CreateProgress = progressText;
                    NotifyStateChanged();
                }
            });

            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = imageName,
                    Tag = imageTag
                },
                null,
                progress
            );
        }
    }

    private CreateContainerParameters BuildCreateContainerParameters(CreateContainerModel model, string fullImage)
    {
        var parameters = new CreateContainerParameters
        {
            Image = fullImage,
            Name = string.IsNullOrWhiteSpace(model.ContainerName) ? null : model.ContainerName,
            Tty = model.Tty,
            AttachStdin = model.AttachStdin,
            AttachStdout = model.AttachStdout,
            AttachStderr = model.AttachStderr,
            WorkingDir = string.IsNullOrWhiteSpace(model.WorkingDir) ? null : model.WorkingDir,
        };

        // Labels
        if (model.Labels.Any())
        {
            parameters.Labels = model.Labels
                .Where(l => !string.IsNullOrWhiteSpace(l.Key))
                .ToDictionary(l => l.Key, l => l.Value);
        }

        // Environment variables
        if (model.EnvironmentVariables.Any())
        {
            parameters.Env = model.EnvironmentVariables
                .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                .Select(e => $"{e.Key}={e.Value}")
                .ToList();
        }

        // Exposed ports
        if (model.Ports.Any())
        {
            parameters.ExposedPorts = new Dictionary<string, EmptyStruct>();
            foreach (var port in model.Ports.Where(p => !string.IsNullOrWhiteSpace(p.ContainerPort)))
            {
                parameters.ExposedPorts[$"{port.ContainerPort}/{port.Protocol}"] = default;
            }
        }

        // Entrypoint and Cmd
        if (model.Entrypoint.Any())
        {
            parameters.Entrypoint = model.Entrypoint;
        }
        if (model.Command.Any())
        {
            parameters.Cmd = model.Command;
        }

        // Host config
        parameters.HostConfig = BuildHostConfig(model);

        // Networking config
        if (!string.IsNullOrWhiteSpace(model.NetworkMode) && model.NetworkMode != "bridge")
        {
            parameters.NetworkingConfig = BuildNetworkingConfig(model);
        }

        return parameters;
    }

    private HostConfig BuildHostConfig(CreateContainerModel model)
    {
        var hostConfig = new HostConfig();

        // Port bindings
        if (model.Ports.Any())
        {
            hostConfig.PortBindings = new Dictionary<string, IList<PortBinding>>();
            foreach (var port in model.Ports.Where(p => !string.IsNullOrWhiteSpace(p.ContainerPort)))
            {
                var key = $"{port.ContainerPort}/{port.Protocol}";
                hostConfig.PortBindings[key] = new List<PortBinding>
                {
                    new PortBinding
                    {
                        HostPort = string.IsNullOrWhiteSpace(port.HostPort) ? "" : port.HostPort,
                        HostIP = ""
                    }
                };
            }
        }

        // Volume binds
        if (model.Volumes.Any())
        {
            hostConfig.Binds = model.Volumes
                .Where(v => !string.IsNullOrWhiteSpace(v.Target))
                .Select(v => $"{v.Source}:{v.Target}:{v.Mode}")
                .ToList();
        }

        // Resource limits
        if (model.CpuLimit > 0)
        {
            hostConfig.NanoCPUs = (long)(model.CpuLimit * 1_000_000_000);
        }
        if (model.MemoryLimitMiB > 0)
        {
            hostConfig.Memory = model.MemoryLimitMiB * 1024 * 1024;
        }

        // Restart policy
        hostConfig.RestartPolicy = new RestartPolicy
        {
            Name = model.RestartPolicyType switch
            {
                "always" => RestartPolicyKind.Always,
                "unless-stopped" => RestartPolicyKind.UnlessStopped,
                "on-failure" => RestartPolicyKind.OnFailure,
                _ => RestartPolicyKind.No
            },
            MaximumRetryCount = model.RestartPolicyType == "on-failure" ? model.MaximumRetryCount : 0
        };

        // Network mode
        if (!string.IsNullOrWhiteSpace(model.NetworkMode))
        {
            hostConfig.NetworkMode = model.NetworkMode;
        }

        return hostConfig;
    }

    private NetworkingConfig BuildNetworkingConfig(CreateContainerModel model)
    {
        var config = new NetworkingConfig
        {
            EndpointsConfig = new Dictionary<string, EndpointSettings>()
        };

        var endpointSettings = new EndpointSettings();

        if (model.NetworkAliases.Any())
        {
            endpointSettings.Aliases = model.NetworkAliases;
        }

        if (!string.IsNullOrWhiteSpace(model.IPv4Address))
        {
            endpointSettings.IPAddress = model.IPv4Address;
        }

        config.EndpointsConfig[model.NetworkMode] = endpointSettings;

        return config;
    }

    private IEnumerable<string> ValidateCreateModel(CreateContainerModel model)
    {
        // Image is required
        if (string.IsNullOrWhiteSpace(model.Image))
        {
            yield return "Image is required";
        }

        // Container name validation (if provided)
        if (!string.IsNullOrWhiteSpace(model.ContainerName))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(model.ContainerName, @"^[a-zA-Z0-9][a-zA-Z0-9_.-]*$"))
            {
                yield return "Invalid container name. Must start with alphanumeric and contain only [a-zA-Z0-9_.-]";
            }
        }

        // Port validation
        var portSet = new HashSet<string>();
        foreach (var port in model.Ports)
        {
            if (string.IsNullOrWhiteSpace(port.ContainerPort))
            {
                yield return "Container port is required for all port mappings";
                continue;
            }

            if (!int.TryParse(port.ContainerPort, out var containerPort) || containerPort < 1 || containerPort > 65535)
            {
                yield return $"Invalid container port: {port.ContainerPort}";
            }

            if (!string.IsNullOrWhiteSpace(port.HostPort))
            {
                if (!int.TryParse(port.HostPort, out var hostPort) || hostPort < 1 || hostPort > 65535)
                {
                    yield return $"Invalid host port: {port.HostPort}";
                }

                var portKey = $"{port.HostPort}/{port.Protocol}";
                if (portSet.Contains(portKey))
                {
                    yield return $"Duplicate host port mapping: {portKey}";
                }
                portSet.Add(portKey);
            }
        }

        // Volume validation
        foreach (var volume in model.Volumes)
        {
            if (string.IsNullOrWhiteSpace(volume.Target))
            {
                yield return "Container path is required for all volume mappings";
            }
        }

        // Environment variable validation
        foreach (var env in model.EnvironmentVariables)
        {
            if (string.IsNullOrWhiteSpace(env.Key))
            {
                yield return "Environment variable key is required";
            }
        }

        // IPv4 address validation
        if (!string.IsNullOrWhiteSpace(model.IPv4Address))
        {
            if (!System.Net.IPAddress.TryParse(model.IPv4Address, out var ip) || ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                yield return "Invalid IPv4 address";
            }
        }
    }

    public void ParseDockerRunCommand()
    {
        IsParsing = true;
        ParseError = null;
        ParseSuccess = null;
        NotifyStateChanged();

        try
        {
            if (string.IsNullOrWhiteSpace(DockerRunCommand))
            {
                ParseError = "Please enter a docker run command";
                return;
            }

            // Parse the docker run command
            var command = DockerRunCommand.Trim();
            
            // Remove "docker run" prefix if present
            if (command.StartsWith("docker run", StringComparison.OrdinalIgnoreCase))
            {
                command = command.Substring("docker run".Length).Trim();
            }

            // Split command while preserving quoted strings
            var args = SplitCommandLine(command);
            
            // Parse arguments
            for (int i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                
                if (arg == "-d" || arg == "--detach")
                {
                    // Detached mode - already default
                    continue;
                }
                else if (arg == "--name" && i + 1 < args.Count)
                {
                    CreateModel.ContainerName = args[++i];
                }
                else if (arg == "--restart" && i + 1 < args.Count)
                {
                    var policy = args[++i];
                    if (policy.StartsWith("on-failure"))
                    {
                        CreateModel.RestartPolicyType = "on-failure";
                        var parts = policy.Split(':');
                        if (parts.Length > 1 && int.TryParse(parts[1], out var count))
                        {
                            CreateModel.MaximumRetryCount = count;
                        }
                    }
                    else
                    {
                        CreateModel.RestartPolicyType = policy;
                    }
                }
                else if ((arg == "-p" || arg == "--publish") && i + 1 < args.Count)
                {
                    var portMapping = args[++i];
                    ParsePortMapping(portMapping);
                }
                else if ((arg == "-v" || arg == "--volume") && i + 1 < args.Count)
                {
                    var volumeMapping = args[++i];
                    ParseVolumeMapping(volumeMapping);
                }
                else if ((arg == "-e" || arg == "--env") && i + 1 < args.Count)
                {
                    var envVar = args[++i];
                    ParseEnvironmentVariable(envVar);
                }
                else if (arg == "--network" && i + 1 < args.Count)
                {
                    CreateModel.NetworkMode = args[++i];
                }
                else if (arg == "-w" || arg == "--workdir" && i + 1 < args.Count)
                {
                    CreateModel.WorkingDir = args[++i];
                }
                else if (arg == "--entrypoint" && i + 1 < args.Count)
                {
                    CreateModel.Entrypoint = new List<string> { args[++i] };
                }
                else if (arg == "-t" || arg == "--tty")
                {
                    CreateModel.Tty = true;
                }
                else if (arg == "-i" || arg == "--interactive")
                {
                    CreateModel.AttachStdin = true;
                }
                else if (arg == "--cpus" && i + 1 < args.Count)
                {
                    if (double.TryParse(args[++i], out var cpus))
                    {
                        CreateModel.CpuLimit = cpus;
                    }
                }
                else if ((arg == "-m" || arg == "--memory") && i + 1 < args.Count)
                {
                    var memory = args[++i];
                    CreateModel.MemoryLimitMiB = ParseMemorySize(memory);
                }
                else if (arg == "--label" && i + 1 < args.Count)
                {
                    var label = args[++i];
                    var parts = label.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        CreateModel.Labels.Add(new LabelModel { Key = parts[0], Value = parts[1] });
                    }
                }
                else if (!arg.StartsWith("-") && string.IsNullOrEmpty(CreateModel.Image))
                {
                    // This should be the image name
                    CreateModel.Image = arg;
                }
                else if (!arg.StartsWith("-") && !string.IsNullOrEmpty(CreateModel.Image))
                {
                    // These are command arguments
                    if (CreateModel.Command == null || CreateModel.Command.Count == 0)
                    {
                        CreateModel.Command = new List<string>();
                    }
                    CreateModel.Command.Add(arg);
                }
            }

            ParseSuccess = "Command parsed successfully!";
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ParseError = $"Failed to parse command: {ex.Message}";
        }
        finally
        {
            IsParsing = false;
            NotifyStateChanged();
        }
    }

    private List<string> SplitCommandLine(string commandLine)
    {
        var args = new List<string>();
        var currentArg = new System.Text.StringBuilder();
        var inQuotes = false;
        var quoteChar = '\0';

        for (int i = 0; i < commandLine.Length; i++)
        {
            var c = commandLine[i];

            if ((c == '"' || c == '\'') && !inQuotes)
            {
                inQuotes = true;
                quoteChar = c;
            }
            else if (c == quoteChar && inQuotes)
            {
                inQuotes = false;
                quoteChar = '\0';
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (currentArg.Length > 0)
                {
                    args.Add(currentArg.ToString());
                    currentArg.Clear();
                }
            }
            else if (c == '\\' && i + 1 < commandLine.Length && commandLine[i + 1] == '\n')
            {
                // Skip line continuation
                i++;
            }
            else
            {
                currentArg.Append(c);
            }
        }

        if (currentArg.Length > 0)
        {
            args.Add(currentArg.ToString());
        }

        return args;
    }

    private void ParsePortMapping(string portMapping)
    {
        // Format: [hostIp:][hostPort:]containerPort[/protocol]
        var parts = portMapping.Split(':');
        string? hostPort = null;
        string? containerPort = null;
        string protocol = "tcp";

        if (parts.Length == 1)
        {
            // Just container port
            containerPort = parts[0];
        }
        else if (parts.Length == 2)
        {
            // host:container
            hostPort = parts[0];
            containerPort = parts[1];
        }
        else if (parts.Length == 3)
        {
            // ip:host:container (ignore IP for now)
            hostPort = parts[1];
            containerPort = parts[2];
        }

        // Extract protocol if present
        if (containerPort != null && containerPort.Contains('/'))
        {
            var portParts = containerPort.Split('/');
            containerPort = portParts[0];
            protocol = portParts[1].ToLower();
        }

        if (!string.IsNullOrEmpty(containerPort))
        {
            CreateModel.Ports.Add(new PortMappingModel
            {
                HostPort = hostPort ?? "",
                ContainerPort = containerPort,
                Protocol = protocol
            });
        }
    }

    private void ParseVolumeMapping(string volumeMapping)
    {
        // Format: [source:]destination[:mode]
        var parts = volumeMapping.Split(':');
        string source = "";
        string target = "";
        string mode = "rw";

        if (parts.Length == 1)
        {
            // Just target (anonymous volume)
            target = parts[0];
        }
        else if (parts.Length >= 2)
        {
            source = parts[0];
            target = parts[1];
            if (parts.Length >= 3)
            {
                mode = parts[2];
            }
        }

        if (!string.IsNullOrEmpty(target))
        {
            CreateModel.Volumes.Add(new VolumeModel
            {
                Source = source,
                Target = target,
                Mode = mode
            });
        }
    }

    private void ParseEnvironmentVariable(string envVar)
    {
        var parts = envVar.Split('=', 2);
        if (parts.Length >= 1)
        {
            CreateModel.EnvironmentVariables.Add(new EnvironmentVariableModel
            {
                Key = parts[0],
                Value = parts.Length > 1 ? parts[1] : ""
            });
        }
    }

    private long ParseMemorySize(string memory)
    {
        // Parse memory size like "512m", "2g", etc.
        memory = memory.ToLower().Trim();
        var numberPart = new string(memory.TakeWhile(c => char.IsDigit(c) || c == '.').ToArray());
        var unit = new string(memory.SkipWhile(c => char.IsDigit(c) || c == '.').ToArray());

        if (!double.TryParse(numberPart, out var size))
        {
            return 0;
        }

        return unit switch
        {
            "k" or "kb" => (long)(size / 1024),
            "m" or "mb" => (long)size,
            "g" or "gb" => (long)(size * 1024),
            "t" or "tb" => (long)(size * 1024 * 1024),
            _ => (long)size // Assume bytes, convert to MiB
        };
    }
}

public class ContainerListItemViewModel
{
    public string ID { get; set; } = string.Empty;

    public IList<string> Names { get; set; } = new List<string>();

    public string Image { get; set; } = string.Empty;

    public string ImageID { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;

    public DateTime Created { get; set; }

    public IList<Port> Ports { get; set; } = new List<Port>();

    public long SizeRw { get; set; }

    public long SizeRootFs { get; set; }

    public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public string State { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public SummaryNetworkSettings NetworkSettings { get; set; } = new SummaryNetworkSettings();

    public IList<MountPoint> Mounts { get; set; } = new List<MountPoint>();

    public string ShortId => ID?.Length > 12 ? ID.Substring(0, 12) : ID ?? string.Empty;

    public string ContainerName => Names?.FirstOrDefault()?.TrimStart('/') ?? ShortId;

    public MudBlazor.Color StateColor =>
        State?.ToLower() switch
        {
            "running" => Color.Success,
            "exited" => Color.Error,
            "paused" => Color.Warning,
            "created" => Color.Info,
            _ => Color.Default,
        };
}
