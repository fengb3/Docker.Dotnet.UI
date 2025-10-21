namespace Docker.Dotnet.UI.ViewModels;

public interface IViewModel
{
    /// <summary>
    /// Initialize viewmodel's content
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();

    /// <summary>
    /// Event fired when the ViewModel state changes and the UI should be refreshed
    /// </summary>
    event Action? OnStateChanged;
}

public abstract class ViewModel : IViewModel
{
    /// <inheritdoc/>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public event Action? OnStateChanged;

    /// <summary>
    /// Notifies the UI that the ViewModel state has changed and should be refreshed
    /// </summary>
    protected void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
