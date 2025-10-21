using System.ComponentModel.DataAnnotations;
using Docker.Dotnet.UI.Database.Models;
using Microsoft.AspNetCore.Identity;

namespace Docker.Dotnet.UI.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserModel
{
    [Required(ErrorMessage = "Username is required")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

[RegisterScoped(typeof(AdminUserManagementViewModel))]
public class AdminUserManagementViewModel : ViewModel
{
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserManagementViewModel(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public IList<UserListItemViewModel>? Users { get; set; }
    public CreateUserModel NewUser { get; set; } = new();
    public bool IsLoading { get; set; }
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }
    public bool ShowCreateDialog { get; set; }

    public override async Task InitializeAsync()
    {
        await LoadUsersAsync();
    }

    public void OpenCreateDialog()
    {
        NewUser = new CreateUserModel();
        ClearMessages();
        ShowCreateDialog = true;
        NotifyStateChanged();
    }

    public void CloseCreateDialog()
    {
        ShowCreateDialog = false;
        NotifyStateChanged();
    }

    public async Task<bool> CreateUserAndCloseDialogAsync()
    {
        var success = await CreateUserAsync();
        if (success)
        {
            CloseCreateDialog();
        }
        return success;
    }

    public async Task LoadUsersAsync()
    {
        IsLoading = true;
        NotifyStateChanged();

        try
        {
            var users = await Task.Run(() => _userManager.Users.ToList());
            Users = users.Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            }).ToList();
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<bool> CreateUserAsync()
    {
        IsLoading = true;
        SuccessMessage = null;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var user = new ApplicationUser
            {
                UserName = NewUser.UserName,
                Email = NewUser.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, NewUser.Password);

            if (result.Succeeded)
            {
                SuccessMessage = "User created successfully";
                NewUser = new CreateUserModel();
                await LoadUsersAsync();
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

    public async Task<bool> DeleteUserAsync(string userId)
    {
        IsLoading = true;
        SuccessMessage = null;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ErrorMessage = "User not found";
                return false;
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                SuccessMessage = "User deleted successfully";
                await LoadUsersAsync();
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
