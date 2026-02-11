using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nuxie.Unity;

internal sealed class TriggerOperationState
{
  private readonly object _gate = new();
  private readonly List<Action<TriggerUpdate>> _listeners = [];
  private readonly TaskCompletionSource<TriggerTerminalUpdate> _doneSource =
    new(TaskCreationOptions.RunContinuationsAsynchronously);

  private bool _completed;

  internal Task<TriggerTerminalUpdate> Done => _doneSource.Task;

  internal IDisposable Subscribe(Action<TriggerUpdate> listener)
  {
    lock (_gate)
    {
      if (_completed)
      {
        return NoopDisposable.Instance;
      }

      _listeners.Add(listener);
      return new Subscription(this, listener);
    }
  }

  internal void Emit(TriggerUpdate update)
  {
    Action<TriggerUpdate>[] snapshot;
    lock (_gate)
    {
      snapshot = _listeners.ToArray();
    }

    foreach (var listener in snapshot)
    {
      listener(update);
    }
  }

  internal void TryComplete(TriggerUpdate terminalUpdate)
  {
    lock (_gate)
    {
      if (_completed)
      {
        return;
      }

      _completed = true;
      _listeners.Clear();
    }

    _doneSource.TrySetResult(TriggerTerminalUpdate.From(terminalUpdate));
  }

  private void RemoveListener(Action<TriggerUpdate> listener)
  {
    lock (_gate)
    {
      if (_completed)
      {
        return;
      }

      _listeners.Remove(listener);
    }
  }

  private sealed class Subscription : IDisposable
  {
    private TriggerOperationState? _state;
    private Action<TriggerUpdate>? _listener;

    public Subscription(TriggerOperationState state, Action<TriggerUpdate> listener)
    {
      _state = state;
      _listener = listener;
    }

    public void Dispose()
    {
      var state = _state;
      var listener = _listener;
      _state = null;
      _listener = null;
      if (state is not null && listener is not null)
      {
        state.RemoveListener(listener);
      }
    }
  }

  private sealed class NoopDisposable : IDisposable
  {
    internal static readonly NoopDisposable Instance = new();

    public void Dispose()
    {
    }
  }
}

public sealed class NuxieTriggerOperation
{
  private readonly TriggerOperationState _state;
  private readonly Func<Task> _cancel;

  internal NuxieTriggerOperation(string requestId, TriggerOperationState state, Func<Task> cancel)
  {
    RequestId = requestId;
    _state = state;
    _cancel = cancel;
  }

  public string RequestId { get; }

  public Task<TriggerTerminalUpdate> Done => _state.Done;

  public IDisposable OnUpdate(Action<TriggerUpdate> listener)
  {
    return _state.Subscribe(listener);
  }

  public Task CancelAsync()
  {
    return _cancel();
  }
}
