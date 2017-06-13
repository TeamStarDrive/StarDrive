using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ns3;
using SgMotion;
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
        private readonly GameContentManager    Content;
        private readonly GraphicsDeviceManager GraphicsManager;

        public RawContentLoader(GameContentManager content)
        {
            Content         = content;
            GraphicsManager = content.Manager;
        }

        public object LoadAsset(string fileNameWithExt, string ext)
        {
            if (ext == "fbx" || ext == "obj")
            {
                Log.Info(ConsoleColor.Magenta, "Raw LoadMesh: {0}", fileNameWithExt);
                return StaticMeshFromFile(fileNameWithExt);
            }

            Log.Info(ConsoleColor.Magenta, "Raw LoadTexture: {0}", fileNameWithExt);
            return Texture2D.FromFile(GraphicsManager.GraphicsDevice, fileNameWithExt);
        }

        private SkinnedModel SkinnedMeshFromFile(string modelName)
        {
            return null;
        }

        private ModelMesh StaticMeshFromFile(string modelName)
        {
            return null;
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
            fx.DiffuseMapTexture        = Content.Load<Texture2D>(fx.DiffuseMapFile);
            fx.DiffuseAmbientMapTexture = Content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
            fx.EmissiveMapTexture       = Content.Load<Texture2D>(fx.EmissiveMapFile);
            fx.NormalMapTexture         = Content.Load<Texture2D>(fx.NormalMapFile);
            fx.SpecularColorMapTexture  = Content.Load<Texture2D>(fx.SpecularColorMapFile);
            fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, Content.Load<Texture2D>(fx.ParallaxMapFile));
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
