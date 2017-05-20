// Decompiled with JetBrains decompiler
// Type: ns6.Class51
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace ns6
{
  [DebuggerNonUserCode]
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
  [CompilerGenerated]
  internal class Class51
  {
    private static ResourceManager resourceManager_0;
    private static CultureInfo cultureInfo_0;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (ReferenceEquals(resourceManager_0, null))
          resourceManager_0 = new ResourceManager("SynapseGaming.LightingSystem.Effects.Resources", typeof (Class51).Assembly);
        return resourceManager_0;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get => cultureInfo_0;
        set => cultureInfo_0 = value;
    }

    internal static byte[] BillboardEffect => (byte[]) ResourceManager.GetObject("BillboardEffect", cultureInfo_0);

      internal static byte[] ConsoleFont => (byte[]) ResourceManager.GetObject("ConsoleFont", cultureInfo_0);

      internal static byte[] DefaultEffect => (byte[]) ResourceManager.GetObject("DefaultEffect", cultureInfo_0);

      internal static byte[] DeferredDepthEffect => (byte[]) ResourceManager.GetObject("DeferredDepthEffect", cultureInfo_0);

      internal static byte[] DeferredLightingEffect => (byte[]) ResourceManager.GetObject("DeferredLightingEffect", cultureInfo_0);

      internal static byte[] DeferredObjectEffect => (byte[]) ResourceManager.GetObject("DeferredObjectEffect", cultureInfo_0);

      internal static byte[] DeferredTerrainEffect => (byte[]) ResourceManager.GetObject("DeferredTerrainEffect", cultureInfo_0);

      internal static byte[] FogEffect => (byte[]) ResourceManager.GetObject("FogEffect", cultureInfo_0);

      internal static byte[] FullSphere => (byte[]) ResourceManager.GetObject("FullSphere", cultureInfo_0);

      internal static byte[] HighDynamicRange => (byte[]) ResourceManager.GetObject("HighDynamicRange", cultureInfo_0);

      internal static byte[] LightIcons => (byte[]) ResourceManager.GetObject("LightIcons", cultureInfo_0);

      internal static byte[] LightingEffect => (byte[]) ResourceManager.GetObject("LightingEffect", cultureInfo_0);

      internal static byte[] Normal => (byte[]) ResourceManager.GetObject("Normal", cultureInfo_0);

      internal static byte[] ShadowEffect => (byte[]) ResourceManager.GetObject("ShadowEffect", cultureInfo_0);

      internal static byte[] SplashScreen => (byte[]) ResourceManager.GetObject("SplashScreen", cultureInfo_0);

      internal static byte[] TerrainEffect => (byte[]) ResourceManager.GetObject("TerrainEffect", cultureInfo_0);

      internal static byte[] VolumeLightBeam => (byte[]) ResourceManager.GetObject("VolumeLightBeam", cultureInfo_0);

      internal static byte[] VolumeLightEffect => (byte[]) ResourceManager.GetObject("VolumeLightEffect", cultureInfo_0);

      internal static byte[] White => (byte[]) ResourceManager.GetObject("White", cultureInfo_0);
  }
}
