// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.BaseSasEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using EmbeddedResources;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Effect class with full support for, and binding of, FX Standard Annotations and Semantics (SAS).
  /// </summary>
  public abstract class BaseSasEffect : BaseSasBindEffect, IEditorObject, IProjectFile, IRenderableEffect, ISkinnedEffect, Interface1
  {
      private Matrix[] matrix_6 = new Matrix[1];
    private Matrix[] matrix_7 = new Matrix[1];
    private Matrix[] matrix_8 = new Matrix[1];
    private Matrix matrix_0;
    private Matrix matrix_1;
    private Matrix matrix_2;
    private Matrix matrix_3;
    private Matrix matrix_4;
      private bool bool_3;

      /// <summary>World matrix applied to geometry using this effect.</summary>
    public Matrix World
    {
      get => this.matrix_0;
          set
      {
        if (this.matrix_0 == value)
          return;
        this.SetWorldAndWorldToObject(value, Matrix.Invert(value));
      }
    }

    /// <summary>View matrix applied to geometry using this effect.</summary>
    public Matrix View
    {
      get => this.matrix_2;
        set
      {
        if (this.matrix_2 == value)
          return;
        this.matrix_2 = value;
        this.matrix_3 = Matrix.Invert(value);
        this.SyncTransformEffectData();
      }
    }

    /// <summary>
    /// Projection matrix applied to geometry using this effect.
    /// </summary>
    public Matrix Projection
    {
      get => this.matrix_4;
        set
      {
        if (this.matrix_4 == value)
          return;
        this.matrix_4 = value;
        this.ProjectionToView = Matrix.Invert(value);
        this.SyncTransformEffectData();
      }
    }

    /// <summary>
    /// Inverse projection matrix applied to geometry using this effect.
    /// </summary>
    protected Matrix ProjectionToView { get; private set; }

      /// <summary>
    /// Applies the user's effect preference. This generally trades detail
    /// for performance based on the user's selection.
    /// </summary>
    public DetailPreference EffectDetail
    {
      get
      {
        return DetailPreference.High;
      }
      set
      {
      }
    }

    /// <summary>
    /// Array of bone transforms for the skeleton's current pose. The matrix index is the
    /// same as the bone order used in the model or vertex buffer.
    /// </summary>
    public Matrix[] SkinBones
    {
      get => this.matrix_6;
        set
      {
        if (!this.bool_3 || this.SkinBonesEffectParameter == null)
          return;
        if (value != null)
        {
          this.matrix_6 = value;
          this.SyncSkinBoneEffectData();
        }
        else
        {
          if (this.matrix_7.Length < this.SkinBonesEffectParameter.Elements.Count)
          {
            this.matrix_7 = new Matrix[this.SkinBonesEffectParameter.Elements.Count];
            for (int index = 0; index < this.matrix_7.Length; ++index)
              this.matrix_7[index] = Matrix.Identity;
          }
          if (this.matrix_6 == this.matrix_7)
            return;
          this.matrix_6 = this.matrix_7;
          this.SyncSkinBoneEffectData();
        }
      }
    }

    /// <summary>
    /// Determines if the effect is currently rendering skinned objects.
    /// </summary>
    public bool Skinned
    {
      get => this.bool_3;
        set
      {
        this.bool_3 = value;
        this.SetTechnique();
      }
    }

    /// <summary>
    /// Notifies the editor that this object is partially controlled via code. The editor
    /// will display information to the user indicating some property values are
    /// overridden in code and changes may not take effect.
    /// </summary>
    public bool AffectedInCode { get; set; }

    internal string MaterialName { get; set; } = "";

      string Interface1.MaterialFile => this.MaterialFile;

      internal string MaterialFile { get; set; } = "";

      internal string ProjectFile { get; set; } = "";

      string IProjectFile.ProjectFile => this.ProjectFile;

      internal string EffectFile { get; set; } = "";

      /// <summary>
    /// Effect parameter used to set the bone transform array.
    /// </summary>
    protected EffectParameter SkinBonesEffectParameter { get; set; }

      /// <summary>
    /// Creates a new BaseSasEffect instance from an effect containing an SAS shader
    /// (often loaded through the content pipeline or from disk).
    /// </summary>
    /// <param name="graphicsdevice"></param>
    /// <param name="effect">Source effect containing an SAS shader.</param>
    public BaseSasEffect(GraphicsDevice graphicsdevice, Effect effect)
      : base(graphicsdevice, effect)
    {
      this.SkinBonesEffectParameter = this.FindBySasAddress("Sas.Skeleton.MeshToJointToWorld[*]");
      LightingSystemEditor.OnCreateResource(this);
    }

    internal BaseSasEffect(GraphicsDevice graphicsDevice_0, Effect effect_0, bool bool_5)
      : base(graphicsDevice_0, effect_0)
    {
      this.SkinBonesEffectParameter = this.FindBySasAddress("Sas.Skeleton.MeshToJointToWorld[*]");
      if (!bool_5)
        return;
      LightingSystemEditor.OnCreateResource(this);
    }

    /// <summary>
    /// Sets both the world and inverse world matrices.  Used to improve
    /// performance in effects that automatically generate an inverse
    /// world matrix when the world matrix is set, by providing a cached
    /// or precalculated inverse matrix with the world matrix.
    /// </summary>
    /// <param name="world">World matrix applied to geometry using this effect.</param>
    /// <param name="worldtoobj">Inverse world matrix applied to geometry using this effect.</param>
    public void SetWorldAndWorldToObject(Matrix world, Matrix worldtoobj)
    {
      if (this.matrix_0 == world)
        return;
      this.matrix_0 = world;
      this.matrix_1 = worldtoobj;
      this.SyncTransformEffectData();
      this.SyncSkinBoneEffectData();
    }

    /// <summary>
    /// Used internally by SunBurn - not recommended for external use.
    /// 
    /// Quickly sets the world and inverse world matrices during an effect
    /// Begin / End block.  Values applied using this method do not persist
    /// after the Begin / End block.
    /// 
    /// This method is highly context sensitive.  Built-in effects that derive from
    /// BaseRenderableEffect fully support this method, however other objects merely
    /// call the non-transposed overload.
    /// </summary>
    /// <param name="world">World matrix applied to geometry using this effect.</param>
    /// <param name="worldtranspose">Transposed world matrix applied to geometry using this effect.</param>
    /// <param name="worldtoobj">Inverse world matrix applied to geometry using this effect.</param>
    /// <param name="worldtoobjtranspose">Transposed inverse world matrix applied to geometry using this effect.</param>
    public void SetWorldAndWorldToObject(ref Matrix world, ref Matrix worldtranspose, ref Matrix worldtoobj, ref Matrix worldtoobjtranspose)
    {
      this.SetWorldAndWorldToObject(world, worldtoobj);
    }

    /// <summary>
    /// Sets both the view, projection, and their inverse matrices.  Used to improve
    /// performance in effects that automatically generate an inverse
    /// matrix when the view and project are set, by providing a cached
    /// or precalculated inverse matrix with the view and project matrices.
    /// </summary>
    /// <param name="view">View matrix applied to geometry using this effect.</param>
    /// <param name="viewtoworld">Inverse view matrix applied to geometry using this effect.</param>
    /// <param name="projection">Projection matrix applied to geometry using this effect.</param>
    /// <param name="projectiontoview">Inverse projection matrix applied to geometry using this effect.</param>
    public void SetViewAndProjection(Matrix view, Matrix viewtoworld, Matrix projection, Matrix projectiontoview)
    {
      bool flag = false;
      if (view != this.matrix_2 || viewtoworld != this.matrix_3)
      {
        this.matrix_2 = view;
        this.matrix_3 = viewtoworld;
        flag = true;
      }
      if (projection != this.matrix_4)
      {
        this.matrix_4 = projection;
        this.ProjectionToView = projectiontoview;
        flag = true;
      }
      if (!flag)
        return;
      this.SyncTransformEffectData();
    }

    /// <summary>
    /// Sets the effect technique based on its current property values.
    /// </summary>
    protected virtual void SetTechnique()
    {
    }

    /// <summary>
    /// Applies the current transform information to the bound effect parameters.
    /// </summary>
    protected virtual void SyncTransformEffectData()
    {
      EffectHelper.smethod_10(this.SasAutoBindTable.method_1("Sas.Camera.Position"), new Vector4(this.matrix_3.Translation, 1f));
      EffectHelper.smethod_11(this.SasAutoBindTable.method_1("Sas.Camera.World"), this.matrix_0);
      EffectHelper.smethod_11(this.SasAutoBindTable.method_1("Sas.Camera.WorldInverse"), this.matrix_1);
      EffectHelper.smethod_11(this.SasAutoBindTable.method_1("Sas.Camera.WorldToView"), this.matrix_2);
      EffectHelper.smethod_11(this.SasAutoBindTable.method_1("Sas.Camera.WorldToViewInverse"), this.matrix_3);
      EffectHelper.smethod_11(this.SasAutoBindTable.method_1("Sas.Camera.Projection"), this.matrix_4);
      EffectHelper.smethod_11(this.SasAutoBindTable.method_1("Sas.Camera.ProjectionInverse"), this.ProjectionToView);
      EffectHelper.smethod_12(this.SasAutoBindTable.method_1("Sas.Camera.WorldTranspose"), this.matrix_0);
      EffectHelper.smethod_12(this.SasAutoBindTable.method_1("Sas.Camera.WorldInverseTranspose"), this.matrix_1);
      EffectHelper.smethod_12(this.SasAutoBindTable.method_1("Sas.Camera.WorldToViewTranspose"), this.matrix_2);
      EffectHelper.smethod_12(this.SasAutoBindTable.method_1("Sas.Camera.WorldToViewInverseTranspose"), this.matrix_3);
      EffectHelper.smethod_12(this.SasAutoBindTable.method_1("Sas.Camera.ProjectionTranspose"), this.matrix_4);
      EffectHelper.smethod_12(this.SasAutoBindTable.method_1("Sas.Camera.ProjectionInverseTranspose"), this.ProjectionToView);
      List<EffectParameter> list_0_1 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToView");
      List<EffectParameter> list_0_2 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToViewTranspose");
      List<EffectParameter> list_0_3 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToProjection");
      List<EffectParameter> list_0_4 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToProjectionTranspose");
      if (list_0_1 != null || list_0_2 != null || (list_0_3 != null || list_0_4 != null))
      {
        Matrix matrix_0_1 = this.matrix_0 * this.matrix_2;
        Matrix matrix_0_2 = matrix_0_1 * this.matrix_4;
        EffectHelper.smethod_11(list_0_1, matrix_0_1);
        EffectHelper.smethod_12(list_0_2, matrix_0_1);
        EffectHelper.smethod_11(list_0_3, matrix_0_2);
        EffectHelper.smethod_12(list_0_4, matrix_0_2);
      }
      List<EffectParameter> list_0_5 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToViewInverse");
      List<EffectParameter> list_0_6 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToViewInverseTranspose");
      List<EffectParameter> list_0_7 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToProjectionInverse");
      List<EffectParameter> list_0_8 = this.SasAutoBindTable.method_1("Sas.Camera.ObjectToProjectionInverseTranspose");
      if (list_0_5 == null && list_0_6 == null && (list_0_7 == null && list_0_8 == null))
        return;
      Matrix matrix_0_3 = this.matrix_3 * this.matrix_1;
      Matrix matrix_0_4 = this.ProjectionToView * matrix_0_3;
      EffectHelper.smethod_11(list_0_5, matrix_0_3);
      EffectHelper.smethod_12(list_0_6, matrix_0_3);
      EffectHelper.smethod_11(list_0_7, matrix_0_4);
      EffectHelper.smethod_12(list_0_8, matrix_0_4);
    }

    /// <summary>
    /// Applies the current bone transform information to the bound effect parameters.
    /// </summary>
    protected virtual void SyncSkinBoneEffectData()
    {
      if (!this.bool_3 || this.SkinBonesEffectParameter == null)
        return;
      if (this.matrix_8.Length < this.matrix_6.Length)
        this.matrix_8 = new Matrix[this.matrix_6.Length];
      for (int index = 0; index < this.matrix_6.Length; ++index)
        this.matrix_8[index] = this.matrix_6[index] * this.matrix_0;
      this.SkinBonesEffectParameter.SetArrayRange(0, Math.Min(this.matrix_8.Length, this.SkinBonesEffectParameter.Elements.Count));
      this.SkinBonesEffectParameter.SetValue(this.matrix_8);
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
