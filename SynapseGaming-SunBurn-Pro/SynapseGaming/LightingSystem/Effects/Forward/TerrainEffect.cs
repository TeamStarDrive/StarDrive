// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.TerrainEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Effects.Forward
{
  /// <summary>
  /// Provides SunBurn's built-in forward terrain rendering.
  /// </summary>
  public class TerrainEffect : BaseTerrainEffect, ILightingEffect
  {
    private ILight ilight_0 = new AmbientLight();
    private const int int_3 = 1;
    private Texture3D texture3D_0;
    private EffectParameter effectParameter_31;
    private EffectParameter effectParameter_32;
    private EffectParameter effectParameter_33;
    private EffectParameter effectParameter_34;
    private static Vector4 vector4_0;
    private static Vector4 vector4_1;
    private static Vector4 vector4_2;

    /// <summary>Maximum number of light sources the effect supports.</summary>
    public int MaxLightSources => 1;

      /// <summary>
    /// Light sources that apply lighting to the effect during rendering.
    /// </summary>
    public List<ILight> LightSources
    {
      set
      {
        if (value.Count != 1)
          throw new ArgumentException("TerrainEffect only supports a single light per-pass at this time.");
        this.ilight_0 = value[0];
        this.method_5();
        ++this.class46_0.lightingSystemStatistic_2.AccumulationValue;
      }
    }

    /// <summary>
    /// Texture that represents a lighting model falloff-map used to apply lighting to materials.
    /// </summary>
    public Texture3D LightingTexture
    {
      get => this.texture3D_0;
        set
      {
        if (this.texture3D_0 == value || this.effectParameter_31 == null)
          return;
        this.texture3D_0 = value;
        this.effectParameter_31.SetValue(value);
      }
    }

    /// <summary>Creates a new TerrainEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    public TerrainEffect(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "TerrainEffect")
    {
      this.method_6(graphicsdevice);
    }

    internal TerrainEffect(GraphicsDevice graphicsDevice_0, bool bool_3)
      : base(graphicsDevice_0, "TerrainEffect", bool_3)
    {
      this.method_6(graphicsDevice_0);
    }

    private void method_5()
    {
      if (this.effectParameter_32 == null || this.effectParameter_33 == null || (this.effectParameter_34 == null || this.ilight_0 == null))
        return;
      vector4_0 = new Vector4(this.ilight_0.CompositeColorAndIntensity, 0.0f);
      vector4_2 = new Vector4();
      if (this.ilight_0 is ISpotSource)
      {
        ISpotSource ilight0 = this.ilight_0 as ISpotSource;
        float w = (float) Math.Cos(MathHelper.ToRadians(MathHelper.Clamp(ilight0.Angle * 0.5f, 0.01f, 89.99f)));
        float num = (float) (1.0 / (1.0 - w));
        vector4_0.W = num;
        vector4_2 = new Vector4(ilight0.Direction, w);
        vector4_1 = new Vector4(ilight0.Position, ilight0.Radius);
      }
      else if (this.ilight_0 is IPointSource)
      {
        IPointSource ilight0 = this.ilight_0 as IPointSource;
        vector4_1 = new Vector4(ilight0.Position, ilight0.Radius);
      }
      else if (this.ilight_0 is IShadowSource)
        vector4_1 = new Vector4((this.ilight_0 as IShadowSource).ShadowPosition, 1E+09f);
      this.effectParameter_32.SetValue(vector4_0);
      this.effectParameter_33.SetValue(vector4_1);
      this.effectParameter_34.SetValue(vector4_2);
      this.SetTechnique();
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected override void SetTechnique()
    {
      if (this.ilight_0 is IAmbientSource)
        this.CurrentTechnique = this.Techniques["Terrain_Ambient_Technique"];
      else
        this.CurrentTechnique = this.Techniques["Terrain_Technique"];
    }

    private void method_6(GraphicsDevice graphicsDevice_0)
    {
      this.effectParameter_31 = this.Parameters["_LightingTexture"];
      this.effectParameter_32 = this.Parameters["_DiffuseColor_And_SpotAngleInv"];
      this.effectParameter_33 = this.Parameters["_Position_And_Radius"];
      this.effectParameter_34 = this.Parameters["_SpotDirection_And_SpotAngle"];
      this.LightingTexture = LightingSystemManager.Instance.method_4(graphicsDevice_0);
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return new TerrainEffect(device);
    }
  }
}
