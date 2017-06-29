// Decompiled with JetBrains decompiler
// Type: ns4.Attribute6
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Property)]
  internal class Attribute6 : Attribute4
  {
      public bool IntegerValue { get; }

      public Attribute6(bool integervalue)
    {
      this.IntegerValue = integervalue;
    }
  }
}
