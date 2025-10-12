namespace Docker.Dotnet.UI;

public static class DependencyInjection
{
    // define default docker url for different O
    private const string DockerDefaultUri = "npipe://./pipe/docker_engine";
    
    public static IServiceCollection AddDockerDotnet(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
        {
            var dockerUri = Environment.GetEnvironmentVariable("DOCKER_HOST") ?? DockerDefaultUri;
            var client = new Docker.DotNet.DockerClientConfiguration(new Uri(dockerUri)).CreateClient();
            return client;
        });

        return services;
    }
}