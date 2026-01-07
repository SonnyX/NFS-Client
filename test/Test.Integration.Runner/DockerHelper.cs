using System.Diagnostics;

namespace Test.Integration.Runner;

/// <summary>
/// Helper class for managing Docker Compose operations.
/// </summary>
public static class DockerHelper
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Checks if Docker is available on the system.
    /// </summary>
    public static async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var result = await RunCommandAsync("docker", "version", TimeSpan.FromSeconds(10));
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Starts containers defined in a Docker Compose file.
    /// </summary>
    public static async Task ComposeUpAsync(
        string composeFilePath,
        string? serviceName = null,
        TimeSpan? timeout = null)
    {
        var args = $"-f \"{composeFilePath}\" up -d";
        if (!string.IsNullOrEmpty(serviceName))
        {
            args += $" {serviceName}";
        }

        var result = await RunCommandAsync("docker-compose", args, timeout ?? DefaultTimeout);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker-compose up failed with exit code {result.ExitCode}. " +
                $"Error: {result.StandardError}");
        }
    }

    /// <summary>
    /// Stops and removes containers defined in a Docker Compose file.
    /// </summary>
    public static async Task ComposeDownAsync(string composeFilePath, TimeSpan? timeout = null)
    {
        var args = $"-f \"{composeFilePath}\" down -v";
        await RunCommandAsync("docker-compose", args, timeout ?? DefaultTimeout);
    }

    /// <summary>
    /// Checks if a container is running.
    /// </summary>
    public static async Task<bool> IsContainerRunningAsync(string containerName)
    {
        var args = $"ps -q -f name={containerName}";
        var result = await RunCommandAsync("docker", args, TimeSpan.FromSeconds(10));
        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    /// <summary>
    /// Waits for a container to become healthy.
    /// </summary>
    public static async Task WaitForHealthyAsync(string containerName, TimeSpan timeout)
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow - startTime < timeout)
        {
            var args = $"inspect --format=\"{{{{.State.Health.Status}}}}\" {containerName}";
            var result = await RunCommandAsync("docker", args, TimeSpan.FromSeconds(10));

            if (result.ExitCode == 0)
            {
                var status = result.StandardOutput.Trim().Trim('"');
                if (status.Equals("healthy", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException(
            $"Container {containerName} did not become healthy within {timeout.TotalSeconds} seconds");
    }

    /// <summary>
    /// Waits for a container to be running and optionally healthy.
    /// </summary>
    public static async Task WaitForContainerAsync(
        string containerName,
        TimeSpan timeout,
        bool waitForHealthy = true)
    {
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            if (await IsContainerRunningAsync(containerName))
            {
                break;
            }
            await Task.Delay(500);
        }

        if (!await IsContainerRunningAsync(containerName))
        {
            throw new TimeoutException(
                $"Container {containerName} did not start within {timeout.TotalSeconds} seconds");
        }

        if (waitForHealthy)
        {
            var remainingTime = timeout - (DateTime.UtcNow - startTime);
            if (remainingTime > TimeSpan.Zero)
            {
                await WaitForHealthyAsync(containerName, remainingTime);
            }
        }
    }

    /// <summary>
    /// Gets the logs from a container.
    /// </summary>
    public static async Task<string> GetContainerLogsAsync(string containerName, int? tail = null)
    {
        var args = $"logs {containerName}";
        if (tail.HasValue)
        {
            args += $" --tail {tail.Value}";
        }

        var result = await RunCommandAsync("docker", args, TimeSpan.FromSeconds(30));
        return result.StandardOutput + result.StandardError;
    }

    /// <summary>
    /// Executes a command in a running container.
    /// </summary>
    public static async Task<CommandResult> ExecAsync(string containerName, string command)
    {
        var args = $"exec {containerName} {command}";
        return await RunCommandAsync("docker", args, TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Gets the infrastructure directory path.
    /// </summary>
    public static string GetInfrastructurePath()
    {
        var assemblyPath = AppContext.BaseDirectory;
        var current = new DirectoryInfo(assemblyPath);

        while (current != null)
        {
            var infrastructurePath = Path.Combine(current.FullName, "Infrastructure");
            if (Directory.Exists(infrastructurePath))
            {
                return infrastructurePath;
            }

            infrastructurePath = Path.Combine(current.FullName, "test", "Test.Integration", "Infrastructure");
            if (Directory.Exists(infrastructurePath))
            {
                return infrastructurePath;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find Infrastructure directory. Ensure the test data is properly deployed.");
    }

    /// <summary>
    /// Gets the compose file path for a specific NFS version.
    /// </summary>
    public static string GetComposeFilePath(int version)
    {
        var infrastructurePath = GetInfrastructurePath();
        return Path.Combine(infrastructurePath, $"nfsv{version}", "docker-compose.yml");
    }

    private static async Task<CommandResult> RunCommandAsync(
        string command,
        string arguments,
        TimeSpan timeout)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                outputBuilder.AppendLine(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                errorBuilder.AppendLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch { }
            throw new TimeoutException($"Command '{command} {arguments}' timed out after {timeout.TotalSeconds} seconds");
        }

        return new CommandResult(
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString());
    }
}

/// <summary>
/// Represents the result of a command execution.
/// </summary>
public record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}
