// Decompiled with JetBrains decompiler
// Type: ns8.Class58
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Rendering.Deferred;

namespace ns8
{
  internal class Class58
  {
    private DeferredBufferType deferredBufferType_0 = DeferredBufferType.None;
    private SurfaceFormat surfaceFormat_0 = SurfaceFormat.Unknown;

    public DeferredBufferType Buffer
    {
      get
      {
        return this.deferredBufferType_0;
      }
    }

    public SurfaceFormat Format
    {
      get
      {
        return this.surfaceFormat_0;
      }
      set
      {
        this.surfaceFormat_0 = value;
      }
    }

    public Class58(DeferredBufferType buffer, SurfaceFormat format)
    {
      this.deferredBufferType_0 = buffer;
      this.surfaceFormat_0 = format;
    }
  }
}
