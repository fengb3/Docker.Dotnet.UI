using Docker.Dotnet.UI.ViewModels;
using Microsoft.AspNetCore.Components;

namespace Docker.Dotnet.UI.Components;

public class MyComponentBase<T> : OwningComponentBase<T> where T : IViewModel
{
    protected T Vm => Service;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        
        if (!firstRender) return;

        await Service.InitializeAsync();
        StateHasChanged();
    }
}