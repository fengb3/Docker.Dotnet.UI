using Docker.DotNet;
using Docker.DotNet.Models;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterTransient(typeof(VolumesPageViewModel))]
public class VolumesPageViewModel(DockerClient client) : ViewModel
{
    public event Action? OnStateChanged;

    public override async Task InitializeAsync()
    {
        await RefreshVolumesAsync();
    }

    public IList<VolumeListItemViewModel>? Volumes { get; set; }

    // Dialog states
    public bool ShowCreateDialog { get; set; }
    public bool ShowInspectDialog { get; set; }
    public string? SelectedVolumeName { get; set; }
    public string? InspectJson { get; set; }

    // Create volume properties
    public string NewVolumeName { get; set; } = string.Empty;
    public string NewVolumeDriver { get; set; } = "local";
    public bool IsCreating { get; set; }

    public DialogOptions DialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };

    public async Task RefreshVolumesAsync()
    {
        var volumes = await client.Volumes.ListAsync();
        Volumes = volumes.Volumes.ToViewModel();
        NotifyStateChanged();
    }

    public async Task DeleteVolumeAsync(string volumeName)
    {
        await client.Volumes.RemoveAsync(volumeName, force: true);
        await RefreshVolumesAsync();
    }

    public void OpenCreateDialog()
    {
        ShowCreateDialog = true;
        NewVolumeName = string.Empty;
        NewVolumeDriver = "local";
        NotifyStateChanged();
    }

    public void CloseCreateDialog()
    {
        ShowCreateDialog = false;
        NotifyStateChanged();
    }

    public async Task CreateVolumeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewVolumeName))
            return;

        IsCreating = true;
        NotifyStateChanged();

        try
        {
            await client.Volumes.CreateAsync(new VolumesCreateParameters
            {
                Name = NewVolumeName,
                Driver = NewVolumeDriver,
            });

            await RefreshVolumesAsync();
            CloseCreateDialog();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            IsCreating = false;
            NotifyStateChanged();
        }
    }

    public async Task ShowInspectAsync(string volumeName)
    {
        SelectedVolumeName = volumeName;
        ShowInspectDialog = true;
        NotifyStateChanged();

        try
        {
            var inspect = await client.Volumes.InspectAsync(volumeName);
            InspectJson = System.Text.Json.JsonSerializer.Serialize(
                inspect,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            InspectJson = $"Error inspecting volume: {ex.Message}";
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void CloseInspectDialog()
    {
        ShowInspectDialog = false;
        InspectJson = null;
        NotifyStateChanged();
    }

    public async Task PruneVolumesAsync()
    {
        await client.Volumes.PruneAsync();
        await RefreshVolumesAsync();
    }

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
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
