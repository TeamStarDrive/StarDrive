// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.SasLightingEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Effects.Forward
{
  /// <summary>
  /// Effect class with complete support for, and binding of, FX Standard Annotations and Semantics (SAS).
  /// </summary>
  public class SasLightingEffect : SasEffect, ILightingEffect
  {
      private List<ILight> list_0 = new List<ILight>();

    /// <summary>Maximum number of light sources the effect supports.</summary>
    public int MaxLightSources { get; private set; } = 1;

      /// <summary>
    /// Light sources that apply lighting to the effect during rendering.
    /// </summary>
    public List<ILight> LightSources
    {
      set
      {
        this.list_0.Clear();
        foreach (ILight light in value)
          this.list_0.Add(light);
        this.SyncLightSourceEffectData();
        ++this.class47_0.lightingSystemStatistic_1.AccumulationValue;
      }
    }

    /// <summary>
    /// Creates a new SasLightingEffect instance from an effect containing an SAS shader
    /// (often loaded through the content pipeline or from disk).
    /// </summary>
    /// <param name="device"></param>
    /// <param name="effect">Source effect containing an SAS shader.</param>
    public SasLightingEffect(GraphicsDevice device, Effect effect)
      : base(device, effect)
    {
      this.FindMaxLightCount();
    }

    internal SasLightingEffect(GraphicsDevice device, Effect effect_0, bool bool_5)
      : base(device, effect_0, bool_5)
    {
      this.FindMaxLightCount();
    }

    /// <summary>Sets the max light count supported by the effect.</summary>
    /// <param name="maxlights"></param>
    protected virtual void SetMaxLightCount(int maxlights)
    {
      this.MaxLightSources = maxlights;
    }

    /// <summary>
    /// Finds the max light count supported by the effect's shader.
    /// </summary>
    protected virtual void FindMaxLightCount()
    {
      this.MaxLightSources = 0;
      int val1 = 0;
      int val2 = 0;
      int length = SASAddress_AmbientLight_Color.Length;
      for (int index = 0; index < length; ++index)
      {
        if (this.SasAutoBindTable.method_1(SASAddress_DirectionalLight_Color[index]) != null)
          val1 = index + 1;
        if (this.SasAutoBindTable.method_1(SASAddress_PointLight_Color[index]) != null)
          val2 = index + 1;
      }
      if (val2 < 1)
        this.MaxLightSources = val1;
      else if (val1 < 1)
        this.MaxLightSources = val2;
      else
        this.MaxLightSources = Math.Min(val1, val2);
    }

    /// <summary>
    /// Applies the current lighting information to the bound effect parameters.
    /// </summary>
    protected virtual void SyncLightSourceEffectData()
    {
      if (this.list_0.Count < 1)
        return;
      bool flag = this.SasAutoBindTable.method_1(SASAddress_DirectionalLight_Color[0]) != null;
      int index1 = 0;
      int index2 = 0;
      int index3 = 0;
      for (int index4 = 0; index4 < this.list_0.Count; ++index4)
      {
        ILight light = this.list_0[0];
        if (light is AmbientLight)
        {
          EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_AmbientLight_Color[index1]), new Vector4(light.CompositeColorAndIntensity, 1f));
          ++index1;
        }
        else if (light is IPointSource)
        {
          EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Color[index3]), new Vector4(light.CompositeColorAndIntensity, 1f));
          EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Position[index3]), new Vector4((light as IPointSource).Position, 1f));
          EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Range[index3]), new Vector4((light as IPointSource).Radius));
          ++index3;
        }
        else if (light is IDirectionalSource)
        {
          if (flag)
          {
            EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_DirectionalLight_Color[index2]), new Vector4(light.CompositeColorAndIntensity, 1f));
            EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_DirectionalLight_Direction[index2]), new Vector4((light as IDirectionalSource).Direction, 1f));
            ++index2;
          }
          else
          {
            EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Color[index3]), new Vector4(light.CompositeColorAndIntensity, 1f));
            EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Position[index3]), new Vector4((light as IShadowSource).ShadowPosition, 1f));
            EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Range[index3]), new Vector4(1E+09f));
            ++index3;
          }
        }
      }
      int length = SASAddress_AmbientLight_Color.Length;
      for (int index4 = index1; index4 < length; ++index4)
        EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_AmbientLight_Color[index4]), new Vector4(0.0f));
      for (int index4 = index2; index4 < length; ++index4)
        EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_DirectionalLight_Color[index4]), new Vector4(0.0f));
      for (int index4 = index3; index4 < length; ++index4)
        EffectHelper.Update(this.SasAutoBindTable.method_1(SASAddress_PointLight_Color[index4]), new Vector4(0.0f));
      EffectHelper.Update(this.SasAutoBindTable.method_1("Sas.NumAmbientLights"), new Vector4(index1));
      EffectHelper.Update(this.SasAutoBindTable.method_1("Sas.NumDirectionalLights"), new Vector4(index2));
      EffectHelper.Update(this.SasAutoBindTable.method_1("Sas.NumPointLights"), new Vector4(index3));
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return new SasLightingEffect(device, this);
    }
  }
}
