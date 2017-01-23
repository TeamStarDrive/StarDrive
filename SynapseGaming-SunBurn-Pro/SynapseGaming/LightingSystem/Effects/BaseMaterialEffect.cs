// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.BaseMaterialEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns4;
using ns6;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Base class that provides data for SunBurn materials (bump, specular, parallax, ...).  Used by the
  /// forward rendering LightingEffect and deferred rendering DeferredObjectEffect classes.
  /// </summary>
  [Attribute0(true)]
  public abstract class BaseMaterialEffect : BaseSkinnedEffect, IEditorObject, Interface0, ISamplerEffect, IAddressableEffect, ITextureAccessEffect, ITransparentEffect, Interface1
  {
    /// <summary />
    protected List<ILight> _LightSources = new List<ILight>();
    private float float_1 = 0.5f;
    private string string_0 = "";
    private bool bool_3;
    private TransparencyMode transparencyMode_0;
    private Texture3D texture3D_0;
    /// <summary />
    protected Texture2D _NormalMapTexture;
    /// <summary />
    protected Texture2D _DiffuseMapTexture;
    private Texture2D texture2D_0;
    private Texture2D texture2D_1;
    /// <summary />
    protected Texture2D _EmissiveMapTexture;
    /// <summary />
    protected Texture2D _SpecularColorMapTexture;
    /// <summary />
    protected Texture2D _ParallaxMapTexture;
    /// <summary />
    protected Texture2D _DefaultDiffuseMapTexture;
    /// <summary />
    protected Texture2D _DefaultNormalMapTexture;
    private Texture2D texture2D_2;
    private EffectParameter effectParameter_12;
    private EffectParameter effectParameter_13;
    private EffectParameter effectParameter_14;
    private EffectParameter effectParameter_15;
    private EffectParameter effectParameter_16;
    private EffectParameter effectParameter_17;
    /// <summary />
    protected EffectParameter _DiffuseColorIndirectParam;
    /// <summary />
    protected EffectParameter _DiffuseMapTextureIndirectParam;
    /// <summary />
    protected EffectParameter _NormalMapTextureIndirectParam;
    /// <summary />
    protected Vector4 _DiffuseColorOriginal;
    /// <summary />
    protected Vector4 _DiffuseColorCached;
    /// <summary />
    protected Vector4 _EmissiveColor;
    private EffectParameter effectParameter_18;
    private float float_2;
    private float float_3;
    private EffectParameter effectParameter_19;
    private float float_4;
    private float float_5;
    private float float_6;
    private Vector4 vector4_0;
    private EffectParameter effectParameter_20;
    private EffectParameter effectParameter_21;

    /// <summary>
    /// Notifies the editor that this object is partially controlled via code. The editor
    /// will display information to the user indicating some property values are
    /// overridden in code and changes may not take effect.
    /// </summary>
    public bool AffectedInCode { get; set; }

    internal string MaterialFile
    {
      get
      {
        return this.string_0;
      }
      set
      {
        this.string_0 = value;
      }
    }

    string Interface1.MaterialFile
    {
      get
      {
        return this.string_0;
      }
    }

    string Interface0.ProjectFile
    {
      get
      {
        return this.ProjectFile;
      }
    }

    internal string MaterialName { get; set; }

    internal string ProjectFile { get; set; }

    internal string NormalMapFile { get; set; }

    internal string DiffuseMapFile { get; set; }

    internal string DiffuseAmbientMapFile { get; set; }

    internal string EmissiveMapFile { get; set; }

    internal string SpecularColorMapFile { get; set; }

    internal string ParallaxMapFile { get; set; }

    /// <summary>
    /// Texture that represents a lighting model falloff-map used to apply lighting to materials.
    /// </summary>
    public Texture3D LightingTexture
    {
      get
      {
        return this.texture3D_0;
      }
      set
      {
        if (this.texture3D_0 == value || this.effectParameter_12 == null)
          return;
        this.texture3D_0 = value;
        this.effectParameter_12.SetValue((Texture) value);
      }
    }

    /// <summary>
    /// Texture that represents the fresnel micro-facet distribution method.
    /// </summary>
    public Texture2D MicrofacetTexture
    {
      get
      {
        return this.texture2D_0;
      }
      set
      {
        if (this.texture2D_0 == value || this.effectParameter_13 == null)
          return;
        this.texture2D_0 = value;
        this.effectParameter_13.SetValue((Texture) value);
      }
    }

    /// <summary>
    /// Texture normal-map used to apply bump mapping to materials. Setting the
    /// texture to null disables this feature.
    /// </summary>
    [Attribute2("NormalMapFile")]
    [Attribute1(true, Description = "Normal Map", HorizontalAlignment = false, MajorGrouping = 1, MinorGrouping = 3, ToolTipText = "")]
    public Texture2D NormalMapTexture
    {
      get
      {
        return this._NormalMapTexture;
      }
      set
      {
        this.SyncDiffuseAndNormalData(this._DiffuseColorOriginal, this._DiffuseMapTexture, value);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Texture used as the primary color map for materials. Generally this texture
    /// includes shading and lighting information when bump mapping is not used. Setting
    /// the texture to null disables this feature.
    /// </summary>
    [Attribute1(true, Description = "Diffuse Map", HorizontalAlignment = false, MajorGrouping = 1, MinorGrouping = 1, ToolTipText = "")]
    [Attribute2("DiffuseMapFile")]
    public Texture2D DiffuseMapTexture
    {
      get
      {
        return this._DiffuseMapTexture;
      }
      set
      {
        this.SyncDiffuseAndNormalData(this._DiffuseColorOriginal, value, this._NormalMapTexture);
      }
    }

    /// <summary>
    /// Diffuse texture used during ambient lighting. Specifies a diffuse map with baked-in
    /// shading for use specifically in ambient lighting, allowing the base DiffuseMapTexture
    /// to remain optimal for bump mapping during the lighting passes. Setting the texture to
    /// null disables this feature.
    /// </summary>
    [Attribute2("DiffuseAmbientMapFile")]
    [Attribute1(true, Description = "Ambient Map", HorizontalAlignment = false, MajorGrouping = 1, MinorGrouping = 2, ToolTipText = "")]
    public Texture2D DiffuseAmbientMapTexture
    {
      get
      {
        return this.texture2D_1;
      }
      set
      {
        EffectHelper.smethod_8(value, ref this.texture2D_1, ref this.effectParameter_14);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Texture used to apply emissive lighting and self-illumination to materials. Setting the
    /// texture to null disables this feature.
    /// </summary>
    [Attribute1(true, Description = "Emissive Map", HorizontalAlignment = false, MajorGrouping = 1, MinorGrouping = 4, ToolTipText = "")]
    [Attribute2("EmissiveMapFile")]
    public Texture2D EmissiveMapTexture
    {
      get
      {
        return this._EmissiveMapTexture;
      }
      set
      {
        EffectHelper.smethod_8(value, ref this._EmissiveMapTexture, ref this.effectParameter_15);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Texture used to apply tint to the specular reflection. Commonly used for
    /// materials with complex reflection properties like metal, oil, and skin.
    /// Setting the texture to null disables this feature.
    /// </summary>
    [Attribute1(true, Description = "Color Spec", HorizontalAlignment = false, MajorGrouping = 3, MinorGrouping = 1, ToolTipText = "")]
    [Attribute2("SpecularColorMapFile")]
    public Texture2D SpecularColorMapTexture
    {
      get
      {
        return this._SpecularColorMapTexture;
      }
      set
      {
        EffectHelper.smethod_8(value ?? this.texture2D_2, ref this._SpecularColorMapTexture, ref this.effectParameter_16);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Gray scale height-map texture used to apply visual depth to materials
    /// without adding geometry. Setting the texture to null disables this feature.
    /// </summary>
    [Attribute1(true, Description = "Parallax", HorizontalAlignment = false, MajorGrouping = 5, MinorGrouping = 1, ToolTipText = "")]
    [Attribute2("ParallaxMapFile")]
    public Texture2D ParallaxMapTexture
    {
      get
      {
        return this._ParallaxMapTexture;
      }
      set
      {
        EffectHelper.smethod_8(value, ref this._ParallaxMapTexture, ref this.effectParameter_17);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Base color applied to materials when no DiffuseMapTexture is specified.
    /// </summary>
    [Attribute1(true, ControlType = ControlType.ColorSelection, Description = "Diffuse Color", HorizontalAlignment = false, MajorGrouping = 2, MinorGrouping = 1, ToolTipText = "")]
    public Vector3 DiffuseColor
    {
      get
      {
        return new Vector3(this._DiffuseColorOriginal.X, this._DiffuseColorOriginal.Y, this._DiffuseColorOriginal.Z);
      }
      set
      {
        this.SyncDiffuseAndNormalData(new Vector4(value, 1f), this._DiffuseMapTexture, this._NormalMapTexture);
      }
    }

    /// <summary>
    /// Color used to apply emissive lighting and self-illumination to materials.
    /// </summary>
    [Attribute1(true, ControlType = ControlType.ColorSelection, Description = "Emissive Color", HorizontalAlignment = false, MajorGrouping = 2, MinorGrouping = 2, ToolTipText = "")]
    public Vector3 EmissiveColor
    {
      get
      {
        return new Vector3(this._EmissiveColor.X, this._EmissiveColor.Y, this._EmissiveColor.Z);
      }
      set
      {
        EffectHelper.smethod_3(new Vector4(value.X, value.Y, value.Z, 1f), ref this._EmissiveColor, ref this.effectParameter_18);
      }
    }

    /// <summary>
    /// Power applied to material specular reflections. Affects how shiny a material appears.
    /// </summary>
    [Attribute5(2, 0.0, 256.0, 0.5)]
    [Attribute1(true, Description = "Specular Power", HorizontalAlignment = false, MajorGrouping = 3, MinorGrouping = 2, ToolTipText = "")]
    public float SpecularPower
    {
      get
      {
        return this.float_2;
      }
      set
      {
        this.method_2(value, this.float_3);
        this.SyncDiffuseAndNormalData(this._DiffuseColorOriginal, this._DiffuseMapTexture, this._NormalMapTexture);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Intensity applied to material specular reflections. Affects how intense the specular appears.
    /// </summary>
    [Attribute1(true, Description = "Specular Amount", HorizontalAlignment = false, MajorGrouping = 3, MinorGrouping = 3, ToolTipText = "")]
    [Attribute5(2, 0.0, 32.0, 0.5)]
    public float SpecularAmount
    {
      get
      {
        return this.float_3;
      }
      set
      {
        this.method_2(this.float_2, value);
        this.SyncDiffuseAndNormalData(this._DiffuseColorOriginal, this._DiffuseMapTexture, this._NormalMapTexture);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Fine tunes fresnel sub-surface scattering on reflection angles facing away from the camera.
    /// </summary>
    [Attribute5(3, 0.0, 4.0, 0.01)]
    [Attribute1(true, Description = "Fresnel Bias", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 1, ToolTipText = "")]
    public float FresnelReflectBias
    {
      get
      {
        return this.float_4;
      }
      set
      {
        this.method_3(value, this.float_5, this.float_6);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Fine tunes fresnel sub-surface scattering on reflection angles facing towards the camera.
    /// </summary>
    [Attribute1(true, Description = "Fresnel Offset", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 2, ToolTipText = "")]
    [Attribute5(3, 0.0, 4.0, 0.01)]
    public float FresnelReflectOffset
    {
      get
      {
        return this.float_5;
      }
      set
      {
        this.method_3(this.float_4, value, this.float_6);
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Determines the material roughness used in fresnel sub-surface scattering.
    /// </summary>
    [Attribute1(true, Description = "Distribution", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 3, ToolTipText = "")]
    [Attribute5(3, 0.0, 1.0, 0.01)]
    public float FresnelMicrofacetDistribution
    {
      get
      {
        return this.float_6;
      }
      set
      {
        this.method_3(this.float_4, this.float_5, value);
        this.SetTechnique();
      }
    }

    /// <summary>Determines the depth applied to parallax mapping.</summary>
    [Attribute5(3, 0.0, 100.0, 0.005)]
    [Attribute1(true, Description = "Parallax Scale", HorizontalAlignment = false, MajorGrouping = 5, MinorGrouping = 2, ToolTipText = "")]
    public float ParallaxScale
    {
      get
      {
        return this.vector4_0.X;
      }
      set
      {
        EffectHelper.smethod_3(new Vector4(value, this.vector4_0.Y, this.vector4_0.Z, this.vector4_0.W), ref this.vector4_0, ref this.effectParameter_21);
      }
    }

    /// <summary>
    /// Fine tunes the parallax map offset to avoid watery artifacts.
    /// </summary>
    [Attribute1(true, Description = "Parallax Offset", HorizontalAlignment = false, MajorGrouping = 5, MinorGrouping = 3, ToolTipText = "")]
    [Attribute5(3, -100.0, 100.0, 0.005)]
    public float ParallaxOffset
    {
      get
      {
        return this.vector4_0.Y;
      }
      set
      {
        EffectHelper.smethod_3(new Vector4(this.vector4_0.X, value, this.vector4_0.Z, this.vector4_0.W), ref this.vector4_0, ref this.effectParameter_21);
      }
    }

    /// <summary>
    /// Determines the effect's texture address mode in the U texture-space direction.
    /// </summary>
    [Attribute1(true, Description = "Addressing U", HorizontalAlignment = true, MajorGrouping = 6, MinorGrouping = 1, ToolTipText = "")]
    public TextureAddressMode AddressModeU { get; set; }

    /// <summary>
    /// Determines the effect's texture address mode in the V texture-space direction.
    /// </summary>
    [Attribute1(true, Description = "Addressing V", HorizontalAlignment = true, MajorGrouping = 6, MinorGrouping = 2, ToolTipText = "")]
    public TextureAddressMode AddressModeV { get; set; }

    /// <summary>
    /// Determines the effect's texture address mode in the W texture-space direction.
    /// </summary>
    [Attribute1(true, Description = "Addressing W", HorizontalAlignment = true, MajorGrouping = 6, MinorGrouping = 3, ToolTipText = "")]
    public TextureAddressMode AddressModeW { get; set; }

    /// <summary>
    /// Determines if the effect's shader changes sampler states while rendering.
    /// </summary>
    public bool AffectsSamplerStates
    {
      get
      {
        return false;
      }
    }

    /// <summary>Number of textures exposed by the effect.</summary>
    public int TextureCount
    {
      get
      {
        return 6;
      }
    }

    /// <summary>
    /// The transparency style used when rendering the effect.
    /// </summary>
    [Attribute6(true)]
    [Attribute1(true, ControlType = ControlType.CheckBox, Description = "Transparent", HorizontalAlignment = true, MajorGrouping = 7, MinorGrouping = 11, ToolTipText = "")]
    public virtual TransparencyMode TransparencyMode
    {
      get
      {
        return this.transparencyMode_0;
      }
      set
      {
        if (this.transparencyMode_0 == value)
          return;
        this.transparencyMode_0 = value;
        this.SyncTransparency(true);
      }
    }

    /// <summary>
    /// Used with TransparencyMode to determine the effect transparency.
    ///   -For Clipped mode this value is a comparison value, where all TransparencyMap
    ///    alpha values below this value are *not* rendered.
    /// </summary>
    [Attribute1(true, Description = "Amount", HorizontalAlignment = true, MajorGrouping = 7, MinorGrouping = 12, ToolTipText = "")]
    [Attribute5(3, 0.0, 1.0, 0.005)]
    public virtual float Transparency
    {
      get
      {
        return this.float_1;
      }
      set
      {
        this.float_1 = value;
        this.SyncTransparency(false);
      }
    }

    /// <summary>
    /// The texture map used for transparency (values are pulled from the alpha channel).
    /// </summary>
    public Texture TransparencyMap
    {
      get
      {
        return (Texture) this._DiffuseMapTexture;
      }
      set
      {
        this.DiffuseMapTexture = (Texture2D) value;
      }
    }

    /// <summary>Creates a new BaseMaterialEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effectname"></param>
    public BaseMaterialEffect(GraphicsDevice graphicsdevice, string effectname)
      : base(graphicsdevice, effectname)
    {
      this.method_4(graphicsdevice, true);
    }

    /// <summary>Creates a new BaseMaterialEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effectname"></param>
    /// <param name="trackeffect"></param>
    internal BaseMaterialEffect(GraphicsDevice graphicsDevice_0, string string_9, bool bool_5)
      : base(graphicsDevice_0, string_9)
    {
      this.method_4(graphicsDevice_0, bool_5);
    }

    /// <summary>Returns the texture at a specific index.</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Texture GetTexture(int index)
    {
      if (index == 0)
        return (Texture) this._DiffuseMapTexture;
      if (index == 1)
        return (Texture) this._NormalMapTexture;
      if (index == 2)
        return (Texture) this.texture2D_1;
      if (index == 3)
        return (Texture) this._SpecularColorMapTexture;
      if (index == 4)
        return (Texture) this._ParallaxMapTexture;
      if (index == 5)
        return (Texture) this._EmissiveMapTexture;
      return (Texture) null;
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
      bool changedmode = this.transparencyMode_0 != mode;
      this.transparencyMode_0 = mode;
      this.float_1 = transparency;
      this.DiffuseMapTexture = map as Texture2D;
      this.SyncTransparency(changedmode);
    }

    /// <summary>
    /// Applies the object's transparency information to its effect parameters.
    /// </summary>
    protected virtual void SyncTransparency(bool changedmode)
    {
    }

    /// <summary>
    /// Applies the provided diffuse information to the object and its effect parameters.
    /// </summary>
    /// <param name="diffusecolor"></param>
    /// <param name="diffusemap"></param>
    /// <param name="normalmap"></param>
    protected virtual void SyncDiffuseAndNormalData(Vector4 diffusecolor, Texture2D diffusemap, Texture2D normalmap)
    {
      this._DiffuseColorOriginal = diffusecolor;
      if (diffusemap != null && diffusemap != this._DefaultDiffuseMapTexture)
      {
        EffectHelper.smethod_8(diffusemap, ref this._DiffuseMapTexture, ref this._DiffuseMapTextureIndirectParam);
        EffectHelper.smethod_3(Vector4.One, ref this._DiffuseColorCached, ref this._DiffuseColorIndirectParam);
      }
      else
      {
        EffectHelper.smethod_8(this._DefaultDiffuseMapTexture, ref this._DiffuseMapTexture, ref this._DiffuseMapTextureIndirectParam);
        EffectHelper.smethod_3(diffusecolor, ref this._DiffuseColorCached, ref this._DiffuseColorIndirectParam);
      }
      if (normalmap != null && normalmap != this._DefaultNormalMapTexture)
        EffectHelper.smethod_8(normalmap, ref this._NormalMapTexture, ref this._NormalMapTextureIndirectParam);
      else if ((double) this.float_3 > 0.0 && (double) this.float_2 > 0.0)
        EffectHelper.smethod_8(this._DefaultNormalMapTexture, ref this._NormalMapTexture, ref this._NormalMapTextureIndirectParam);
      else
        EffectHelper.smethod_8((Texture2D) null, ref this._NormalMapTexture, ref this._NormalMapTextureIndirectParam);
    }

    private void method_2(float float_7, float float_8)
    {
      if ((double) this.float_2 == (double) float_7 && (double) this.float_3 == (double) float_8 || this.effectParameter_19 == null)
        return;
      this.float_2 = float_7;
      this.float_3 = float_8;
      if ((double) this.float_2 > 0.0 && (double) this.float_3 > 0.0)
        this.effectParameter_19.SetValue(new Vector4(this.float_2, this.float_3, 0.0f, 0.0f));
      else
        this.effectParameter_19.SetValue(new Vector4(10000f, 0.0f, 0.0f, 0.0f));
    }

    private void method_3(float float_7, float float_8, float float_9)
    {
      if ((double) this.float_4 == (double) float_7 && (double) this.float_5 == (double) float_8 && (double) this.float_6 == (double) float_9 || this.effectParameter_20 == null)
        return;
      this.float_4 = float_7;
      this.float_5 = float_8;
      this.float_6 = float_9;
      this.effectParameter_20.SetValue(new Vector4(this.float_4, this.float_5, this.float_6, 0.0f));
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected override void SetTechnique()
    {
      ++this.class46_0.lightingSystemStatistic_0.AccumulationValue;
      bool bool_0 = this.DoubleSided && this.bool_3;
      if (this._LightSources != null && this._LightSources.Count > 0)
      {
        bool flag1 = this._LightSources.Count == 1 && this._LightSources[0] is AmbientLight;
        bool flag2 = this._LightSources.Count == 1 && this._LightSources[0].FillLight;
        bool flag3 = this.EffectDetail <= DetailPreference.High && !flag2;
        bool flag4 = this.EffectDetail <= DetailPreference.Medium && this.transparencyMode_0 == TransparencyMode.None;
        bool flag5 = this.EffectDetail <= DetailPreference.Low && !flag2;
        if (flag1)
        {
          if (flag4 && this._ParallaxMapTexture != null && this._NormalMapTexture != null)
          {
            if (this._EmissiveMapTexture != null)
              this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseParallaxAmbientEmissive, 1, false, false, this.Skinned, false)];
            else
              this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseParallaxAmbient, 1, false, false, this.Skinned, false)];
          }
          else if (this.texture2D_1 != null && this._EmissiveMapTexture == null)
            this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseAmbientCustom, 1, false, false, this.Skinned, false)];
          else if (this._NormalMapTexture != null)
          {
            if (this._EmissiveMapTexture != null)
              this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseBumpAmbientEmissive, 1, false, false, this.Skinned, false)];
            else
              this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseBumpAmbient, 1, false, false, this.Skinned, false)];
          }
          else if (this._EmissiveMapTexture != null)
            this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseAmbientEmissive, 1, false, false, this.Skinned, false)];
          else
            this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseAmbient, 1, false, false, this.Skinned, false)];
        }
        else
        {
          int int_0 = Math.Min(Math.Max(this._LightSources.Count, 1), 3);
          Class48.Enum4 enum4_0 = Class48.Enum4.Diffuse;
          bool flag6 = this._SpecularColorMapTexture != null && this._SpecularColorMapTexture != this.texture2D_2;
          if (this._NormalMapTexture != null)
            enum4_0 = !flag4 || this._ParallaxMapTexture == null ? (!flag5 || (double) this.float_2 <= 0.0 || (double) this.float_3 <= 0.0 ? Class48.Enum4.DiffuseBump : (!flag3 || (double) this.float_4 <= 0.0 || (double) this.float_5 <= 0.0 ? (!flag6 ? Class48.Enum4.DiffuseBumpSpecular : Class48.Enum4.DiffuseBumpSpecularColor) : (!flag6 ? Class48.Enum4.DiffuseBumpFresnel : Class48.Enum4.DiffuseBumpFresnelColor))) : (!flag5 || (double) this.float_2 <= 0.0 || (double) this.float_3 <= 0.0 ? Class48.Enum4.DiffuseParallax : (!flag3 || (double) this.float_4 <= 0.0 || (double) this.float_5 <= 0.0 ? (!flag6 ? Class48.Enum4.DiffuseParallaxSpecular : Class48.Enum4.DiffuseParallaxSpecularColor) : (!flag6 ? Class48.Enum4.DiffuseParallaxFresnel : Class48.Enum4.DiffuseParallaxFresnelColor)));
          this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, enum4_0, int_0, bool_0, false, this.Skinned, false)];
        }
      }
      else
        this.CurrentTechnique = this.Techniques[Class48.smethod_2(Class48.Enum3.Lighting, Class48.Enum4.DiffuseAmbientEmissive, 1, false, false, this.Skinned, false)];
    }

    private void method_4(GraphicsDevice graphicsDevice_0, bool bool_5)
    {
      this.bool_3 = LightingSystemManager.Instance.GetGraphicsDeviceSupport(graphicsDevice_0).PixelShaderMajorVersion >= 3;
      this.effectParameter_19 = this.Parameters["_SpecularPower_And_Amount"];
      this.effectParameter_20 = this.Parameters["_FresnelReflectBias_Offset_Microfacet"];
      this.effectParameter_21 = this.Parameters["_ParallaxScale_And_Offset"];
      this._DiffuseColorIndirectParam = this.Parameters["_DiffuseColor"];
      this._DiffuseMapTextureIndirectParam = this.Parameters["_DiffuseMapTexture"];
      this._NormalMapTextureIndirectParam = this.Parameters["_NormalMapTexture"];
      this.effectParameter_18 = this.Parameters["_EmissiveColor"];
      this.effectParameter_12 = this.Parameters["_LightingTexture"];
      this.effectParameter_13 = this.Parameters["_MicrofacetTexture"];
      this.effectParameter_14 = this.Parameters["_DiffuseAmbientMapTexture"];
      this.effectParameter_15 = this.Parameters["_EmissiveMapTexture"];
      this.effectParameter_16 = this.Parameters["_SpecularColorMapTexture"];
      this.effectParameter_17 = this.Parameters["_ParallaxMapTexture"];
      this.LightingTexture = LightingSystemManager.Instance.method_4(graphicsDevice_0);
      this.MicrofacetTexture = LightingSystemManager.Instance.method_7(graphicsDevice_0);
      this._DefaultDiffuseMapTexture = LightingSystemManager.Instance.method_2("White");
      this._DefaultNormalMapTexture = LightingSystemManager.Instance.method_2("Normal");
      this.texture2D_2 = LightingSystemManager.Instance.method_2("White");
      this.DiffuseColor = Vector3.One;
      this.SpecularPower = 4f;
      this.SpecularAmount = 0.25f;
      this.FresnelReflectBias = 0.0f;
      this.FresnelReflectOffset = 1f;
      this.FresnelMicrofacetDistribution = 0.4f;
      this.SpecularColorMapTexture = this.texture2D_2;
      this.SetTechnique();
      this.MaterialName = string.Empty;
      this.ProjectFile = string.Empty;
      this.NormalMapFile = string.Empty;
      this.DiffuseMapFile = string.Empty;
      this.DiffuseAmbientMapFile = string.Empty;
      this.EmissiveMapFile = string.Empty;
      this.SpecularColorMapFile = string.Empty;
      this.ParallaxMapFile = string.Empty;
      if (!bool_5)
        return;
      LightingSystemEditor.OnCreateResource((IDisposable) this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the Effect and optionally releases the managed resources.
    /// </summary>
    /// <param name="releasemanaged"></param>
    protected override void Dispose(bool releasemanaged)
    {
      base.Dispose(releasemanaged);
      LightingSystemEditor.OnDisposeResource((IDisposable) this);
    }
  }
}
