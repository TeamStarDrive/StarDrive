// Decompiled with JetBrains decompiler
// Type: ns6.Class45
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects;

namespace EmbeddedResources
{
  internal class Class45 : BaseRenderableEffect
  {
    private Vector3 vector3_0;
    private Vector3 vector3_1;
    private Vector3 vector3_2;
    private Vector2 vector2_0;
    private Texture2D texture2D_0;
    private Texture2D texture2D_1;
    private EffectParameter effectParameter_11;
    private EffectParameter effectParameter_12;
    private EffectParameter effectParameter_13;
    private EffectParameter effectParameter_14;
    private EffectParameter effectParameter_15;
    private EffectParameter effectParameter_16;

    public Vector3 Color
    {
      get => this.vector3_2;
        set => EffectHelper.Update(value, ref this.vector3_2, ref this.effectParameter_13);
    }

    public Texture2D BeamTexture
    {
      get => this.texture2D_0;
        set => EffectHelper.Update(value, ref this.texture2D_0, this.effectParameter_15);
    }

    public Texture2D SceneDepthMap
    {
      get => this.texture2D_1;
        set
      {
        EffectHelper.Update(value, ref this.texture2D_1, this.effectParameter_16);
        if (this.effectParameter_14 != null && this.texture2D_1 != null)
          EffectHelper.Update(new Vector2(this.texture2D_1.Width, this.texture2D_1.Height), ref this.vector2_0, ref this.effectParameter_14);
        this.SetTechnique();
      }
    }

    public Class45(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "VolumeLightEffect")
    {
      this.effectParameter_11 = this.Parameters["_AngleAndRadiusScale"];
      this.effectParameter_12 = this.Parameters["_NearClipFarClipScale"];
      this.effectParameter_13 = this.Parameters["_Color"];
      this.effectParameter_14 = this.Parameters["_TargetWidthHeight"];
      this.effectParameter_15 = this.Parameters["_BeamTexture"];
      this.effectParameter_16 = this.Parameters["_SceneDepthTexture"];
      this.Color = Vector3.One;
      this.BeamTexture = LightingSystemManager.Instance.EmbeddedTexture("VolumeLightBeam");
    }

    public void method_2(float float_1, float float_2)
    {
      if (this.effectParameter_11 == null)
        return;
      float_1 = MathHelper.ToRadians(MathHelper.Clamp(float_1, 0.1f, 90f));
      float num = (float) Math.Sin(float_1) * float_2;
      EffectHelper.Update(new Vector3(num, num, float_2), ref this.vector3_0, ref this.effectParameter_11);
    }

    public void method_3(Matrix matrix_9)
    {
      if (this.effectParameter_12 == null)
        return;
      Vector3 vector3_0 = new Vector3();
      Vector4 vector4_1 = Vector4.Transform(new Vector4(0.0f, 0.0f, 0.0f, 1f), matrix_9);
      Vector4 vector4_2 = Vector4.Transform(new Vector4(0.0f, 0.0f, 1f, 1f), matrix_9);
      if (vector4_1.W != 0.0)
        vector3_0.X = Math.Abs(vector4_1.Z / vector4_1.W);
      if (vector4_2.W != 0.0)
        vector3_0.Y = Math.Abs(vector4_2.Z / vector4_2.W);
      float num = vector3_0.Y - vector3_0.X;
      if (num != 0.0)
        vector3_0.Z = 10000f / num;
      EffectHelper.Update(vector3_0, ref this.vector3_1, ref this.effectParameter_12);
    }

    protected override void SetTechnique()
    {
      if (this.texture2D_1 != null)
        this.CurrentTechnique = this.Techniques["Technique_Deferred"];
      else
        this.CurrentTechnique = this.Techniques["Technique_Forward"];
    }

    protected override Effect Create(GraphicsDevice device)
    {
      return new Class45(device);
    }
  }
}
