// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Processors.TerrainMaterialReader_Pro
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Effects;
using SynapseGaming.LightingSystem.Effects.Deferred;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;
using System;

namespace SynapseGaming.LightingSystem.Processors
{
  /// <summary />
  public class TerrainMaterialReader_Pro : ContentTypeReader<MeshData>
  {
    /// <summary />
    protected override MeshData Read(ContentReader input, MeshData instance)
    {
      IGraphicsDeviceService service = (IGraphicsDeviceService) input.ContentManager.ServiceProvider.GetService(typeof (IGraphicsDeviceService));
      MeshData meshData = new MeshData();
      BaseTerrainEffect baseTerrainEffect = !input.ReadBoolean() ? (BaseTerrainEffect) new TerrainEffect(service.GraphicsDevice) : (BaseTerrainEffect) new DeferredTerrainEffect(service.GraphicsDevice);
      meshData.Effect = (Effect) baseTerrainEffect;
      meshData.InfiniteBounds = true;
      meshData.MeshToObject = input.ReadMatrix();
      meshData.PrimitiveCount = input.ReadInt32();
      meshData.VertexCount = input.ReadInt32();
      meshData.VertexStride = input.ReadInt32();
      baseTerrainEffect.MeshSegments = input.ReadInt32();
      meshData.ObjectSpaceBoundingSphere = input.ReadObject<BoundingSphere>();
      meshData.VertexDeclaration = input.ReadObject<VertexDeclaration>();
      meshData.VertexBuffer = input.ReadObject<VertexBuffer>();
      meshData.IndexBuffer = input.ReadObject<IndexBuffer>();
      baseTerrainEffect.MaterialName = input.ReadString();
      baseTerrainEffect.MaterialFile = input.ReadString();
      baseTerrainEffect.ProjectFile = input.ReadString();
      baseTerrainEffect.NormalMapStrength = input.ReadSingle();
      baseTerrainEffect.DiffuseScale = input.ReadSingle();
      baseTerrainEffect.HeightScale = input.ReadSingle();
      baseTerrainEffect.Tiling = input.ReadSingle();
      baseTerrainEffect.SpecularPower = input.ReadSingle();
      baseTerrainEffect.SpecularAmount = input.ReadSingle();
      Vector4 vector4 = input.ReadVector4();
      baseTerrainEffect.SpecularColor = new Vector3(vector4.X, vector4.Y, vector4.Z);
      baseTerrainEffect.DiffuseMapLayer1File = input.ReadString();
      baseTerrainEffect.DiffuseMapLayer1Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.DiffuseMapLayer2File = input.ReadString();
      baseTerrainEffect.DiffuseMapLayer2Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.DiffuseMapLayer3File = input.ReadString();
      baseTerrainEffect.DiffuseMapLayer3Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.DiffuseMapLayer4File = input.ReadString();
      baseTerrainEffect.DiffuseMapLayer4Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.NormalMapLayer1File = input.ReadString();
      baseTerrainEffect.NormalMapLayer1Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.NormalMapLayer2File = input.ReadString();
      baseTerrainEffect.NormalMapLayer2Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.NormalMapLayer3File = input.ReadString();
      baseTerrainEffect.NormalMapLayer3Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.NormalMapLayer4File = input.ReadString();
      baseTerrainEffect.NormalMapLayer4Texture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.BlendMapFile = input.ReadString();
      baseTerrainEffect.BlendMapTexture = input.ReadExternalReference<Texture2D>();
      baseTerrainEffect.HeightMapFile = input.ReadString();
      baseTerrainEffect.HeightMapTexture = CoreUtils.smethod_28(service.GraphicsDevice, input.ReadExternalReference<Texture2D>());
      Class55.smethod_0(input);
      if (input.ReadInt32() != 1234)
        throw new Exception("Error loading asset.");
      return meshData;
    }
  }
}
