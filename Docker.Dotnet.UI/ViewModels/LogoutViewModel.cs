using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(LogoutViewModel))]
public class LogoutViewModel(IJSRuntime jsRuntime) : ViewModel
{
    public override async Task InitializeAsync()
    {
        // Submit logout form via JavaScript
        await jsRuntime.InvokeVoidAsync("submitLogoutForm");
    }
}
