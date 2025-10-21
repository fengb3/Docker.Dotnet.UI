using System.ComponentModel.DataAnnotations;
using Docker.Dotnet.UI.Database.Models;
using Microsoft.AspNetCore.Identity;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(UserProfileViewModel))]
public class UserProfileViewModel : ViewModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UserProfileViewModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Current password is required")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsLoading { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public override async Task InitializeAsync()
    {
        var user = await _signInManager.UserManager.GetUserAsync(
            _signInManager.Context.User
        );
        if (user != null)
        {
            Username = user.UserName ?? string.Empty;
            Email = user.Email ?? string.Empty;
        }
    }

    public async Task<bool> ChangePasswordAsync()
    {
        IsLoading = true;
        SuccessMessage = null;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var user = await _signInManager.UserManager.GetUserAsync(
                _signInManager.Context.User
            );
            if (user == null)
            {
                ErrorMessage = "User not found";
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                CurrentPassword,
                NewPassword
            );

            if (result.Succeeded)
            {
                SuccessMessage = "Password updated successfully";
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
                return true;
            }
            else
            {
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public void ClearMessages()
    {
        SuccessMessage = null;
        ErrorMessage = null;
        NotifyStateChanged();
    }
}
