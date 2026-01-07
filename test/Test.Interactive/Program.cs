using System.Text;
using NFSLibrary;
using NFSLibrary.Protocols.Commons;
using Test.Interactive;

// Welcome banner
Console.WriteLine();
Console.WriteLine("====================================================");
Console.WriteLine("         NFS Interactive Test Console");
Console.WriteLine("====================================================");
Console.WriteLine();

var interactiveSession = new InteractiveSession();

try
{
    await interactiveSession.RunAsync();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.ResetColor();
    Environment.Exit(1);
}

/// <summary>
/// Interactive session for NFS operations.
/// </summary>
public class InteractiveSession
{
    private NfsClient? _client;
    private NfsServerConfig? _config;
    private string _currentDirectory = ".";
    private bool _serverStartedByUs;

    public async Task RunAsync()
    {
        // Check Docker availability
        Console.Write("Checking Docker availability... ");
        var dockerAvailable = await DockerHelper.IsDockerAvailableAsync();
        if (!dockerAvailable)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NOT AVAILABLE");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Docker is required to run the NFS server.");
            Console.WriteLine("Please install Docker Desktop (Windows/Mac) or Docker Engine (Linux) and ensure it is running.");
            return;
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("OK");
        Console.ResetColor();

        // Initialize server configuration
        _config = NfsServerConfig.CreateV3Config();

        // Start the NFS server
        await StartServerAsync();

        if (!_config.IsReady)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to start NFS server. Exiting.");
            Console.ResetColor();
            return;
        }

        // Connect the client
        Console.Write("Connecting NFS client... ");
        try
        {
            _client = _config.CreateConnectedClient();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("CONNECTED");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED: {ex.Message}");
            Console.ResetColor();
            await StopServerAsync();
            return;
        }

        Console.WriteLine();
        Console.WriteLine("Type 'help' for available commands, 'quit' to exit.");
        Console.WriteLine();

        // Main command loop
        try
        {
            await CommandLoopAsync();
        }
        finally
        {
            // Cleanup
            if (_client != null)
            {
                Console.Write("Disconnecting client... ");
                try
                {
                    if (_client.IsMounted)
                        _client.UnMountDevice();
                    _client.Disconnect();
                    _client.Dispose();
                    Console.WriteLine("OK");
                }
                catch
                {
                    Console.WriteLine("SKIPPED");
                }
            }

            await StopServerAsync();
        }
    }

    private async Task StartServerAsync()
    {
        Console.Write($"Starting {_config!.Name} server... ");

        try
        {
            var isRunning = await DockerHelper.IsContainerRunningAsync(_config.ContainerName);
            if (!isRunning)
            {
                await DockerHelper.ComposeUpAsync(_config.ComposeFilePath, _config.ServiceName, TimeSpan.FromMinutes(2));
                _serverStartedByUs = true;
            }
            else
            {
                Console.Write("(already running) ");
            }

            await DockerHelper.WaitForContainerAsync(_config.ContainerName, TimeSpan.FromSeconds(60));

            // Wait for NFS to be ready
            await WaitForNfsReadyAsync();

            _config.IsReady = true;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("READY");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"FAILED ({ex.Message})");
            Console.ResetColor();
            _config.IsReady = false;
        }
    }

    private async Task WaitForNfsReadyAsync()
    {
        // NFSv3 - check showmount
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var result = await DockerHelper.ExecAsync(_config!.ContainerName, "showmount -e localhost");
                if (result.Success && result.StandardOutput.Contains("/export"))
                {
                    return;
                }
            }
            catch { }
            await Task.Delay(1000);
        }
    }

    private async Task StopServerAsync()
    {
        if (_serverStartedByUs)
        {
            Console.Write($"Stopping {_config!.Name} server... ");
            try
            {
                await DockerHelper.ComposeDownAsync(_config.ComposeFilePath);
                Console.WriteLine("OK");
            }
            catch
            {
                Console.WriteLine("SKIPPED");
            }
        }
    }

    private async Task CommandLoopAsync()
    {
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"nfs:{_currentDirectory}> ");
            Console.ResetColor();

            var input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input))
                continue;

            var parts = ParseCommand(input);
            var command = parts[0].ToLowerInvariant();
            var args = parts.Skip(1).ToArray();

            try
            {
                switch (command)
                {
                    case "help":
                    case "?":
                        ShowHelp();
                        break;
                    case "quit":
                    case "exit":
                    case "q":
                        return;
                    case "cls":
                    case "clear":
                        Console.Clear();
                        break;
                    case "ls":
                    case "dir":
                        ListDirectory(args);
                        break;
                    case "cd":
                        ChangeDirectory(args);
                        break;
                    case "pwd":
                        Console.WriteLine(_currentDirectory);
                        break;
                    case "cat":
                    case "read":
                        ReadFile(args);
                        break;
                    case "write":
                        WriteFile(args);
                        break;
                    case "touch":
                    case "create":
                        CreateFile(args);
                        break;
                    case "rm":
                    case "del":
                    case "delete":
                        DeleteFile(args);
                        break;
                    case "mkdir":
                        CreateDirectory(args);
                        break;
                    case "rmdir":
                        DeleteDirectory(args);
                        break;
                    case "stat":
                    case "info":
                        ShowFileInfo(args);
                        break;
                    case "exists":
                        CheckExists(args);
                        break;
                    case "isdir":
                        CheckIsDirectory(args);
                        break;
                    case "mv":
                    case "move":
                    case "rename":
                        MoveFile(args);
                        break;
                    case "size":
                        ShowSize(args);
                        break;
                    case "truncate":
                        TruncateFile(args);
                        break;
                    case "exports":
                        ShowExports();
                        break;
                    case "tree":
                        await ShowTree(args);
                        break;
                    case "hex":
                    case "hexdump":
                        HexDump(args);
                        break;
                    case "append":
                        AppendToFile(args);
                        break;
                    case "copy":
                    case "cp":
                        CopyFile(args);
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Unknown command: {command}. Type 'help' for available commands.");
                        Console.ResetColor();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine();
        }
    }

    private static List<string> ParseCommand(string input)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ' ' && !inQuotes)
            {
                if (current.Length > 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            parts.Add(current.ToString());

        return parts;
    }

    private void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Available commands:");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Navigation:");
        Console.ResetColor();
        Console.WriteLine("    ls, dir [path]        - List directory contents");
        Console.WriteLine("    cd <path>             - Change current directory");
        Console.WriteLine("    pwd                   - Print working directory");
        Console.WriteLine("    tree [path] [depth]   - Show directory tree (default depth: 3)");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  File Operations:");
        Console.ResetColor();
        Console.WriteLine("    cat, read <file>      - Display file contents (as text)");
        Console.WriteLine("    hex, hexdump <file>   - Display file contents (as hex)");
        Console.WriteLine("    write <file> <text>   - Write text to file (overwrites)");
        Console.WriteLine("    append <file> <text>  - Append text to file");
        Console.WriteLine("    touch, create <file>  - Create empty file");
        Console.WriteLine("    rm, del <file>        - Delete file");
        Console.WriteLine("    mv, move <src> <dst>  - Move/rename file");
        Console.WriteLine("    copy, cp <src> <dst>  - Copy file");
        Console.WriteLine("    truncate <file> <sz>  - Set file size");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Directory Operations:");
        Console.ResetColor();
        Console.WriteLine("    mkdir <dir>           - Create directory");
        Console.WriteLine("    rmdir <dir>           - Delete empty directory");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Information:");
        Console.ResetColor();
        Console.WriteLine("    stat, info <path>     - Show file/directory attributes");
        Console.WriteLine("    exists <path>         - Check if path exists");
        Console.WriteLine("    isdir <path>          - Check if path is a directory");
        Console.WriteLine("    size <file>           - Show file size");
        Console.WriteLine("    exports               - List NFS exports");
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("  Other:");
        Console.ResetColor();
        Console.WriteLine("    help, ?               - Show this help");
        Console.WriteLine("    cls, clear            - Clear screen");
        Console.WriteLine("    quit, exit, q         - Exit the program");
        Console.WriteLine();
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrEmpty(path) || path == ".")
            return _currentDirectory;

        // Handle absolute paths
        if (path.StartsWith(".\\") || path.StartsWith("./"))
            return path.Replace("/", "\\");

        // Handle relative paths
        if (_currentDirectory == ".")
            return $".\\{path}".Replace("/", "\\");
        else
            return $"{_currentDirectory}\\{path}".Replace("/", "\\");
    }

    private void ListDirectory(string[] args)
    {
        var path = args.Length > 0 ? ResolvePath(args[0]) : _currentDirectory;
        var items = _client!.GetItemList(path, excludeNavigationDots: true);

        if (items.Count == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (empty directory)");
            Console.ResetColor();
            return;
        }

        foreach (var item in items.OrderBy(i => i))
        {
            var itemPath = path == "." ? $".\\{item}" : $"{path}\\{item}";
            var isDir = _client.IsDirectory(itemPath);

            if (isDir)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"  [DIR]  {item}");
                Console.ResetColor();
            }
            else
            {
                var attrs = _client.GetItemAttributes(itemPath, false);
                var size = attrs?.Size ?? 0;
                Console.WriteLine($"  {size,10:N0}  {item}");
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {items.Count} item(s)");
        Console.ResetColor();
    }

    private void ChangeDirectory(string[] args)
    {
        if (args.Length == 0)
        {
            _currentDirectory = ".";
            return;
        }

        var path = args[0];

        // Handle special cases
        if (path == "..")
        {
            if (_currentDirectory == ".")
            {
                Console.WriteLine("Already at root directory.");
                return;
            }

            var lastSep = _currentDirectory.LastIndexOf('\\');
            if (lastSep <= 1)
                _currentDirectory = ".";
            else
                _currentDirectory = _currentDirectory.Substring(0, lastSep);
            return;
        }

        var targetPath = ResolvePath(path);

        // Verify directory exists
        if (!_client!.IsDirectory(targetPath))
        {
            // Check if it exists at all
            var attrs = _client.GetItemAttributes(targetPath, false);
            if (attrs == null)
                throw new DirectoryNotFoundException($"Directory not found: {path}");
            else
                throw new InvalidOperationException($"Not a directory: {path}");
        }

        _currentDirectory = targetPath;
    }

    private void ReadFile(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: read <filename>");

        var path = ResolvePath(args[0]);
        var attrs = _client!.GetItemAttributes(path);

        if (attrs.NFSType == NFSItemTypes.NFDIR)
            throw new InvalidOperationException("Cannot read a directory");

        var size = (int)attrs.Size;
        if (size == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(empty file)");
            Console.ResetColor();
            return;
        }

        // Limit read size for display
        var readSize = Math.Min(size, 64 * 1024); // Max 64KB for display
        var buffer = new byte[readSize];
        _client.Read(path, 0, readSize, ref buffer);

        // Try to display as text
        var text = Encoding.UTF8.GetString(buffer);
        Console.WriteLine(text);

        if (readSize < size)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"... (truncated, showing {readSize:N0} of {size:N0} bytes)");
            Console.ResetColor();
        }
    }

    private void HexDump(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: hex <filename>");

        var path = ResolvePath(args[0]);
        var attrs = _client!.GetItemAttributes(path);

        if (attrs.NFSType == NFSItemTypes.NFDIR)
            throw new InvalidOperationException("Cannot read a directory");

        var size = (int)attrs.Size;
        if (size == 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("(empty file)");
            Console.ResetColor();
            return;
        }

        // Limit read size for display
        var readSize = Math.Min(size, 1024); // Max 1KB for hex dump
        var buffer = new byte[readSize];
        _client.Read(path, 0, readSize, ref buffer);

        // Display hex dump
        for (int i = 0; i < readSize; i += 16)
        {
            Console.Write($"{i:X8}  ");

            // Hex
            for (int j = 0; j < 16; j++)
            {
                if (i + j < readSize)
                    Console.Write($"{buffer[i + j]:X2} ");
                else
                    Console.Write("   ");
                if (j == 7) Console.Write(" ");
            }

            Console.Write(" |");

            // ASCII
            for (int j = 0; j < 16 && i + j < readSize; j++)
            {
                var b = buffer[i + j];
                Console.Write(b >= 32 && b < 127 ? (char)b : '.');
            }

            Console.WriteLine("|");
        }

        if (readSize < size)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"... (truncated, showing {readSize:N0} of {size:N0} bytes)");
            Console.ResetColor();
        }
    }

    private void WriteFile(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: write <filename> <content>");

        var path = ResolvePath(args[0]);
        var content = string.Join(" ", args.Skip(1));
        var data = Encoding.UTF8.GetBytes(content);

        // Create file if it doesn't exist
        if (!_client!.FileExists(path))
            _client.CreateFile(path);
        else
            _client.SetFileSize(path, 0); // Truncate existing file

        _client.Write(path, 0, data.Length, data);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Wrote {data.Length} bytes to {args[0]}");
        Console.ResetColor();
    }

    private void AppendToFile(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: append <filename> <content>");

        var path = ResolvePath(args[0]);
        var content = string.Join(" ", args.Skip(1));
        var data = Encoding.UTF8.GetBytes(content);

        // Get current size
        var attrs = _client!.GetItemAttributes(path);
        var currentSize = (long)attrs.Size;

        _client.Write(path, currentSize, data.Length, data);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Appended {data.Length} bytes to {args[0]}");
        Console.ResetColor();
    }

    private void CreateFile(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: touch <filename>");

        var path = ResolvePath(args[0]);

        if (_client!.FileExists(path))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("File already exists.");
            Console.ResetColor();
            return;
        }

        _client.CreateFile(path);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Created {args[0]}");
        Console.ResetColor();
    }

    private void DeleteFile(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: rm <filename>");

        var path = ResolvePath(args[0]);
        _client!.DeleteFile(path);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Deleted {args[0]}");
        Console.ResetColor();
    }

    private void CreateDirectory(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: mkdir <dirname>");

        var path = ResolvePath(args[0]);
        _client!.CreateDirectory(path);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Created directory {args[0]}");
        Console.ResetColor();
    }

    private void DeleteDirectory(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: rmdir <dirname>");

        var path = ResolvePath(args[0]);
        _client!.DeleteDirectory(path);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Deleted directory {args[0]}");
        Console.ResetColor();
    }

    private void ShowFileInfo(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: stat <path>");

        var path = ResolvePath(args[0]);
        var attrs = _client!.GetItemAttributes(path);

        Console.WriteLine($"  Path:          {args[0]}");
        Console.WriteLine($"  Type:          {attrs.NFSType}");
        Console.WriteLine($"  Size:          {attrs.Size:N0} bytes");
        Console.WriteLine($"  Mode:          {attrs.Mode}");
        Console.WriteLine($"  Created:       {FormatTime(attrs.CreateDateTime)}");
        Console.WriteLine($"  Modified:      {FormatTime(attrs.ModifiedDateTime)}");
        Console.WriteLine($"  Accessed:      {FormatTime(attrs.LastAccessedDateTime)}");
    }

    private static string FormatTime(DateTime dt)
    {
        if (dt == DateTime.MinValue)
            return "(unknown)";
        return dt.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void CheckExists(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: exists <path>");

        var path = ResolvePath(args[0]);
        var exists = _client!.FileExists(path);

        if (exists)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("YES - path exists");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NO - path does not exist");
        }
        Console.ResetColor();
    }

    private void CheckIsDirectory(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: isdir <path>");

        var path = ResolvePath(args[0]);
        var isDir = _client!.IsDirectory(path);

        if (isDir)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("YES - is a directory");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("NO - is not a directory (or does not exist)");
        }
        Console.ResetColor();
    }

    private void MoveFile(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: mv <source> <destination>");

        var srcPath = ResolvePath(args[0]);
        var dstPath = ResolvePath(args[1]);

        _client!.Move(srcPath, dstPath);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Moved {args[0]} -> {args[1]}");
        Console.ResetColor();
    }

    private void CopyFile(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: cp <source> <destination>");

        var srcPath = ResolvePath(args[0]);
        var dstPath = ResolvePath(args[1]);

        // Read source
        var attrs = _client!.GetItemAttributes(srcPath);
        if (attrs.NFSType == NFSItemTypes.NFDIR)
            throw new InvalidOperationException("Cannot copy a directory");

        var size = (int)attrs.Size;
        var buffer = new byte[size];
        if (size > 0)
            _client.Read(srcPath, 0, size, ref buffer);

        // Write destination
        if (!_client.FileExists(dstPath))
            _client.CreateFile(dstPath);
        else
            _client.SetFileSize(dstPath, 0);

        if (size > 0)
            _client.Write(dstPath, 0, size, buffer);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Copied {args[0]} -> {args[1]} ({size:N0} bytes)");
        Console.ResetColor();
    }

    private void ShowSize(string[] args)
    {
        if (args.Length == 0)
            throw new ArgumentException("Usage: size <file>");

        var path = ResolvePath(args[0]);
        var attrs = _client!.GetItemAttributes(path);

        Console.WriteLine($"{attrs.Size:N0} bytes");
    }

    private void TruncateFile(string[] args)
    {
        if (args.Length < 2)
            throw new ArgumentException("Usage: truncate <file> <size>");

        var path = ResolvePath(args[0]);
        if (!long.TryParse(args[1], out var size))
            throw new ArgumentException("Size must be a number");

        _client!.SetFileSize(path, size);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Set file size to {size:N0} bytes");
        Console.ResetColor();
    }

    private void ShowExports()
    {
        var exports = _client!.GetExportedDevices();

        Console.WriteLine("NFS Exports:");
        foreach (var export in exports)
        {
            Console.WriteLine($"  {export}");
        }
    }

    private async Task ShowTree(string[] args)
    {
        var path = args.Length > 0 ? ResolvePath(args[0]) : _currentDirectory;
        var maxDepth = args.Length > 1 && int.TryParse(args[1], out var d) ? d : 3;

        Console.WriteLine(path);
        await ShowTreeRecursive(path, "", maxDepth, 0);
    }

    private async Task ShowTreeRecursive(string path, string prefix, int maxDepth, int currentDepth)
    {
        if (currentDepth >= maxDepth)
            return;

        var items = _client!.GetItemList(path, excludeNavigationDots: true);
        var sortedItems = items.OrderBy(i => i).ToList();

        for (int i = 0; i < sortedItems.Count; i++)
        {
            var item = sortedItems[i];
            var isLast = i == sortedItems.Count - 1;
            var itemPath = path == "." ? $".\\{item}" : $"{path}\\{item}";
            var isDir = _client.IsDirectory(itemPath);

            var connector = isLast ? "\\-- " : "|-- ";
            var newPrefix = isLast ? "    " : "|   ";

            if (isDir)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{prefix}{connector}{item}/");
                Console.ResetColor();

                await ShowTreeRecursive(itemPath, prefix + newPrefix, maxDepth, currentDepth + 1);
            }
            else
            {
                Console.WriteLine($"{prefix}{connector}{item}");
            }
        }

        await Task.CompletedTask;
    }
}
