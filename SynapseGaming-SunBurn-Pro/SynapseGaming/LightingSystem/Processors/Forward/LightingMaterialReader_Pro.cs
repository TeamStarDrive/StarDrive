// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.Forward.LightingMaterialReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using System;

namespace SynapseGaming.LightingSystem.Processors.Forward
{
  /// <summary />
  public class LightingMaterialReader_Pro : ContentTypeReader<LightingEffect>
  {
    /// <summary />
    protected override LightingEffect Read(ContentReader input, LightingEffect instance)
    {
      IGraphicsDeviceService service = (IGraphicsDeviceService) input.ContentManager.ServiceProvider.GetService(typeof (IGraphicsDeviceService));
      LightingEffect lightingEffect = new LightingEffect(service.GraphicsDevice);
      lightingEffect.MaterialName = input.ReadString();
      lightingEffect.MaterialFile = input.ReadString();
      lightingEffect.ProjectFile = input.ReadString();
      lightingEffect.DiffuseMapFile = input.ReadString();
      lightingEffect.DiffuseMapTexture = input.ReadExternalReference<Texture2D>();
      lightingEffect.DiffuseAmbientMapFile = input.ReadString();
      lightingEffect.DiffuseAmbientMapTexture = input.ReadExternalReference<Texture2D>();
      lightingEffect.EmissiveMapFile = input.ReadString();
      lightingEffect.EmissiveMapTexture = input.ReadExternalReference<Texture2D>();
      lightingEffect.NormalMapFile = input.ReadString();
      lightingEffect.NormalMapTexture = input.ReadExternalReference<Texture2D>();
      lightingEffect.SpecularColorMapFile = input.ReadString();
      lightingEffect.SpecularColorMapTexture = input.ReadExternalReference<Texture2D>();
      lightingEffect.ParallaxMapFile = input.ReadString();
      lightingEffect.ParallaxMapTexture = CoreUtils.smethod_28(service.GraphicsDevice, input.ReadExternalReference<Texture2D>());
      lightingEffect.Skinned = input.ReadBoolean();
      lightingEffect.DoubleSided = input.ReadBoolean();
      TransparencyMode mode = (TransparencyMode) input.ReadInt32();
      float transparency = input.ReadSingle();
      lightingEffect.SetTransparencyModeAndMap(mode, transparency, (Texture) lightingEffect.DiffuseMapTexture);
      lightingEffect.SpecularPower = input.ReadSingle();
      lightingEffect.SpecularAmount = input.ReadSingle();
      lightingEffect.FresnelReflectBias = input.ReadSingle();
      lightingEffect.FresnelReflectOffset = input.ReadSingle();
      lightingEffect.FresnelMicrofacetDistribution = input.ReadSingle();
      lightingEffect.ParallaxScale = input.ReadSingle();
      lightingEffect.ParallaxOffset = input.ReadSingle();
      Vector4 vector4_1 = input.ReadVector4();
      lightingEffect.DiffuseColor = new Vector3(vector4_1.X, vector4_1.Y, vector4_1.Z);
      Vector4 vector4_2 = input.ReadVector4();
      lightingEffect.EmissiveColor = new Vector3(vector4_2.X, vector4_2.Y, vector4_2.Z);
      lightingEffect.AddressModeU = (TextureAddressMode) input.ReadInt32();
      lightingEffect.AddressModeV = (TextureAddressMode) input.ReadInt32();
      lightingEffect.AddressModeW = (TextureAddressMode) input.ReadInt32();
      Class55.smethod_0(input);
      if (input.ReadInt32() != 1234)
        throw new Exception("Error loading asset.");
      return lightingEffect;
    }
  }
}
