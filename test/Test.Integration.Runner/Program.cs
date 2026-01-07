using System.Diagnostics;
using Test.Integration.Runner;

// Welcome banner
Console.WriteLine();
Console.WriteLine("====================================================");
Console.WriteLine("       NFS Library Integration Test Runner");
Console.WriteLine("====================================================");
Console.WriteLine();

var runner = new TestRunner();

try
{
    await runner.RunAllTestsAsync();
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Fatal error: {ex.Message}");
    Console.ResetColor();
    Environment.Exit(1);
}
