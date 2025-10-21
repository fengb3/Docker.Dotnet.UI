using Docker.DotNet;
using Docker.DotNet.Models;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(DashboardViewModel))]
public class DashboardViewModel(DockerClient dockerClient) : ViewModel
{
    public SystemInfoResponse? SystemInfo { get; set; }
    public int RunningContainers { get; set; }
    public int StoppedContainers { get; set; }
    public int PausedContainers { get; set; }
    public int TotalContainers { get; set; }
    public int TotalImages { get; set; }
    public int TotalVolumes { get; set; }
    public int TotalNetworks { get; set; }
    
    public string? ErrorMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public override async Task InitializeAsync()
    {
        await LoadDashboardDataAsync();
    }

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            ErrorMessage = null;

            // Get system info
            SystemInfo = await dockerClient.System.GetSystemInfoAsync();

            // Get containers statistics
            var containers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }
            );
            
            TotalContainers = containers.Count;
            RunningContainers = containers.Count(c => c.State.Equals("running", StringComparison.OrdinalIgnoreCase));
            StoppedContainers = containers.Count(c => c.State.Equals("exited", StringComparison.OrdinalIgnoreCase));
            PausedContainers = containers.Count(c => c.State.Equals("paused", StringComparison.OrdinalIgnoreCase));

            // Get images count
            var images = await dockerClient.Images.ListImagesAsync(
                new ImagesListParameters { All = true }
            );
            TotalImages = images.Count;

            // Get volumes count
            var volumes = await dockerClient.Volumes.ListAsync();
            TotalVolumes = volumes.Volumes?.Count ?? 0;

            // Get networks count
            var networks = await dockerClient.Networks.ListNetworksAsync();
            TotalNetworks = networks.Count;
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running and accessible.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load dashboard data: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public string FormatBytes(long bytes)
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
}
