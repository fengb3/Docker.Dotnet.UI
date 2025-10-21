using Microsoft.AspNetCore.Components;
using System.Diagnostics;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(ErrorViewModel))]
public class ErrorViewModel : ViewModel
{
    [CascadingParameter]
    public HttpContext? HttpContext { get; set; }

    public string? RequestId { get; private set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    public override Task InitializeAsync()
    {
        RequestId = Activity.Current?.Id ?? HttpContext?.TraceIdentifier;
        return Task.CompletedTask;
    }
}
