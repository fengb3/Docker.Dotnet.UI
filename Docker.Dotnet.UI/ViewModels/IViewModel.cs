namespace Docker.Dotnet.UI.ViewModels;

public interface IViewModel
{
    /// <summary>
    /// Initialize viewmodel's content
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();
}