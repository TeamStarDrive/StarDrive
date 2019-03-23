// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.BaseRenderableEffect
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using ns4;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Effects
{
    /// <summary>Provides basic rendering support.</summary>
    public abstract class BaseRenderableEffect : Effect, IRenderableEffect
    {
        internal Class46 class46_0 = new Class46();
        bool HasWorldMatrix;
        DetailPreference detailPreference_0;
        Matrix world;
        Matrix worldToObject;
        Matrix view;
        Matrix viewToWorld;
        Matrix matrix_4;
        Matrix matrix_6;
        Matrix matrix_7;
        Matrix matrix_8;
        float float_0;
        EffectParameter FxWorld;
        EffectParameter FxWorldToObject;
        EffectParameter FxView;
        EffectParameter FxViewToWorld;
        EffectParameter FxProjection;
        EffectParameter FxProjectionToView;
        EffectParameter FxViewProjection;
        EffectParameter FxWorldView;
        EffectParameter FxWorldViewProjection;
        EffectParameter FxWindingDirection;
        EffectParameter FxFarClippingDistance;
        /// <summary>
        /// Set value to true when changes to a property cause calls to EffectParameter.SetValue.
        /// This tells the renderer to commit changes made during Effect Begin/End.
        /// </summary>
        protected bool _UpdatedByBatch;

        /// <summary>World matrix applied to geometry using this effect.</summary>
        public Matrix World
        {
            get => world;
            set
            {
                if (!SetWorldMatrix(ref value))
                {
                    EffectHelper.UpdateWithInverse(value, ref world, ref worldToObject,
                                                   ref FxWorld, ref FxWorldToObject);
                    SetWorldViewProjection(false, true);
                }
            }
        }

        /// <summary>
        /// Inverse world matrix applied to geometry using this effect.
        /// </summary>
        public Matrix WorldToObject => worldToObject;

        /// <summary>View matrix applied to geometry using this effect.</summary>
        public Matrix View
        {
            get => view;
            set
            {
                EffectHelper.UpdateWithInverse(value, ref view, ref viewToWorld,
                                               ref FxView, ref FxViewToWorld);
                SetWorldViewProjection(true, true);
            }
        }

        /// <summary>
        /// Inverse view matrix applied to geometry using this effect.
        /// </summary>
        public Matrix ViewToWorld => viewToWorld;

        /// <summary>
        /// Projection matrix applied to geometry using this effect.
        /// </summary>
        public Matrix Projection
        {
            get => matrix_4;
            set
            {
                if (value != matrix_4)
                {
                    matrix_4 = value;
                    FxProjection?.SetValue(matrix_4);
                    if (FxProjectionToView != null || FxFarClippingDistance != null)
                    {
                        ProjectionToView = Matrix.Invert(matrix_4);
                        FxProjectionToView?.SetValue(ProjectionToView);
                    }
                }
                SetWorldViewProjection(true, true);
            }
        }

        /// <summary>
        /// Inverse projection matrix applied to geometry using this effect.
        /// </summary>
        public Matrix ProjectionToView { get; private set; }

        /// <summary>
        /// Determines if the effect's vertex transform differs from the built-in
        /// effects, this will cause z-fighting that must be accounted for. If
        /// the value is false (meaning it varies and is different from the built-in
        /// effects) a depth adjustment technique like depth-offset needs to be applied.
        /// </summary>
        public virtual bool Invariant => true;

        /// <summary>
        /// Surfaces rendered with the effect should be visible from both sides.
        /// </summary>
        [Attribute1(true, Description = "Double Sided", HorizontalAlignment = true, MajorGrouping = 7, MinorGrouping = 10, ToolTipText = "")]
        public bool DoubleSided { get; set; }

        /// <summary>
        /// Applies the user's effect preference. This generally trades detail
        /// for performance based on the user's selection.
        /// </summary>
        public DetailPreference EffectDetail
        {
            get => detailPreference_0;
            set
            {
                if (value == detailPreference_0)
                    return;
                detailPreference_0 = value;
                SetTechnique();
            }
        }

        /// <summary>
        /// Determines if the renderer should call CommitChanges within an effect Begin/End due
        /// to internal calls to EffectParameter.SetValue. The renderer should set this value
        /// to false after calling CommitChanges.
        /// </summary>
        public bool UpdatedByBatch
        {
            get => _UpdatedByBatch;
            set => _UpdatedByBatch = false;
        }

        //private static int NumEffectsCreated;

        internal BaseRenderableEffect(GraphicsDevice device, string embeddedEffect)
            : base(device, LightingSystemManager.Instance.EmbeddedEffect(embeddedEffect))
        {
            //Console.WriteLine("Creating embedded effect: {0} {1}", embeddedEffect, ++NumEffectsCreated);
            FxWorld               = Parameters["_World"];
            FxWorldToObject       = Parameters["_WorldToObject"];
            FxView                = Parameters["_View"];
            FxViewToWorld         = Parameters["_ViewToWorld"];
            FxProjection          = Parameters["_Projection"];
            FxProjectionToView    = Parameters["_ProjectionToView"];
            FxViewProjection      = Parameters["_ViewProjection"];
            FxWorldView           = Parameters["_WorldView"];
            FxWorldViewProjection = Parameters["_WorldViewProjection"];
            FxWindingDirection    = Parameters["_WindingDirection"];
            FxFarClippingDistance = Parameters["_FarClippingDistance"];
        }

        float GetWinding()
        {
            return matrix_8.Determinant() < 0.0 ? 1f : -1f;
        }

        /// <summary>
        /// Sets the effect technique based on its current property values.
        /// </summary>
        protected abstract void SetTechnique();

        /// <summary>
        /// Recalculates the combination view-projection and world-view-projection matrix
        /// based on the individual world, view, and projection.
        /// </summary>
        protected virtual void SetWorldViewProjection(bool viewProjChanged, bool setSlowWindingDir)
        {
            if (FxWorldViewProjection == null && FxWorldView == null && (FxViewProjection == null && FxWindingDirection == null) && FxFarClippingDistance == null)
                return;
            if (FxWorldView != null)
            {
                Matrix.Multiply(ref world, ref view, out Matrix result);
                if (!result.Equals(matrix_7))
                {
                    matrix_7 = result;
                    FxWorldView.SetValue(matrix_7);
                    _UpdatedByBatch = true;
                    ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
            }
            if (FxViewProjection != null || FxWorldViewProjection != null || FxWindingDirection != null)
            {
                if (viewProjChanged)
                {
                    Matrix.Multiply(ref view, ref matrix_4, out matrix_6);
                    if (FxViewProjection != null)
                    {
                        FxViewProjection.SetValue(matrix_6);
                        _UpdatedByBatch = true;
                        ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                    }
                }
                if (FxWorldViewProjection != null || FxWindingDirection != null)
                {
                    Matrix.Multiply(ref world, ref matrix_6, out Matrix result);
                    if (!result.Equals(matrix_8))
                    {
                        matrix_8 = result;
                        if (FxWorldViewProjection != null)
                        {
                            FxWorldViewProjection.SetValue(matrix_8);
                            _UpdatedByBatch = true;
                            ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                        }
                        if (setSlowWindingDir && FxWindingDirection != null)
                        {
                            FxWindingDirection.SetValue(GetWinding());
                            _UpdatedByBatch = true;
                            ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                        }
                    }
                }
            }
            if (!viewProjChanged || FxFarClippingDistance == null)
                return;
            Vector4 vector4 = Vector4.Transform(new Vector4(0.0f, 0.0f, 1f, 1f), ProjectionToView);
            float num = 0.0f;
            if (vector4.W != 0.0)
                num = Math.Abs(vector4.Z / vector4.W);
            if (float_0 == (double) num)
                return;
            float_0 = num;
            FxFarClippingDistance.SetValue(num);
            _UpdatedByBatch = true;
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
            if (view != this.view)
            {
                this.view = view;
                if (FxView != null)
                {
                    FxView.SetValue(this.view);
                    ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
                if (FxViewToWorld != null)
                {
                    viewToWorld = viewtoworld;
                    FxViewToWorld.SetValue(viewToWorld);
                    ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
                flag = true;
            }
            if (projection != matrix_4)
            {
                matrix_4 = projection;
                if (FxProjection != null)
                {
                    FxProjection.SetValue(matrix_4);
                    ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
                if (FxProjectionToView != null || FxFarClippingDistance != null)
                {
                    ProjectionToView = projectiontoview;
                    if (FxProjectionToView != null)
                    {
                        FxProjectionToView.SetValue(ProjectionToView);
                        ++class46_0.lightingSystemStatistic_1.AccumulationValue;
                    }
                }
                flag = true;
            }
            if (!flag)
                return;
            SetWorldViewProjection(true, true);
        }

        bool SetWorldMatrix(ref Matrix newWorldMat)
        {
            if (!HasWorldMatrix)
                return newWorldMat.Equals(world);
            world = Matrix.Identity;
            HasWorldMatrix = false;
            return false;
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
            if (SetWorldMatrix(ref world))
                return;
            _UpdatedByBatch = true;
            this.world = world;
            if (FxWorld != null)
            {
                FxWorld.SetValue(this.world);
                ++class46_0.lightingSystemStatistic_1.AccumulationValue;
            }
            if (FxWorldToObject != null)
            {
                worldToObject = worldtoobj;
                FxWorldToObject.SetValue(worldToObject);
                ++class46_0.lightingSystemStatistic_1.AccumulationValue;
            }
            SetWorldViewProjection(false, true);
        }

        /// <summary>
        /// Used internally by SunBurn - not recommended for external use.
        /// 
        /// Quickly sets the world and inverse world matrices during an effect
        /// Begin / End block.  Values applied using this method do not persist
        /// after the Begin / End block.
        /// 
        /// This method is highly context sensitive.  Built-in effects that derive from
        /// BaseRenderableEffect fully support this method, however other effects merely
        /// call the non-transposed overload.
        /// </summary>
        /// <param name="world">World matrix applied to geometry using this effect.</param>
        /// <param name="worldtranspose">Transposed world matrix applied to geometry using this effect.</param>
        /// <param name="worldtoobj">Inverse world matrix applied to geometry using this effect.</param>
        /// <param name="worldtoobjtranspose">Transposed inverse world matrix applied to geometry using this effect.</param>
        public void SetWorldAndWorldToObject(ref Matrix world, ref Matrix worldtranspose, ref Matrix worldtoobj, ref Matrix worldtoobjtranspose)
        {
            if (FxWorld != null)
                GraphicsDevice.SetVertexShaderConstant(0, worldtranspose);
            if (FxWorldToObject != null)
            {
                if (FxWorldToObject.RowCount >= 4)
                {
                    GraphicsDevice.SetVertexShaderConstant(4, worldtoobjtranspose);
                }
                else
                {
                    GraphicsDevice.SetVertexShaderConstant(4, worldtoobjtranspose.Right);
                    GraphicsDevice.SetVertexShaderConstant(5, worldtoobjtranspose.Up);
                    GraphicsDevice.SetVertexShaderConstant(6, worldtoobjtranspose.Backward);
                }
            }
            HasWorldMatrix = true;
            if (!world.Equals(this.world))
            {
                this.world = world;
                if (FxWorldToObject != null)
                    worldToObject = worldtoobj;
                SetWorldViewProjection(false, false);
            }
            if (FxWindingDirection == null)
                return;
            GraphicsDevice.SetPixelShaderConstant(0, new Vector2(GetWinding()));
        }

        /// <summary>
        /// Creates a new effect of the same class type, with the same property values, and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public override Effect Clone(GraphicsDevice device)
        {
            Effect effect = Create(device);
            Class12.smethod_1(this, effect);
            return effect;
        }

        /// <summary>
        /// Creates a new empty effect of the same class type and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        protected abstract Effect Create(GraphicsDevice device);

        internal class Class46
        {
            public LightingSystemStatistic lightingSystemStatistic_0 = LightingSystemStatistics.GetStatistic("Effect_TechniqueChanges", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_1 = LightingSystemStatistics.GetStatistic("Effect_MatrixParameterChanges", LightingSystemStatisticCategory.Rendering);
            public LightingSystemStatistic lightingSystemStatistic_2 = LightingSystemStatistics.GetStatistic("Effect_LightSourceChanges", LightingSystemStatisticCategory.Rendering);
        }
    }
}
