namespace Docker.Dotnet.UI;

public static class DependencyInjection
{
    // 根据操作系统自动选择 Docker 连接方式
    private static string GetDefaultDockerUri()
    {
        // Linux/Unix 使用 unix socket
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return "unix:///var/run/docker.sock";
        }
        // Windows 使用 named pipe
        return "npipe://./pipe/docker_engine";
    }

    public static IServiceCollection AddDockerDotnet(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var dockerUri =
                Environment.GetEnvironmentVariable("DOCKER_HOST") ?? GetDefaultDockerUri();
            var client = new Docker.DotNet.DockerClientConfiguration(
                new Uri(dockerUri)
            ).CreateClient();
            return client;
        });

        return services;
    }
}
