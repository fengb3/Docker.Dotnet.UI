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

    public IList<ContainerListItemViewModel>? Containers { get; set; }

    public async Task RefreshContainersAsync()
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters() { All = true }
        );

        Containers = containers.ToViewModel();
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

    public async Task DeleteContainerAsync(string containerId)
    {
        await dockerClient.Containers.RemoveContainerAsync(
            containerId,
            new ContainerRemoveParameters() { Force = true }
        );

        await RefreshContainersAsync();
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
