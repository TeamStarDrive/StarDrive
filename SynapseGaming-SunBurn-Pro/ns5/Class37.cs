// Decompiled with JetBrains decompiler
// Type: ns5.Class37
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Graphics;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Effects;

namespace ns5
{
  internal class Class37 : BaseRenderableEffect, Interface2
  {
    private Texture2D texture2D_0;
    private EffectParameter effectParameter_11;

    public Texture2D SceneDepthMap
    {
      get => this.texture2D_0;
        set => EffectHelper.Update(value, ref this.texture2D_0, this.effectParameter_11);
    }

    public Texture2D SceneNormalSpecularMap
    {
      get
      {
        return null;
      }
      set
      {
      }
    }

    public Class37(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "DeferredDepthEffect")
    {
      this.effectParameter_11 = this.Parameters["_SceneDepthMap"];
    }

    protected override void SetTechnique()
    {
    }

    protected override Effect Create(GraphicsDevice device)
    {
      return new Class37(device);
    }
  }
}
