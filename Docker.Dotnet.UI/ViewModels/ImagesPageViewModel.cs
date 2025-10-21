using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(ImagesPageViewModel))]
public class ImagesPageViewModel(DockerClient client, IJSRuntime jsRuntime)
    : ViewModel
{

    public override async Task InitializeAsync()
    {
        await RefreshImagesAsync();
    }

    private IList<ImageListItemViewModel>? _allImages;
    public IList<ImageListItemViewModel>? Images { get; set; }
    
    // Search/Filter state
    public string SearchText { get; set; } = string.Empty;
    
    // Batch operations state
    public HashSet<string> SelectedImageIds { get; } = new();
    public bool SelectAll { get; set; }

    // Dialog state
    public bool ShowAddImageDialog { get; set; }
    public bool ShowInspectDialog { get; set; }
    public string? SelectedImageId { get; set; }
    public string? SelectedImageName { get; set; }
    public string? InspectJson { get; set; }

    public DialogOptions DialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
        };

    public DialogOptions LargeDialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Large,
            FullWidth = true,
            CloseButton = true,
        };

    // Pull image properties
    public string PullImageName { get; set; } = "nginx";
    public string PullImageTag { get; set; } = "latest";
    public bool IsPulling { get; private set; }
    private Dictionary<string, string> _pullLogsByIdOrIndex { get; } = new();
    private int _pullLogCounter = 0;
    public IEnumerable<string> PullLogs => _pullLogsByIdOrIndex.Values;

    // Load from tarball properties
    public IBrowserFile? SelectedTarball { get; set; }
    public bool IsLoading { get; private set; }
    public List<string> LoadLogs { get; } = new();

    // Export properties
    public bool IsExporting { get; set; }

    public async Task RefreshImagesAsync()
    {
        var images = await client.Images.ListImagesAsync(new ImagesListParameters());
        _allImages = images.ToViewModel();
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        if (_allImages == null)
        {
            Images = null;
            return;
        }

        var filtered = _allImages.AsEnumerable();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLowerInvariant();
            filtered = filtered.Where(i =>
                i.ImageName.ToLowerInvariant().Contains(searchLower) ||
                i.ShortId.ToLowerInvariant().Contains(searchLower) ||
                (i.RepoTags != null && i.RepoTags.Any(t => t.ToLowerInvariant().Contains(searchLower)))
            );
        }

        Images = filtered.ToList();
        NotifyStateChanged();
    }

    public async Task DeleteImageAsync(string imageId)
    {
        await client.Images.DeleteImageAsync(imageId, new ImageDeleteParameters() { Force = true });
        await RefreshImagesAsync();
    }

    public async Task ShowInspectAsync(string imageId, string imageName)
    {
        SelectedImageId = imageId;
        SelectedImageName = imageName;
        ShowInspectDialog = true;
        NotifyStateChanged();

        try
        {
            var inspect = await client.Images.InspectImageAsync(imageId);
            InspectJson = System.Text.Json.JsonSerializer.Serialize(
                inspect,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch (Exception ex)
        {
            InspectJson = $"Error inspecting image: {ex.Message}";
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

    public async Task ExportImageAsync(string imageId, string imageName)
    {
        IsExporting = true;
        NotifyStateChanged();

        try
        {
            var stream = await client.Images.SaveImageAsync(imageId);
            
            // Read the stream into memory
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();

            // Trigger download in browser
            var fileName = $"{imageName.Replace(':', '_').Replace('/', '_')}.tar";
            await jsRuntime.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(bytes));
        }
        catch (Exception)
        {
            // Handle error
            throw;
        }
        finally
        {
            IsExporting = false;
            NotifyStateChanged();
        }
    }

    public void OpenAddImageDialog()
    {
        ShowAddImageDialog = true;
        ResetDialogState();
        NotifyStateChanged();
    }

    public void CloseDialog()
    {
        ShowAddImageDialog = false;
        ResetDialogState();
        NotifyStateChanged();
    }

    private void ResetDialogState()
    {
        PullImageName = "nginx";
        PullImageTag = "latest";
        IsPulling = false;
        _pullLogsByIdOrIndex.Clear();
        _pullLogCounter = 0;

        SelectedTarball = null;
        IsLoading = false;
        LoadLogs.Clear();
    }

    public async Task HandlePullImageAsync()
    {
        if (string.IsNullOrWhiteSpace(PullImageName))
            return;

        IsPulling = true;
        _pullLogsByIdOrIndex.Clear();
        _pullLogCounter = 0;
        NotifyStateChanged();

        try
        {
            var progress = new Progress<JSONMessage>(message =>
            {
                string key;
                string output = "";

                if (!string.IsNullOrEmpty(message.Status))
                {
                    // 如果有 ID，使用 ID 作为 key，这样同一个 ID 的消息会被更新
                    if (!string.IsNullOrEmpty(message.ID))
                    {
                        key = message.ID;
                        output = $"{message.ID}: {message.Status} {message.ProgressMessage}";
                    }
                    else
                    {
                        // 如果没有 ID，使用计数器作为 key，这样每条消息都会被保留
                        key = $"msg_{_pullLogCounter++}";
                        output = message.Status;
                    }
                }
                else if (!string.IsNullOrEmpty(message.Stream))
                {
                    // Stream 消息使用计数器，每条都保留
                    key = $"msg_{_pullLogCounter++}";
                    output = message.Stream;
                }
                else
                {
                    return;
                }

                if (!string.IsNullOrEmpty(output))
                {
                    _pullLogsByIdOrIndex[key] = output.Trim();
                    NotifyStateChanged();
                }
            });

            await client.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = PullImageName, Tag = PullImageTag },
                null,
                progress
            );

            _pullLogsByIdOrIndex[$"msg_{_pullLogCounter++}"] = "✓ 镜像拉取完成！";
            NotifyStateChanged();
            await Task.Delay(1500);
            await RefreshImagesAsync();
            CloseDialog();
        }
        catch (Exception ex)
        {
            _pullLogsByIdOrIndex[$"error_{_pullLogCounter++}"] = $"✗ 错误: {ex.Message}";
            NotifyStateChanged();
        }
        finally
        {
            IsPulling = false;
            NotifyStateChanged();
        }
    }

    public void OnTarballSelected(IBrowserFile file)
    {
        SelectedTarball = file;
        NotifyStateChanged();
    }

    public void ClearTarballSelection()
    {
        SelectedTarball = null;
        NotifyStateChanged();
    }

    public async Task HandleLoadFromTarballAsync()
    {
        if (SelectedTarball == null)
            return;

        IsLoading = true;
        LoadLogs.Clear();
        NotifyStateChanged();

        try
        {
            using var stream = SelectedTarball.OpenReadStream(maxAllowedSize: 1024 * 1024 * 1024); // 1GB max

            LoadLogs.Add("开始导入镜像...");
            NotifyStateChanged();

            var progress = new Progress<JSONMessage>(message =>
            {
                var output = "";
                if (!string.IsNullOrEmpty(message.Stream))
                {
                    output = message.Stream;
                }
                else if (!string.IsNullOrEmpty(message.Status))
                {
                    output = message.Status;
                }

                if (!string.IsNullOrEmpty(output))
                {
                    LoadLogs.Add(output.Trim());
                    NotifyStateChanged();
                }
            });

            await client.Images.LoadImageAsync(new ImageLoadParameters(), stream, progress);

            LoadLogs.Add("✓ 镜像导入完成！");
            NotifyStateChanged();
            await Task.Delay(1500);
            await RefreshImagesAsync();
            CloseDialog();
        }
        catch (Exception ex)
        {
            LoadLogs.Add($"✗ 错误: {ex.Message}");
            NotifyStateChanged();
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public string FormatFileSize(long bytes)
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
        SelectedImageIds.Clear();
        
        if (SelectAll && Images != null)
        {
            foreach (var image in Images)
            {
                SelectedImageIds.Add(image.ID);
            }
        }
        
        NotifyStateChanged();
    }

    public void ToggleImageSelection(string imageId)
    {
        if (SelectedImageIds.Contains(imageId))
        {
            SelectedImageIds.Remove(imageId);
            SelectAll = false;
        }
        else
        {
            SelectedImageIds.Add(imageId);
            
            // Check if all images are now selected
            if (Images != null && SelectedImageIds.Count == Images.Count)
            {
                SelectAll = true;
            }
        }
        
        NotifyStateChanged();
    }

    public async Task RemoveSelectedImagesAsync()
    {
        if (SelectedImageIds.Count == 0) return;

        var tasks = SelectedImageIds
            .Select(id => client.Images.DeleteImageAsync(id, new ImageDeleteParameters { Force = true }))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception)
        {
            // Ignore errors - some images may fail to delete
        }
        finally
        {
            SelectedImageIds.Clear();
            SelectAll = false;
            await RefreshImagesAsync();
        }
    }

}

public class ImageListItemViewModel
{
    public long Containers { get; set; }

    public DateTime Created { get; set; }

    public string ID { get; set; } = string.Empty;

    public IDictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();

    public string ParentID { get; set; } = string.Empty;

    public IList<string> RepoDigests { get; set; } = new List<string>();

    public IList<string> RepoTags { get; set; } = new List<string>();

    public long SharedSize { get; set; }

    public long Size { get; set; }

    public long VirtualSize { get; set; }

    public string ShortId => ID?.Length > 12 ? ID.Substring(7, 12) : ID ?? string.Empty;

    public string ImageName
    {
        get
        {
            if (RepoTags != null && RepoTags.Any() && !string.IsNullOrEmpty(RepoTags.First()))
            {
                return RepoTags.First();
            }
            return ShortId;
        }
    }

    public string SizeFormatted
    {
        get
        {
            var sizeInMB = Size / 1024.0 / 1024.0;
            if (sizeInMB > 1024)
            {
                return $"{sizeInMB / 1024:F2} GB";
            }
            return $"{sizeInMB:F2} MB";
        }
    }
}
