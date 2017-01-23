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
        if (object.ReferenceEquals((object) Class51.resourceManager_0, (object) null))
          Class51.resourceManager_0 = new ResourceManager("SynapseGaming.LightingSystem.Effects.Resources", typeof (Class51).Assembly);
        return Class51.resourceManager_0;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return Class51.cultureInfo_0;
      }
      set
      {
        Class51.cultureInfo_0 = value;
      }
    }

    internal static byte[] BillboardEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("BillboardEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] ConsoleFont
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("ConsoleFont", Class51.cultureInfo_0);
      }
    }

    internal static byte[] DefaultEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("DefaultEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] DeferredDepthEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("DeferredDepthEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] DeferredLightingEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("DeferredLightingEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] DeferredObjectEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("DeferredObjectEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] DeferredTerrainEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("DeferredTerrainEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] FogEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("FogEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] FullSphere
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("FullSphere", Class51.cultureInfo_0);
      }
    }

    internal static byte[] HighDynamicRange
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("HighDynamicRange", Class51.cultureInfo_0);
      }
    }

    internal static byte[] LightIcons
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("LightIcons", Class51.cultureInfo_0);
      }
    }

    internal static byte[] LightingEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("LightingEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] Normal
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("Normal", Class51.cultureInfo_0);
      }
    }

    internal static byte[] ShadowEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("ShadowEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] SplashScreen
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("SplashScreen", Class51.cultureInfo_0);
      }
    }

    internal static byte[] TerrainEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("TerrainEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] VolumeLightBeam
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("VolumeLightBeam", Class51.cultureInfo_0);
      }
    }

    internal static byte[] VolumeLightEffect
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("VolumeLightEffect", Class51.cultureInfo_0);
      }
    }

    internal static byte[] White
    {
      get
      {
        return (byte[]) Class51.ResourceManager.GetObject("White", Class51.cultureInfo_0);
      }
    }

    internal Class51()
    {
    }
  }
}
