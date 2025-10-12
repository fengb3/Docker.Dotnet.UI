#!/bin/bash
# Shell 脚本 - 用于在 Linux/Mac 上构建和运行 Docker 镜像

echo "正在构建 Docker 镜像..."

# 构建镜像
docker build -t docker-dotnet-ui:latest -f Docker.Dotnet.UI/Dockerfile .

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ 镜像构建成功！"
    echo ""
    echo "运行容器的方式:"
    echo ""
    echo "方式 1 - 使用 docker-compose:"
    echo "  docker-compose up -d"
    echo ""
    echo "方式 2 - 使用 docker run:"
    echo "  docker run -d --name docker-dotnet-ui -p 8080:8080 -p 8081:8081 -v /var/run/docker.sock:/var/run/docker.sock docker-dotnet-ui:latest"
    echo ""
    echo "访问地址: http://localhost:8080"
else
    echo ""
    echo "✗ 镜像构建失败！"
    exit 1
fi

