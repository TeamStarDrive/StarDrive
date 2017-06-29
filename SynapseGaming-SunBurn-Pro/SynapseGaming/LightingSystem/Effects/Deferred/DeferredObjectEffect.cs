// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Deferred.DeferredObjectEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects.Deferred
{
  /// <summary>
  /// Provides SunBurn's built-in deferred object rendering. Supplies depth, diffuse,
  /// normal, specular, fresnel, and emissive information to the deferred buffers for
  /// later lighting, shadow, and material calculation.
  /// </summary>
  public class DeferredObjectEffect : BaseMaterialEffect, IShadowGenerateEffect, IDeferredObjectEffect
  {
    private Vector3[] vector3_2 = new Vector3[3];
    private Vector4 vector4_1;
    private Vector4 vector4_2;
    private Vector4 vector4_3;
    private DeferredEffectOutput deferredEffectOutput_0;
    private Texture2D texture2D_3;
    private Texture2D texture2D_4;
    private Vector2 vector2_0;
    private bool bool_5;
    private float float_7;
    private float float_8;
    private Vector3 vector3_0;
    private Vector3 vector3_1;
    private EffectParameter effectParameter_22;
    private EffectParameter effectParameter_23;
    private EffectParameter effectParameter_24;
    private EffectParameter effectParameter_25;
    private EffectParameter effectParameter_26;
    private EffectParameter effectParameter_27;
    private EffectParameter effectParameter_28;
    private EffectParameter effectParameter_29;
    private EffectParameter effectParameter_30;
    private EffectParameter effectParameter_31;

    /// <summary>Main property used to eliminate shadow artifacts.</summary>
    public float ShadowPrimaryBias
    {
      get => this.vector4_2.X;
        set => EffectHelper.smethod_3(new Vector4(value, this.vector4_2.Y, this.vector4_2.Z, this.vector4_2.W), ref this.vector4_2, ref this.effectParameter_26);
    }

    /// <summary>
    /// Additional fine-tuned property used to eliminate shadow artifacts.
    /// </summary>
    public float ShadowSecondaryBias
    {
      get => this.vector4_2.Y;
        set => EffectHelper.smethod_3(new Vector4(this.vector4_2.X, value, this.vector4_2.Z, this.vector4_2.W), ref this.vector4_2, ref this.effectParameter_26);
    }

    /// <summary>
    /// Bounding area of the shadow source, where the bounds center is the actual shadow source location,
    /// and the radius is either the source radius (for point sources) or the maximum view based casting
    /// distance (for directional sources).
    /// </summary>
    public BoundingSphere ShadowArea
    {
      set => EffectHelper.smethod_3(new Vector4(value.Center, value.Radius), ref this.vector4_3, ref this.effectParameter_27);
    }

    /// <summary>
    /// Determines the type of shader output for the effects to generate.
    /// </summary>
    public DeferredEffectOutput DeferredEffectOutput
    {
      get => this.deferredEffectOutput_0;
        set
      {
        if (value == this.deferredEffectOutput_0)
          return;
        this.deferredEffectOutput_0 = value;
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Texture containing the screen-space lighting generated during deferred rendering (used during the Final rendering pass).
    /// </summary>
    public Texture2D SceneLightingDiffuseMap
    {
      get => this.texture2D_3;
        set
      {
        EffectHelper.SetParam(value, ref this.texture2D_3, this.effectParameter_28);
        if (this.effectParameter_24 == null || this.texture2D_3 == null)
          return;
        EffectHelper.smethod_7(new Vector2(this.texture2D_3.Width, this.texture2D_3.Height), ref this.vector2_0, ref this.effectParameter_24);
      }
    }

    /// <summary>
    /// Texture containing the screen-space specular generated during deferred rendering (used during the Final rendering pass).
    /// </summary>
    public Texture2D SceneLightingSpecularMap
    {
      get => this.texture2D_4;
        set => EffectHelper.SetParam(value, ref this.texture2D_4, this.effectParameter_29);
    }

    /// <summary>
    /// Determines if the effect is capable of generating shadow maps. Objects using effects unable to
    /// generate shadow maps automatically use the built-in shadow effect, however this puts
    /// heavy restrictions on how the effects handle rendering (only basic vertex transforms are supported).
    /// </summary>
    public bool SupportsShadowGeneration => false;

      /// <summary>Enables scene fog.</summary>
    public bool FogEnabled
    {
      get => this.bool_5;
          set
      {
        this.bool_5 = value;
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Distance from the camera in world space that fog begins.
    /// </summary>
    public float FogStartDistance
    {
      get => this.float_7;
        set => this.method_6(value, this.float_8);
    }

    /// <summary>
    /// Distance from the camera in world space that fog ends.
    /// </summary>
    public float FogEndDistance
    {
      get => this.float_8;
        set => this.method_6(this.float_7, value);
    }

    /// <summary>Color applied to scene fog.</summary>
    public Vector3 FogColor
    {
      get => this.vector3_0;
        set => EffectHelper.smethod_4(value, ref this.vector3_0, ref this.effectParameter_31);
    }

    /// <summary>Creates a new DeferredObjectEffect instance.</summary>
    /// <param name="graphicsdevice"></param>
    public DeferredObjectEffect(GraphicsDevice graphicsdevice)
      : base(graphicsdevice, "DeferredObjectEffect")
    {
      this.method_5(graphicsdevice);
    }

    internal DeferredObjectEffect(GraphicsDevice device, bool bool_6)
      : base(device, "DeferredObjectEffect", bool_6)
    {
      this.method_5(device);
    }

    /// <summary>
    /// Sets scene ambient lighting (used during the Final rendering pass).
    /// </summary>
    public void SetAmbientLighting(IAmbientSource light, Vector3 directionhint)
    {
      if (this.effectParameter_22 == null || this.effectParameter_23 == null || !(light is ILight))
        return;
      Vector3 vector3_2;
      Vector3 vector3_3;
      CoreUtils.smethod_1((light as ILight).CompositeColorAndIntensity, light.Depth, 0.65f, out vector3_2, out vector3_3);
      this.vector3_2[0] = vector3_2;
      this.vector3_2[1] = vector3_3;
      this.vector3_2[2] = (vector3_2 + vector3_3) * 0.2f;
      this.effectParameter_22.SetValue(this.vector3_2);
      EffectHelper.smethod_4(directionhint, ref this.vector3_1, ref this.effectParameter_23);
    }

    /// <summary>
    /// Sets the camera view and inverse camera view matrices. These are the matrices used in the final
    /// on-screen render from the camera / player point of view.
    /// 
    /// These matrices will differ from the standard view and inverse view matrices when rendering from
    /// an alternate point of view (for instance during shadow map and cube map generation).
    /// </summary>
    /// <param name="view">Camera view matrix applied to geometry using this effect.</param>
    /// <param name="viewtoworld">Camera inverse view matrix applied to geometry using this effect.</param>
    public void SetCameraView(Matrix view, Matrix viewtoworld)
    {
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return new DeferredObjectEffect(device);
    }

    private void method_5(GraphicsDevice graphicsDevice_0)
    {
      this.effectParameter_25 = this.Parameters["_TransClipRef"];
      this.effectParameter_26 = this.Parameters["_OffsetBias_DepthBias_TransClipRef"];
      this.effectParameter_27 = this.Parameters["_Direction_Or_Position_And_Radius"];
      this.effectParameter_24 = this.Parameters["_TargetWidthHeight"];
      this.effectParameter_22 = this.Parameters["_AmbientColor"];
      this.effectParameter_23 = this.Parameters["_AmbientDirection"];
      this.effectParameter_28 = this.Parameters["_SceneLightingDiffuseMap"];
      this.effectParameter_29 = this.Parameters["_SceneLightingSpecularMap"];
      this.effectParameter_30 = this.Parameters["_FogStartDist_EndDistInv"];
      this.effectParameter_31 = this.Parameters["_FogColor"];
      this.ShadowPrimaryBias = 1f;
      this.ShadowSecondaryBias = 0.2f;
      this.SetTechnique();
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected override void SetTechnique()
    {
      ++this.class46_0.lightingSystemStatistic_0.AccumulationValue;
      bool bool_1 = this.TransparencyMap != null && this.TransparencyMode != TransparencyMode.None;
      if (this.deferredEffectOutput_0 == DeferredEffectOutput.ShadowDepth)
      {
        this.CurrentTechnique = this.Techniques[TechniquNames.Get(TechniquNames.Enum3.ShadowGen, TechniquNames.Enum4.Point, 0, false, bool_1, this.Skinned, false)];
      }
      else
      {
        int effectDetail1 = (int) this.EffectDetail;
        bool flag1 = this.EffectDetail <= DetailPreference.Medium && this.TransparencyMode == TransparencyMode.None;
        int effectDetail2 = (int) this.EffectDetail;
        bool flag2 = this._EmissiveMapTexture != null;
        if (this.deferredEffectOutput_0 == DeferredEffectOutput.GBuffer)
          this.CurrentTechnique = this.Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredGBuffer, !flag1 || this._ParallaxMapTexture == null ? TechniquNames.Enum4.DiffuseBump : TechniquNames.Enum4.DiffuseParallax, 0, this.DoubleSided, bool_1, this.Skinned, false)];
        else if (this.deferredEffectOutput_0 == DeferredEffectOutput.Depth)
        {
          TechniquNames.Enum4 enum4_0 = TechniquNames.Enum4.DiffuseBump;
          if (bool_1 && flag1 && this._ParallaxMapTexture != null)
            enum4_0 = TechniquNames.Enum4.DiffuseParallax;
          this.CurrentTechnique = this.Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredDepth, enum4_0, 0, false, bool_1, this.Skinned, false)];
        }
        else
        {
          if (this.deferredEffectOutput_0 != DeferredEffectOutput.Final)
            return;
          TechniquNames.Enum4 enum4_0 = !flag1 || this._ParallaxMapTexture == null ? (!flag2 ? TechniquNames.Enum4.DiffuseBump : TechniquNames.Enum4.DiffuseBumpEmissive) : (!flag2 ? TechniquNames.Enum4.DiffuseParallax : TechniquNames.Enum4.DiffuseParallaxEmissive);
          if (this.bool_5)
            this.CurrentTechnique = this.Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredFinalFog, enum4_0, 0, false, bool_1, this.Skinned, false)];
          else
            this.CurrentTechnique = this.Techniques[TechniquNames.Get(TechniquNames.Enum3.DeferredFinal, enum4_0, 0, false, bool_1, this.Skinned, false)];
        }
      }
    }

    private void method_6(float float_9, float float_10)
    {
      if (this.effectParameter_30 == null || this.float_7 == (double) float_9 && this.float_8 == (double) float_10)
        return;
      this.float_7 = Math.Max(float_9, 0.0f);
      this.float_8 = Math.Max(this.float_7 * 1.01f, float_10);
      float y = this.float_8 - this.float_7;
      if (y != 0.0)
        y = 1f / y;
      this.effectParameter_30.SetValue(new Vector2(this.float_7, y));
    }

    /// <summary>
    /// Applies the object's transparency information to its effect parameters.
    /// </summary>
    protected override void SyncTransparency(bool changedmode)
    {
      Vector4 vector42 = this.vector4_2;
      Vector4 vector41 = this.vector4_1;
      if (this.TransparencyMode == TransparencyMode.Clip)
      {
        vector42.Z = this.Transparency;
        vector41.X = this.Transparency;
      }
      else
      {
        vector42.Z = 0.0f;
        vector41.X = 0.0f;
      }
      EffectHelper.smethod_3(vector42, ref this.vector4_2, ref this.effectParameter_26);
      EffectHelper.smethod_3(vector41, ref this.vector4_1, ref this.effectParameter_25);
      if (!changedmode)
        return;
      this.SetTechnique();
    }

    /// <summary>
    /// Applies the provided diffuse information to the object and its effect parameters.
    /// </summary>
    /// <param name="diffusecolor"></param>
    /// <param name="diffusemap"></param>
    /// <param name="normalmap"></param>
    protected override void SyncDiffuseAndNormalData(Vector4 diffusecolor, Texture2D diffusemap, Texture2D normalmap)
    {
      this._DiffuseColorOriginal = diffusecolor;
      if (diffusemap != null && diffusemap != this._DefaultDiffuseMapTexture)
      {
        EffectHelper.SetParam(diffusemap, ref this._DiffuseMapTexture, this._DiffuseMapTextureIndirectParam);
        EffectHelper.smethod_3(Vector4.One, ref this._DiffuseColorCached, ref this._DiffuseColorIndirectParam);
      }
      else
      {
        EffectHelper.SetParam(this._DefaultDiffuseMapTexture, ref this._DiffuseMapTexture, this._DiffuseMapTextureIndirectParam);
        EffectHelper.smethod_3(diffusecolor, ref this._DiffuseColorCached, ref this._DiffuseColorIndirectParam);
      }
      if (normalmap != null && normalmap != this._DefaultNormalMapTexture)
        EffectHelper.SetParam(normalmap, ref this._NormalMapTexture, this._NormalMapTextureIndirectParam);
      else
        EffectHelper.SetParam(this._DefaultNormalMapTexture, ref this._NormalMapTexture, this._NormalMapTextureIndirectParam);
    }
  }
}
