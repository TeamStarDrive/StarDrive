// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.Deferred.DeferredObjectMaterialReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Deferred;

namespace SynapseGaming.LightingSystem.Processors.Deferred
{
  /// <summary />
  public class DeferredObjectMaterialReader_Pro : ContentTypeReader<DeferredObjectEffect>
  {
    /// <summary />
    protected override DeferredObjectEffect Read(ContentReader input, DeferredObjectEffect instance)
    {
      IGraphicsDeviceService service = (IGraphicsDeviceService) input.ContentManager.ServiceProvider.GetService(typeof (IGraphicsDeviceService));
      DeferredObjectEffect deferredObjectEffect = new DeferredObjectEffect(service.GraphicsDevice);
      deferredObjectEffect.MaterialName = input.ReadString();
      deferredObjectEffect.MaterialFile = input.ReadString();
      deferredObjectEffect.ProjectFile = input.ReadString();
      deferredObjectEffect.DiffuseMapFile = input.ReadString();
      deferredObjectEffect.DiffuseMapTexture = input.ReadExternalReference<Texture2D>();
      deferredObjectEffect.DiffuseAmbientMapFile = input.ReadString();
      deferredObjectEffect.DiffuseAmbientMapTexture = input.ReadExternalReference<Texture2D>();
      deferredObjectEffect.EmissiveMapFile = input.ReadString();
      deferredObjectEffect.EmissiveMapTexture = input.ReadExternalReference<Texture2D>();
      deferredObjectEffect.NormalMapFile = input.ReadString();
      deferredObjectEffect.NormalMapTexture = input.ReadExternalReference<Texture2D>();
      deferredObjectEffect.SpecularColorMapFile = input.ReadString();
      deferredObjectEffect.SpecularColorMapTexture = input.ReadExternalReference<Texture2D>();
      deferredObjectEffect.ParallaxMapFile = input.ReadString();
      deferredObjectEffect.ParallaxMapTexture = CoreUtils.smethod_28(service.GraphicsDevice, input.ReadExternalReference<Texture2D>());
      deferredObjectEffect.Skinned = input.ReadBoolean();
      deferredObjectEffect.DoubleSided = input.ReadBoolean();
      TransparencyMode mode = (TransparencyMode) input.ReadInt32();
      float transparency = input.ReadSingle();
      deferredObjectEffect.SetTransparencyModeAndMap(mode, transparency, deferredObjectEffect.DiffuseMapTexture);
      deferredObjectEffect.SpecularPower = input.ReadSingle();
      deferredObjectEffect.SpecularAmount = input.ReadSingle();
      deferredObjectEffect.FresnelReflectBias = input.ReadSingle();
      deferredObjectEffect.FresnelReflectOffset = input.ReadSingle();
      deferredObjectEffect.FresnelMicrofacetDistribution = input.ReadSingle();
      deferredObjectEffect.ParallaxScale = input.ReadSingle();
      deferredObjectEffect.ParallaxOffset = input.ReadSingle();
      Vector4 vector4_1 = input.ReadVector4();
      deferredObjectEffect.DiffuseColor = new Vector3(vector4_1.X, vector4_1.Y, vector4_1.Z);
      Vector4 vector4_2 = input.ReadVector4();
      deferredObjectEffect.EmissiveColor = new Vector3(vector4_2.X, vector4_2.Y, vector4_2.Z);
      deferredObjectEffect.AddressModeU = (TextureAddressMode) input.ReadInt32();
      deferredObjectEffect.AddressModeV = (TextureAddressMode) input.ReadInt32();
      deferredObjectEffect.AddressModeW = (TextureAddressMode) input.ReadInt32();
      BlockUtil.SkipBlock(input);
      if (input.ReadInt32() != 1234)
        throw new Exception("Error loading asset.");
      return deferredObjectEffect;
    }
  }
}
