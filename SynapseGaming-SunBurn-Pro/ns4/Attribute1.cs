// Decompiled with JetBrains decompiler
// Type: ns4.Attribute1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Editor;
using System;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
  internal class Attribute1 : Attribute
  {
    private bool jia;
    private string jik;
    private string jiV;
    private int jij;
    private int jiq;
    private bool jic;
    private ControlType jiR;

    public bool EditorVisible
    {
      get
      {
        return this.jia;
      }
    }

    public string Description
    {
      get
      {
        return this.jik;
      }
      set
      {
        this.jik = value;
      }
    }

    public string ToolTipText
    {
      get
      {
        return this.jiV;
      }
      set
      {
        this.jiV = value;
      }
    }

    public int MajorGrouping
    {
      get
      {
        return this.jij;
      }
      set
      {
        this.jij = value;
      }
    }

    public int MinorGrouping
    {
      get
      {
        return this.jiq;
      }
      set
      {
        this.jiq = value;
      }
    }

    public bool HorizontalAlignment
    {
      get
      {
        return this.jic;
      }
      set
      {
        this.jic = value;
      }
    }

    public ControlType ControlType
    {
      get
      {
        return this.jiR;
      }
      set
      {
        this.jiR = value;
      }
    }

    public Attribute1(bool editorvisible)
    {
      this.jia = editorvisible;
      this.jiR = ControlType.Default;
    }
  }
}
