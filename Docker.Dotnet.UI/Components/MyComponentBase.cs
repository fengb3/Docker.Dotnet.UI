using Docker.Dotnet.UI.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Docker.Dotnet.UI.Components;

public class MyComponentBase<T> : ComponentBase
    where T : notnull, IViewModel
{
    [Inject]
    protected T? Vm { get; set; }

    [Inject]
    protected IStringLocalizer Localizer { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
            return;

        if (Vm == null)
        {
            throw new NullReferenceException($"ViewModel is null when loading {typeof(T).Name}");
        }

        await Vm.InitializeAsync();
        StateHasChanged();
    }
}
