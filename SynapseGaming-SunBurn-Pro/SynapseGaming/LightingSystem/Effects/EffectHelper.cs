// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.EffectHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Effects
{
    /// <summary>
    /// Provides methods that help with effect setup and data synchronization.
    /// </summary>
    public class EffectHelper
    {
        internal static void UpdateViewToWorld(Matrix viewToWorld, ref Matrix outViewToWorld, ref EffectParameter parameter)
        {
            if (parameter != null && !(viewToWorld == outViewToWorld))
            {
                parameter.SetValue(viewToWorld);
            }
            outViewToWorld = viewToWorld;
        }

        internal static void Update(Matrix[] newValue, ref Matrix[] oldValue, ref EffectParameter parameter)
        {
            oldValue = newValue;
            if (parameter != null && newValue != null)
            {
                int end = Math.Min(newValue.Length, parameter.Elements.Count);
                parameter.SetArrayRange(0, end);
                parameter.SetValue(newValue);
            }
        }

        internal static void UpdateWithInverse(in Matrix newValue, ref Matrix oldValue, ref Matrix outInverseValue, 
                                               ref EffectParameter parameter1, ref EffectParameter parameter2)
        {
            if (newValue != oldValue)
            {
                oldValue = newValue;
                parameter1?.SetValue(oldValue);
                if (parameter2 != null)
                {
                    outInverseValue = Matrix.Invert(oldValue);
                    parameter2.SetValue(outInverseValue);
                }
            }
        }

        internal static void Update(Vector4 newValue, ref Vector4 oldValue, ref EffectParameter parameter)
        {
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(Vector3 newValue, ref Vector3 oldValue, ref EffectParameter parameter)
        {
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(int newValue, ref int oldValue, ref EffectParameter parameter)
        {
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(float newValue, ref float oldValue, ref EffectParameter parameter)
        {
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(Vector2 newValue, ref Vector2 oldValue, ref EffectParameter parameter)
        {
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(Texture2D newValue, ref Texture2D oldValue, EffectParameter parameter)
        {
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(Texture2D newValue, Texture2D defaultValue, ref Texture2D oldValue, ref EffectParameter parameter)
        {
            if (newValue == null)
                newValue = defaultValue;
            if (parameter != null && newValue != oldValue)
            {
                oldValue = newValue;
                parameter.SetValue(oldValue);
            }
        }

        internal static void Update(List<EffectParameter> parameters, in Vector4 newValue)
        {
            if (parameters == null || parameters.Count < 1)
                return;
            for (int i = 0; i < parameters.Count; ++i)
            {
                EffectParameter param = parameters[i];
                if (param.ParameterType == EffectParameterType.Int32 && param.RowCount == 1)
                    param.SetValue((int)newValue.X);
                if (param.ParameterType == EffectParameterType.Single && param.RowCount <= 1)
                {
                    if (param.ColumnCount == 1)
                        param.SetValue(newValue.X);
                    else if (param.ColumnCount == 2)
                        param.SetValue(new Vector2(newValue.X, newValue.Y));
                    else if (param.ColumnCount == 3)
                        param.SetValue(new Vector3(newValue.X, newValue.Y, newValue.Z));
                    else
                        param.SetValue(newValue);
                }
            }
        }

        internal static void Update(List<EffectParameter> parameters, in Matrix newValue)
        {
            if (parameters != null && parameters.Count >= 1)
            {
                for (int i = 0; i < parameters.Count; ++i)
                    parameters[i].SetValue(newValue);
            }
        }

        internal static void UpdateTransposed(List<EffectParameter> parameters, Matrix newValue)
        {
            if (parameters != null && parameters.Count >= 1)
            {
                Update(parameters, Matrix.Transpose(newValue));
            }
        }

        /// <summary>
        /// Synchronizes all recognized object effect properties with the shadow effect.
        /// Allows shadow effects to support material transparency.
        /// </summary>
        /// <param name="objEffect">The object's effect</param>
        /// <param name="shadowEffect">The shadow effect</param>
        public static void SyncObjectAndShadowEffects(Effect objEffect, Effect shadowEffect)
        {
            if (shadowEffect is IRenderableEffect shadowRender)
            {
                shadowRender.DoubleSided = objEffect is IRenderableEffect renderObjEffect && renderObjEffect.DoubleSided;
            }

            if (shadowEffect is ITransparentEffect tranShadow)
            {
                if (objEffect is ITransparentEffect tranObj)
                {
                    if (tranObj.TransparencyMode != TransparencyMode.None)
                    {
                        tranShadow.SetTransparencyModeAndMap(tranObj.TransparencyMode, tranObj.Transparency, tranObj.TransparencyMap);
                    }
                    else
                    {
                        tranShadow.SetTransparencyModeAndMap(TransparencyMode.None, tranShadow.Transparency, null);
                    }
                }
                else
                {
                    tranShadow.SetTransparencyModeAndMap(TransparencyMode.None, tranShadow.Transparency, null);
                }
            }

            if (shadowEffect is IAddressableEffect addrShadowEffect)
            {
                if (objEffect is IAddressableEffect addrObjEffect)
                {
                    addrShadowEffect.AddressModeU = addrObjEffect.AddressModeU;
                    addrShadowEffect.AddressModeV = addrObjEffect.AddressModeV;
                    addrShadowEffect.AddressModeW = addrObjEffect.AddressModeW;
                }
                else
                {
                    addrShadowEffect.AddressModeU = TextureAddressMode.Wrap;
                    addrShadowEffect.AddressModeV = TextureAddressMode.Wrap;
                    addrShadowEffect.AddressModeW = TextureAddressMode.Wrap;
                }
            }

            if (shadowEffect is ITerrainEffect terrainShadow)
            {
                if (objEffect is ITerrainEffect terrainObj)
                {
                    terrainShadow.HeightMapTexture = terrainObj.HeightMapTexture;
                    terrainShadow.HeightScale = terrainObj.HeightScale;
                    terrainShadow.MeshSegments = terrainObj.MeshSegments;
                    terrainShadow.Tiling = terrainObj.Tiling;
                }
                else
                {
                    terrainShadow.HeightMapTexture = null;
                }
            }
        }
    }
}
