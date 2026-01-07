using System.Diagnostics;

namespace Test.Integration.Helpers;

/// <summary>
/// Helper class for managing Docker Compose operations.
/// Provides methods to start, stop, and manage Docker containers for NFS testing.
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
    /// <param name="composeFilePath">Path to the docker-compose.yml file.</param>
    /// <param name="serviceName">Optional service name to start. If null, starts all services.</param>
    /// <param name="timeout">Optional timeout for the operation.</param>
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
    /// <param name="composeFilePath">Path to the docker-compose.yml file.</param>
    /// <param name="timeout">Optional timeout for the operation.</param>
    public static async Task ComposeDownAsync(string composeFilePath, TimeSpan? timeout = null)
    {
        var args = $"-f \"{composeFilePath}\" down -v";
        var result = await RunCommandAsync("docker-compose", args, timeout ?? DefaultTimeout);

        // Don't throw on compose down - it might fail if containers were already stopped
        if (result.ExitCode != 0)
        {
            Console.WriteLine($"Warning: docker-compose down returned exit code {result.ExitCode}");
        }
    }

    /// <summary>
    /// Checks if a container is running.
    /// </summary>
    /// <param name="containerName">Name of the container to check.</param>
    public static async Task<bool> IsContainerRunningAsync(string containerName)
    {
        var args = $"ps -q -f name={containerName}";
        var result = await RunCommandAsync("docker", args, TimeSpan.FromSeconds(10));
        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    /// <summary>
    /// Waits for a container to become healthy.
    /// </summary>
    /// <param name="containerName">Name of the container to check.</param>
    /// <param name="timeout">Maximum time to wait for healthy status.</param>
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
    /// <param name="containerName">Name of the container.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="waitForHealthy">Whether to also wait for healthy status.</param>
    public static async Task WaitForContainerAsync(
        string containerName,
        TimeSpan timeout,
        bool waitForHealthy = true)
    {
        var startTime = DateTime.UtcNow;

        // First wait for container to be running
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

        // Then wait for healthy if requested
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
    /// <param name="containerName">Name of the container.</param>
    /// <param name="tail">Number of lines to retrieve (null for all).</param>
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
    /// <param name="containerName">Name of the container.</param>
    /// <param name="command">Command to execute.</param>
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
        // Start from the assembly location and navigate to Infrastructure
        var assemblyPath = AppContext.BaseDirectory;

        // Try to find Infrastructure directory by walking up
        var current = new DirectoryInfo(assemblyPath);
        while (current != null)
        {
            var infrastructurePath = Path.Combine(current.FullName, "Infrastructure");
            if (Directory.Exists(infrastructurePath))
            {
                return infrastructurePath;
            }

            // Also check test/Test.Integration/Infrastructure
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
    /// <param name="version">NFS version (2, 3, or 4).</param>
    public static string GetComposeFilePath(int version)
    {
        var infrastructurePath = GetInfrastructurePath();
        return Path.Combine(infrastructurePath, $"nfsv{version}", "docker-compose.yml");
    }

    /// <summary>
    /// Gets the combined compose file path.
    /// </summary>
    public static string GetCombinedComposeFilePath()
    {
        var infrastructurePath = GetInfrastructurePath();
        return Path.Combine(infrastructurePath, "docker-compose.yml");
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
            catch
            {
                // Ignore errors during kill
            }
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
    /// <summary>
    /// Gets whether the command succeeded (exit code 0).
    /// </summary>
    public bool Success => ExitCode == 0;
}
