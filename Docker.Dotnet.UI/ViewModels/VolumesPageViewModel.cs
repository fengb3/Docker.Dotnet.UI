using Docker.DotNet;
using Docker.DotNet.Models;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterTransient(typeof(VolumesPageViewModel))]
public class VolumesPageViewModel(DockerClient client) : ViewModel
{
    public override async Task InitializeAsync()
    {
        await RefreshVolumesAsync();
    }

    public IList<VolumeListItemViewModel>? Volumes { get; set; }

    public async Task RefreshVolumesAsync()
    {
        var volumes = await client.Volumes.ListAsync();
        Volumes = volumes.Volumes.ToViewModel();
    }

    public async Task DeleteVolumeAsync(string volumeName)
    {
        await client.Volumes.RemoveAsync(volumeName, force: true);
        await RefreshVolumesAsync();
    }
}

public class VolumeListItemViewModel
{
    public string CreatedAt { get; set; } = string.Empty;

    public string Driver { get; set; } = string.Empty;

    public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public string Mountpoint { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public IDictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

    public string Scope { get; set; } = string.Empty;

    public IDictionary<string, object> Status { get; set; } = new Dictionary<string, object>();

    public VolumeUsageData UsageData { get; set; } = new VolumeUsageData();

    public string ShortMountpoint
    {
        get
        {
            if (string.IsNullOrEmpty(Mountpoint))
                return "-";
            
            // 只显示最后两级路径
            var parts = Mountpoint.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 2)
            {
                return $".../{parts[^2]}/{parts[^1]}";
            }
            return Mountpoint;
        }
    }

    public string SizeFormatted
    {
        get
        {
            if (UsageData?.Size == null)
                return "-";

            var sizeInBytes = UsageData.Size;
            var sizeInMB = sizeInBytes / 1024.0 / 1024.0;
            if (sizeInMB > 1024)
            {
                return $"{sizeInMB / 1024:F2} GB";
            }
            return $"{sizeInMB:F2} MB";
        }
    }

    public string CreatedAtFormatted
    {
        get
        {
            if (string.IsNullOrEmpty(CreatedAt))
                return "-";

            if (DateTime.TryParse(CreatedAt, out var date))
            {
                return date.ToString("yyyy-MM-dd HH:mm");
            }
            return CreatedAt;
        }
    }
}
