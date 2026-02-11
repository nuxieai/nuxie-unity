#if UNITY_5_3_OR_NEWER
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace Nuxie.Unity;

public static class NuxieTaskExtensions
{
  public static IEnumerator AsCoroutine(this Task task, Action<Exception>? onError = null)
  {
    if (task is null)
    {
      throw new ArgumentNullException(nameof(task));
    }

    while (!task.IsCompleted)
    {
      yield return null;
    }

    if (task.IsCanceled)
    {
      var canceled = new TaskCanceledException(task);
      HandleError(canceled, onError);
      yield break;
    }

    if (task.IsFaulted)
    {
      var error = task.Exception?.InnerException ?? task.Exception!;
      HandleError(error, onError);
    }
  }

  public static IEnumerator AsCoroutine<T>(
    this Task<T> task,
    Action<T>? onSuccess = null,
    Action<Exception>? onError = null
  )
  {
    if (task is null)
    {
      throw new ArgumentNullException(nameof(task));
    }

    while (!task.IsCompleted)
    {
      yield return null;
    }

    if (task.IsCanceled)
    {
      var canceled = new TaskCanceledException(task);
      HandleError(canceled, onError);
      yield break;
    }

    if (task.IsFaulted)
    {
      var error = task.Exception?.InnerException ?? task.Exception!;
      HandleError(error, onError);
      yield break;
    }

    onSuccess?.Invoke(task.Result);
  }

  private static void HandleError(Exception error, Action<Exception>? onError)
  {
    if (onError is not null)
    {
      onError(error);
      return;
    }

    Debug.LogException(error);
  }
}
#endif
