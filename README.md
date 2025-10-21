# Docker.Dotnet.UI

一个基于 Blazor 的 Docker 管理界面，可以通过 Web 浏览器管理 Docker 容器、镜像和卷。

## 前置要求

- 已安装 Docker Desktop（Windows）或 Docker Engine（Linux/Mac）
- 已安装 .NET 9.0 SDK（用于本地开发运行）

## 快速开始

### 方法 1：本地直接运行（最简单）⭐

```bash
cd Docker.Dotnet.UI
dotnet run
```

然后在浏览器访问: http://localhost:5000 或 https://localhost:5001

### 方法 2：使用一键脚本

**Windows (PowerShell):**

```powershell
.\build-and-run.ps1
```

**Linux/Mac:**

```bash
chmod +x build-and-run.sh
./build-and-run.sh
```

脚本会自动构建 Docker 镜像，然后提示您运行容器。

### 方法 3：使用 Docker Compose

```bash
# 构建并启动
docker-compose up -d

# 查看日志
docker-compose logs -f

# 停止
docker-compose down
```

访问地址: http://localhost:8080

## 功能特性

### 容器管理 📦
- 查看所有容器（运行中/已停止）
- 启动、停止、重启容器
- 暂停/恢复容器
- 查看容器日志
- 查看容器详细信息
- 删除容器

### 镜像管理 🖼️
- 查看所有镜像
- 从注册表拉取镜像
- 从 Tarball 导入镜像
- 导出镜像到 Tarball
- 查看镜像详细信息
- 删除镜像

### 卷管理 💾
- 查看所有卷
- 创建新卷
- 查看卷详细信息
- 清理未使用的卷
- 删除卷

### 网络管理 🌐 (新功能)
- 查看所有网络
- 创建新网络（支持多种驱动）
- 查看网络详细信息
- 删除网络

### 其他特性
- 🎨 **现代化 UI**: 使用 MudBlazor 构建的美观界面
- 🌍 **多语言支持**: 支持中文、英文、日文、韩文、法文、西班牙文
- 🔒 **身份验证**: ASP.NET Core Identity 用户认证
- 📊 **实时更新**: 操作后自动刷新列表

## 详细文档

- 功能详情和实现说明请参阅 [ENHANCEMENTS.md](ENHANCEMENTS.md)
- Docker 部署详细信息请参阅 [DOCKER_DEPLOY.md](DOCKER_DEPLOY.md)

## 注意事项

⚠️ 本应用需要访问 Docker socket 来管理容器。在生产环境中使用时，请确保有适当的安全措施。

## 许可证

MIT License
