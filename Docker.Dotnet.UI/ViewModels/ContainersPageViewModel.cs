using Docker.DotNet;
using Docker.DotNet.Models;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(ContainersPageViewModel))]
public class ContainersPageViewModel(DockerClient dockerClient) : ViewModel
{
    public event Action? OnStateChanged;

    public override async Task InitializeAsync()
    {
        await RefreshContainersAsync();
    }

    public IList<ContainerListItemViewModel>? Containers { get; set; }

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

    public DialogOptions DialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };

    public async Task RefreshContainersAsync()
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters() { All = true }
        );

        Containers = containers.ToViewModel();
        NotifyStateChanged();
    }

    public async Task StartContainerAsync(string containerId)
    {
        await dockerClient.Containers.StartContainerAsync(
            containerId,
            new ContainerStartParameters()
        );

        await RefreshContainersAsync();
    }

    public async Task StopContainerAsync(string containerId)
    {
        await dockerClient.Containers.StopContainerAsync(
            containerId,
            new ContainerStopParameters()
        );

        await RefreshContainersAsync();
    }

    public async Task RestartContainerAsync(string containerId)
    {
        await dockerClient.Containers.RestartContainerAsync(
            containerId,
            new ContainerRestartParameters()
        );

        await RefreshContainersAsync();
    }

    public async Task PauseContainerAsync(string containerId)
    {
        await dockerClient.Containers.PauseContainerAsync(containerId);
        await RefreshContainersAsync();
    }

    public async Task UnpauseContainerAsync(string containerId)
    {
        await dockerClient.Containers.UnpauseContainerAsync(containerId);
        await RefreshContainersAsync();
    }

    public async Task DeleteContainerAsync(string containerId)
    {
        await dockerClient.Containers.RemoveContainerAsync(
            containerId,
            new ContainerRemoveParameters() { Force = true }
        );

        await RefreshContainersAsync();
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

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
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
