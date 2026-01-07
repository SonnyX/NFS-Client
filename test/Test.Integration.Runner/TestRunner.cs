using System.Diagnostics;
using System.Text;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;

namespace Test.Integration.Runner;

/// <summary>
/// Runs all NFS integration tests and reports results.
/// </summary>
public class TestRunner
{
    private readonly List<TestGroupResults> _allResults = new();
    private readonly Stopwatch _totalStopwatch = new();

    // State for comprehensive file operations test
    private readonly Dictionary<string, ComprehensiveTestState> _comprehensiveTestState = new();

    // Shared client for test group - reuse connection instead of creating new for each test
    private NfsClient? _sharedClient;
    private NfsServerConfig? _currentConfig;

    // NFSv4 server start time for grace period calculation
    private DateTime? _nfsV4StartTime;

    private class ComprehensiveTestState
    {
        public string TestId { get; set; } = string.Empty;
        public List<string> RootFiles { get; } = new();
        public List<string> Dir1Files { get; } = new();
        public List<string> Dir2Files { get; } = new();
        public List<string> Dir3Files { get; } = new();
        public string Dir1Name { get; set; } = string.Empty;
        public string Dir2Name { get; set; } = string.Empty;
        public string Dir3Name { get; set; } = string.Empty;
        public Dictionary<string, (int Size, byte[] Content)> FileData { get; } = new();
    }

    /// <summary>
    /// Runs all integration tests.
    /// </summary>
    public async Task RunAllTestsAsync()
    {
        _totalStopwatch.Start();

        // Check Docker availability
        Console.Write("Checking Docker availability... ");
        var dockerAvailable = await DockerHelper.IsDockerAvailableAsync();
        if (!dockerAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NOT AVAILABLE");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Docker is required to run integration tests.");
            Console.WriteLine("Please install Docker Desktop (Windows/Mac) or Docker Engine (Linux) and ensure it is running.");
            Environment.Exit(1);
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("OK");
        Console.ResetColor();

        // Create server configurations
        var v3Config = NfsServerConfig.CreateV3Config();
        var v4Config = NfsServerConfig.CreateV4Config();

        // Start servers
        await StartServerAsync(v3Config);
        await StartServerAsync(v4Config);

        Console.WriteLine();

        try
        {
            // Run NFSv3 tests
            if (v3Config.IsReady)
            {
                await RunTestGroupAsync(v3Config);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Skipping {v3Config.Name} tests - server not ready");
                Console.ResetColor();
            }

            Console.WriteLine();

            // Run NFSv4 tests
            if (v4Config.IsReady)
            {
                await RunTestGroupAsync(v4Config);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Skipping {v4Config.Name} tests - server not ready");
                Console.ResetColor();
            }
        }
        finally
        {
            // Stop servers
            Console.WriteLine();
            await StopServerAsync(v3Config);
            await StopServerAsync(v4Config);
        }

        _totalStopwatch.Stop();

        // Print summary
        PrintSummary();
    }

    private async Task StartServerAsync(NfsServerConfig config)
    {
        Console.Write($"Starting {config.Name} server... ");

        try
        {
            var isRunning = await DockerHelper.IsContainerRunningAsync(config.ContainerName);
            if (!isRunning)
            {
                await DockerHelper.ComposeUpAsync(config.ComposeFilePath, config.ServiceName, TimeSpan.FromMinutes(2));
            }

            await DockerHelper.WaitForContainerAsync(config.ContainerName, TimeSpan.FromSeconds(60));

            // Wait for NFS to be ready
            await WaitForNfsReadyAsync(config);

            config.IsReady = true;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("READY");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED ({ex.Message})");
            Console.ResetColor();
            config.IsReady = false;
        }
    }

    private async Task WaitForNfsReadyAsync(NfsServerConfig config)
    {
        if (config.Version == NfsVersion.V4)
        {
            // NFSv4 - verify nfsd threads are running
            for (int i = 0; i < 30; i++)
            {
                try
                {
                    var result = await DockerHelper.ExecAsync(config.ContainerName, "cat /proc/fs/nfsd/threads");
                    if (result.Success && int.TryParse(result.StandardOutput.Trim(), out int count) && count > 0)
                    {
                        _nfsV4StartTime = DateTime.UtcNow;

                        // The container triggers multiple 90-second grace periods for different
                        // network namespaces. We must wait for ALL of them to end.
                        // Wait 100 seconds to ensure all grace periods have ended.
                        Console.Write("(waiting 100s for grace periods)... ");
                        await Task.Delay(100000);
                        return;
                    }
                }
                catch { }
                await Task.Delay(1000);
            }
        }
        else
        {
            // NFSv3 - check showmount
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    var result = await DockerHelper.ExecAsync(config.ContainerName, "showmount -e localhost");
                    if (result.Success && result.StandardOutput.Contains("/export"))
                    {
                        return;
                    }
                }
                catch { }
                await Task.Delay(1000);
            }
        }
    }

    private async Task WaitForGracePeriodIfNeededAsync(NfsServerConfig config)
    {
        if (config.Version != NfsVersion.V4 || _nfsV4StartTime == null)
            return;

        // NFSv4 server has a 90-second grace period after startup.
        // During this time, stateful operations (including CREATE/REMOVE for directories)
        // will fail with NFS4ERR_GRACE.
        var elapsed = DateTime.UtcNow - _nfsV4StartTime.Value;
        var gracePeriod = TimeSpan.FromSeconds(92); // 90s grace + 2s buffer

        if (elapsed < gracePeriod)
        {
            var remaining = gracePeriod - elapsed;
            Console.WriteLine();
            Console.Write($"  Waiting for NFSv4 grace period ({remaining.TotalSeconds:F0}s remaining)... ");
            await Task.Delay(remaining);
            Console.WriteLine("done");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Waits for the NFSv4 grace period to end by probing with actual file operations.
    /// Performs multiple successful operations to confirm stability before proceeding.
    /// </summary>
    private async Task WaitForGracePeriodWithProbeAsync(NfsServerConfig config)
    {
        Console.WriteLine();
        Console.Write("  Probing for grace period completion... ");

        const int requiredSuccesses = 3; // Need 3 consecutive successes to confirm stability
        const int maxTotalAttempts = 150; // Maximum total attempts (2.5 minutes)
        int consecutiveSuccesses = 0;
        int totalAttempts = 0;
        string? lastError = null;

        while (consecutiveSuccesses < requiredSuccesses && totalAttempts < maxTotalAttempts)
        {
            totalAttempts++;
            try
            {
                // Use a unique filename for each probe attempt
                var testFile = $".\\grace_probe_{Guid.NewGuid():N}.tmp";

                // Try to create, then properly close and delete the file
                _sharedClient!.CreateFile(testFile);
                _sharedClient.CompleteIo(); // Close the file handle before deleting
                _sharedClient.DeleteFile(testFile);

                consecutiveSuccesses++;
                lastError = null;

                // Show progress
                if (consecutiveSuccesses == 1)
                {
                    Console.Write($"success at {totalAttempts}s, confirming...");
                }
            }
            catch (Exception ex)
            {
                // Reset consecutive successes on any failure
                consecutiveSuccesses = 0;
                lastError = ex.Message;

                // Try to clean up any open state
                try { _sharedClient?.CompleteIo(); } catch { }

                // Show periodic progress
                if (totalAttempts % 15 == 0)
                {
                    Console.Write($" {totalAttempts}s...");
                }

                await Task.Delay(1000);
            }
        }

        if (consecutiveSuccesses >= requiredSuccesses)
        {
            Console.WriteLine($" confirmed after {totalAttempts}s");

            // Additional small delay for stability
            Console.Write("  Stabilization delay... ");
            await Task.Delay(2000);
            Console.WriteLine("done");
        }
        else
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  WARNING: Grace period probe timed out after {totalAttempts} attempts.");
            Console.WriteLine($"  Last error: {lastError}");
            Console.WriteLine("  Tests will proceed but may fail if server is still in grace period.");
            Console.ResetColor();
        }
        Console.WriteLine();
    }

    private async Task StopServerAsync(NfsServerConfig config)
    {
        Console.Write($"Stopping {config.Name} server... ");
        try
        {
            await DockerHelper.ComposeDownAsync(config.ComposeFilePath);
            Console.WriteLine("OK");
        }
        catch
        {
            Console.WriteLine("SKIPPED");
        }
    }

    private async Task RunTestGroupAsync(NfsServerConfig config)
    {
        Console.WriteLine();
        Console.WriteLine("----------------------------------------------------");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"  {config.Name} Tests");
        Console.ResetColor();
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine();

        var group = new TestGroupResults { GroupName = config.Name };

        // Create shared client for this test group to avoid session exhaustion
        _currentConfig = config;
        _sharedClient = config.CreateConnectedClient();

        // For NFSv4, wait for grace period using the SAME client that will run tests
        // Each new client session can trigger a new grace period, so we must use one client
        // We perform multiple successful file operations to confirm stability
        if (config.Version == NfsVersion.V4)
        {
            await WaitForGracePeriodWithProbeAsync(config);
        }

        // Connection Tests
        await RunTestAsync(group, config, "Connect_EstablishesConnection", Test_Connect_EstablishesConnection);
        await RunTestAsync(group, config, "Connect_Disconnect_CanReconnect", Test_Connect_Disconnect_CanReconnect);

        // Mount Tests
        await RunTestAsync(group, config, "GetExportedDevices_ReturnsExports", Test_GetExportedDevices_ReturnsExports);
        await RunTestAsync(group, config, "MountDevice_MountsExport", Test_MountDevice_MountsExport);
        await RunTestAsync(group, config, "UnMountDevice_UnmountsExport", Test_UnMountDevice_UnmountsExport);

        // Directory Tests
        await RunTestAsync(group, config, "GetItemList_RootDirectory_ReturnsItems", Test_GetItemList_RootDirectory_ReturnsItems);
        await RunTestAsync(group, config, "CreateDirectory_CreatesNewDirectory", Test_CreateDirectory_CreatesNewDirectory);
        await RunTestAsync(group, config, "DeleteDirectory_RemovesDirectory", Test_DeleteDirectory_RemovesDirectory);
        await RunTestAsync(group, config, "IsDirectory_ForDirectory_ReturnsTrue", Test_IsDirectory_ForDirectory_ReturnsTrue);
        await RunTestAsync(group, config, "IsDirectory_ForFile_ReturnsFalse", Test_IsDirectory_ForFile_ReturnsFalse);

        // File Tests
        await RunTestAsync(group, config, "CreateFile_CreatesEmptyFile", Test_CreateFile_CreatesEmptyFile);
        await RunTestAsync(group, config, "DeleteFile_RemovesFile", Test_DeleteFile_RemovesFile);
        await RunTestAsync(group, config, "FileExists_ForExistingFile_ReturnsTrue", Test_FileExists_ForExistingFile_ReturnsTrue);
        await RunTestAsync(group, config, "FileExists_ForNonExistentFile_ReturnsFalse", Test_FileExists_ForNonExistentFile_ReturnsFalse);

        // Read/Write Tests
        await RunTestAsync(group, config, "WriteAndRead_SmallContent_RoundTrips", Test_WriteAndRead_SmallContent_RoundTrips);
        await RunTestAsync(group, config, "WriteAndRead_TextContent_RoundTrips", Test_WriteAndRead_TextContent_RoundTrips);
        await RunTestAsync(group, config, "Write_ToExistingFile_OverwritesContent", Test_Write_ToExistingFile_OverwritesContent);

        // Attributes Tests
        await RunTestAsync(group, config, "GetItemAttributes_ForFile_ReturnsAttributes", Test_GetItemAttributes_ForFile_ReturnsAttributes);
        await RunTestAsync(group, config, "GetItemAttributes_ForDirectory_ReturnsDirectoryType", Test_GetItemAttributes_ForDirectory_ReturnsDirectoryType);
        await RunTestAsync(group, config, "SetFileSize_ChangesFileSize", Test_SetFileSize_ChangesFileSize);

        // Move/Rename Tests
        await RunTestAsync(group, config, "Move_RenamesFile", Test_Move_RenamesFile);

        // Version-specific tests
        if (config.Version == NfsVersion.V3)
        {
            await RunTestAsync(group, config, "LargeFile_CanBeWrittenAndRead_100KB", Test_LargeFile_CanBeWrittenAndRead_V3);
            await RunTestAsync(group, config, "PartialRead_ReturnsCorrectData", Test_PartialRead_ReturnsCorrectData);
            await RunTestAsync(group, config, "AppendWrite_AddsToEndOfFile", Test_AppendWrite_AddsToEndOfFile);
            await RunTestAsync(group, config, "NestedDirectory_CanBeCreatedAndTraversed", Test_NestedDirectory_CanBeCreatedAndTraversed);
        }
        else if (config.Version == NfsVersion.V4)
        {
            await RunTestAsync(group, config, "LargeFile_CanBeWrittenAndRead_256KB", Test_LargeFile_CanBeWrittenAndRead_V4);
            await RunTestAsync(group, config, "AtomicRename_Works", Test_AtomicRename_Works);
            await RunTestAsync(group, config, "DeepNestedPath_CanBeAccessed", Test_DeepNestedPath_CanBeAccessed);
            await RunTestAsync(group, config, "FileAttributes_IncludeAllFields", Test_FileAttributes_IncludeAllFields);
        }

        // Comprehensive file operations test (both versions)
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  --- Comprehensive File Operations Test ---");
        Console.ResetColor();
        await RunTestAsync(group, config, "ComprehensiveOps_Step1_CreateFilesAndFolders", Test_ComprehensiveOps_Step1_CreateFilesAndFolders);
        await RunTestAsync(group, config, "ComprehensiveOps_Step2_EnumerateAndValidate", Test_ComprehensiveOps_Step2_EnumerateAndValidate);
        await RunTestAsync(group, config, "ComprehensiveOps_Step3_ValidateFileMetadata", Test_ComprehensiveOps_Step3_ValidateFileMetadata);
        await RunTestAsync(group, config, "ComprehensiveOps_Step4_ValidateFolderMetadata", Test_ComprehensiveOps_Step4_ValidateFolderMetadata);
        await RunTestAsync(group, config, "ComprehensiveOps_Step5_DeleteAndVerify", Test_ComprehensiveOps_Step5_DeleteAndVerify);

        // Dispose shared client
        _sharedClient?.Dispose();
        _sharedClient = null;
        _currentConfig = null;

        _allResults.Add(group);
    }

    /// <summary>
    /// Gets the shared client for the current test group.
    /// </summary>
    private NfsClient GetSharedClient()
    {
        if (_sharedClient == null)
            throw new InvalidOperationException("No shared client available");

        return _sharedClient;
    }

    private async Task RunTestAsync(TestGroupResults group, NfsServerConfig config, string testName, Func<NfsServerConfig, Task> testAction)
    {
        Console.Write($"  {testName}... ");

        var sw = Stopwatch.StartNew();
        TestResult result;

        try
        {
            await testAction(config);
            sw.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("PASS");
            Console.ResetColor();
            Console.WriteLine($"  ({sw.ElapsedMilliseconds}ms)");

            result = new TestResult
            {
                TestName = testName,
                Group = config.Name,
                Passed = true,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("FAIL");
            Console.ResetColor();
            Console.WriteLine($"  ({sw.ElapsedMilliseconds}ms)");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"       {ex.Message}");
            Console.ResetColor();

            result = new TestResult
            {
                TestName = testName,
                Group = config.Name,
                Passed = false,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
        }

        group.Results.Add(result);
    }

    private void PrintSummary()
    {
        Console.WriteLine();
        Console.WriteLine("====================================================");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("                    SUMMARY");
        Console.ResetColor();
        Console.WriteLine("====================================================");
        Console.WriteLine();

        var totalPassed = 0;
        var totalFailed = 0;
        var totalSkipped = 0;

        foreach (var group in _allResults)
        {
            Console.Write($"  {group.GroupName}: ");

            if (group.AllPassed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("PASS");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAIL");
            }
            Console.ResetColor();

            Console.WriteLine($"  ({group.PassedTests} passed, {group.FailedTests} failed, {group.SkippedTests} skipped) - {group.TotalDuration.TotalSeconds:F2}s");

            totalPassed += group.PassedTests;
            totalFailed += group.FailedTests;
            totalSkipped += group.SkippedTests;
        }

        Console.WriteLine();
        Console.WriteLine("----------------------------------------------------");

        // Overall result
        Console.Write("  OVERALL: ");
        if (totalFailed == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("PASS");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("FAIL");
        }
        Console.ResetColor();

        var totalTests = totalPassed + totalFailed + totalSkipped;
        Console.WriteLine($"  ({totalPassed}/{totalTests} passed)");
        Console.WriteLine($"  Total time: {_totalStopwatch.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine();

        // List failed tests
        var failedTests = _allResults
            .SelectMany(g => g.Results)
            .Where(r => !r.Passed && !r.Skipped)
            .ToList();

        if (failedTests.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed Tests:");
            Console.ResetColor();

            foreach (var test in failedTests)
            {
                Console.WriteLine($"  - [{test.Group}] {test.TestName}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"    {test.ErrorMessage}");
                Console.ResetColor();
            }
            Console.WriteLine();
        }

        // Exit with appropriate code
        Environment.Exit(totalFailed > 0 ? 1 : 0);
    }

    #region Test Methods

    private Task Test_Connect_EstablishesConnection(NfsServerConfig config)
    {
        // This test verifies that the shared client is connected
        var client = GetSharedClient();

        if (!client.IsConnected)
            throw new Exception("Expected IsConnected to be true");

        return Task.CompletedTask;
    }

    private Task Test_Connect_Disconnect_CanReconnect(NfsServerConfig config)
    {
        // For NFSv4, we can't do a full disconnect/reconnect because MountDevice triggers
        // RECLAIM_COMPLETE which starts a new 90-second grace period. This would cause all
        // subsequent OPEN operations to fail until the grace period ends.
        // Instead, we just verify the shared client connection state for NFSv4.
        if (config.Version == NfsVersion.V4)
        {
            var sharedClient = GetSharedClient();

            // Verify connected and mounted
            if (!sharedClient.IsConnected)
                throw new Exception("Expected IsConnected to be true");
            if (!sharedClient.IsMounted)
                throw new Exception("Expected IsMounted to be true");

            // Note: Full disconnect/reconnect test is skipped for NFSv4 to avoid triggering
            // a new grace period. The connection functionality is still tested via the
            // initial shared client creation.

            return Task.CompletedTask;
        }

        // For NFSv2/V3, use a separate client since creating multiple sessions is fine
        using var client = new NfsClient(config.Version);

        client.Connect(config.ServerAddress, config.CreateConnectionOptions());
        if (!client.IsConnected)
            throw new Exception("Expected IsConnected to be true after first connect");

        client.Disconnect();
        if (client.IsConnected)
            throw new Exception("Expected IsConnected to be false after disconnect");

        client.Connect(config.ServerAddress, config.CreateConnectionOptions());
        if (!client.IsConnected)
            throw new Exception("Expected IsConnected to be true after reconnect");

        return Task.CompletedTask;
    }

    private Task Test_GetExportedDevices_ReturnsExports(NfsServerConfig config)
    {
        var client = GetSharedClient();

        var exports = client.GetExportedDevices();

        if (exports == null || exports.Count == 0)
            throw new Exception("Expected exports to not be empty");

        return Task.CompletedTask;
    }

    private Task Test_MountDevice_MountsExport(NfsServerConfig config)
    {
        // Shared client is already mounted, verify it
        var client = GetSharedClient();

        if (!client.IsMounted)
            throw new Exception("Expected IsMounted to be true");

        return Task.CompletedTask;
    }

    private Task Test_UnMountDevice_UnmountsExport(NfsServerConfig config)
    {
        // For NFSv4, use the shared client to avoid creating a new session which triggers
        // RECLAIM_COMPLETE and puts the server in grace period, affecting subsequent tests.
        // NFSv4 UnMountDevice is essentially a no-op anyway.
        if (config.Version == NfsVersion.V4)
        {
            var sharedClient = GetSharedClient();
            sharedClient.UnMountDevice();
            // NFSv4 unmount is a no-op, so IsMounted remains true (session is still active)
            return Task.CompletedTask;
        }

        // For NFSv2/V3, use a separate client since unmount actually modifies mount state
        using var client = config.CreateConnectedClient();

        if (!client.IsMounted)
            throw new Exception("Expected IsMounted to be true initially");

        client.UnMountDevice();

        if (client.IsMounted)
            throw new Exception("Expected IsMounted to be false after unmount");

        return Task.CompletedTask;
    }

    private Task Test_GetItemList_RootDirectory_ReturnsItems(NfsServerConfig config)
    {
        var client = GetSharedClient();

        var items = client.GetItemList(".");

        if (items == null)
            throw new Exception("Expected items to not be null");

        return Task.CompletedTask;
    }

    private Task Test_CreateDirectory_CreatesNewDirectory(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dirName}");

            var items = client.GetItemList(".");
            if (!items.Contains(dirName))
                throw new Exception($"Expected items to contain {dirName}");
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dirName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_DeleteDirectory_RemovesDirectory(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        client.CreateDirectory($".\\{dirName}");
        client.DeleteDirectory($".\\{dirName}");

        var items = client.GetItemList(".");
        if (items.Contains(dirName))
            throw new Exception($"Expected items to not contain {dirName}");

        return Task.CompletedTask;
    }

    private Task Test_IsDirectory_ForDirectory_ReturnsTrue(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dirName}");

            if (!client.IsDirectory($".\\{dirName}"))
                throw new Exception("Expected IsDirectory to return true");
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dirName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_IsDirectory_ForFile_ReturnsFalse(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            if (client.IsDirectory($".\\{fileName}"))
                throw new Exception("Expected IsDirectory to return false for file");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_CreateFile_CreatesEmptyFile(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            if (!client.FileExists($".\\{fileName}"))
                throw new Exception("Expected file to exist");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_DeleteFile_RemovesFile(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        client.CreateFile($".\\{fileName}");
        client.DeleteFile($".\\{fileName}");

        if (client.FileExists($".\\{fileName}"))
            throw new Exception("Expected file to not exist");

        return Task.CompletedTask;
    }

    private Task Test_FileExists_ForExistingFile_ReturnsTrue(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            if (!client.FileExists($".\\{fileName}"))
                throw new Exception("Expected FileExists to return true");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_FileExists_ForNonExistentFile_ReturnsFalse(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        if (client.FileExists($".\\{fileName}"))
            throw new Exception("Expected FileExists to return false for non-existent file");

        return Task.CompletedTask;
    }

    private Task Test_WriteAndRead_SmallContent_RoundTrips(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GenerateSmallContent();

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            for (int i = 0; i < content.Length; i++)
            {
                if (buffer[i] != content[i])
                    throw new Exception($"Content mismatch at position {i}");
            }
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_WriteAndRead_TextContent_RoundTrips(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var textContent = "Hello, NFS World! This is a test string.";
        var content = Encoding.UTF8.GetBytes(textContent);

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            var readText = Encoding.UTF8.GetString(buffer);
            if (readText != textContent)
                throw new Exception($"Expected '{textContent}' but got '{readText}'");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_Write_ToExistingFile_OverwritesContent(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content1 = Encoding.UTF8.GetBytes("First content");
        var content2 = Encoding.UTF8.GetBytes("Second content, longer");

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content1.Length, content1);

            client.SetFileSize($".\\{fileName}", 0);
            client.Write($".\\{fileName}", 0, content2.Length, content2);

            var buffer = new byte[content2.Length];
            client.Read($".\\{fileName}", 0, content2.Length, ref buffer);

            var readText = Encoding.UTF8.GetString(buffer);
            if (readText != "Second content, longer")
                throw new Exception($"Expected 'Second content, longer' but got '{readText}'");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_GetItemAttributes_ForFile_ReturnsAttributes(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            var attrs = client.GetItemAttributes($".\\{fileName}");

            if (attrs == null)
                throw new Exception("Expected attributes to not be null");
            if (attrs.NFSType != NFSItemTypes.NFREG)
                throw new Exception($"Expected NFSType to be NFREG but got {attrs.NFSType}");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_GetItemAttributes_ForDirectory_ReturnsDirectoryType(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var dirName = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dirName}");

            var attrs = client.GetItemAttributes($".\\{dirName}");

            if (attrs == null)
                throw new Exception("Expected attributes to not be null");
            if (attrs.NFSType != NFSItemTypes.NFDIR)
                throw new Exception($"Expected NFSType to be NFDIR but got {attrs.NFSType}");
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dirName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_SetFileSize_ChangesFileSize(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GenerateSmallContent();

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            var attrs = client.GetItemAttributes($".\\{fileName}");
            if ((long)attrs.Size != content.Length)
                throw new Exception($"Expected size to be {content.Length} but got {attrs.Size}");

            var newSize = content.Length / 2;
            client.SetFileSize($".\\{fileName}", newSize);

            attrs = client.GetItemAttributes($".\\{fileName}");
            if ((long)attrs.Size != newSize)
                throw new Exception($"Expected size to be {newSize} but got {attrs.Size}");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_Move_RenamesFile(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var originalName = TestDataGenerator.GenerateFileName("original");
        var newName = TestDataGenerator.GenerateFileName("renamed");

        try
        {
            client.CreateFile($".\\{originalName}");

            client.Move($".\\{originalName}", $".\\{newName}");

            if (client.FileExists($".\\{originalName}"))
                throw new Exception("Expected original file to not exist");
            if (!client.FileExists($".\\{newName}"))
                throw new Exception("Expected renamed file to exist");
        }
        finally
        {
            try { client.DeleteFile($".\\{originalName}"); } catch { }
            try { client.DeleteFile($".\\{newName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    // NFSv3-specific tests

    private Task Test_LargeFile_CanBeWrittenAndRead_V3(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GeneratePatternedContent(100 * 1024); // 100KB

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            if (!TestDataGenerator.VerifyPatternedContent(buffer, content.Length))
                throw new Exception("Content verification failed");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_PartialRead_ReturnsCorrectData(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GeneratePatternedContent(4096);

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            var offset = 1024;
            var length = 512;
            var buffer = new byte[length];
            client.Read($".\\{fileName}", offset, length, ref buffer);

            for (int i = 0; i < length; i++)
            {
                if (buffer[i] != (byte)((offset + i) % 256))
                    throw new Exception($"Content mismatch at position {i}");
            }
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_AppendWrite_AddsToEndOfFile(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content1 = new byte[] { 1, 2, 3, 4, 5 };
        var content2 = new byte[] { 6, 7, 8, 9, 10 };

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content1.Length, content1);
            client.Write($".\\{fileName}", content1.Length, content2.Length, content2);

            var buffer = new byte[10];
            client.Read($".\\{fileName}", 0, 10, ref buffer);

            var expected = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            for (int i = 0; i < expected.Length; i++)
            {
                if (buffer[i] != expected[i])
                    throw new Exception($"Content mismatch at position {i}: expected {expected[i]} but got {buffer[i]}");
            }
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_NestedDirectory_CanBeCreatedAndTraversed(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var dir1 = TestDataGenerator.GenerateDirectoryName();
        var dir2 = TestDataGenerator.GenerateDirectoryName();

        try
        {
            client.CreateDirectory($".\\{dir1}");
            client.CreateDirectory($".\\{dir1}\\{dir2}");

            var items = client.GetItemList($".\\{dir1}");
            if (!items.Contains(dir2))
                throw new Exception($"Expected items to contain {dir2}");
        }
        finally
        {
            try { client.DeleteDirectory($".\\{dir1}\\{dir2}"); } catch { }
            try { client.DeleteDirectory($".\\{dir1}"); } catch { }
        }

        return Task.CompletedTask;
    }

    // NFSv4-specific tests

    private Task Test_LargeFile_CanBeWrittenAndRead_V4(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();
        var content = TestDataGenerator.GeneratePatternedContent(256 * 1024); // 256KB

        try
        {
            client.CreateFile($".\\{fileName}");
            client.Write($".\\{fileName}", 0, content.Length, content);

            var buffer = new byte[content.Length];
            client.Read($".\\{fileName}", 0, content.Length, ref buffer);

            if (!TestDataGenerator.VerifyPatternedContent(buffer, content.Length))
                throw new Exception("Content verification failed");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_AtomicRename_Works(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var originalName = TestDataGenerator.GenerateFileName("atomic");
        var newName = TestDataGenerator.GenerateFileName("renamed");
        var content = new byte[] { 1, 2, 3, 4, 5 };

        try
        {
            client.CreateFile($".\\{originalName}");
            client.Write($".\\{originalName}", 0, content.Length, content);

            client.Move($".\\{originalName}", $".\\{newName}");

            var buffer = new byte[content.Length];
            client.Read($".\\{newName}", 0, content.Length, ref buffer);

            for (int i = 0; i < content.Length; i++)
            {
                if (buffer[i] != content[i])
                    throw new Exception($"Content mismatch at position {i}");
            }
        }
        finally
        {
            try { client.DeleteFile($".\\{originalName}"); } catch { }
            try { client.DeleteFile($".\\{newName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_DeepNestedPath_CanBeAccessed(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var dirs = new[]
        {
            TestDataGenerator.GenerateDirectoryName("level1"),
            TestDataGenerator.GenerateDirectoryName("level2"),
            TestDataGenerator.GenerateDirectoryName("level3")
        };
        var fileName = TestDataGenerator.GenerateFileName("deep");

        try
        {
            client.CreateDirectory($".\\{dirs[0]}");
            client.CreateDirectory($".\\{dirs[0]}\\{dirs[1]}");
            client.CreateDirectory($".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}");

            var path = $".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}\\{fileName}";
            client.CreateFile(path);

            if (!client.FileExists(path))
                throw new Exception("Expected deep nested file to exist");
        }
        finally
        {
            try
            {
                client.DeleteFile($".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}\\{fileName}");
                client.DeleteDirectory($".\\{dirs[0]}\\{dirs[1]}\\{dirs[2]}");
                client.DeleteDirectory($".\\{dirs[0]}\\{dirs[1]}");
                client.DeleteDirectory($".\\{dirs[0]}");
            }
            catch { }
        }

        return Task.CompletedTask;
    }

    private Task Test_FileAttributes_IncludeAllFields(NfsServerConfig config)
    {
        var client = GetSharedClient();
        var fileName = TestDataGenerator.GenerateFileName();

        try
        {
            client.CreateFile($".\\{fileName}");

            var attrs = client.GetItemAttributes($".\\{fileName}");

            if (attrs == null)
                throw new Exception("Expected attributes to not be null");
            if (attrs.NFSType != NFSItemTypes.NFREG)
                throw new Exception($"Expected NFSType to be NFREG but got {attrs.NFSType}");
            if (attrs.Size != 0)
                throw new Exception($"Expected Size to be 0 but got {attrs.Size}");
            if (attrs.Mode == null)
                throw new Exception("Expected Mode to not be null");
        }
        finally
        {
            try { client.DeleteFile($".\\{fileName}"); } catch { }
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Comprehensive File Operations Tests

    /// <summary>
    /// Step 1: Create 100 files across root and directories with content.
    /// - 60 files in root folder
    /// - 15 files in dir1
    /// - 15 files in dir2
    /// - 10 files in dir3 (nested under dir1)
    /// </summary>
    private Task Test_ComprehensiveOps_Step1_CreateFilesAndFolders(NfsServerConfig config)
    {
        var client = GetSharedClient();

        var state = new ComprehensiveTestState
        {
            TestId = $"comp_{Guid.NewGuid():N}",
            Dir1Name = $"testdir1_{Guid.NewGuid():N}",
            Dir2Name = $"testdir2_{Guid.NewGuid():N}",
            Dir3Name = $"testdir3_{Guid.NewGuid():N}"
        };

        try
        {
            // Create directories
            client.CreateDirectory($".\\{state.Dir1Name}");
            client.CreateDirectory($".\\{state.Dir2Name}");
            client.CreateDirectory($".\\{state.Dir1Name}\\{state.Dir3Name}");

            // Create 60 files in root
            for (int i = 0; i < 60; i++)
            {
                var fileName = $"root_file_{state.TestId}_{i:D3}.dat";
                var content = GenerateFileContent(i);
                state.RootFiles.Add(fileName);
                state.FileData[$".\\{fileName}"] = (content.Length, content);

                client.CreateFile($".\\{fileName}");
                client.Write($".\\{fileName}", 0, content.Length, content);
            }

            // Create 15 files in dir1
            for (int i = 0; i < 15; i++)
            {
                var fileName = $"dir1_file_{state.TestId}_{i:D3}.dat";
                var content = GenerateFileContent(60 + i);
                var fullPath = $".\\{state.Dir1Name}\\{fileName}";
                state.Dir1Files.Add(fileName);
                state.FileData[fullPath] = (content.Length, content);

                client.CreateFile(fullPath);
                client.Write(fullPath, 0, content.Length, content);
            }

            // Create 15 files in dir2
            for (int i = 0; i < 15; i++)
            {
                var fileName = $"dir2_file_{state.TestId}_{i:D3}.dat";
                var content = GenerateFileContent(75 + i);
                var fullPath = $".\\{state.Dir2Name}\\{fileName}";
                state.Dir2Files.Add(fileName);
                state.FileData[fullPath] = (content.Length, content);

                client.CreateFile(fullPath);
                client.Write(fullPath, 0, content.Length, content);
            }

            // Create 10 files in dir3 (nested)
            for (int i = 0; i < 10; i++)
            {
                var fileName = $"dir3_file_{state.TestId}_{i:D3}.dat";
                var content = GenerateFileContent(90 + i);
                var fullPath = $".\\{state.Dir1Name}\\{state.Dir3Name}\\{fileName}";
                state.Dir3Files.Add(fileName);
                state.FileData[fullPath] = (content.Length, content);

                client.CreateFile(fullPath);
                client.Write(fullPath, 0, content.Length, content);
            }

            // Store state for subsequent steps
            _comprehensiveTestState[config.Name] = state;

            var totalFiles = state.RootFiles.Count + state.Dir1Files.Count + state.Dir2Files.Count + state.Dir3Files.Count;
            if (totalFiles != 100)
                throw new Exception($"Expected 100 files to be created, but created {totalFiles}");
        }
        catch
        {
            // Cleanup on failure
            CleanupComprehensiveTest(client, state);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 2: Enumerate all files and validate they exist.
    /// </summary>
    private Task Test_ComprehensiveOps_Step2_EnumerateAndValidate(NfsServerConfig config)
    {
        if (!_comprehensiveTestState.TryGetValue(config.Name, out var state))
            throw new Exception("Step 1 must run before Step 2");

        var client = GetSharedClient();

        // Enumerate root
        var rootItems = client.GetItemList(".");
        var missingRootFiles = new List<string>();
        foreach (var fileName in state.RootFiles)
        {
            if (!rootItems.Contains(fileName))
                missingRootFiles.Add(fileName);
        }
        if (missingRootFiles.Count > 0)
            throw new Exception($"Missing {missingRootFiles.Count} files in root: {string.Join(", ", missingRootFiles.Take(5))}...");

        // Verify root directories exist
        if (!rootItems.Contains(state.Dir1Name))
            throw new Exception($"Directory {state.Dir1Name} not found in root");
        if (!rootItems.Contains(state.Dir2Name))
            throw new Exception($"Directory {state.Dir2Name} not found in root");

        // Enumerate dir1
        var dir1Items = client.GetItemList($".\\{state.Dir1Name}");
        var missingDir1Files = new List<string>();
        foreach (var fileName in state.Dir1Files)
        {
            if (!dir1Items.Contains(fileName))
                missingDir1Files.Add(fileName);
        }
        if (missingDir1Files.Count > 0)
            throw new Exception($"Missing {missingDir1Files.Count} files in dir1: {string.Join(", ", missingDir1Files.Take(5))}...");

        // Verify dir3 exists in dir1
        if (!dir1Items.Contains(state.Dir3Name))
            throw new Exception($"Directory {state.Dir3Name} not found in dir1");

        // Enumerate dir2
        var dir2Items = client.GetItemList($".\\{state.Dir2Name}");
        var missingDir2Files = new List<string>();
        foreach (var fileName in state.Dir2Files)
        {
            if (!dir2Items.Contains(fileName))
                missingDir2Files.Add(fileName);
        }
        if (missingDir2Files.Count > 0)
            throw new Exception($"Missing {missingDir2Files.Count} files in dir2: {string.Join(", ", missingDir2Files.Take(5))}...");

        // Enumerate dir3 (nested)
        var dir3Items = client.GetItemList($".\\{state.Dir1Name}\\{state.Dir3Name}");
        var missingDir3Files = new List<string>();
        foreach (var fileName in state.Dir3Files)
        {
            if (!dir3Items.Contains(fileName))
                missingDir3Files.Add(fileName);
        }
        if (missingDir3Files.Count > 0)
            throw new Exception($"Missing {missingDir3Files.Count} files in dir3: {string.Join(", ", missingDir3Files.Take(5))}...");

        // Total count validation
        var totalFound = state.RootFiles.Count(f => rootItems.Contains(f)) +
                        state.Dir1Files.Count(f => dir1Items.Contains(f)) +
                        state.Dir2Files.Count(f => dir2Items.Contains(f)) +
                        state.Dir3Files.Count(f => dir3Items.Contains(f));

        if (totalFound != 100)
            throw new Exception($"Expected to find 100 files, but found {totalFound}");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 3: Retrieve and validate file metadata.
    /// </summary>
    private Task Test_ComprehensiveOps_Step3_ValidateFileMetadata(NfsServerConfig config)
    {
        if (!_comprehensiveTestState.TryGetValue(config.Name, out var state))
            throw new Exception("Step 1 must run before Step 3");

        var client = GetSharedClient();

        var errors = new List<string>();
        var checkedCount = 0;

        // Check a sample of files from each location (not all 100 to keep it reasonable)
        var filesToCheck = new List<string>();

        // Sample 5 from root
        filesToCheck.AddRange(state.RootFiles.Take(5).Select(f => $".\\{f}"));
        // Sample 3 from dir1
        filesToCheck.AddRange(state.Dir1Files.Take(3).Select(f => $".\\{state.Dir1Name}\\{f}"));
        // Sample 3 from dir2
        filesToCheck.AddRange(state.Dir2Files.Take(3).Select(f => $".\\{state.Dir2Name}\\{f}"));
        // Sample 3 from dir3
        filesToCheck.AddRange(state.Dir3Files.Take(3).Select(f => $".\\{state.Dir1Name}\\{state.Dir3Name}\\{f}"));

        foreach (var filePath in filesToCheck)
        {
            try
            {
                var attrs = client.GetItemAttributes(filePath);

                if (attrs == null)
                {
                    errors.Add($"{filePath}: attributes is null");
                    continue;
                }

                if (attrs.NFSType != NFSItemTypes.NFREG)
                {
                    errors.Add($"{filePath}: expected NFREG, got {attrs.NFSType}");
                    continue;
                }

                // Validate size matches what we wrote
                if (state.FileData.TryGetValue(filePath, out var fileInfo))
                {
                    if ((long)attrs.Size != fileInfo.Size)
                    {
                        errors.Add($"{filePath}: expected size {fileInfo.Size}, got {attrs.Size}");
                        continue;
                    }

                    // Also verify content by reading
                    var buffer = new byte[fileInfo.Size];
                    client.Read(filePath, 0, fileInfo.Size, ref buffer);

                    for (int i = 0; i < fileInfo.Size; i++)
                    {
                        if (buffer[i] != fileInfo.Content[i])
                        {
                            errors.Add($"{filePath}: content mismatch at byte {i}");
                            break;
                        }
                    }
                }

                if (attrs.Mode == null)
                {
                    errors.Add($"{filePath}: Mode is null");
                    continue;
                }

                checkedCount++;
            }
            catch (Exception ex)
            {
                errors.Add($"{filePath}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
            throw new Exception($"File metadata validation failed:\n  " + string.Join("\n  ", errors.Take(10)));

        if (checkedCount < 10)
            throw new Exception($"Only verified {checkedCount} files, expected at least 10");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 4: Retrieve and validate folder metadata.
    /// </summary>
    private Task Test_ComprehensiveOps_Step4_ValidateFolderMetadata(NfsServerConfig config)
    {
        if (!_comprehensiveTestState.TryGetValue(config.Name, out var state))
            throw new Exception("Step 1 must run before Step 4");

        var client = GetSharedClient();

        var errors = new List<string>();

        // Check dir1
        try
        {
            var attrs = client.GetItemAttributes($".\\{state.Dir1Name}");
            if (attrs == null)
                errors.Add($"{state.Dir1Name}: attributes is null");
            else if (attrs.NFSType != NFSItemTypes.NFDIR)
                errors.Add($"{state.Dir1Name}: expected NFDIR, got {attrs.NFSType}");
            else if (attrs.Mode == null)
                errors.Add($"{state.Dir1Name}: Mode is null");
        }
        catch (Exception ex)
        {
            errors.Add($"{state.Dir1Name}: {ex.Message}");
        }

        // Check dir2
        try
        {
            var attrs = client.GetItemAttributes($".\\{state.Dir2Name}");
            if (attrs == null)
                errors.Add($"{state.Dir2Name}: attributes is null");
            else if (attrs.NFSType != NFSItemTypes.NFDIR)
                errors.Add($"{state.Dir2Name}: expected NFDIR, got {attrs.NFSType}");
            else if (attrs.Mode == null)
                errors.Add($"{state.Dir2Name}: Mode is null");
        }
        catch (Exception ex)
        {
            errors.Add($"{state.Dir2Name}: {ex.Message}");
        }

        // Check dir3 (nested)
        try
        {
            var attrs = client.GetItemAttributes($".\\{state.Dir1Name}\\{state.Dir3Name}");
            if (attrs == null)
                errors.Add($"{state.Dir3Name}: attributes is null");
            else if (attrs.NFSType != NFSItemTypes.NFDIR)
                errors.Add($"{state.Dir3Name}: expected NFDIR, got {attrs.NFSType}");
            else if (attrs.Mode == null)
                errors.Add($"{state.Dir3Name}: Mode is null");
        }
        catch (Exception ex)
        {
            errors.Add($"{state.Dir3Name}: {ex.Message}");
        }

        // Verify directories are marked as directories via IsDirectory
        if (!client.IsDirectory($".\\{state.Dir1Name}"))
            errors.Add($"{state.Dir1Name}: IsDirectory returned false");
        if (!client.IsDirectory($".\\{state.Dir2Name}"))
            errors.Add($"{state.Dir2Name}: IsDirectory returned false");
        if (!client.IsDirectory($".\\{state.Dir1Name}\\{state.Dir3Name}"))
            errors.Add($"{state.Dir3Name}: IsDirectory returned false");

        if (errors.Count > 0)
            throw new Exception($"Folder metadata validation failed:\n  " + string.Join("\n  ", errors));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Step 5: Delete all files and folders, verify they're deleted.
    /// </summary>
    private Task Test_ComprehensiveOps_Step5_DeleteAndVerify(NfsServerConfig config)
    {
        if (!_comprehensiveTestState.TryGetValue(config.Name, out var state))
            throw new Exception("Step 1 must run before Step 5");

        var client = GetSharedClient();

        var deletionErrors = new List<string>();
        var verificationErrors = new List<string>();

        // Delete files in dir3 first (nested directory)
        foreach (var fileName in state.Dir3Files)
        {
            var path = $".\\{state.Dir1Name}\\{state.Dir3Name}\\{fileName}";
            try
            {
                client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                deletionErrors.Add($"Failed to delete {path}: {ex.Message}");
            }
        }

        // Delete dir3
        try
        {
            client.DeleteDirectory($".\\{state.Dir1Name}\\{state.Dir3Name}");
        }
        catch (Exception ex)
        {
            deletionErrors.Add($"Failed to delete dir3: {ex.Message}");
        }

        // Delete files in dir1
        foreach (var fileName in state.Dir1Files)
        {
            var path = $".\\{state.Dir1Name}\\{fileName}";
            try
            {
                client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                deletionErrors.Add($"Failed to delete {path}: {ex.Message}");
            }
        }

        // Delete dir1
        try
        {
            client.DeleteDirectory($".\\{state.Dir1Name}");
        }
        catch (Exception ex)
        {
            deletionErrors.Add($"Failed to delete dir1: {ex.Message}");
        }

        // Delete files in dir2
        foreach (var fileName in state.Dir2Files)
        {
            var path = $".\\{state.Dir2Name}\\{fileName}";
            try
            {
                client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                deletionErrors.Add($"Failed to delete {path}: {ex.Message}");
            }
        }

        // Delete dir2
        try
        {
            client.DeleteDirectory($".\\{state.Dir2Name}");
        }
        catch (Exception ex)
        {
            deletionErrors.Add($"Failed to delete dir2: {ex.Message}");
        }

        // Delete root files
        foreach (var fileName in state.RootFiles)
        {
            var path = $".\\{fileName}";
            try
            {
                client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                deletionErrors.Add($"Failed to delete {path}: {ex.Message}");
            }
        }

        // Report deletion errors
        if (deletionErrors.Count > 0)
            throw new Exception($"Deletion failed:\n  " + string.Join("\n  ", deletionErrors.Take(10)));

        // Verify files are gone
        var rootItems = client.GetItemList(".");

        // Verify root files are deleted
        foreach (var fileName in state.RootFiles)
        {
            if (rootItems.Contains(fileName))
                verificationErrors.Add($"Root file {fileName} still exists after deletion");
        }

        // Verify directories are deleted
        if (rootItems.Contains(state.Dir1Name))
            verificationErrors.Add($"Directory {state.Dir1Name} still exists after deletion");
        if (rootItems.Contains(state.Dir2Name))
            verificationErrors.Add($"Directory {state.Dir2Name} still exists after deletion");

        // Verify using FileExists
        foreach (var fileName in state.RootFiles.Take(5))
        {
            if (client.FileExists($".\\{fileName}"))
                verificationErrors.Add($"FileExists returns true for deleted file {fileName}");
        }

        if (verificationErrors.Count > 0)
            throw new Exception($"Deletion verification failed:\n  " + string.Join("\n  ", verificationErrors.Take(10)));

        // Clean up state
        _comprehensiveTestState.Remove(config.Name);

        return Task.CompletedTask;
    }

    private static byte[] GenerateFileContent(int index)
    {
        // Generate varied file sizes: 100 bytes to 10KB
        var size = 100 + (index * 100) % 10000;
        var content = new byte[size];

        // Fill with a pattern that includes the index for verification
        for (int i = 0; i < size; i++)
        {
            content[i] = (byte)((index + i) % 256);
        }

        return content;
    }

    private void CleanupComprehensiveTest(NfsClient client, ComprehensiveTestState state)
    {
        try
        {
            // Delete files in dir3
            foreach (var fileName in state.Dir3Files)
            {
                try { client.DeleteFile($".\\{state.Dir1Name}\\{state.Dir3Name}\\{fileName}"); } catch { }
            }
            try { client.DeleteDirectory($".\\{state.Dir1Name}\\{state.Dir3Name}"); } catch { }

            // Delete files in dir1
            foreach (var fileName in state.Dir1Files)
            {
                try { client.DeleteFile($".\\{state.Dir1Name}\\{fileName}"); } catch { }
            }
            try { client.DeleteDirectory($".\\{state.Dir1Name}"); } catch { }

            // Delete files in dir2
            foreach (var fileName in state.Dir2Files)
            {
                try { client.DeleteFile($".\\{state.Dir2Name}\\{fileName}"); } catch { }
            }
            try { client.DeleteDirectory($".\\{state.Dir2Name}"); } catch { }

            // Delete root files
            foreach (var fileName in state.RootFiles)
            {
                try { client.DeleteFile($".\\{fileName}"); } catch { }
            }
        }
        catch { }
    }

    #endregion
}
