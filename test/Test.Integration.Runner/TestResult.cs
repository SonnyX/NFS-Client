namespace Test.Integration.Runner;

/// <summary>
/// Represents the result of a single test execution.
/// </summary>
public class TestResult
{
    /// <summary>
    /// Gets or sets the name of the test.
    /// </summary>
    public required string TestName { get; init; }

    /// <summary>
    /// Gets or sets whether the test passed.
    /// </summary>
    public bool Passed { get; init; }

    /// <summary>
    /// Gets or sets whether the test was skipped.
    /// </summary>
    public bool Skipped { get; init; }

    /// <summary>
    /// Gets or sets the skip reason if the test was skipped.
    /// </summary>
    public string? SkipReason { get; init; }

    /// <summary>
    /// Gets or sets the error message if the test failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the duration of the test execution.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the group/category this test belongs to.
    /// </summary>
    public required string Group { get; init; }
}

/// <summary>
/// Represents the results of a test group.
/// </summary>
public class TestGroupResults
{
    /// <summary>
    /// Gets or sets the name of the test group.
    /// </summary>
    public required string GroupName { get; init; }

    /// <summary>
    /// Gets or sets the list of test results in this group.
    /// </summary>
    public List<TestResult> Results { get; } = new();

    /// <summary>
    /// Gets the total number of tests in this group.
    /// </summary>
    public int TotalTests => Results.Count;

    /// <summary>
    /// Gets the number of passed tests.
    /// </summary>
    public int PassedTests => Results.Count(r => r.Passed && !r.Skipped);

    /// <summary>
    /// Gets the number of failed tests.
    /// </summary>
    public int FailedTests => Results.Count(r => !r.Passed && !r.Skipped);

    /// <summary>
    /// Gets the number of skipped tests.
    /// </summary>
    public int SkippedTests => Results.Count(r => r.Skipped);

    /// <summary>
    /// Gets the total duration of all tests in this group.
    /// </summary>
    public TimeSpan TotalDuration => TimeSpan.FromTicks(Results.Sum(r => r.Duration.Ticks));

    /// <summary>
    /// Gets whether all tests in this group passed (or were skipped).
    /// </summary>
    public bool AllPassed => FailedTests == 0;
}
