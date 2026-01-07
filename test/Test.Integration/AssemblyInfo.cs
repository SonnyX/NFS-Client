using Xunit;

// Disable test parallelization to prevent socket exhaustion
// when running integration tests against Docker NFS servers
[assembly: CollectionBehavior(DisableTestParallelization = true)]
