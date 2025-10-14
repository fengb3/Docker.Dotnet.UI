namespace Docker.Dotnet.UI.ViewModels;

public interface IViewModel
{
    /// <summary>
    /// Initialize viewmodel's content
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();
}

public abstract class ViewModel : IViewModel
{
    /// <inheritdoc/>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }
}
