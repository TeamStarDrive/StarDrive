// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.FogEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Effects.Forward
{
  /// <summary>Effect provides per-pixel fog.</summary>
  public class FogEffect : BaseSkinnedEffect, ISamplerEffect, IAddressableEffect, ITransparentEffect, ITerrainEffect
  {
      private float float_1;
    private float float_2;
    private Vector3 vector3_0;
      private Texture2D texture2D_0;
    private int int_0;
    private float float_4;
    private float float_5;
    private Texture2D texture2D_1;
    private EffectParameter effectParameter_12;
    private EffectParameter effectParameter_13;
    private EffectParameter effectParameter_14;
    private EffectParameter effectParameter_15;
    private EffectParameter effectParameter_16;
    private EffectParameter effectParameter_17;
    private EffectParameter effectParameter_18;
    private EffectParameter effectParameter_19;

    /// <summary>
    /// Distance from the camera in world space that fog begins.
    /// </summary>
    public float StartDistance
    {
      get => this.float_1;
        set => this.method_2(value, this.float_2);
    }

    /// <summary>
    /// Distance from the camera in world space that fog ends.
    /// </summary>
    public float EndDistance
    {
      get => this.float_2;
        set => this.method_2(this.float_1, value);
    }

    /// <summary>Color of the applied fog.</summary>
    public Vector3 Color
    {
      get => this.vector3_0;
        set
      {
        if (this.vector3_0 == value || this.effectParameter_13 == null)
          return;
        this.vector3_0 = value;
        this.effectParameter_13.SetValue(new Vector4(this.vector3_0.X, this.vector3_0.Y, this.vector3_0.Z, 0.0f));
      }
    }

    /// <summary>
    /// Determines the effect's texture address mode in the U texture-space direction.
    /// </summary>
    public TextureAddressMode AddressModeU { get; set; } = TextureAddressMode.Wrap;

      /// <summary>
    /// Determines the effect's texture address mode in the V texture-space direction.
    /// </summary>
    public TextureAddressMode AddressModeV { get; set; } = TextureAddressMode.Wrap;

      /// <summary>
    /// Determines the effect's texture address mode in the W texture-space direction.
    /// </summary>
    public TextureAddressMode AddressModeW { get; set; } = TextureAddressMode.Wrap;

      /// <summary>
    /// Determines if the effect's shader changes sampler states while rendering.
    /// </summary>
    public bool AffectsSamplerStates => false;

      /// <summary>
    /// The transparency style used when rendering the effect.
    /// </summary>
    public TransparencyMode TransparencyMode { get; private set; }

      /// <summary>
    /// Used with TransparencyMode to determine the effect transparency.
    ///   -For Clipped mode this value is a comparison value, where all TransparencyMap
    ///    alpha values below this value are *not* rendered.
    /// </summary>
    public float Transparency { get; private set; } = -1f;

      /// <summary>
    /// The texture map used for transparency (values are pulled from the alpha channel).
    /// </summary>
    public Texture TransparencyMap => this.texture2D_0;

      /// <summary>
    /// Texture containing height values used to displace a terrain mesh. Also used
    /// for low frequency lighting.
    /// </summary>
    public Texture2D HeightMapTexture
    {
      get => this.texture2D_1;
          set
      {
        if (value == this.texture2D_1)
          return;
        EffectHelper.Update(value, ref this.texture2D_1, this.effectParameter_19);
        this.SetTechnique();
      }
    }

    /// <summary>Adjusts the terrain displacement magnitude.</summary>
    public float HeightScale
    {
      get => this.float_4;
        set => EffectHelper.Update(value, ref this.float_4, ref this.effectParameter_17);
    }

    /// <summary>
    /// Adjusts the number of times the height map tiles across a terrain's
    /// mesh. Similar to uv scale when texture mapping.
    /// </summary>
    public float Tiling
    {
      get => this.float_5;
        set => EffectHelper.Update(value, ref this.float_5, ref this.effectParameter_18);
    }

    /// <summary>Density or tessellation of the terrain mesh.</summary>
    public int MeshSegments
    {
      get => this.int_0;
        set => EffectHelper.Update(value, ref this.int_0, ref this.effectParameter_16);
    }

    /// <summary>Creates a new FogEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    public FogEffect(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "FogEffect")
    {
      this.effectParameter_12 = this.Parameters["_FogStartDist_And_EndDistInv"];
      this.effectParameter_13 = this.Parameters["_FogColor"];
      this.effectParameter_14 = this.Parameters["_TransparencyClipReference"];
      this.effectParameter_15 = this.Parameters["_TransparencyMap"];
      this.effectParameter_19 = this.Parameters["HeightMapTexture"];
      this.effectParameter_16 = this.Parameters["MeshSegments"];
      this.effectParameter_17 = this.Parameters["HeightScale"];
      this.effectParameter_18 = this.Parameters["Tiling"];
      this.StartDistance = 1000f;
      this.EndDistance = 100000f;
      this.Color = new Vector3(0.5f, 0.5f, 0.5f);
      this.SetTechnique();
    }

    /// <summary>
    /// Sets all transparency information at once.  Used to improve performance
    /// by avoiding multiple effect technique changes.
    /// </summary>
    /// <param name="mode">The transparency style used when rendering the effect.</param>
    /// <param name="transparency">Used with TransparencyMode to determine the effect transparency.
    /// -For Clipped mode this value is a comparison value, where all TransparencyMap
    ///  alpha values below this value are *not* rendered.</param>
    /// <param name="map">The texture map used for transparency (values are pulled from the alpha channel).</param>
    public void SetTransparencyModeAndMap(TransparencyMode mode, float transparency, Texture map)
    {
      bool flag = false;
      if (mode != this.TransparencyMode)
      {
        this.TransparencyMode = mode;
        flag = true;
      }
      if (this.effectParameter_14 != null && transparency != (double) this.Transparency)
      {
        this.Transparency = transparency;
        this.effectParameter_14.SetValue(this.Transparency);
        flag = true;
      }
      Texture2D texture2D = map as Texture2D;
      if (this.effectParameter_15 != null && texture2D != this.texture2D_0)
      {
        this.texture2D_0 = texture2D;
        this.effectParameter_15.SetValue(this.texture2D_0);
        flag = true;
      }
      if (!flag)
        return;
      this.SetTechnique();
    }

    private void method_2(float float_6, float float_7)
    {
      if (this.effectParameter_12 == null || this.float_1 == (double) float_6 && this.float_2 == (double) float_7)
        return;
      this.float_1 = Math.Max(float_6, 0.0f);
      this.float_2 = Math.Max(this.float_1 * 1.01f, float_7);
      float y = this.float_2 - this.float_1;
      if (y != 0.0)
        y = 1f / y;
      this.effectParameter_12.SetValue(new Vector4(this.float_1, y, 0.0f, 0.0f));
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected override void SetTechnique()
    {
      ++this.class46_0.lightingSystemStatistic_0.AccumulationValue;
      if (this.texture2D_1 != null)
        this.CurrentTechnique = this.Techniques["Fog_Terrain_Technique"];
      else
        this.CurrentTechnique = this.Techniques[TechniquNames.Get(TechniquNames.Enum3.Fog, TechniquNames.Enum4.None, 0, false, this.TransparencyMode != TransparencyMode.None, this.Skinned, false)];
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return new FogEffect(device);
    }
  }
}
