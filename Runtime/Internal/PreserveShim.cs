#if !UNITY_5_3_OR_NEWER
using System;

namespace UnityEngine.Scripting;

[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class PreserveAttribute : Attribute
{
  public bool AllMembers { get; set; }
  public bool Conditional { get; set; }
}
#endif
