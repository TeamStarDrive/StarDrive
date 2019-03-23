// Decompiled with JetBrains decompiler
// Type: ns6.Interface3
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;

namespace EmbeddedResources
{
  internal interface IShadowEffect
  {
    TextureCube ShadowFaceMap { get; set; }

    TextureCube ShadowCoordMap { get; set; }

    Texture2D ShadowMap { get; }

    BoundingSphere ShadowArea { set; }

    Vector4 ShadowViewDistance { get; set; }

    Vector4[] ShadowMapLocationAndSpan { set; }

    Matrix[] ShadowViewProjection { set; }

    DetailPreference EffectDetail { get; set; }

    void SetShadowMapAndType(Texture2D shadowmap, Enum5 type);
  }
}
