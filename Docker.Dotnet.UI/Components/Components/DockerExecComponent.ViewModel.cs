using System.Text.Json;
using Docker.DotNet;
using Docker.DotNet.Models;
using Docker.Dotnet.UI.ViewModels;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Docker.Dotnet.UI.Components.Components;

[RegisterScoped(typeof(DockerExecComponentViewModel))]
public class DockerExecComponentViewModel(
    DockerClient dockerClient,
    ILogger<DockerExecComponentViewModel> logger) : ViewModel
{
    public override async Task InitializeAsync()
    {
        // try
        // {
        //     // var exec = await dockerClient.Exec.StartWithConfigContainerExecAsync(
        //     //     ContainerId,
        //     //     new ContainerExecStartParameters
        //     //     {
        //     //         User = null,
        //     //         Privileged = false,
        //     //         Tty = false,
        //     //         AttachStdin = true,
        //     //         AttachStderr = true,
        //     //         AttachStdout = true,
        //     //         Detach = false,
        //     //         DetachKeys = null,
        //     //         Env = null,
        //     //         WorkingDir = null,
        //     //         Cmd = ["/bin/sh"]
        //     //     }
        //     // );
        //     // var exec = await dockerClient.Exec.StartContainerExecAsync(ContainerId,
        //     //     new ContainerExecCreateParameters
        //     //     {
        //     //         User = null,
        //     //         Privileged = false,
        //     //         Tty = false,
        //     //         AttachStdin = true,
        //     //         AttachStderr = true,
        //     //         AttachStdout = true,
        //     //         Detach = false,
        //     //         DetachKeys = null,
        //     //         Env = null,
        //     //         WorkingDir = "/",
        //     //         Cmd = ["/bin/sh", "-c", "ls"]
        //     //     }
        //     //     );
        //     
        //     var exec = await dockerClient.Exec.ExecCreateContainerAsync(
        //         ContainerId,
        //         new Docker.DotNet.Models.ContainerExecCreateParameters
        //         {
        //             AttachStdin = true,
        //             AttachStdout = true,
        //             AttachStderr = true,
        //             Detach = false,
        //             Tty = true,
        //             Cmd = ["/bin/sh"]
        //         }
        //     );
        //     
        //     logger.LogInformation("Exec created with ID: {response}", exec.ID);
        //
        //     var multiplexedStream = await dockerClient.Exec.StartAndAttachContainerExecAsync(exec.ID, true);
        //     
        //     var buffer = new byte[1024];
        //     var result = await multiplexedStream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
        //     var outputString = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
        //     logger.LogInformation("Initial exec output: {output}", outputString);
        //     // var output = await multiplexedStream.ReadOutputToEndAsync(CancellationToken.None);
        //     // logger.LogInformation("Initial exec output: {output}", output);
        //     
        //     var inspect = await dockerClient.Exec.InspectContainerExecAsync(exec.ID);
        //     logger.LogInformation("Exec inspect: {inspect}", JsonSerializer.Serialize(inspect));
        //     
        //     // try to execute a command inside the exec session
        //     var command = "ls -la";
        //     var commandBytes = System.Text.Encoding.UTF8.GetBytes(command + "\n");
        //     await multiplexedStream.WriteAsync(commandBytes, 0, commandBytes.Length, CancellationToken.None);
        //     logger.LogInformation("Wrote command to exec stream: {command}", command);
        //     
        //     result = await multiplexedStream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
        //     outputString = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
        //     logger.LogInformation("Exec output after command '{command}': {output}", command, outputString);
        //     
        // }
        // catch (Exception ex)
        // {
        //     logger.LogError(ex, "Error executing command in container {ContainerId}", ContainerId);
        //     // ExecOutput += $"Error: {ex.Message}\n";
        // }
    }

    public string ContainerId { get; set; } = string.Empty;

    public string InputCommand { get; set; } = string.Empty;

    public List<string> ExecOutputList { get; set; } = new();
    public List<string> InputHistory { get; set; } = new();

    public MudTextField<string>? InputField = null!;

    public async Task HandleKeyPress(KeyboardEventArgs args)
    {
        if (args.Key == "Enter" && !string.IsNullOrWhiteSpace(InputCommand))
        {
            var commandToExecute = InputCommand;
            InputHistory.Add(commandToExecute);
            await ExecuteCommandAsync(commandToExecute);

            // clear the input field and set focus back to it
            if (InputField is not null)
            {
                await InputField.Clear();
                await InputField.BlurAsync();
                await InputField.FocusAsync();
            }
        }
    }

    public async Task ExecuteCommandAsync(string command)
    {
        try
        {
            logger.LogInformation("Starting exec in container {ContainerId} with command: {InputCommand}", ContainerId,
                InputCommand);
            var exec = await dockerClient.Exec.ExecCreateContainerAsync(
                ContainerId,
                new Docker.DotNet.Models.ContainerExecCreateParameters
                {
                    AttachStdin = true,
                    AttachStdout = true,
                    AttachStderr = true,
                    Detach = false,
                    Tty = true,
                    Cmd = ["/bin/sh", "-c", command]
                }
            );

            logger.LogInformation("Exec created with ID: {response}", exec.ID);

            var stream = await dockerClient.Exec.StartAndAttachContainerExecAsync(exec.ID, false);
            var output = await stream.ReadOutputToEndAsync(CancellationToken.None);
            ExecOutputList.Add($"{output.stdout}{output.stderr}");

            var inspect = await dockerClient.Exec.InspectContainerExecAsync(exec.ID);
            logger.LogInformation("Exec inspect: {inspect}", JsonSerializer.Serialize(inspect));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command in container {ContainerId}", ContainerId);
            // ExecOutput += $"Error: {ex.Message}\n";
        }
    }
}