// Decompiled with JetBrains decompiler
// Type: ns4.Attribute3
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
  internal class Attribute3 : Attribute
  {
    private string jia;
    private bool jik;

    public string Description
    {
      get
      {
        return this.jia;
      }
    }

    public bool Ignore
    {
      get
      {
        return this.jik;
      }
    }

    public Attribute3(string description)
    {
      this.jia = description;
      this.jik = false;
    }

    public Attribute3(bool ignore)
    {
      this.jik = ignore;
    }
  }
}
