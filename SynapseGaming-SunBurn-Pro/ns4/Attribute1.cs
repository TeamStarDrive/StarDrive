// Decompiled with JetBrains decompiler
// Type: ns4.Attribute1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using SynapseGaming.LightingSystem.Editor;

namespace ns4
{
  [AttributeUsage(AttributeTargets.Property)]
  internal class Attribute1 : Attribute
  {
      public bool EditorVisible { get; }

      public string Description { get; set; }

      public string ToolTipText { get; set; }

      public int MajorGrouping { get; set; }

      public int MinorGrouping { get; set; }

      public bool HorizontalAlignment { get; set; }

      public ControlType ControlType { get; set; }

      public Attribute1(bool editorvisible)
    {
      this.EditorVisible = editorvisible;
      this.ControlType = ControlType.Default;
    }
  }
}
