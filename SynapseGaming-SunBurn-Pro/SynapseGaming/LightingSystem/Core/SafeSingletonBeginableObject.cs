// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.SafeSingletonBeginableObject
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Base object for singleton Begin/End statements.  Forces a Begin called on an object
  /// to be followed by an End on the same object before Begin can be called on any other
  /// object derived from this type.
  /// </summary>
  public class SafeSingletonBeginableObject
  {
    private static bool bool_0;
    private static object object_0;

    /// <summary>Verifies no other Begin is in process.</summary>
    public virtual void Begin()
    {
      if (bool_0)
        throw new Exception("Cannot call begin within previous begin statement.  Try calling end on the previously begun object.");
      bool_0 = true;
      object_0 = this;
    }

    /// <summary>Verifies a Begin is in process on this object.</summary>
    public virtual void End()
    {
      if (!bool_0)
        throw new Exception("Cannot call end without first calling begin.");
      if (object_0 != this)
        throw new Exception("Cannot call end on this object.  Begin was last called on another object.");
      bool_0 = false;
      object_0 = null;
    }
  }
}
