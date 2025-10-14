using Docker.Dotnet.UI.Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Dotnet.UI.Controllers;

[Route("api/[controller]")]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    ILogger<AccountController> logger
) : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly ILogger<AccountController> _logger = logger;

    /// <summary>
    /// 处理用户登录
    /// </summary>
    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromForm] string username,
        [FromForm] string password,
        [FromForm] bool rememberMe,
        [FromForm] string? returnUrl
    )
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = "/";
        }

        var result = await _signInManager.PasswordSignInAsync(
            username,
            password,
            rememberMe,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            _logger.LogInformation("用户 {Username} 登录成功", username);
            return Redirect(returnUrl);
        }

        string error;
        if (result.IsLockedOut)
        {
            error = "账户已被锁定";
            _logger.LogWarning("用户 {Username} 账户被锁定", username);
        }
        else
        {
            error = "用户名或密码错误";
            _logger.LogWarning("用户 {Username} 登录失败", username);
        }

        return Redirect(
            $"/Account/Login?error={Uri.EscapeDataString(error)}&returnUrl={Uri.EscapeDataString(returnUrl)}"
        );
    }

    /// <summary>
    /// 处理用户登出
    /// </summary>
    [HttpPost("Logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromForm] string? returnUrl)
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("用户已登出");

        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = "/Account/Login";
        }

        return Redirect(returnUrl);
    }

    /// <summary>
    /// GET 方式登出（用于导航链接）
    /// </summary>
    [HttpGet("Logout")]
    [Authorize]
    public async Task<IActionResult> LogoutGet()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("用户已登出");
        return Redirect("/Account/Login");
    }
}
