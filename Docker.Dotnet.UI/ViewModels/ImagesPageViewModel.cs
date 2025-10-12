using Docker.DotNet;
using Docker.DotNet.Models;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterTransient(typeof(ImagesPageViewModel))]
public class ImagesPageViewModel(DockerClient client) : IViewModel
{
    public async Task InitializeAsync()
    {
        await RefreshImagesAsync();
    }

    public IList<ImageListItemViewModel>? Images { get; set; }

    public async Task RefreshImagesAsync()
    {
        var images = await client.Images.ListImagesAsync(new ImagesListParameters());

        Images = images.ToViewModel();
    }

    public async Task DeleteImageAsync(string imageId)
    {
        await client.Images.DeleteImageAsync(imageId, new ImageDeleteParameters() { Force = true });

        await RefreshImagesAsync();
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
