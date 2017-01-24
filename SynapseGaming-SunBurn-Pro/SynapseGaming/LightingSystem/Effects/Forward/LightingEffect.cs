// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.LightingEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Effects.Forward
{
  /// <summary>
  /// Effect provides SunBurn's built-in lighting and material support.
  /// 
  /// Including:
  /// -Diffuse mapping
  /// -Bump mapping
  /// -Specular mapping (with specular intensity mapping)
  /// -Point, spot, directional, and ambient lighting
  /// </summary>
  public class LightingEffect : BaseMaterialEffect, ILightingEffect
  {
    private static Vector4[] vector4_1 = new Vector4[1];
    private static Vector4[] vector4_2 = new Vector4[1];
    private static Vector4[] vector4_3 = new Vector4[1];
    private const int int_0 = 1;
    private EffectParameter effectParameter_22;
    private EffectParameter effectParameter_23;
    private EffectParameter effectParameter_24;

    /// <summary>Maximum number of light sources the effect supports.</summary>
    public int MaxLightSources
    {
      get
      {
        return 1;
      }
    }

    /// <summary>
    /// Light sources that apply lighting to the effect during rendering.
    /// </summary>
    public List<ILight> LightSources
    {
      set
      {
        this._LightSources.Clear();
        foreach (ILight light in value)
          this._LightSources.Add(light);
        this.method_5();
        ++this.class46_0.lightingSystemStatistic_2.AccumulationValue;
      }
    }

    /// <summary>Creates a new LightingEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    public LightingEffect(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "LightingEffect")
    {
      this.method_6(graphicsdevice);
    }

    internal LightingEffect(GraphicsDevice graphicsDevice_0, bool bool_5)
      : base(graphicsDevice_0, "LightingEffect", bool_5)
    {
      this.method_6(graphicsDevice_0);
    }

    private void method_5()
    {
      if (this.effectParameter_22 == null || this.effectParameter_23 == null || (this.effectParameter_24 == null || this._LightSources == null))
        return;
      if (this._LightSources.Count > LightingEffect.vector4_1.Length)
        throw new ArgumentException("Too many light sources provided for effect.");
      if (this._LightSources.Count != 1)
        throw new ArgumentException("LightingEffect only supports a single light per-pass at this time.");
      for (int index = 0; index < this._LightSources.Count; ++index)
      {
        ILight lightSource = this._LightSources[index];
        Vector3 colorAndIntensity = lightSource.CompositeColorAndIntensity;
        LightingEffect.vector4_1[index] = new Vector4(colorAndIntensity, 0.0f);
        LightingEffect.vector4_3[index] = new Vector4();
        if (lightSource is ISpotSource)
        {
          ISpotSource spotSource = lightSource as ISpotSource;
          float w = (float) Math.Cos((double) MathHelper.ToRadians(MathHelper.Clamp(spotSource.Angle * 0.5f, 0.01f, 89.99f)));
          float num = (float) (1.0 / (1.0 - (double) w));
          LightingEffect.vector4_1[index].W = num;
          LightingEffect.vector4_3[index] = new Vector4(spotSource.Direction, w);
          LightingEffect.vector4_2[index] = new Vector4(spotSource.Position, spotSource.Radius);
        }
        else if (lightSource is IPointSource)
        {
          IPointSource pointSource = lightSource as IPointSource;
          LightingEffect.vector4_2[index] = new Vector4(pointSource.Position, pointSource.Radius);
        }
        else if (lightSource is IShadowSource)
        {
          IShadowSource shadowSource = lightSource as IShadowSource;
          LightingEffect.vector4_2[index] = new Vector4(shadowSource.ShadowPosition, 1E+09f);
        }
      }
      this.effectParameter_22.SetValue(LightingEffect.vector4_1);
      this.effectParameter_23.SetValue(LightingEffect.vector4_2);
      this.effectParameter_24.SetValue(LightingEffect.vector4_3);
      this.SetTechnique();
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return (Effect) new LightingEffect(device);
    }

    private void method_6(GraphicsDevice graphicsDevice_0)
    {
      this.effectParameter_22 = this.Parameters["_DiffuseColor_And_SpotAngleInv"];
      this.effectParameter_23 = this.Parameters["_Position_And_Radius"];
      this.effectParameter_24 = this.Parameters["_SpotDirection_And_SpotAngle"];
    }
  }
}
