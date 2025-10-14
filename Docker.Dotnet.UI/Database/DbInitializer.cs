using Docker.Dotnet.UI.Database.Models;
using Microsoft.AspNetCore.Identity;

namespace Docker.Dotnet.UI.Database;

public static class DbInitializer
{
    public static async Task SeedDefaultUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        const string defaultUsername = "admin";
        const string defaultPassword = "Test123!";

        try
        {
            // 检查是否已存在管理员用户
            var existingUser = await userManager.FindByNameAsync(defaultUsername);
            if (existingUser != null)
            {
                logger.LogInformation("默认管理员用户已存在");
                return;
            }

            // 创建默认管理员用户
            var adminUser = new ApplicationUser
            {
                UserName = defaultUsername,
                Email = "admin@localhost.com",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, defaultPassword);

            if (result.Succeeded)
            {
                logger.LogInformation("成功创建默认管理员用户: {Username}", defaultUsername);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("创建默认管理员用户失败: {Errors}", errors);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "初始化数据库种子数据时发生错误");
        }
    }
}

