﻿// Decompiled with JetBrains decompiler
// Type: ns4.Attribute3
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
  internal class Attribute3 : Attribute
  {
      public string Description { get; }

      public bool Ignore { get; }

      public Attribute3(string description)
    {
      this.Description = description;
      this.Ignore = false;
    }

    public Attribute3(bool ignore)
    {
      this.Ignore = ignore;
    }
  }
}
