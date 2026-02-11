namespace Nuxie.Unity.Core.Tests;

internal static class TestTaskExtensions
{
  public static async Task<T> WaitWithTimeout<T>(this Task<T> task, TimeSpan timeout)
  {
    using var timeoutCts = new CancellationTokenSource(timeout);
    var completed = await Task.WhenAny(task, TimeoutTask<T>(timeoutCts.Token));
    if (completed == task)
    {
      return await task;
    }

    throw new TimeoutException($"Task did not complete within {timeout}.");
  }

  private static async Task<T> TimeoutTask<T>(CancellationToken cancellationToken)
  {
    try
    {
      await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }
    catch (OperationCanceledException)
    {
      throw new TimeoutException();
    }

    throw new TimeoutException();
  }
}
