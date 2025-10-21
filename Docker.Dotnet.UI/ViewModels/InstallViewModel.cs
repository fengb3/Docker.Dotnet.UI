using System.ComponentModel.DataAnnotations;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(InstallViewModel))]
public class InstallViewModel : ViewModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Reset form state
    /// </summary>
    public void Reset()
    {
        Username = string.Empty;
        Password = string.Empty;
        IsLoading = false;
        ErrorMessage = null;
        SuccessMessage = null;
    }

    /// <summary>
    /// Set loading state
    /// </summary>
    public void SetLoading(bool isLoading)
    {
        IsLoading = isLoading;
    }

    /// <summary>
    /// Set error message
    /// </summary>
    public void SetError(string? errorMessage)
    {
        ErrorMessage = errorMessage;
        SuccessMessage = null;
    }

    /// <summary>
    /// Set success message
    /// </summary>
    public void SetSuccess(string? successMessage)
    {
        SuccessMessage = successMessage;
        ErrorMessage = null;
    }
}
