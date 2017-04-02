// Decompiled with JetBrains decompiler
// Type: ns4.Attribute2
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  internal class Attribute2 : Attribute
  {
    private string jia;

    public string TexturePathProperty
    {
      get
      {
        return this.jia;
      }
    }

    public Attribute2(string texturepathproperty)
    {
      this.jia = texturepathproperty;
    }
  }
}
