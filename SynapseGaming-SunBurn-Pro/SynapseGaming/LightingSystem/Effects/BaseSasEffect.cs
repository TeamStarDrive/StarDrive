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
        Matrix[] skinBones = new Matrix[1];
        Matrix[] skinBonesCache = new Matrix[1];
        Matrix[] matrix_8 = new Matrix[1];
        Matrix world;
        Matrix matrix_1;
        Matrix view;
        Matrix inverseView;
        Matrix projection;
        bool IsSkinned;

        /// <summary>World matrix applied to geometry using this effect.</summary>
        public Matrix World
        {
            get => world;
            set
            {
                if (world != value) SetWorldAndWorldToObject(value, Matrix.Invert(value));
            }
        }

        /// <summary>View matrix applied to geometry using this effect.</summary>
        public Matrix View
        {
            get => view;
            set
            {
                if (view != value)
                {
                    view = value;
                    inverseView = Matrix.Invert(value);
                    SyncTransformEffectData();
                }
            }
        }

        /// <summary>
        /// Projection matrix applied to geometry using this effect.
        /// </summary>
        public Matrix Projection
        {
            get => projection;
            set
            {
                if (projection != value)
                {
                    projection = value;
                    ProjectionToView = Matrix.Invert(value);
                    SyncTransformEffectData();
                }
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
            get => DetailPreference.High;
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
            get => skinBones;
            set
            {
                if (!IsSkinned || SkinBonesEffectParameter == null)
                    return;
                if (value != null)
                {
                    skinBones = value;
                    SyncSkinBoneEffectData();
                }
                else
                {
                    if (skinBonesCache.Length < SkinBonesEffectParameter.Elements.Count)
                    {
                        skinBonesCache = new Matrix[SkinBonesEffectParameter.Elements.Count];
                        for (int index = 0; index < skinBonesCache.Length; ++index)
                            skinBonesCache[index] = Matrix.Identity;
                    }
                    if (skinBones == skinBonesCache)
                        return;
                    skinBones = skinBonesCache;
                    SyncSkinBoneEffectData();
                }
            }
        }

        /// <summary>
        /// Determines if the effect is currently rendering skinned objects.
        /// </summary>
        public bool Skinned
        {
            get => IsSkinned;
            set
            {
                IsSkinned = value;
                SetTechnique();
            }
        }

        /// <summary>
        /// Notifies the editor that this object is partially controlled via code. The editor
        /// will display information to the user indicating some property values are
        /// overridden in code and changes may not take effect.
        /// </summary>
        public bool AffectedInCode { get; set; }

        internal string MaterialName { get; set; } = "";

        string Interface1.MaterialFile => MaterialFile;

        internal string MaterialFile { get; set; } = "";

        internal string ProjectFile { get; set; } = "";

        string IProjectFile.ProjectFile => ProjectFile;

        internal string EffectFile { get; set; } = "";

        /// <summary>
        /// Effect parameter used to set the bone transform array.
        /// </summary>
        protected EffectParameter SkinBonesEffectParameter { get; set; }

        /// <summary>
        /// Creates a new BaseSasEffect instance from an effect containing an SAS shader
        /// (often loaded through the content pipeline or from disk).
        /// </summary>
        /// <param name="device"></param>
        /// <param name="effect">Source effect containing an SAS shader.</param>
        protected BaseSasEffect(GraphicsDevice device, Effect effect)
          : base(device, effect)
        {
            SkinBonesEffectParameter = FindBySasAddress("Sas.Skeleton.MeshToJointToWorld[*]");
            LightingSystemEditor.OnCreateResource(this);
        }

        internal BaseSasEffect(GraphicsDevice device, Effect effect_0, bool bool_5)
          : base(device, effect_0)
        {
            SkinBonesEffectParameter = FindBySasAddress("Sas.Skeleton.MeshToJointToWorld[*]");
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
        public void SetWorldAndWorldToObject(Matrix world, in Matrix worldtoobj)
        {
            if (this.world == world)
                return;
            this.world = world;
            matrix_1 = worldtoobj;
            SyncTransformEffectData();
            SyncSkinBoneEffectData();
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
            SetWorldAndWorldToObject(world, worldtoobj);
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
            if (view != this.view || viewtoworld != inverseView)
            {
                this.view = view;
                inverseView = viewtoworld;
                flag = true;
            }
            if (projection != this.projection)
            {
                this.projection = projection;
                ProjectionToView = projectiontoview;
                flag = true;
            }
            if (!flag)
                return;
            SyncTransformEffectData();
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
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.Position"), new Vector4(inverseView.Translation, 1f));
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.World"), world);
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.WorldInverse"), matrix_1);
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.WorldToView"), view);
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.WorldToViewInverse"), inverseView);
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.Projection"), projection);
            EffectHelper.Update(SasAutoBindTable.method_1("Sas.Camera.ProjectionInverse"), ProjectionToView);
            EffectHelper.UpdateTransposed(SasAutoBindTable.method_1("Sas.Camera.WorldTranspose"), world);
            EffectHelper.UpdateTransposed(SasAutoBindTable.method_1("Sas.Camera.WorldInverseTranspose"), matrix_1);
            EffectHelper.UpdateTransposed(SasAutoBindTable.method_1("Sas.Camera.WorldToViewTranspose"), view);
            EffectHelper.UpdateTransposed(SasAutoBindTable.method_1("Sas.Camera.WorldToViewInverseTranspose"), inverseView);
            EffectHelper.UpdateTransposed(SasAutoBindTable.method_1("Sas.Camera.ProjectionTranspose"), projection);
            EffectHelper.UpdateTransposed(SasAutoBindTable.method_1("Sas.Camera.ProjectionInverseTranspose"), ProjectionToView);
            List<EffectParameter> list_0_1 = SasAutoBindTable.method_1("Sas.Camera.ObjectToView");
            List<EffectParameter> list_0_2 = SasAutoBindTable.method_1("Sas.Camera.ObjectToViewTranspose");
            List<EffectParameter> list_0_3 = SasAutoBindTable.method_1("Sas.Camera.ObjectToProjection");
            List<EffectParameter> list_0_4 = SasAutoBindTable.method_1("Sas.Camera.ObjectToProjectionTranspose");
            if (list_0_1 != null || list_0_2 != null || (list_0_3 != null || list_0_4 != null))
            {
                Matrix matrix_0_1 = world * view;
                Matrix matrix_0_2 = matrix_0_1 * projection;
                EffectHelper.Update(list_0_1, matrix_0_1);
                EffectHelper.UpdateTransposed(list_0_2, matrix_0_1);
                EffectHelper.Update(list_0_3, matrix_0_2);
                EffectHelper.UpdateTransposed(list_0_4, matrix_0_2);
            }
            List<EffectParameter> list_0_5 = SasAutoBindTable.method_1("Sas.Camera.ObjectToViewInverse");
            List<EffectParameter> list_0_6 = SasAutoBindTable.method_1("Sas.Camera.ObjectToViewInverseTranspose");
            List<EffectParameter> list_0_7 = SasAutoBindTable.method_1("Sas.Camera.ObjectToProjectionInverse");
            List<EffectParameter> list_0_8 = SasAutoBindTable.method_1("Sas.Camera.ObjectToProjectionInverseTranspose");
            if (list_0_5 == null && list_0_6 == null && (list_0_7 == null && list_0_8 == null))
                return;
            Matrix matrix_0_3 = inverseView * matrix_1;
            Matrix matrix_0_4 = ProjectionToView * matrix_0_3;
            EffectHelper.Update(list_0_5, matrix_0_3);
            EffectHelper.UpdateTransposed(list_0_6, matrix_0_3);
            EffectHelper.Update(list_0_7, matrix_0_4);
            EffectHelper.UpdateTransposed(list_0_8, matrix_0_4);
        }

        /// <summary>
        /// Applies the current bone transform information to the bound effect parameters.
        /// </summary>
        protected virtual void SyncSkinBoneEffectData()
        {
            if (!IsSkinned || SkinBonesEffectParameter == null)
                return;
            if (matrix_8.Length < skinBones.Length)
                matrix_8 = new Matrix[skinBones.Length];
            for (int index = 0; index < skinBones.Length; ++index)
                matrix_8[index] = skinBones[index] * world;
            SkinBonesEffectParameter.SetArrayRange(0, Math.Min(matrix_8.Length, SkinBonesEffectParameter.Elements.Count));
            SkinBonesEffectParameter.SetValue(matrix_8);
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
