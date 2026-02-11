using System;

namespace Nuxie.Unity;

public sealed class NuxieException : Exception
{
  public NuxieException(string code, string message, string? nativeStack = null, Exception? inner = null)
    : base(message, inner)
  {
    Code = code;
    NativeStack = nativeStack;
  }

  public string Code { get; }

  public string? NativeStack { get; }
}
