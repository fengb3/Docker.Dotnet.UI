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
