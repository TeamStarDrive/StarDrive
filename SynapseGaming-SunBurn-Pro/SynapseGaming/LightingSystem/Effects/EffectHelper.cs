// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Effects.EffectHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;

namespace SynapseGaming.LightingSystem.Effects
{
  /// <summary>
  /// Provides methods that help with effect setup and data synchronization.
  /// </summary>
  public class EffectHelper
  {
    internal static void smethod_0(Matrix matrix_0, ref Matrix matrix_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 != null && !(matrix_0 == matrix_1))
      {
        matrix_1 = matrix_0;
        effectParameter_0.SetValue(matrix_1);
      }
      else
        matrix_1 = matrix_0;
    }

    internal static void smethod_1(Matrix[] matrix_0, ref Matrix[] matrix_1, ref EffectParameter effectParameter_0)
    {
      matrix_1 = matrix_0;
      if (effectParameter_0 == null || matrix_0 == null)
        return;
      int end = Math.Min(matrix_0.Length, effectParameter_0.Elements.Count);
      effectParameter_0.SetArrayRange(0, end);
      effectParameter_0.SetValue(matrix_0);
    }

    internal static void smethod_2(Matrix matrix_0, ref Matrix matrix_1, ref Matrix matrix_2, ref EffectParameter effectParameter_0, ref EffectParameter effectParameter_1)
    {
      if (matrix_0 == matrix_1)
        return;
      matrix_1 = matrix_0;
      if (effectParameter_0 != null)
        effectParameter_0.SetValue(matrix_1);
      if (effectParameter_1 == null)
        return;
      matrix_2 = Matrix.Invert(matrix_1);
      effectParameter_1.SetValue(matrix_2);
    }

    internal static void smethod_3(Vector4 vector4_0, ref Vector4 vector4_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 == null || vector4_0 == vector4_1)
        return;
      vector4_1 = vector4_0;
      effectParameter_0.SetValue(vector4_1);
    }

    internal static void smethod_4(Vector3 vector3_0, ref Vector3 vector3_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 == null || vector3_0 == vector3_1)
        return;
      vector3_1 = vector3_0;
      effectParameter_0.SetValue(vector3_1);
    }

    internal static void smethod_5(int int_0, ref int int_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 == null || int_0 == int_1)
        return;
      int_1 = int_0;
      effectParameter_0.SetValue(int_1);
    }

    internal static void smethod_6(float float_0, ref float float_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 == null || (double) float_0 == (double) float_1)
        return;
      float_1 = float_0;
      effectParameter_0.SetValue(float_1);
    }

    internal static void smethod_7(Vector2 vector2_0, ref Vector2 vector2_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 == null || vector2_0 == vector2_1)
        return;
      vector2_1 = vector2_0;
      effectParameter_0.SetValue(vector2_1);
    }

    internal static void smethod_8(Texture2D texture2D_0, ref Texture2D texture2D_1, ref EffectParameter effectParameter_0)
    {
      if (effectParameter_0 == null || texture2D_0 == texture2D_1)
        return;
      texture2D_1 = texture2D_0;
      effectParameter_0.SetValue((Texture) texture2D_1);
    }

    internal static void smethod_9(Texture2D texture2D_0, Texture2D texture2D_1, ref Texture2D texture2D_2, ref EffectParameter effectParameter_0)
    {
      if (texture2D_0 == null)
        texture2D_0 = texture2D_1;
      if (effectParameter_0 == null || texture2D_0 == texture2D_2)
        return;
      texture2D_2 = texture2D_0;
      effectParameter_0.SetValue((Texture) texture2D_2);
    }

    internal static void smethod_10(List<EffectParameter> list_0, Vector4 vector4_0)
    {
      if (list_0 == null || list_0.Count < 1)
        return;
      for (int index = 0; index < list_0.Count; ++index)
      {
        EffectParameter effectParameter = list_0[index];
        if (effectParameter.ParameterType == EffectParameterType.Int32 && effectParameter.RowCount == 1)
          effectParameter.SetValue((int) vector4_0.X);
        if (effectParameter.ParameterType == EffectParameterType.Single && effectParameter.RowCount <= 1)
        {
          if (effectParameter.ColumnCount == 1)
            effectParameter.SetValue(vector4_0.X);
          else if (effectParameter.ColumnCount == 2)
            effectParameter.SetValue(new Vector2(vector4_0.X, vector4_0.Y));
          else if (effectParameter.ColumnCount == 3)
            effectParameter.SetValue(new Vector3(vector4_0.X, vector4_0.Y, vector4_0.Z));
          else
            effectParameter.SetValue(vector4_0);
        }
      }
    }

    internal static void smethod_11(List<EffectParameter> list_0, Matrix matrix_0)
    {
      if (list_0 == null || list_0.Count < 1)
        return;
      for (int index = 0; index < list_0.Count; ++index)
        list_0[index].SetValue(matrix_0);
    }

    internal static void smethod_12(List<EffectParameter> list_0, Matrix matrix_0)
    {
      if (list_0 == null || list_0.Count < 1)
        return;
      EffectHelper.smethod_11(list_0, Matrix.Transpose(matrix_0));
    }

    /// <summary>
    /// Synchronizes all recognized object effect properties with the shadow effect.
    /// Allows shadow effects to support material transparency.
    /// </summary>
    /// <param name="objeffect">The object's effect</param>
    /// <param name="shadoweffect">The shadow effect</param>
    public static void SyncObjectAndShadowEffects(Effect objeffect, Effect shadoweffect)
    {
      if (shadoweffect is IRenderableEffect)
        (shadoweffect as IRenderableEffect).DoubleSided = objeffect is IRenderableEffect && (objeffect as IRenderableEffect).DoubleSided;
      if (shadoweffect is ITransparentEffect)
      {
        ITransparentEffect transparentEffect1 = shadoweffect as ITransparentEffect;
        if (objeffect is ITransparentEffect)
        {
          ITransparentEffect transparentEffect2 = objeffect as ITransparentEffect;
          if (transparentEffect2.TransparencyMode != TransparencyMode.None)
            transparentEffect1.SetTransparencyModeAndMap(transparentEffect2.TransparencyMode, transparentEffect2.Transparency, transparentEffect2.TransparencyMap);
          else
            transparentEffect1.SetTransparencyModeAndMap(TransparencyMode.None, transparentEffect1.Transparency, (Texture) null);
        }
        else
          transparentEffect1.SetTransparencyModeAndMap(TransparencyMode.None, transparentEffect1.Transparency, (Texture) null);
      }
      if (shadoweffect is IAddressableEffect)
      {
        IAddressableEffect addressableEffect1 = shadoweffect as IAddressableEffect;
        if (objeffect is IAddressableEffect)
        {
          IAddressableEffect addressableEffect2 = objeffect as IAddressableEffect;
          addressableEffect1.AddressModeU = addressableEffect2.AddressModeU;
          addressableEffect1.AddressModeV = addressableEffect2.AddressModeV;
          addressableEffect1.AddressModeW = addressableEffect2.AddressModeW;
        }
        else
        {
          addressableEffect1.AddressModeU = TextureAddressMode.Wrap;
          addressableEffect1.AddressModeV = TextureAddressMode.Wrap;
          addressableEffect1.AddressModeW = TextureAddressMode.Wrap;
        }
      }
      if (!(shadoweffect is ITerrainEffect))
        return;
      ITerrainEffect terrainEffect1 = shadoweffect as ITerrainEffect;
      if (objeffect is ITerrainEffect)
      {
        ITerrainEffect terrainEffect2 = objeffect as ITerrainEffect;
        terrainEffect1.HeightMapTexture = terrainEffect2.HeightMapTexture;
        terrainEffect1.HeightScale = terrainEffect2.HeightScale;
        terrainEffect1.MeshSegments = terrainEffect2.MeshSegments;
        terrainEffect1.Tiling = terrainEffect2.Tiling;
      }
      else
        terrainEffect1.HeightMapTexture = (Texture2D) null;
    }
  }
}
