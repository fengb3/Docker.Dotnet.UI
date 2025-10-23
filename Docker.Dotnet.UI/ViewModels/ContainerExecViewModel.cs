using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Concurrent;
using System.Text;

namespace Docker.Dotnet.UI.ViewModels;

[RegisterScoped(typeof(ContainerExecViewModel))]
public class ContainerExecViewModel(DockerClient dockerClient, ILogger<ContainerExecViewModel> logger) : ViewModel
{
    // Connection state
    public string? ContainerId { get; set; }
    public string? ContainerName { get; set; }
    public string? ExecId { get; set; }
    public bool IsConnected { get; set; }
    public bool IsConnecting { get; set; }
    public string? ErrorMessage { get; set; }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Terminal output
    private readonly ConcurrentQueue<string> _outputBuffer = new();
    private const int MaxBufferLines = 5000;
    public List<string> TerminalOutput { get; } = new();

    // Exec settings
    public string Shell { get; set; } = "/bin/sh";
    public string? WorkingDir { get; set; }
    public string? User { get; set; }
    public bool Tty { get; set; } = true;
    public int Rows { get; set; } = 24;
    public int Cols { get; set; } = 80;

    // Stream and cancellation
    private MultiplexedStream? _execStream;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readTask;

    public override Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    public async Task OpenExecAsync(string containerId, string containerName, string? shell = null)
    {
        try
        {
            ContainerId = containerId;
            ContainerName = containerName;
            ErrorMessage = null;
            IsConnecting = true;
            NotifyStateChanged();

            // Detect platform and set default shell if not provided
            if (!string.IsNullOrEmpty(shell))
            {
                Shell = shell;
            }
            else
            {
                await DetectAndSetDefaultShellAsync(containerId);
            }

            // Create exec instance
            var execCreateParams = new ContainerExecCreateParameters
            {
                AttachStdin = true,
                AttachStdout = true,
                AttachStderr = true,
                Tty = Tty,
                Cmd = Shell.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                WorkingDir = WorkingDir,
                User = User
            };

            var execCreateResponse = await dockerClient.Exec.ExecCreateContainerAsync(containerId, execCreateParams);
            ExecId = execCreateResponse.ID;

            // Start and attach to exec
            _cancellationTokenSource = new CancellationTokenSource();
            _execStream = await dockerClient.Exec.StartAndAttachContainerExecAsync(ExecId, false, _cancellationTokenSource.Token);

            IsConnected = true;
            IsConnecting = false;
            NotifyStateChanged();

            // Start reading output
            _readTask = Task.Run(ReadOutputLoopAsync, _cancellationTokenSource.Token);

            logger.LogDebug("Container exec session started for {ContainerId} with shell {Shell}", containerId, Shell);
        }
        catch (TimeoutException)
        {
            ErrorMessage = "Connection to Docker timed out. Please ensure Docker Desktop is running.";
            IsConnecting = false;
            IsConnected = false;
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to start exec session: {ex.Message}";
            IsConnecting = false;
            IsConnected = false;
            NotifyStateChanged();
            logger.LogError(ex, "Failed to start exec session for container {ContainerId}", containerId);
        }
    }

    private async Task DetectAndSetDefaultShellAsync(string containerId)
    {
        try
        {
            // Inspect container to determine platform
            var containerInspect = await dockerClient.Containers.InspectContainerAsync(containerId);
            var platform = containerInspect.Platform ?? "linux";

            if (platform.Contains("windows", StringComparison.OrdinalIgnoreCase))
            {
                Shell = "powershell.exe";
            }
            else
            {
                // Try to detect if bash is available, fallback to sh
                Shell = "/bin/sh";
                // We could try to exec "which bash" first, but for simplicity we start with sh
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to detect platform for container {ContainerId}, using default shell", containerId);
            Shell = "/bin/sh";
        }
    }

    private async Task ReadOutputLoopAsync()
    {
        if (_execStream == null || _cancellationTokenSource == null)
            return;

        var buffer = new byte[4096];
        var tempOutput = new List<string>();
        
        try
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await _execStream.ReadOutputAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);

                if (result.EOF)
                {
                    logger.LogDebug("Exec stream reached EOF");
                    break;
                }

                if (result.Count > 0)
                {
                    var output = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _outputBuffer.Enqueue(output);
                    tempOutput.Add(output);

                    // Batch updates to avoid too many UI refreshes
                    if (tempOutput.Count >= 10 || _outputBuffer.Count > 100)
                    {
                        FlushOutputToTerminal();
                        tempOutput.Clear();
                        NotifyStateChanged();
                    }
                }
            }

            // Flush remaining output
            if (tempOutput.Count > 0)
            {
                FlushOutputToTerminal();
                NotifyStateChanged();
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("Exec read loop cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in exec read loop");
            ErrorMessage = "Session disconnected. Container may have stopped.";
            IsConnected = false;
            NotifyStateChanged();
        }
    }

    private void FlushOutputToTerminal()
    {
        while (_outputBuffer.TryDequeue(out var output))
        {
            TerminalOutput.Add(output);
        }

        // Limit buffer size
        while (TerminalOutput.Count > MaxBufferLines)
        {
            TerminalOutput.RemoveAt(0);
        }
    }

    public async Task SendInputAsync(string input)
    {
        if (_execStream == null || !IsConnected || string.IsNullOrEmpty(input))
            return;

        try
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            await _execStream.WriteAsync(bytes, 0, bytes.Length, _cancellationTokenSource?.Token ?? CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send input to exec session");
            ErrorMessage = "Failed to send input. Session may be disconnected.";
            NotifyStateChanged();
        }
    }

    public Task ResizeAsync(int rows, int cols)
    {
        // Store resize parameters for display purposes
        Rows = rows;
        Cols = cols;
        NotifyStateChanged();
        
        // Note: Docker.DotNet may not support ResizeContainerExecAsync in all versions
        // This is a placeholder for future implementation
        return Task.CompletedTask;
    }

    public void ClearTerminal()
    {
        TerminalOutput.Clear();
        while (_outputBuffer.TryDequeue(out _)) { }
        NotifyStateChanged();
    }

    public async Task CloseExecAsync()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            
            if (_readTask != null)
            {
                await _readTask;
            }

            _execStream?.Dispose();
            _cancellationTokenSource?.Dispose();

            IsConnected = false;
            ExecId = null;
            NotifyStateChanged();

            logger.LogDebug("Container exec session closed for {ContainerId}", ContainerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error closing exec session");
        }
    }
}
