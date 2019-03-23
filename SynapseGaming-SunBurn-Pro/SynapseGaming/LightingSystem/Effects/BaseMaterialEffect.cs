// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.BaseMaterialEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns4;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects
{
    /// <summary>
    /// Base class that provides data for SunBurn materials (bump, specular, parallax, ...).  Used by the
    /// forward rendering LightingEffect and deferred rendering DeferredObjectEffect classes.
    /// </summary>
    [Attribute0(true)]
    public abstract class BaseMaterialEffect : BaseSkinnedEffect, IEditorObject, IProjectFile, ISamplerEffect, IAddressableEffect, ITextureAccessEffect, ITransparentEffect, Interface1
    {
        /// <summary />
        protected List<ILight> _LightSources = new List<ILight>();

        float float_1 = 0.5f;
        bool bool_3;
        TransparencyMode transparencyMode_0;
        Texture3D texture3D_0;
        /// <summary />
        protected Texture2D _NormalMapTexture;
        /// <summary />
        protected Texture2D _DiffuseMapTexture;

        Texture2D texture2D_0;
        Texture2D texture2D_1;
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

        Texture2D texture2D_2;
        EffectParameter effectParameter_12;
        EffectParameter effectParameter_13;
        EffectParameter effectParameter_14;
        EffectParameter effectParameter_15;
        EffectParameter effectParameter_16;
        EffectParameter effectParameter_17;
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

        EffectParameter effectParameter_18;
        float float_2;
        float float_3;
        EffectParameter effectParameter_19;
        float float_4;
        float float_5;
        float float_6;
        Vector4 vector4_0;
        EffectParameter effectParameter_20;
        EffectParameter effectParameter_21;

        /// <summary>
        /// Notifies the editor that this object is partially controlled via code. The editor
        /// will display information to the user indicating some property values are
        /// overridden in code and changes may not take effect.
        /// </summary>
        public bool AffectedInCode { get; set; }

        public string MaterialFile { get; set; } = "";

        string Interface1.MaterialFile => MaterialFile;

        string IProjectFile.ProjectFile => ProjectFile;

        public string MaterialName { get; set; }

        public string ProjectFile { get; set; }

        public string NormalMapFile { get; set; }

        public string DiffuseMapFile { get; set; }

        public string DiffuseAmbientMapFile { get; set; }

        public string EmissiveMapFile { get; set; }

        public string SpecularColorMapFile { get; set; }

        public string ParallaxMapFile { get; set; }

        /// <summary>
        /// Texture that represents a lighting model falloff-map used to apply lighting to materials.
        /// </summary>
        public Texture3D LightingTexture
        {
            get => texture3D_0;
            set
            {
                if (texture3D_0 == value || effectParameter_12 == null)
                    return;
                texture3D_0 = value;
                effectParameter_12.SetValue(value);
            }
        }

        /// <summary>
        /// Texture that represents the fresnel micro-facet distribution method.
        /// </summary>
        public Texture2D MicrofacetTexture
        {
            get => texture2D_0;
            set
            {
                if (texture2D_0 == value || effectParameter_13 == null)
                    return;
                texture2D_0 = value;
                effectParameter_13.SetValue(value);
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
            get => _NormalMapTexture;
            set
            {
                SyncDiffuseAndNormalData(_DiffuseColorOriginal, _DiffuseMapTexture, value);
                SetTechnique();
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
            get => _DiffuseMapTexture;
            set => SyncDiffuseAndNormalData(_DiffuseColorOriginal, value, _NormalMapTexture);
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
            get => texture2D_1;
            set
            {
                EffectHelper.Update(value, ref texture2D_1, effectParameter_14);
                SetTechnique();
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
            get => _EmissiveMapTexture;
            set
            {
                EffectHelper.Update(value, ref _EmissiveMapTexture, effectParameter_15);
                SetTechnique();
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
            get => _SpecularColorMapTexture;
            set
            {
                EffectHelper.Update(value ?? texture2D_2, ref _SpecularColorMapTexture, effectParameter_16);
                SetTechnique();
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
            get => _ParallaxMapTexture;
            set
            {
                EffectHelper.Update(value, ref _ParallaxMapTexture, effectParameter_17);
                SetTechnique();
            }
        }

        /// <summary>
        /// Base color applied to materials when no DiffuseMapTexture is specified.
        /// </summary>
        [Attribute1(true, ControlType = ControlType.ColorSelection, Description = "Diffuse Color", HorizontalAlignment = false, MajorGrouping = 2, MinorGrouping = 1, ToolTipText = "")]
        public Vector3 DiffuseColor
        {
            get => new Vector3(_DiffuseColorOriginal.X, _DiffuseColorOriginal.Y, _DiffuseColorOriginal.Z);
            set => SyncDiffuseAndNormalData(new Vector4(value, 1f), _DiffuseMapTexture, _NormalMapTexture);
        }

        /// <summary>
        /// Color used to apply emissive lighting and self-illumination to materials.
        /// </summary>
        [Attribute1(true, ControlType = ControlType.ColorSelection, Description = "Emissive Color", HorizontalAlignment = false, MajorGrouping = 2, MinorGrouping = 2, ToolTipText = "")]
        public Vector3 EmissiveColor
        {
            get => new Vector3(_EmissiveColor.X, _EmissiveColor.Y, _EmissiveColor.Z);
            set => EffectHelper.Update(new Vector4(value.X, value.Y, value.Z, 1f), ref _EmissiveColor, ref effectParameter_18);
        }

        /// <summary>
        /// Power applied to material specular reflections. Affects how shiny a material appears.
        /// </summary>
        [Attribute5(2, 0.0, 256.0, 0.5)]
        [Attribute1(true, Description = "Specular Power", HorizontalAlignment = false, MajorGrouping = 3, MinorGrouping = 2, ToolTipText = "")]
        public float SpecularPower
        {
            get => float_2;
            set
            {
                method_2(value, float_3);
                SyncDiffuseAndNormalData(_DiffuseColorOriginal, _DiffuseMapTexture, _NormalMapTexture);
                SetTechnique();
            }
        }

        /// <summary>
        /// Intensity applied to material specular reflections. Affects how intense the specular appears.
        /// </summary>
        [Attribute1(true, Description = "Specular Amount", HorizontalAlignment = false, MajorGrouping = 3, MinorGrouping = 3, ToolTipText = "")]
        [Attribute5(2, 0.0, 32.0, 0.5)]
        public float SpecularAmount
        {
            get => float_3;
            set
            {
                method_2(float_2, value);
                SyncDiffuseAndNormalData(_DiffuseColorOriginal, _DiffuseMapTexture, _NormalMapTexture);
                SetTechnique();
            }
        }

        /// <summary>
        /// Fine tunes fresnel sub-surface scattering on reflection angles facing away from the camera.
        /// </summary>
        [Attribute5(3, 0.0, 4.0, 0.01)]
        [Attribute1(true, Description = "Fresnel Bias", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 1, ToolTipText = "")]
        public float FresnelReflectBias
        {
            get => float_4;
            set
            {
                method_3(value, float_5, float_6);
                SetTechnique();
            }
        }

        /// <summary>
        /// Fine tunes fresnel sub-surface scattering on reflection angles facing towards the camera.
        /// </summary>
        [Attribute1(true, Description = "Fresnel Offset", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 2, ToolTipText = "")]
        [Attribute5(3, 0.0, 4.0, 0.01)]
        public float FresnelReflectOffset
        {
            get => float_5;
            set
            {
                method_3(float_4, value, float_6);
                SetTechnique();
            }
        }

        /// <summary>
        /// Determines the material roughness used in fresnel sub-surface scattering.
        /// </summary>
        [Attribute1(true, Description = "Distribution", HorizontalAlignment = true, MajorGrouping = 4, MinorGrouping = 3, ToolTipText = "")]
        [Attribute5(3, 0.0, 1.0, 0.01)]
        public float FresnelMicrofacetDistribution
        {
            get => float_6;
            set
            {
                method_3(float_4, float_5, value);
                SetTechnique();
            }
        }

        /// <summary>Determines the depth applied to parallax mapping.</summary>
        [Attribute5(3, 0.0, 100.0, 0.005)]
        [Attribute1(true, Description = "Parallax Scale", HorizontalAlignment = false, MajorGrouping = 5, MinorGrouping = 2, ToolTipText = "")]
        public float ParallaxScale
        {
            get => vector4_0.X;
            set => EffectHelper.Update(new Vector4(value, vector4_0.Y, vector4_0.Z, vector4_0.W), ref vector4_0, ref effectParameter_21);
        }

        /// <summary>
        /// Fine tunes the parallax map offset to avoid watery artifacts.
        /// </summary>
        [Attribute1(true, Description = "Parallax Offset", HorizontalAlignment = false, MajorGrouping = 5, MinorGrouping = 3, ToolTipText = "")]
        [Attribute5(3, -100.0, 100.0, 0.005)]
        public float ParallaxOffset
        {
            get => vector4_0.Y;
            set => EffectHelper.Update(new Vector4(vector4_0.X, value, vector4_0.Z, vector4_0.W), ref vector4_0, ref effectParameter_21);
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
        public bool AffectsSamplerStates => false;

        /// <summary>Number of textures exposed by the effect.</summary>
        public int TextureCount => 6;

        /// <summary>
        /// The transparency style used when rendering the effect.
        /// </summary>
        [Attribute6(true)]
        [Attribute1(true, ControlType = ControlType.CheckBox, Description = "Transparent", HorizontalAlignment = true, MajorGrouping = 7, MinorGrouping = 11, ToolTipText = "")]
        public virtual TransparencyMode TransparencyMode
        {
            get => transparencyMode_0;
            set
            {
                if (transparencyMode_0 == value)
                    return;
                transparencyMode_0 = value;
                SyncTransparency(true);
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
            get => float_1;
            set
            {
                float_1 = value;
                SyncTransparency(false);
            }
        }

        /// <summary>
        /// The texture map used for transparency (values are pulled from the alpha channel).
        /// </summary>
        public Texture TransparencyMap
        {
            get => _DiffuseMapTexture;
            set => DiffuseMapTexture = (Texture2D) value;
        }

        /// <summary>Creates a new BaseMaterialEffect instance.</summary>
        /// <param name="device"></param>
        /// <param name="effectName"></param>
        protected BaseMaterialEffect(GraphicsDevice device, string effectName) : base(device, effectName)
        {
            InitializeParameters(device, true);
        }

        /// <summary>Creates a new BaseMaterialEffect instance.</summary>
        /// <param name="device"></param>
        /// <param name="effectName"></param>
        /// <param name="trackEffect"></param>
        internal BaseMaterialEffect(GraphicsDevice device, string effectName, bool trackEffect) : base(device, effectName)
        {
            InitializeParameters(device, trackEffect);
        }

        /// <summary>Returns the texture at a specific index.</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Texture GetTexture(int index)
        {
            switch (index)
            {
                case 0: return _DiffuseMapTexture;
                case 1: return _NormalMapTexture;
                case 2: return texture2D_1;
                case 3: return _SpecularColorMapTexture;
                case 4: return _ParallaxMapTexture;
                case 5: return _EmissiveMapTexture;
                default: return null;
            }
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
            bool changedmode = transparencyMode_0 != mode;
            transparencyMode_0 = mode;
            float_1 = transparency;
            DiffuseMapTexture = map as Texture2D;
            SyncTransparency(changedmode);
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
            _DiffuseColorOriginal = diffusecolor;
            if (diffusemap != null && diffusemap != _DefaultDiffuseMapTexture)
            {
                EffectHelper.Update(diffusemap, ref _DiffuseMapTexture, _DiffuseMapTextureIndirectParam);
                EffectHelper.Update(Vector4.One, ref _DiffuseColorCached, ref _DiffuseColorIndirectParam);
            }
            else
            {
                EffectHelper.Update(_DefaultDiffuseMapTexture, ref _DiffuseMapTexture, _DiffuseMapTextureIndirectParam);
                EffectHelper.Update(diffusecolor, ref _DiffuseColorCached, ref _DiffuseColorIndirectParam);
            }
            if (normalmap != null && normalmap != _DefaultNormalMapTexture)
                EffectHelper.Update(normalmap, ref _NormalMapTexture, _NormalMapTextureIndirectParam);
            else if (float_3 > 0.0 && float_2 > 0.0)
                EffectHelper.Update(_DefaultNormalMapTexture, ref _NormalMapTexture, _NormalMapTextureIndirectParam);
            else
                EffectHelper.Update(null, ref _NormalMapTexture, _NormalMapTextureIndirectParam);
        }

        void method_2(float float_7, float float_8)
        {
            if (float_2 == (double) float_7 && float_3 == (double) float_8 || effectParameter_19 == null)
                return;
            float_2 = float_7;
            float_3 = float_8;
            if (float_2 > 0.0 && float_3 > 0.0)
                effectParameter_19.SetValue(new Vector4(float_2, float_3, 0.0f, 0.0f));
            else
                effectParameter_19.SetValue(new Vector4(10000f, 0.0f, 0.0f, 0.0f));
        }

        void method_3(float float_7, float float_8, float float_9)
        {
            if (float_4 == (double) float_7 && float_5 == (double) float_8 && float_6 == (double) float_9 || effectParameter_20 == null)
                return;
            float_4 = float_7;
            float_5 = float_8;
            float_6 = float_9;
            effectParameter_20.SetValue(new Vector4(float_4, float_5, float_6, 0.0f));
        }

        /// <summary>
        /// Sets the effect technique based on its current property values.
        /// </summary>
        protected override void SetTechnique()
        {
            ++class46_0.lightingSystemStatistic_0.AccumulationValue;
            bool bool_0 = DoubleSided && bool_3;
            if (_LightSources != null && _LightSources.Count > 0)
            {
                bool flag1 = _LightSources.Count == 1 && _LightSources[0] is AmbientLight;
                bool flag2 = _LightSources.Count == 1 && _LightSources[0].FillLight;
                bool flag3 = EffectDetail <= DetailPreference.High && !flag2;
                bool flag4 = EffectDetail <= DetailPreference.Medium && transparencyMode_0 == TransparencyMode.None;
                bool flag5 = EffectDetail <= DetailPreference.Low && !flag2;
                if (flag1)
                {
                    if (flag4 && _ParallaxMapTexture != null && _NormalMapTexture != null)
                    {
                        if (_EmissiveMapTexture != null)
                            CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseParallaxAmbientEmissive, 1, false, false, Skinned, false)];
                        else
                            CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseParallaxAmbient, 1, false, false, Skinned, false)];
                    }
                    else if (texture2D_1 != null && _EmissiveMapTexture == null)
                        CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseAmbientCustom, 1, false, false, Skinned, false)];
                    else if (_NormalMapTexture != null)
                    {
                        if (_EmissiveMapTexture != null)
                            CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseBumpAmbientEmissive, 1, false, false, Skinned, false)];
                        else
                            CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseBumpAmbient, 1, false, false, Skinned, false)];
                    }
                    else if (_EmissiveMapTexture != null)
                        CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseAmbientEmissive, 1, false, false, Skinned, false)];
                    else
                        CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseAmbient, 1, false, false, Skinned, false)];
                }
                else
                {
                    int int_0 = Math.Min(Math.Max(_LightSources.Count, 1), 3);
                    TechniquNames.Enum4 enum4_0 = TechniquNames.Enum4.Diffuse;
                    bool flag6 = _SpecularColorMapTexture != null && _SpecularColorMapTexture != texture2D_2;
                    if (_NormalMapTexture != null)
                        enum4_0 = !flag4 || _ParallaxMapTexture == null ? (!flag5 || float_2 <= 0f || float_3 <= 0f ? TechniquNames.Enum4.DiffuseBump : (!flag3 || (double)float_4 <= 0.0 || (double)float_5 <= 0.0 ? (!flag6 ? TechniquNames.Enum4.DiffuseBumpSpecular : TechniquNames.Enum4.DiffuseBumpSpecularColor) : (!flag6 ? TechniquNames.Enum4.DiffuseBumpFresnel : TechniquNames.Enum4.DiffuseBumpFresnelColor))) : (!flag5 || (double)float_2 <= 0.0 || (double)float_3 <= 0.0 ? TechniquNames.Enum4.DiffuseParallax : (!flag3 || (double)float_4 <= 0.0 || (double)float_5 <= 0.0 ? (!flag6 ? TechniquNames.Enum4.DiffuseParallaxSpecular : TechniquNames.Enum4.DiffuseParallaxSpecularColor) : (!flag6 ? TechniquNames.Enum4.DiffuseParallaxFresnel : TechniquNames.Enum4.DiffuseParallaxFresnelColor)));
                    CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, enum4_0, int_0, bool_0, false, Skinned, false)];
                }
            }
            else
                CurrentTechnique = Techniques[TechniquNames.Get(TechniquNames.Enum3.Lighting, TechniquNames.Enum4.DiffuseAmbientEmissive, 1, false, false, Skinned, false)];
        }

        void InitializeParameters(GraphicsDevice graphicsDevice_0, bool trackeffect)
        {
            bool_3 = LightingSystemManager.Instance.GetGraphicsDeviceSupport(graphicsDevice_0).PixelShaderMajorVersion >= 3;
            effectParameter_19 = Parameters["_SpecularPower_And_Amount"];
            effectParameter_20 = Parameters["_FresnelReflectBias_Offset_Microfacet"];
            effectParameter_21 = Parameters["_ParallaxScale_And_Offset"];
            _DiffuseColorIndirectParam = Parameters["_DiffuseColor"];
            _DiffuseMapTextureIndirectParam = Parameters["_DiffuseMapTexture"];
            _NormalMapTextureIndirectParam = Parameters["_NormalMapTexture"];
            effectParameter_18 = Parameters["_EmissiveColor"];
            effectParameter_12 = Parameters["_LightingTexture"];
            effectParameter_13 = Parameters["_MicrofacetTexture"];
            effectParameter_14 = Parameters["_DiffuseAmbientMapTexture"];
            effectParameter_15 = Parameters["_EmissiveMapTexture"];
            effectParameter_16 = Parameters["_SpecularColorMapTexture"];
            effectParameter_17 = Parameters["_ParallaxMapTexture"];
            LightingTexture = LightingSystemManager.Instance.method_4(graphicsDevice_0);
            MicrofacetTexture = LightingSystemManager.Instance.method_7(graphicsDevice_0);
            _DefaultDiffuseMapTexture = LightingSystemManager.Instance.EmbeddedTexture("White");
            _DefaultNormalMapTexture = LightingSystemManager.Instance.EmbeddedTexture("Normal");
            texture2D_2 = LightingSystemManager.Instance.EmbeddedTexture("White");
            DiffuseColor = Vector3.One;
            SpecularPower = 4f;
            SpecularAmount = 0.25f;
            FresnelReflectBias = 0.0f;
            FresnelReflectOffset = 1f;
            FresnelMicrofacetDistribution = 0.4f;
            SpecularColorMapTexture = texture2D_2;
            SetTechnique();
            MaterialName = string.Empty;
            ProjectFile = string.Empty;
            NormalMapFile = string.Empty;
            DiffuseMapFile = string.Empty;
            DiffuseAmbientMapFile = string.Empty;
            EmissiveMapFile = string.Empty;
            SpecularColorMapFile = string.Empty;
            ParallaxMapFile = string.Empty;
            if (trackeffect)
                LightingSystemEditor.OnCreateResource(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Effect and optionally releases the managed resources.
        /// </summary>
        /// <param name="releasemanaged"></param>
        protected override void Dispose(bool releasemanaged)
        {
            base.Dispose(releasemanaged);
            LightingSystemEditor.OnDisposeResource(this);
        }
    }
}
