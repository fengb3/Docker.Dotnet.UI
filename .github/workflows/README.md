# GitHub Action 配置说明

## 功能说明

当 `Docker.Dotnet.UI/Docker.Dotnet.UI.csproj` 文件中的 `<Version>` 标签发生变化并提交到 master/main 分支时，将自动触发以下操作：

1. 检测版本号是否发生变化
2. 提取新的版本号
3. 构建 Docker 镜像（支持 linux/amd64 和 linux/arm64 平台）
4. 为镜像打上两个标签：`latest` 和 `{version}`
5. 推送到配置的多个镜像仓库

## 配置步骤

### 1. 配置 GitHub Secrets

在你的 GitHub 仓库中，进入 `Settings` -> `Secrets and variables` -> `Actions`，添加以下 Secrets：

#### Docker Hub（可选）
- `DOCKERHUB_USERNAME`: Docker Hub 用户名
- `DOCKERHUB_TOKEN`: Docker Hub 访问令牌（在 Docker Hub 的 Account Settings -> Security 中创建）

#### 自定义镜像仓库 1（可选）
- `CUSTOM_REGISTRY_1_USERNAME`: 自定义仓库 1 的用户名
- `CUSTOM_REGISTRY_1_PASSWORD`: 自定义仓库 1 的密码或令牌

#### 自定义镜像仓库 2（可选）
- `CUSTOM_REGISTRY_2_USERNAME`: 自定义仓库 2 的用户名
- `CUSTOM_REGISTRY_2_PASSWORD`: 自定义仓库 2 的密码或令牌

**注意：** `GITHUB_TOKEN` 是自动提供的，无需手动配置。

### 2. 配置 GitHub Variables

在你的 GitHub 仓库中，进入 `Settings` -> `Secrets and variables` -> `Actions` -> `Variables` 标签页，添加以下 Variables：

#### Docker Hub（可选）
- `DOCKERHUB_REGISTRY`: 例如 `docker.io/your-username` 或留空不使用

#### GitHub Container Registry（可选）
- `GHCR_REGISTRY`: 例如 `ghcr.io` 或留空不使用

#### 自定义镜像仓库（可选）
- `CUSTOM_REGISTRY_1`: 例如 `registry.example.com`
- `CUSTOM_REGISTRY_2`: 例如 `harbor.yourdomain.com`

### 3. 配置示例

#### 仅使用 Docker Hub
```
Variables:
- DOCKERHUB_REGISTRY = docker.io/fengb3

Secrets:
- DOCKERHUB_USERNAME = fengb3
- DOCKERHUB_TOKEN = dckr_pat_xxxxx
```

生成的镜像标签：
- `docker.io/fengb3/docker-dotnet-ui:latest`
- `docker.io/fengb3/docker-dotnet-ui:0.0.1`

#### 使用 GitHub Container Registry
```
Variables:
- GHCR_REGISTRY = ghcr.io

Secrets:
（无需额外配置，使用自动提供的 GITHUB_TOKEN）
```

生成的镜像标签：
- `ghcr.io/{your-username}/docker-dotnet-ui:latest`
- `ghcr.io/{your-username}/docker-dotnet-ui:0.0.1`

#### 使用多个镜像仓库
```
Variables:
- DOCKERHUB_REGISTRY = docker.io/fengb3
- GHCR_REGISTRY = ghcr.io
- CUSTOM_REGISTRY_1 = harbor.example.com

Secrets:
- DOCKERHUB_USERNAME = fengb3
- DOCKERHUB_TOKEN = dckr_pat_xxxxx
- CUSTOM_REGISTRY_1_USERNAME = admin
- CUSTOM_REGISTRY_1_PASSWORD = Harbor12345
```

生成的镜像标签：
- `docker.io/fengb3/docker-dotnet-ui:latest`
- `docker.io/fengb3/docker-dotnet-ui:0.0.1`
- `ghcr.io/{your-username}/docker-dotnet-ui:latest`
- `ghcr.io/{your-username}/docker-dotnet-ui:0.0.1`
- `harbor.example.com/docker-dotnet-ui:latest`
- `harbor.example.com/docker-dotnet-ui:0.0.1`

## 手动触发

如果需要手动触发构建，可以：
1. 进入 GitHub 仓库的 `Actions` 标签页
2. 选择 `构建和推送 Docker 镜像` 工作流
3. 点击 `Run workflow` 按钮

## 使用说明

1. **修改版本号**：编辑 `Docker.Dotnet.UI/Docker.Dotnet.UI.csproj` 文件中的 `<Version>` 标签
   ```xml
   <Version>0.0.2</Version>
   ```

2. **提交并推送**：
   ```bash
   git add Docker.Dotnet.UI/Docker.Dotnet.UI.csproj
   git commit -m "bump version to 0.0.2"
   git push origin master
   ```

3. **查看构建进度**：访问 GitHub 仓库的 Actions 页面查看构建状态

4. **拉取镜像**：
   ```bash
   docker pull docker-dotnet-ui:latest
   # 或
   docker pull docker-dotnet-ui:0.0.2
   ```

## 注意事项

1. 只有当 `<Version>` 标签的值真正发生变化时，才会触发构建和推送
2. 如果未配置任何镜像仓库变量，镜像仍会构建但只会使用本地标签
3. 构建支持 linux/amd64 和 linux/arm64 两种平台
4. 使用 GitHub Actions 缓存加速构建过程
5. 推送到 GHCR 需要在仓库设置中启用 Package 权限

## 故障排查

### 推送失败
- 检查 Secrets 中的凭据是否正确
- 检查 Variables 中的仓库地址格式是否正确
- 检查镜像仓库的访问权限

### 版本检测失败
- 确保 `.csproj` 文件中有 `<Version>` 标签
- 确保提交的分支是 master 或 main

### GHCR 推送失败
- 在仓库 Settings -> Actions -> General 中，确保 Workflow permissions 设置为 "Read and write permissions"

