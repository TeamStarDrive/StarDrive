using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace SynapseGaming.LightingSystem.Processors.Forward
{
    /// <summary />
    public class LightingMaterialReader_Pro : ContentTypeReader<LightingEffect>
    {
        /// <summary />
        protected override LightingEffect Read(ContentReader input, LightingEffect instance)
        {
            var service = (IGraphicsDeviceService)input.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService));
            Console.WriteLine($"Read LightningEffect {input.AssetName} {instance?.MaterialName}");
            var fx = new LightingEffect(service.GraphicsDevice);
            fx.MaterialName             = input.ReadString();
            fx.MaterialFile             = input.ReadString();
            fx.ProjectFile              = input.ReadString();
            fx.DiffuseMapFile           = input.ReadString();
            fx.DiffuseMapTexture        = input.ReadExternalReference<Texture2D>();
            fx.DiffuseAmbientMapFile    = input.ReadString();
            fx.DiffuseAmbientMapTexture = input.ReadExternalReference<Texture2D>();
            fx.EmissiveMapFile          = input.ReadString();
            fx.EmissiveMapTexture       = input.ReadExternalReference<Texture2D>();
            fx.NormalMapFile            = input.ReadString();
            fx.NormalMapTexture         = input.ReadExternalReference<Texture2D>();
            fx.SpecularColorMapFile     = input.ReadString();
            fx.SpecularColorMapTexture  = input.ReadExternalReference<Texture2D>();
            fx.ParallaxMapFile          = input.ReadString();
            fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(service.GraphicsDevice, input.ReadExternalReference<Texture2D>());
            fx.Skinned                  = input.ReadBoolean();
            fx.DoubleSided              = input.ReadBoolean();
            var mode = (TransparencyMode)input.ReadInt32();
            float transparency = input.ReadSingle();
            fx.SetTransparencyModeAndMap(mode, transparency, fx.DiffuseMapTexture);
            fx.SpecularPower                 = input.ReadSingle();
            fx.SpecularAmount                = input.ReadSingle();
            fx.FresnelReflectBias            = input.ReadSingle();
            fx.FresnelReflectOffset          = input.ReadSingle();
            fx.FresnelMicrofacetDistribution = input.ReadSingle();
            fx.ParallaxScale                 = input.ReadSingle();
            fx.ParallaxOffset                = input.ReadSingle();
            Vector4 diffuse  = input.ReadVector4();
            Vector4 emissive = input.ReadVector4();
            fx.DiffuseColor  = new Vector3(diffuse.X, diffuse.Y, diffuse.Z);
            fx.EmissiveColor = new Vector3(emissive.X, emissive.Y, emissive.Z);
            fx.AddressModeU  = (TextureAddressMode)input.ReadInt32();
            fx.AddressModeV  = (TextureAddressMode)input.ReadInt32();
            fx.AddressModeW  = (TextureAddressMode)input.ReadInt32();
            BlockUtil.SkipBlock(input);
            if (input.ReadInt32() != 1234)
                throw new Exception("Error loading asset.");
            return fx;
        }
    }
}
