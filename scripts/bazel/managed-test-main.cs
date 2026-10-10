using System;
using System.Threading;
using Xunit.Runners;

internal static class ManagedTestMain
{
    private static int Main()
    {
        using var complete = new ManualResetEventSlim();
        using var runner = AssemblyRunner.WithoutAppDomain(typeof(ManagedTestMain).Assembly.Location);
        var exitCode = 1;
        var errors = 0;
        runner.OnTestPassed = test => Console.WriteLine("PASS " + test.TestDisplayName);
        runner.OnTestFailed = test => Console.Error.WriteLine("FAIL " + test.TestDisplayName + ": " + test.ExceptionMessage);
        runner.OnErrorMessage = error =>
        {
            Interlocked.Increment(ref errors);
            Console.Error.WriteLine(error.ExceptionMessage);
        };
        runner.OnExecutionComplete = result =>
        {
            Console.WriteLine($"xUnit: {result.TotalTests} tests, {result.TestsFailed} failed, {result.TestsSkipped} skipped.");
            exitCode = result.TotalTests > 0 && result.TestsFailed == 0 && errors == 0 ? 0 : 1;
            complete.Set();
        };
        runner.Start(new AssemblyRunnerStartOptions { Parallel = false, MaxParallelThreads = 1 });
        complete.Wait();
        while (runner.Status != AssemblyRunnerStatus.Idle) Thread.Sleep(10);
        return exitCode;
    }
}
