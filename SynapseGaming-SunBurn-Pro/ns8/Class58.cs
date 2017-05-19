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
      public DeferredBufferType Buffer { get; } = DeferredBufferType.None;

      public SurfaceFormat Format { get; set; } = SurfaceFormat.Unknown;

      public Class58(DeferredBufferType buffer, SurfaceFormat format)
    {
      this.Buffer = buffer;
      this.Format = format;
    }
  }
}
