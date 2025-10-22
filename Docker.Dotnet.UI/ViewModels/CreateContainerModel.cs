using System.ComponentModel.DataAnnotations;

namespace Docker.Dotnet.UI.ViewModels;

public class CreateContainerModel
{
    // Basic tab
    [Required]
    public string Image { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public bool PullIfNotExists { get; set; } = true;
    public bool AutoStartAfterCreate { get; set; } = true;
    public List<LabelModel> Labels { get; set; } = new();

    // Ports tab
    public List<PortMappingModel> Ports { get; set; } = new();

    // Volumes tab
    public List<VolumeModel> Volumes { get; set; } = new();

    // Environment variables tab
    public List<EnvironmentVariableModel> EnvironmentVariables { get; set; } = new();

    // Network tab
    public string NetworkMode { get; set; } = "bridge";
    public List<string> NetworkAliases { get; set; } = new();
    public string IPv4Address { get; set; } = string.Empty;

    // Resources tab
    public double CpuLimit { get; set; } = 0;
    public long MemoryLimitMiB { get; set; } = 0;
    public string RestartPolicyType { get; set; } = "no";
    public int MaximumRetryCount { get; set; } = 0;

    // Advanced tab
    public List<string> Entrypoint { get; set; } = new();
    public List<string> Command { get; set; } = new();
    public string WorkingDir { get; set; } = string.Empty;
    public bool Tty { get; set; } = false;
    public bool AttachStdin { get; set; } = false;
    public bool AttachStdout { get; set; } = true;
    public bool AttachStderr { get; set; } = true;
}

public class LabelModel
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class PortMappingModel
{
    public string HostPort { get; set; } = string.Empty;
    [Required]
    public string ContainerPort { get; set; } = string.Empty;
    public string Protocol { get; set; } = "tcp";
}

public class VolumeModel
{
    public string Source { get; set; } = string.Empty;
    [Required]
    public string Target { get; set; } = string.Empty;
    public string Mode { get; set; } = "rw";
}

public class EnvironmentVariableModel
{
    [Required]
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
