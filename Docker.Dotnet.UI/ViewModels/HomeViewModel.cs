using Microsoft.AspNetCore.Components;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(HomeViewModel))]
public class HomeViewModel(NavigationManager navigationManager) : ViewModel
{
    public override Task InitializeAsync()
    {
        // Redirect to containers page on initialization
        navigationManager.NavigateTo("/containers");
        return Task.CompletedTask;
    }
}
