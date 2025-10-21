using Docker.DotNet.Models;
using Riok.Mapperly.Abstractions;

namespace Docker.Dotnet.UI.ViewModels;

[Mapper]
public static partial class ViewModelMapper
{
    public static partial IList<ContainerListItemViewModel> ToViewModel(
        this IList<ContainerListResponse> responses
    );

    public static partial ContainerListItemViewModel ToViewModel(
        this ContainerListResponse response
    );

    public static partial IList<ImageListItemViewModel> ToViewModel(
        this IList<ImagesListResponse> responses
    );

    public static partial ImageListItemViewModel ToViewModel(this ImagesListResponse response);

    public static partial IList<VolumeListItemViewModel> ToViewModel(
        this IList<VolumeResponse> responses
    );

    public static partial VolumeListItemViewModel ToViewModel(this VolumeResponse response);

    public static partial IList<NetworkListItemViewModel> ToViewModel(
        this IList<NetworkResponse> responses
    );

    public static partial NetworkListItemViewModel ToViewModel(this NetworkResponse response);
}
