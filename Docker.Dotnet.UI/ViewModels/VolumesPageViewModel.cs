using Docker.DotNet;
using Docker.DotNet.Models;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterTransient(typeof(VolumesPageViewModel))]
public class VolumesPageViewModel(DockerClient client) : ViewModel
{
    

    public override async Task InitializeAsync()
    {
        await RefreshVolumesAsync();
    }

    private IList<VolumeListItemViewModel>? _allVolumes;
    public IList<VolumeListItemViewModel>? Volumes { get; set; }
    
    // Search/Filter state
    public string SearchText { get; set; } = string.Empty;
    
    // Batch operations state
    public HashSet<string> SelectedVolumeNames { get; } = new();
    public bool SelectAll { get; set; }

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
        _allVolumes = volumes.Volumes.ToViewModel();
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        if (_allVolumes == null)
        {
            Volumes = null;
            return;
        }

        var filtered = _allVolumes.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(v =>
                v.Name.ToLowerInvariant().Contains(searchLower) ||
                v.Driver.ToLowerInvariant().Contains(searchLower)
            );
        }

        Volumes = filtered.ToList();
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

    public void ToggleSelectAll()
    {
        SelectAll = !SelectAll;
        SelectedVolumeNames.Clear();
        
        if (SelectAll && Volumes != null)
        {
            foreach (var volume in Volumes)
            {
                SelectedVolumeNames.Add(volume.Name);
            }
        }
        
        NotifyStateChanged();
    }

    public void ToggleVolumeSelection(string volumeName)
    {
        if (SelectedVolumeNames.Contains(volumeName))
        {
            SelectedVolumeNames.Remove(volumeName);
            SelectAll = false;
        }
        else
        {
            SelectedVolumeNames.Add(volumeName);
            
            // Check if all volumes are now selected
            if (Volumes != null && SelectedVolumeNames.Count == Volumes.Count)
            {
                SelectAll = true;
            }
        }
        
        NotifyStateChanged();
    }

    public async Task RemoveSelectedVolumesAsync()
    {
        if (SelectedVolumeNames.Count == 0) return;

        var tasks = SelectedVolumeNames
            .Select(name => client.Volumes.RemoveAsync(name, force: true))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            // Ignore errors - some volumes may fail to delete
        }
        finally
        {
            SelectedVolumeNames.Clear();
            SelectAll = false;
            await RefreshVolumesAsync();
        }
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
