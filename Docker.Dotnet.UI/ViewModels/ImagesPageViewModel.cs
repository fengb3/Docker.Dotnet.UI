using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(ImagesPageViewModel))]
public class ImagesPageViewModel(DockerClient client) : IViewModel
{
    public event Action? OnStateChanged;

    public async Task InitializeAsync()
    {
        await RefreshImagesAsync();
    }

    public IList<ImageListItemViewModel>? Images { get; set; }

    // Dialog state
    public bool ShowAddImageDialog { get; set; }
    public DialogOptions DialogOptions { get; } =
        new()
        {
            MaxWidth = MaxWidth.Medium,
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

    public async Task RefreshImagesAsync()
    {
        var images = await client.Images.ListImagesAsync(new ImagesListParameters());
        Images = images.ToViewModel();
        NotifyStateChanged();
    }

    public async Task DeleteImageAsync(string imageId)
    {
        await client.Images.DeleteImageAsync(imageId, new ImageDeleteParameters() { Force = true });
        await RefreshImagesAsync();
    }

    public void GoToImageDetailsAsync(string imageId)
    {
        // 暂不实现
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

                        // if (message.Progress != null)
                        // {
                        //     output = $"{output} {message.Progress.Current}/{message.Progress.Total}";
                        // }
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

    private void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}

public class ImageListItemViewModel
{
    public long Containers { get; set; }

    public DateTime Created { get; set; }

    public string ID { get; set; }

    public IDictionary<string, string> Labels { get; set; }

    public string ParentID { get; set; }

    public IList<string> RepoDigests { get; set; }

    public IList<string> RepoTags { get; set; }

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
