// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Deferred.DeferredSasEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Effects.Deferred
{
  /// <summary>
  /// Provides custom deferred object rendering from user FX shaders, with full support for, and binding of, FX Standard
  /// Annotations and Semantics (SAS). Potentially supplying depth, diffuse, normal, specular,
  /// fresnel, and emissive information to the deferred buffers for later lighting, shadow, and
  /// material calculation.
  /// </summary>
  public class DeferredSasEffect : BaseSasEffect, IShadowGenerateEffect, IDeferredObjectEffect
  {
    private DeferredEffectOutput deferredEffectOutput_0 = DeferredEffectOutput.GBuffer;
    private bool bool_5;
    private float float_1;
    private float float_2;
    private Vector3 vector3_0;
    private float float_3;
    private Vector4 vector4_0;
    private Vector4 vector4_1;
    private Vector4 vector4_2;
    private Vector2 vector2_0;
    private Texture2D texture2D_0;
    private Texture2D texture2D_1;
    private EffectParameter effectParameter_1;
    private EffectParameter effectParameter_2;
    private EffectParameter effectParameter_3;
    private EffectParameter effectParameter_4;
    private EffectParameter effectParameter_5;
    private EffectParameter effectParameter_6;
    private EffectParameter effectParameter_7;
    private EffectParameter effectParameter_8;
    private EffectParameter effectParameter_9;

    /// <summary>Main property used to eliminate shadow artifacts.</summary>
    public float ShadowPrimaryBias
    {
      get => this.vector4_0.X;
        set => EffectHelper.smethod_3(new Vector4(value, this.vector4_0.Y, this.vector4_0.Z, this.vector4_0.W), ref this.vector4_0, ref this.effectParameter_4);
    }

    /// <summary>
    /// Additional fine-tuned property used to eliminate shadow artifacts.
    /// </summary>
    public float ShadowSecondaryBias
    {
      get => this.vector4_0.Y;
        set => EffectHelper.smethod_3(new Vector4(this.vector4_0.X, value, this.vector4_0.Z, this.vector4_0.W), ref this.vector4_0, ref this.effectParameter_4);
    }

    /// <summary>
    /// Bounding area of the shadow source, where the bounds center is the actual shadow source location,
    /// and the radius is either the source radius (for point sources) or the maximum view based casting
    /// distance (for directional sources).
    /// </summary>
    public BoundingSphere ShadowArea
    {
      set => EffectHelper.smethod_3(new Vector4(value.Center, value.Radius), ref this.vector4_2, ref this.effectParameter_6);
    }

    /// <summary>
    /// Determines if the effect is capable of generating shadow maps. Objects using effects unable to
    /// generate shadow maps automatically use the built-in shadow effect, however this puts
    /// heavy restrictions on how the effects handle rendering (only basic vertex transforms are supported).
    /// </summary>
    public bool SupportsShadowGeneration
    {
      get
      {
        if (!this.Properties.ContainsKey("ShadowGenerationTechnique"))
          return false;
        return this.Techniques[(string) this.Properties["ShadowGenerationTechnique"]] != null;
      }
    }

    /// <summary>
    /// Determines the type of shader output for the effects to generate.
    /// </summary>
    public DeferredEffectOutput DeferredEffectOutput
    {
      get => this.deferredEffectOutput_0;
        set
      {
        this.deferredEffectOutput_0 = value;
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Texture containing the screen-space lighting generated during deferred rendering (used during the Final rendering pass).
    /// </summary>
    public Texture2D SceneLightingDiffuseMap
    {
      get => this.texture2D_0;
        set
      {
        EffectHelper.smethod_8(value, ref this.texture2D_0, ref this.effectParameter_8);
        if (this.effectParameter_7 == null || this.texture2D_0 == null)
          return;
        EffectHelper.smethod_7(new Vector2(this.texture2D_0.Width, this.texture2D_0.Height), ref this.vector2_0, ref this.effectParameter_7);
      }
    }

    /// <summary>
    /// Texture containing the screen-space specular generated during deferred rendering (used during the Final rendering pass).
    /// </summary>
    public Texture2D SceneLightingSpecularMap
    {
      get => this.texture2D_1;
        set => EffectHelper.smethod_8(value, ref this.texture2D_1, ref this.effectParameter_9);
    }

    /// <summary>Enables scene fog.</summary>
    public bool FogEnabled
    {
      get => this.bool_5;
        set
      {
        this.bool_5 = value;
        this.method_10(float.MaxValue, float.MaxValue);
      }
    }

    /// <summary>
    /// Distance from the camera in world space that fog begins.
    /// </summary>
    public float FogStartDistance
    {
      get => this.float_1;
        set => this.method_10(value, this.float_2);
    }

    /// <summary>
    /// Distance from the camera in world space that fog ends.
    /// </summary>
    public float FogEndDistance
    {
      get => this.float_2;
        set => this.method_10(this.float_1, value);
    }

    /// <summary>Color applied to scene fog.</summary>
    public Vector3 FogColor
    {
      get => this.vector3_0;
        set => EffectHelper.smethod_4(value, ref this.vector3_0, ref this.effectParameter_2);
    }

    /// <summary>
    /// Creates a new DeferredSasEffect instance from an effect containing an SAS shader
    /// (often loaded through the content pipeline or from disk).
    /// </summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effect">Source effect containing an SAS shader.</param>
    public DeferredSasEffect(GraphicsDevice graphicsdevice, Effect effect)
      : base(graphicsdevice, effect)
    {
      this.method_9();
    }

    internal DeferredSasEffect(GraphicsDevice graphicsDevice_0, Effect effect_0, bool bool_6)
      : base(graphicsDevice_0, effect_0, bool_6)
    {
      this.method_9();
    }

    /// <summary>
    /// Sets scene ambient lighting (used during the Final rendering pass).
    /// </summary>
    public void SetAmbientLighting(IAmbientSource light, Vector3 directionhint)
    {
      if (!(light is ILight))
        return;
      Vector4 vector4_0 = new Vector4((light as ILight).CompositeColorAndIntensity, 1f);
      EffectHelper.smethod_10(this.SasAutoBindTable.method_1(SASAddress_AmbientLight_Color[0]), vector4_0);
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

    private void method_9()
    {
      this.effectParameter_3 = this.FindBySemantic("FARCLIPDIST");
      this.effectParameter_4 = this.FindBySemantic("SHADOWINFO");
      this.effectParameter_6 = this.FindBySemantic("SHADOWSOURCE");
      this.effectParameter_7 = this.FindBySemantic("TARGETINFO");
      this.effectParameter_5 = this.FindBySemantic("RENDERINFO");
      this.effectParameter_1 = this.FindBySemantic("FOGINFO");
      this.effectParameter_2 = this.FindBySemantic("FOGCOLOR");
      this.effectParameter_8 = this.FindBySemantic("SCENELIGHTINGDIFFUSEMAP");
      this.effectParameter_9 = this.FindBySemantic("SCENELIGHTINGSPECULARMAP");
      this.FogEnabled = false;
    }

    /// <summary>
    /// Creates a new empty effect of the same class type and using the same effect file as this object.
    /// </summary>
    /// <param name="device"></param>
    /// <returns></returns>
    protected override Effect Create(GraphicsDevice device)
    {
      return new DeferredSasEffect(device, this);
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected override void SetTechnique()
    {
      ++this.class47_0.lightingSystemStatistic_0.AccumulationValue;
      EffectTechnique effectTechnique = null;
      switch (this.deferredEffectOutput_0)
      {
        case DeferredEffectOutput.Depth:
          if (this.Properties.ContainsKey("DepthTechnique"))
          {
            effectTechnique = this.Techniques[(string) this.Properties["DepthTechnique"]];
          }
          break;
        case DeferredEffectOutput.GBuffer:
          if (this.Properties.ContainsKey("GBufferTechnique"))
          {
            effectTechnique = this.Techniques[(string) this.Properties["GBufferTechnique"]];
          }
          break;
        case DeferredEffectOutput.ShadowDepth:
          if (this.Properties.ContainsKey("ShadowGenerationTechnique"))
          {
            effectTechnique = this.Techniques[(string) this.Properties["ShadowGenerationTechnique"]];
          }
          break;
        case DeferredEffectOutput.Final:
          if (this.Properties.ContainsKey("FinalTechnique"))
          {
            effectTechnique = this.Techniques[(string) this.Properties["FinalTechnique"]];
          }
          break;
      }
      if (effectTechnique == null)
        return;
      this.CurrentTechnique = effectTechnique;
    }

    private void method_10(float float_4, float float_5)
    {
      if (this.effectParameter_1 == null || this.float_1 == (double) float_4 && this.float_2 == (double) float_5)
        return;
      this.float_1 = Math.Max(float_4, 0.0f);
      this.float_2 = Math.Max(this.float_1 * 1.01f, float_5);
      float y = this.float_2 - this.float_1;
      if (y != 0.0)
        y = 1f / y;
      this.effectParameter_1.SetValue(new Vector2(this.float_1, y));
    }

    /// <summary>
    /// Applies the current transform information to the bound effect parameters.
    /// </summary>
    protected override void SyncTransformEffectData()
    {
      base.SyncTransformEffectData();
      if (this.effectParameter_3 == null)
        return;
      Vector4 vector4 = Vector4.Transform(new Vector4(0.0f, 0.0f, 1f, 1f), this.ProjectionToView);
      float num = 0.0f;
      if (vector4.W != 0.0)
        num = Math.Abs(vector4.Z / vector4.W);
      if (this.float_3 == (double) num)
        return;
      this.float_3 = num;
      this.effectParameter_3.SetValue(num);
    }

    /// <summary>
    /// Applies the object's transparency information to its effect parameters.
    /// </summary>
    protected override void SyncTransparency()
    {
      Vector4 vector40 = this.vector4_0;
      Vector4 vector41 = this.vector4_1;
      if (this.TransparencyMode == TransparencyMode.Clip)
      {
        vector40.Z = this.Transparency;
        vector41.X = this.Transparency;
      }
      else
      {
        vector40.Z = 0.0f;
        vector41.X = 0.0f;
      }
      EffectHelper.smethod_3(vector40, ref this.vector4_0, ref this.effectParameter_4);
      EffectHelper.smethod_3(vector41, ref this.vector4_1, ref this.effectParameter_5);
    }
  }
}
