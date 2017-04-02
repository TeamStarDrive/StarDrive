// Decompiled with JetBrains decompiler
// Type: ns4.Class30
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ns4
{
  [DebuggerNonUserCode]
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
  [CompilerGenerated]
  internal class Class30
  {
    private static ResourceManager resourceManager_0;
    private static CultureInfo cultureInfo_0;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) Class30.resourceManager_0, (object) null))
          Class30.resourceManager_0 = new ResourceManager("SynapseGaming.LightingSystem.Editor.EditorResources", typeof (Class30).Assembly);
        return Class30.resourceManager_0;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return Class30.cultureInfo_0;
      }
      set
      {
        Class30.cultureInfo_0 = value;
      }
    }

    internal static Bitmap ClearImage
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("ClearImage", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap ColorSelectBg
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("ColorSelectBg", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap ErrorImage
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("ErrorImage", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap LightAdd
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("LightAdd", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap LightDelete
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("LightDelete", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap LightMove
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("LightMove", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap LightRotate
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("LightRotate", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap Redo
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("Redo", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap Refresh
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("Refresh", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap SaveAll
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("SaveAll", Class30.cultureInfo_0);
      }
    }

    internal static Bitmap Undo
    {
      get
      {
        return (Bitmap) Class30.ResourceManager.GetObject("Undo", Class30.cultureInfo_0);
      }
    }

    internal Class30()
    {
    }
  }
}
