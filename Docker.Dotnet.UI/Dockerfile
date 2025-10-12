FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# 需要 root 权限来访问 Docker socket
# USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Docker.Dotnet.UI/Docker.Dotnet.UI.csproj", "Docker.Dotnet.UI/"]
RUN dotnet restore "Docker.Dotnet.UI/Docker.Dotnet.UI.csproj"
COPY . .
WORKDIR "/src/Docker.Dotnet.UI"
RUN dotnet build "./Docker.Dotnet.UI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Docker.Dotnet.UI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Docker.Dotnet.UI.dll"]
