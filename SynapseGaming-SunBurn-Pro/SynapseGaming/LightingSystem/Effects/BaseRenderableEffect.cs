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
        private bool bool_0;
        private DetailPreference detailPreference_0;
        private Matrix matrix_0;
        private Matrix matrix_1;
        private Matrix matrix_2;
        private Matrix matrix_3;
        private Matrix matrix_4;
        private Matrix matrix_6;
        private Matrix matrix_7;
        private Matrix matrix_8;
        private float float_0;
        private EffectParameter effectParameter_0;
        private EffectParameter effectParameter_1;
        private EffectParameter effectParameter_2;
        private EffectParameter effectParameter_3;
        private EffectParameter effectParameter_4;
        private EffectParameter effectParameter_5;
        private EffectParameter effectParameter_6;
        private EffectParameter effectParameter_7;
        private EffectParameter effectParameter_8;
        private EffectParameter effectParameter_9;
        private EffectParameter effectParameter_10;
        /// <summary>
        /// Set value to true when changes to a property cause calls to EffectParameter.SetValue.
        /// This tells the renderer to commit changes made during Effect Begin/End.
        /// </summary>
        protected bool _UpdatedByBatch;

        /// <summary>World matrix applied to geometry using this effect.</summary>
        public Matrix World
        {
            get => this.matrix_0;
            set
            {
                if (this.method_1(ref value))
                    return;
                EffectHelper.smethod_2(value, ref this.matrix_0, ref this.matrix_1, ref this.effectParameter_0, ref this.effectParameter_1);
                this.SetWorldViewProjection(false, true);
            }
        }

        /// <summary>
        /// Inverse world matrix applied to geometry using this effect.
        /// </summary>
        public Matrix WorldToObject => this.matrix_1;

        /// <summary>View matrix applied to geometry using this effect.</summary>
        public Matrix View
        {
            get => this.matrix_2;
            set
            {
                EffectHelper.smethod_2(value, ref this.matrix_2, ref this.matrix_3, ref this.effectParameter_2, ref this.effectParameter_3);
                this.SetWorldViewProjection(true, true);
            }
        }

        /// <summary>
        /// Inverse view matrix applied to geometry using this effect.
        /// </summary>
        public Matrix ViewToWorld => this.matrix_3;

        /// <summary>
        /// Projection matrix applied to geometry using this effect.
        /// </summary>
        public Matrix Projection
        {
            get => this.matrix_4;
            set
            {
                if (value != this.matrix_4)
                {
                    this.matrix_4 = value;
                    if (this.effectParameter_4 != null)
                        this.effectParameter_4.SetValue(this.matrix_4);
                    if (this.effectParameter_5 != null || this.effectParameter_10 != null)
                    {
                        this.ProjectionToView = Matrix.Invert(this.matrix_4);
                        if (this.effectParameter_5 != null)
                            this.effectParameter_5.SetValue(this.ProjectionToView);
                    }
                }
                this.SetWorldViewProjection(true, true);
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
            get => this.detailPreference_0;
            set
            {
                if (value == this.detailPreference_0)
                    return;
                this.detailPreference_0 = value;
                this.SetTechnique();
            }
        }

        /// <summary>
        /// Determines if the renderer should call CommitChanges within an effect Begin/End due
        /// to internal calls to EffectParameter.SetValue. The renderer should set this value
        /// to false after calling CommitChanges.
        /// </summary>
        public bool UpdatedByBatch
        {
            get => this._UpdatedByBatch;
            set => this._UpdatedByBatch = false;
        }

        //private static int NumEffectsCreated;

        internal BaseRenderableEffect(GraphicsDevice device, string embeddedEffect)
            : base(device, LightingSystemManager.Instance.EmbeddedEffect(embeddedEffect))
        {
            //Console.WriteLine("Creating embedded effect: {0} {1}", embeddedEffect, ++NumEffectsCreated);
            this.effectParameter_0 = this.Parameters["_World"];
            this.effectParameter_1 = this.Parameters["_WorldToObject"];
            this.effectParameter_2 = this.Parameters["_View"];
            this.effectParameter_3 = this.Parameters["_ViewToWorld"];
            this.effectParameter_4 = this.Parameters["_Projection"];
            this.effectParameter_5 = this.Parameters["_ProjectionToView"];
            this.effectParameter_6 = this.Parameters["_ViewProjection"];
            this.effectParameter_7 = this.Parameters["_WorldView"];
            this.effectParameter_8 = this.Parameters["_WorldViewProjection"];
            this.effectParameter_9 = this.Parameters["_WindingDirection"];
            this.effectParameter_10 = this.Parameters["_FarClippingDistance"];
        }

        private float method_0()
        {
            return (double) this.matrix_8.Determinant() < 0.0 ? 1f : -1f;
        }

        /// <summary>
        /// Sets the effect technique based on its current property values.
        /// </summary>
        protected abstract void SetTechnique();

        /// <summary>
        /// Recalculates the combination view-projection and world-view-projection matrix
        /// based on the individual world, view, and projection.
        /// </summary>
        protected virtual void SetWorldViewProjection(bool viewprojectionchanged, bool setslowwindingdirection)
        {
            if (this.effectParameter_8 == null && this.effectParameter_7 == null && (this.effectParameter_6 == null && this.effectParameter_9 == null) && this.effectParameter_10 == null)
                return;
            if (this.effectParameter_7 != null)
            {
                Matrix result;
                Matrix.Multiply(ref this.matrix_0, ref this.matrix_2, out result);
                if (!result.Equals(this.matrix_7))
                {
                    this.matrix_7 = result;
                    this.effectParameter_7.SetValue(this.matrix_7);
                    this._UpdatedByBatch = true;
                    ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
            }
            if (this.effectParameter_6 != null || this.effectParameter_8 != null || this.effectParameter_9 != null)
            {
                if (viewprojectionchanged)
                {
                    Matrix.Multiply(ref this.matrix_2, ref this.matrix_4, out this.matrix_6);
                    if (this.effectParameter_6 != null)
                    {
                        this.effectParameter_6.SetValue(this.matrix_6);
                        this._UpdatedByBatch = true;
                        ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                    }
                }
                if (this.effectParameter_8 != null || this.effectParameter_9 != null)
                {
                    Matrix result;
                    Matrix.Multiply(ref this.matrix_0, ref this.matrix_6, out result);
                    if (!result.Equals(this.matrix_8))
                    {
                        this.matrix_8 = result;
                        if (this.effectParameter_8 != null)
                        {
                            this.effectParameter_8.SetValue(this.matrix_8);
                            this._UpdatedByBatch = true;
                            ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                        }
                        if (setslowwindingdirection && this.effectParameter_9 != null)
                        {
                            this.effectParameter_9.SetValue(this.method_0());
                            this._UpdatedByBatch = true;
                            ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                        }
                    }
                }
            }
            if (!viewprojectionchanged || this.effectParameter_10 == null)
                return;
            Vector4 vector4 = Vector4.Transform(new Vector4(0.0f, 0.0f, 1f, 1f), this.ProjectionToView);
            float num = 0.0f;
            if (vector4.W != 0.0)
                num = Math.Abs(vector4.Z / vector4.W);
            if (this.float_0 == (double) num)
                return;
            this.float_0 = num;
            this.effectParameter_10.SetValue(num);
            this._UpdatedByBatch = true;
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
            if (view != this.matrix_2)
            {
                this.matrix_2 = view;
                if (this.effectParameter_2 != null)
                {
                    this.effectParameter_2.SetValue(this.matrix_2);
                    ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
                if (this.effectParameter_3 != null)
                {
                    this.matrix_3 = viewtoworld;
                    this.effectParameter_3.SetValue(this.matrix_3);
                    ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
                flag = true;
            }
            if (projection != this.matrix_4)
            {
                this.matrix_4 = projection;
                if (this.effectParameter_4 != null)
                {
                    this.effectParameter_4.SetValue(this.matrix_4);
                    ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                }
                if (this.effectParameter_5 != null || this.effectParameter_10 != null)
                {
                    this.ProjectionToView = projectiontoview;
                    if (this.effectParameter_5 != null)
                    {
                        this.effectParameter_5.SetValue(this.ProjectionToView);
                        ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
                    }
                }
                flag = true;
            }
            if (!flag)
                return;
            this.SetWorldViewProjection(true, true);
        }

        private bool method_1(ref Matrix matrix_9)
        {
            if (!this.bool_0)
                return matrix_9.Equals(this.matrix_0);
            this.matrix_0 = Matrix.Identity;
            this.bool_0 = false;
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
        public void SetWorldAndWorldToObject(Matrix world, Matrix worldtoobj)
        {
            if (this.method_1(ref world))
                return;
            this._UpdatedByBatch = true;
            this.matrix_0 = world;
            if (this.effectParameter_0 != null)
            {
                this.effectParameter_0.SetValue(this.matrix_0);
                ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
            }
            if (this.effectParameter_1 != null)
            {
                this.matrix_1 = worldtoobj;
                this.effectParameter_1.SetValue(this.matrix_1);
                ++this.class46_0.lightingSystemStatistic_1.AccumulationValue;
            }
            this.SetWorldViewProjection(false, true);
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
            if (this.effectParameter_0 != null)
                this.GraphicsDevice.SetVertexShaderConstant(0, worldtranspose);
            if (this.effectParameter_1 != null)
            {
                if (this.effectParameter_1.RowCount >= 4)
                {
                    this.GraphicsDevice.SetVertexShaderConstant(4, worldtoobjtranspose);
                }
                else
                {
                    this.GraphicsDevice.SetVertexShaderConstant(4, worldtoobjtranspose.Right);
                    this.GraphicsDevice.SetVertexShaderConstant(5, worldtoobjtranspose.Up);
                    this.GraphicsDevice.SetVertexShaderConstant(6, worldtoobjtranspose.Backward);
                }
            }
            this.bool_0 = true;
            if (!world.Equals(this.matrix_0))
            {
                this.matrix_0 = world;
                if (this.effectParameter_1 != null)
                    this.matrix_1 = worldtoobj;
                this.SetWorldViewProjection(false, false);
            }
            if (this.effectParameter_9 == null)
                return;
            this.GraphicsDevice.SetPixelShaderConstant(0, new Vector2(this.method_0()));
        }

        /// <summary>
        /// Creates a new effect of the same class type, with the same property values, and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public override Effect Clone(GraphicsDevice device)
        {
            Effect effect = this.Create(device);
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
