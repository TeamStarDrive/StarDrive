using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;

namespace Ship_Game
{
    /// <summary>
    /// Helper class for GameContentManager
    /// Allows loading FBX, OBJ and PNG files instead of .XNB content
    /// </summary>
    public class RawContentLoader
    {
        private GameContentManager    Content;
        private GraphicsDeviceManager GraphicsManager;

        public RawContentLoader(GameContentManager content)
        {
            Content         = content;
            GraphicsManager = content.Manager;
        }

        private Texture2D LoadTexture(string textureName)
        {
            if (textureName.IsEmpty())
                return null;

            int width = 16;
            int height = 16;
            int mipLevels = 7;
            var texture = new Texture2D(GraphicsManager.GraphicsDevice, width, height, mipLevels, 
                                        TextureUsage.AutoGenerateMipMap|TextureUsage.Linear, SurfaceFormat.Bgr24);

            byte[] bytes = new byte[width*height];
            texture.SetData(bytes, 0, bytes.Length, SetDataOptions.None);

            //texture.Save("output.png", ImageFileFormat.Png);

            return null;
        }

        public T LoadAsset<T>(string fileNameWithExt, string fileExtension)
        {
            Log.Info(ConsoleColor.Magenta, "RawContent LoadAsset: {0}", fileNameWithExt);
            return default(T);
        }

        public LightingEffect CreateLightingEffect(GraphicsDevice device)
        {
            var fx = new LightingEffect(device);
            fx.MaterialName             = "shipMatName";
            fx.MaterialFile             = "material.mat";
            fx.ProjectFile              = "Content.contentproj";
            fx.DiffuseMapFile           = "diffuse.tga";
            fx.DiffuseAmbientMapFile    = "";
            fx.EmissiveMapFile          = "";
            fx.NormalMapFile            = "normal.tga";
            fx.SpecularColorMapFile     = "specular.tga";
            fx.ParallaxMapFile          = "";
            fx.DiffuseMapTexture        = LoadTexture(fx.DiffuseMapFile);
            fx.DiffuseAmbientMapTexture = LoadTexture(fx.DiffuseAmbientMapFile);
            fx.EmissiveMapTexture       = LoadTexture(fx.EmissiveMapFile);
            fx.NormalMapTexture         = LoadTexture(fx.NormalMapFile);
            fx.SpecularColorMapTexture  = LoadTexture(fx.SpecularColorMapFile);
            fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, LoadTexture(fx.ParallaxMapFile));
            fx.Skinned                  = false;
            fx.DoubleSided              = false;
            float transparency = 1.0f;
            fx.SetTransparencyModeAndMap(TransparencyMode.None, transparency, fx.DiffuseMapTexture);
            fx.SpecularPower                 = 14.0f;
            fx.SpecularAmount                = 6.0f;
            fx.FresnelReflectBias            = 0.0f;
            fx.FresnelReflectOffset          = 0.0f;
            fx.FresnelMicrofacetDistribution = 0.0f;
            fx.ParallaxScale                 = 0.0f;
            fx.ParallaxOffset                = 0.0f;
            fx.DiffuseColor  = new Vector3(1f, 1f, 1f);
            fx.EmissiveColor = new Vector3(0f, 0f, 0f);
            fx.AddressModeU  = TextureAddressMode.Wrap;
            fx.AddressModeV  = TextureAddressMode.Wrap;
            fx.AddressModeW  = TextureAddressMode.Wrap;
            return fx;
        }
    }
}
