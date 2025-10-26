using Docker.Dotnet.UI.Services;
using Docker.Dotnet.UI.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Docker.Dotnet.UI.Components;

/// <summary>
/// Base component class for components that don't require a ViewModel.
/// Provides localization support and language change handling.
/// </summary>
public class MyComponentBase : ComponentBase, IDisposable
{
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
        
        await MyOnInitializedAsync();

        StateHasChanged();
    }
    
    protected virtual Task MyOnInitializedAsync()
    {
        return Task.CompletedTask;
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            // Unsubscribe from language change events
            if (Localizer is MyLocalizer myLocalizer)
            {
                myLocalizer.LanguageChanged -= OnLanguageChanged;
            }
        }

        _isDisposed = true;
    }
}

/// <summary>
/// Base component class with ViewModel support.
/// Provides ViewModel lifecycle management, localization, and state change handling.
/// </summary>
public class MyComponentBase<T> : MyComponentBase
    where T : notnull, IViewModel
{
    [Inject]
    protected T? Vm { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (!firstRender)
            return;

        if (Vm == null)
        {
            throw new NullReferenceException($"ViewModel is null when loading {typeof(T).Name}");
        }

        // Subscribe to ViewModel state change events
        Vm.OnStateChanged += OnViewModelStateChanged;

        await MyOnInitializedAsync();
        await Vm.InitializeAsync();
        StateHasChanged();
    }

    /// <summary>
    /// Called when the ViewModel state changes. Override to add custom behavior.
    /// </summary>
    protected virtual void OnViewModelStateChanged()
    {
        // Console.WriteLine("on view model state changed invoked");
        InvokeAsync(StateHasChanged);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from ViewModel state change events
            if (Vm != null)
            {
                Vm.OnStateChanged -= OnViewModelStateChanged;
            }
        }

        base.Dispose(disposing);
    }
}
