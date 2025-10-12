# Docker 部署说明

本文档说明如何将 Docker.Dotnet.UI 应用打包为 Docker 镜像并运行。

## 前提条件

- 已安装 Docker Desktop（Windows）或 Docker Engine（Linux）
- 已安装 .NET 9.0 SDK（仅用于本地开发）

## 重要说明

此应用使用 Docker.DotNet 包来管理宿主机的 Docker 容器和镜像。因此，运行容器时需要：
1. **挂载 Docker socket**：让容器能够访问宿主机的 Docker daemon
2. **以 root 权限运行**：确保有权限访问 Docker socket

## 构建镜像

### 方式 1：使用脚本（推荐）

**Windows (PowerShell):**
```powershell
.\build-and-run.ps1
```

**Linux/Mac (Bash):**
```bash
chmod +x build-and-run.sh
./build-and-run.sh
```

### 方式 2：手动构建

```bash
docker build -t docker-dotnet-ui:latest -f Docker.Dotnet.UI/Dockerfile .
```

## 运行容器

### 方式 1：使用 Docker Compose（推荐）

```bash
docker-compose up -d
```

停止容器：
```bash
docker-compose down
```

### 方式 2：使用 Docker Run

**Linux/Mac 或 WSL2:**
```bash
docker run -d \
  --name docker-dotnet-ui \
  -p 8080:8080 \
  -p 8081:8081 \
  -v /var/run/docker.sock:/var/run/docker.sock \
  docker-dotnet-ui:latest
```

**Windows + Docker Desktop:**
```powershell
docker run -d `
  --name docker-dotnet-ui `
  -p 8080:8080 `
  -p 8081:8081 `
  -v //var/run/docker.sock:/var/run/docker.sock `
  docker-dotnet-ui:latest
```

## 访问应用

容器启动后，在浏览器中访问：
- HTTP: http://localhost:8080
- HTTPS: https://localhost:8081

## 常用命令

```bash
# 查看运行中的容器
docker ps

# 查看容器日志
docker logs docker-dotnet-ui

# 停止容器
docker stop docker-dotnet-ui

# 启动容器
docker start docker-dotnet-ui

# 删除容器
docker rm docker-dotnet-ui

# 删除镜像
docker rmi docker-dotnet-ui:latest
```

## 故障排除

### 问题 1：无法连接到 Docker

**错误信息**: 类似 "Cannot connect to Docker daemon"

**解决方案**:
1. 确保 Docker socket 已正确挂载：`-v /var/run/docker.sock:/var/run/docker.sock`
2. 检查 Docker 服务是否正在运行
3. Windows 用户确保 Docker Desktop 已启动并启用 WSL2 集成

### 问题 2：权限被拒绝

**错误信息**: "Permission denied while trying to connect to Docker daemon"

**解决方案**:
- 容器需要以 root 权限运行才能访问 Docker socket
- Dockerfile 已配置为不使用非特权用户（注释掉了 `USER $APP_UID`）

### 问题 3：端口已被占用

**错误信息**: "Bind for 0.0.0.0:8080 failed: port is already allocated"

**解决方案**:
修改端口映射，例如：
```bash
docker run -d --name docker-dotnet-ui -p 9080:8080 -p 9081:8081 -v /var/run/docker.sock:/var/run/docker.sock docker-dotnet-ui:latest
```

## 环境变量

可以通过环境变量配置应用：

```bash
docker run -d \
  --name docker-dotnet-ui \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e DOCKER_HOST=unix:///var/run/docker.sock \
  -v /var/run/docker.sock:/var/run/docker.sock \
  docker-dotnet-ui:latest
```

### 可用的环境变量

- `ASPNETCORE_ENVIRONMENT`: ASP.NET Core 环境（Development/Production）
- `ASPNETCORE_URLS`: 监听的 URL
- `DOCKER_HOST`: Docker 连接字符串（默认会自动检测）

## 生产环境部署建议

1. **使用反向代理**：建议在生产环境中使用 Nginx 或 Traefik 作为反向代理
2. **启用 HTTPS**：配置 SSL 证书以启用 HTTPS
3. **资源限制**：为容器设置 CPU 和内存限制
   ```bash
   docker run -d --name docker-dotnet-ui \
     --memory="512m" --cpus="1.0" \
     -p 8080:8080 \
     -v /var/run/docker.sock:/var/run/docker.sock \
     docker-dotnet-ui:latest
   ```
4. **日志管理**：配置日志驱动程序以便于日志收集和分析
5. **健康检查**：添加健康检查以确保容器正常运行

## 安全注意事项

⚠️ **重要**: 挂载 Docker socket 意味着容器内的应用拥有控制宿主机 Docker 的完全权限。请确保：
1. 只在可信环境中运行此容器
2. 如果需要远程访问，务必启用身份验证和授权
3. 定期更新基础镜像以修复安全漏洞
4. 考虑使用 Docker socket 代理（如 Tecnativa/docker-socket-proxy）来限制容器的权限

