# 用户认证说明

## 概述

本应用使用 ASP.NET Core Identity 实现了完整的用户登录认证机制。

## 功能特性

### 1. 用户登录
- **登录页面**: `/Account/Login`
- **登录要求**: 用户名和密码
- **记住我功能**: 支持保持登录状态（7天）
- **登录后跳转**: 自动跳转到原本想访问的页面

### 2. 页面保护
- 所有页面（除了登录、登出和错误页面）都需要用户登录才能访问
- 未登录用户访问受保护页面时，会自动跳转到登录页面
- 未登录时，导航栏和标题栏会被隐藏

### 3. 默认管理员账户
系统会在首次启动时自动创建默认管理员账户：

- **用户名**: `admin`
- **密码**: `Test123!`

## 密码策略

当前密码策略要求：
- 最小长度: 6 个字符
- 必须包含数字
- 必须包含大写字母
- 必须包含小写字母
- 必须包含特殊字符

## 访问流程

1. **未登录用户访问受保护页面**
   - 系统检测到未登录状态
   - 自动跳转到 `/Account/Login?ReturnUrl=原始URL`
   - 登录后自动跳转回原始页面

2. **已登录用户**
   - 可以正常访问所有受保护页面
   - 右上角显示用户菜单，包含：
     - 当前用户名
     - 退出登录按钮

3. **退出登录**
   - 点击用户菜单中的"退出登录"
   - 系统清除登录状态
   - 自动跳转到登录页面

## 技术实现

### 使用的技术
- **ASP.NET Core Identity**: 用户身份验证和授权
- **Entity Framework Core**: 数据持久化
- **SQLite**: 数据库存储
- **MudBlazor**: UI 组件库

### 关键配置

#### Program.cs
```csharp
// Identity 配置
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie 配置
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});
```

#### 页面授权
每个受保护的页面都添加了 `[Authorize]` 属性：

```razor
@page "/containers"
@attribute [Microsoft.AspNetCore.Authorization.Authorize]
```

### 数据库迁移

数据库会在应用启动时自动应用迁移并创建默认用户。相关代码在 `Program.cs` 中：

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    // 应用迁移
    context.Database.Migrate();
    
    // 创建默认用户
    await DbInitializer.SeedDefaultUserAsync(userManager, logger);
}
```

## 相关文件

### 页面组件
- `Components/Pages/Account/Login.razor` - 登录页面
- `Components/Pages/Account/Logout.razor` - 登出页面
- `Components/Pages/Account/AccessDenied.razor` - 访问拒绝页面

### ViewModel
- `ViewModels/LoginViewModel.cs` - 登录业务逻辑

### 数据库
- `Database/ApplicationDbContext.cs` - 数据库上下文
- `Database/Models/ApplicationUser.cs` - 用户模型
- `Database/DbInitializer.cs` - 数据库种子数据

### 布局
- `Components/Layout/MainLayout.razor` - 主布局（包含登录状态检测）

## 使用说明

### 首次运行

1. 运行应用：
   ```bash
   dotnet run
   ```

2. 访问任何页面，系统会自动跳转到登录页面

3. 使用默认管理员账户登录：
   - 用户名: `admin`
   - 密码: `Test123!`

4. 登录成功后，可以正常使用所有功能

### 修改密码策略

如需修改密码策略，请编辑 `Program.cs` 中的 Identity 配置：

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => 
{
    options.Password.RequiredLength = 8; // 修改最小长度
    options.Password.RequireDigit = false; // 不要求数字
    // ... 其他配置
})
```

## 安全建议

1. **生产环境部署前，请务必修改默认管理员密码**
2. 考虑使用更强的密码策略
3. 启用 HTTPS（生产环境必需）
4. 考虑添加双因素认证（2FA）
5. 实施账户锁定策略以防止暴力破解

## 未来增强功能

可能的功能扩展：
- [ ] 用户注册功能
- [ ] 忘记密码/重置密码
- [ ] 邮箱验证
- [ ] 双因素认证（2FA）
- [ ] 角色和权限管理
- [ ] 用户管理界面
- [ ] 登录历史记录
- [ ] 会话管理

