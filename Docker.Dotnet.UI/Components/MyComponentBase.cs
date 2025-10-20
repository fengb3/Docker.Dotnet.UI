using Docker.Dotnet.UI.Services;
using Docker.Dotnet.UI.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Docker.Dotnet.UI.Components;

public class MyComponentBase<T> : ComponentBase, IDisposable
    where T : notnull, IViewModel
{
    [Inject]
    protected T? Vm { get; set; }

    [Inject]
    protected IStringLocalizer Localizer { get; set; } = null!;

    private bool _isDisposed;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
            return;

        // Subscribe to language change events
        if (Localizer is MyLocalizer myLocalizer)
        {
            myLocalizer.LanguageChanged += OnLanguageChanged;
        }

        if (Vm == null)
        {
            throw new NullReferenceException($"ViewModel is null when loading {typeof(T).Name}");
        }

        await Vm.InitializeAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Called when the language is changed. Override to add custom behavior.
    /// </summary>
    protected virtual void OnLanguageChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        // Unsubscribe from language change events
        if (Localizer is MyLocalizer myLocalizer)
        {
            myLocalizer.LanguageChanged -= OnLanguageChanged;
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
