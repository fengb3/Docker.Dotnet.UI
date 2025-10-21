using Docker.Dotnet.UI.Database.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Docker.Dotnet.UI.Controllers;

[Route("api/[controller]")]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<AccountController> logger
) : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
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

    /// <summary>
    /// 处理初始安装 - 创建第一个管理员用户
    /// </summary>
    [HttpPost("Install")]
    [AllowAnonymous]
    public async Task<IActionResult> Install(
        [FromForm] string username,
        [FromForm] string password
    )
    {
        try
        {
            // 检查是否已经存在用户
            var users = _userManager.Users.ToList();
            if (users.Count > 0)
            {
                _logger.LogWarning("尝试安装但系统中已存在用户");
                return Redirect("/Account/Login");
            }

            // 创建管理员用户
            var adminUser = new ApplicationUser
            {
                UserName = username,
                Email = $"{username}@localhost.com",
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(adminUser, password);

            if (result.Succeeded)
            {
                _logger.LogInformation("成功创建初始管理员用户: {Username}", username);
                
                // 自动登录新创建的用户
                await _signInManager.SignInAsync(adminUser, isPersistent: false);
                
                return Redirect("/");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("创建初始管理员用户失败: {Errors}", errors);
                return Redirect($"/Account/Install?error={Uri.EscapeDataString("INSTALL_ERROR")}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "安装过程中发生错误");
            return Redirect($"/Account/Install?error={Uri.EscapeDataString("INSTALL_ERROR")}");
        }
    }
}
