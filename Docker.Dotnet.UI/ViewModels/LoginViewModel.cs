using System.ComponentModel.DataAnnotations;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(LoginViewModel))]
public class LoginViewModel : ViewModel
{
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 重置表单状态
    /// </summary>
    public void Reset()
    {
        Username = string.Empty;
        Password = string.Empty;
        RememberMe = false;
        IsLoading = false;
        ErrorMessage = null;
    }

    /// <summary>
    /// 设置加载状态
    /// </summary>
    public void SetLoading(bool isLoading)
    {
        IsLoading = isLoading;
    }

    /// <summary>
    /// 设置错误消息
    /// </summary>
    public void SetError(string? errorMessage)
    {
        ErrorMessage = errorMessage;
    }
}
