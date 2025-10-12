# PowerShell 脚本 - 用于在 Windows 上构建和运行 Docker 镜像

Write-Host "正在构建 Docker 镜像..." -ForegroundColor Green

# 构建镜像
docker build -t docker-dotnet-ui:latest -f Docker.Dotnet.UI/Dockerfile .

if ($LASTEXITCODE -eq 0) {
    Write-Host "镜像构建成功！" -ForegroundColor Green
    Write-Host ""
    Write-Host "运行容器的方式:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "方式 1 - 使用 docker-compose:" -ForegroundColor Cyan
    Write-Host "  docker-compose up -d"
    Write-Host ""
    Write-Host "方式 2 - 使用 docker run (Linux/WSL2):" -ForegroundColor Cyan
    Write-Host "  docker run -d --name docker-dotnet-ui -p 8080:8080 -p 8081:8081 -v /var/run/docker.sock:/var/run/docker.sock docker-dotnet-ui:latest"
    Write-Host ""
    Write-Host "方式 3 - 使用 docker run (Windows + Docker Desktop):" -ForegroundColor Cyan
    Write-Host "  docker run -d --name docker-dotnet-ui -p 8080:8080 -p 8081:8081 -v //var/run/docker.sock:/var/run/docker.sock docker-dotnet-ui:latest"
    Write-Host ""
    Write-Host "访问地址: http://localhost:8080" -ForegroundColor Green
} else {
    Write-Host "镜像构建失败！" -ForegroundColor Red
    exit 1
}

