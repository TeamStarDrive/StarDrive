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
        if (ReferenceEquals(resourceManager_0, null))
          resourceManager_0 = new ResourceManager("SynapseGaming.LightingSystem.Editor.EditorResources", typeof (Class30).Assembly);
        return resourceManager_0;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get => cultureInfo_0;
        set => cultureInfo_0 = value;
    }

    internal static Bitmap ClearImage => (Bitmap) ResourceManager.GetObject("ClearImage", cultureInfo_0);

      internal static Bitmap ColorSelectBg => (Bitmap) ResourceManager.GetObject("ColorSelectBg", cultureInfo_0);

      internal static Bitmap ErrorImage => (Bitmap) ResourceManager.GetObject("ErrorImage", cultureInfo_0);

      internal static Bitmap LightAdd => (Bitmap) ResourceManager.GetObject("LightAdd", cultureInfo_0);

      internal static Bitmap LightDelete => (Bitmap) ResourceManager.GetObject("LightDelete", cultureInfo_0);

      internal static Bitmap LightMove => (Bitmap) ResourceManager.GetObject("LightMove", cultureInfo_0);

      internal static Bitmap LightRotate => (Bitmap) ResourceManager.GetObject("LightRotate", cultureInfo_0);

      internal static Bitmap Redo => (Bitmap) ResourceManager.GetObject("Redo", cultureInfo_0);

      internal static Bitmap Refresh => (Bitmap) ResourceManager.GetObject("Refresh", cultureInfo_0);

      internal static Bitmap SaveAll => (Bitmap) ResourceManager.GetObject("SaveAll", cultureInfo_0);

      internal static Bitmap Undo => (Bitmap) ResourceManager.GetObject("Undo", cultureInfo_0);
  }
}
