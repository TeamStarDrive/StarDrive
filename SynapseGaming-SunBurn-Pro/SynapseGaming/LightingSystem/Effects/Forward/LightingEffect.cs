// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.Forward.LightingEffect
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
    /// Effect provides SunBurn's built-in lighting and material support.
    /// 
    /// Including:
    /// -Diffuse mapping
    /// -Bump mapping
    /// -Specular mapping (with specular intensity mapping)
    /// -Point, spot, directional, and ambient lighting
    /// </summary>
    public class LightingEffect : BaseMaterialEffect, ILightingEffect
    {
        private static Vector4[] vector4_1 = new Vector4[1];
        private static Vector4[] vector4_2 = new Vector4[1];
        private static Vector4[] vector4_3 = new Vector4[1];
        private const int int_0 = 1;
        private EffectParameter DiffuseColorAndSpotAngleInv;
        private EffectParameter PositionAndRadius;
        private EffectParameter SpotDirectionAndAngle;

        /// <summary>Maximum number of light sources the effect supports.</summary>
        public int MaxLightSources => 1;

        /// <summary>
        /// Light sources that apply lighting to the effect during rendering.
        /// </summary>
        public List<ILight> LightSources
        {
            set
            {
                _LightSources.Clear();
                foreach (ILight light in value)
                    _LightSources.Add(light);
                method_5();
                ++class46_0.lightingSystemStatistic_2.AccumulationValue;
            }
        }

        /// <summary>Creates a new LightingEffect instance.</summary>
        /// <param name="device"></param>
        public LightingEffect(GraphicsDevice device) : base(device, "LightingEffect")
        {
            InitializeEffectParameters();
        }

        internal LightingEffect(GraphicsDevice device, bool bool_5)
          : base(device, "LightingEffect", bool_5)
        {
            InitializeEffectParameters();
        }

        private void method_5()
        {
            if (DiffuseColorAndSpotAngleInv == null || PositionAndRadius == null || (SpotDirectionAndAngle == null || _LightSources == null))
                return;
            if (_LightSources.Count > vector4_1.Length)
                throw new ArgumentException("Too many light sources provided for effect.");
            if (_LightSources.Count != 1)
                throw new ArgumentException("LightingEffect only supports a single light per-pass at this time.");
            for (int index = 0; index < _LightSources.Count; ++index)
            {
                ILight lightSource = _LightSources[index];
                Vector3 colorAndIntensity = lightSource.CompositeColorAndIntensity;
                vector4_1[index] = new Vector4(colorAndIntensity, 0.0f);
                vector4_3[index] = new Vector4();
                if (lightSource is ISpotSource)
                {
                    ISpotSource spotSource = lightSource as ISpotSource;
                    float w = (float)Math.Cos(MathHelper.ToRadians(MathHelper.Clamp(spotSource.Angle * 0.5f, 0.01f, 89.99f)));
                    float num = (float)(1.0 / (1.0 - w));
                    vector4_1[index].W = num;
                    vector4_3[index] = new Vector4(spotSource.Direction, w);
                    vector4_2[index] = new Vector4(spotSource.Position, spotSource.Radius);
                }
                else if (lightSource is IPointSource)
                {
                    IPointSource pointSource = lightSource as IPointSource;
                    vector4_2[index] = new Vector4(pointSource.Position, pointSource.Radius);
                }
                else if (lightSource is IShadowSource)
                {
                    IShadowSource shadowSource = lightSource as IShadowSource;
                    vector4_2[index] = new Vector4(shadowSource.ShadowPosition, 1E+09f);
                }
            }
            DiffuseColorAndSpotAngleInv.SetValue(vector4_1);
            PositionAndRadius.SetValue(vector4_2);
            SpotDirectionAndAngle.SetValue(vector4_3);
            SetTechnique();
        }

        /// <summary>
        /// Creates a new empty effect of the same class type and using the same effect file as this object.
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        protected override Effect Create(GraphicsDevice device)
        {
            return new LightingEffect(device);
        }

        void InitializeEffectParameters()
        {
            DiffuseColorAndSpotAngleInv = Parameters["_DiffuseColor_And_SpotAngleInv"];
            PositionAndRadius = Parameters["_Position_And_Radius"];
            SpotDirectionAndAngle = Parameters["_SpotDirection_And_SpotAngle"];
        }
    }
}
