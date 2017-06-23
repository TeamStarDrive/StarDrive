using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SgMotion;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using SynapseGaming.LightingSystem.Rendering;

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

        public static bool IsSupportedMeshExtension(string extension)
        {
            return extension.NotEmpty() && (extension == "fbx" || extension == "obj");
        }

        public static bool IsSupportedMesh(string modelNameWithExtension)
        {
            return IsSupportedMeshExtension(Path.GetExtension(modelNameWithExtension));
        }

        public object LoadAsset(string fileNameWithExt, string ext)
        {
            if (IsSupportedMeshExtension(ext))
            {
                Log.Info(ConsoleColor.Magenta, "Raw LoadMesh: {0}", fileNameWithExt);
                return StaticMeshFromFile(fileNameWithExt);
            }
            Log.Info(ConsoleColor.Magenta, "Raw LoadTexture: {0}", fileNameWithExt);
            return Texture2D.FromFile(GraphicsManager.GraphicsDevice, fileNameWithExt);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private unsafe struct SDMesh
        {
            public readonly CStrView Name;
            public readonly int NumGroups;
            public readonly int NumFaces;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private unsafe struct SDMaterial
        {
            public readonly CStrView Name; // name of the material instance
            public readonly CStrView MaterialFile;
            public readonly CStrView DiffusePath;
            public readonly CStrView AlphaPath;
            public readonly CStrView SpecularPath;
            public readonly CStrView NormalPath;
            public readonly CStrView EmissivePath;
            public readonly Vector3 AmbientColor;
            public readonly Vector3 DiffuseColor;
            public readonly Vector3 SpecularColor;
            public readonly Vector3 EmissiveColor;
            public readonly float Specular;
            public readonly float Alpha;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private unsafe struct SDMeshGroup
        {
            public readonly int GroupId;
            public readonly CStrView Name;
            public readonly SDMaterial Mat;
            public readonly int NumVertices;
            public readonly int NumIndices;
            public readonly SDVertex* Vertices;
            public readonly ushort*   Indices;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private unsafe struct SDVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Coords;
            public Vector3 Tangent;
            public Vector3 Binormal;
        }

        [DllImport("SDNative.dll")] private static extern unsafe SDMesh* SDMeshOpen([MarshalAs(UnmanagedType.LPWStr)] string filename);
        [DllImport("SDNative.dll")] private static extern unsafe void SDMeshClose(SDMesh* mesh);
        [DllImport("SDNative.dll")] private static extern unsafe SDMeshGroup* SDMeshGetGroup(SDMesh* mesh, int groupId);

        private static string GetContentPath(string modelName)
        {
            if (modelName.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase)) 
            {
                if (File.Exists(modelName))
                    return modelName;
            }
            else if (GlobalStats.HasMod)
            {
                string modPath = GlobalStats.ModPath + modelName;
                if (File.Exists(modPath)) return modPath;
            }
            return "Content/" + modelName;
        }
        private unsafe StaticMesh StaticMeshFromFile(string modelName)
        {
            string meshPath = GetContentPath(modelName);

            SDMesh* sdmesh = SDMeshOpen(meshPath);
            if (sdmesh == null)
                return null;

            Log.Info(ConsoleColor.Green, $"SDStaticMesh {sdmesh->Name} | faces:{sdmesh->NumFaces} | groups:{sdmesh->NumGroups}");

            var staticMesh = new StaticMesh { Name = sdmesh->Name.AsString };

            for (int i = 0; i < sdmesh->NumGroups; ++i)
            {
                SDMeshGroup* g = SDMeshGetGroup(sdmesh, i);
                if (g->NumVertices == 0 || g->NumIndices == 0)
                    continue;

                Log.Info(ConsoleColor.Green, $"  group {g->GroupId}: {g->Name}  verts:{g->NumVertices}  ids:{g->NumIndices}");

                var meshData = new MeshData();

                meshData.Name   = g->Name.AsString;
                meshData.Effect = CreateLightingEffect(g);

                staticMesh.Meshes.Add(meshData);
            }

            SDMeshClose(sdmesh);
            return staticMesh;
        }

        
        private SkinnedModel SkinnedMeshFromFile(string modelName)
        {
            return null;
        }


        private unsafe LightingEffect CreateLightingEffect(SDMeshGroup* g)
        {
            GraphicsDevice device = GraphicsManager.GraphicsDevice;
            var fx = new LightingEffect(device);
            fx.MaterialName             = g->Mat.Name.AsString;
            fx.MaterialFile             = g->Mat.MaterialFile.AsString;
            fx.ProjectFile              = "Ship_Game/Data/RawContentLoader.cs";
            fx.DiffuseMapFile           = g->Mat.DiffusePath.AsString;
            fx.EmissiveMapFile          = g->Mat.EmissivePath.AsString;
            fx.NormalMapFile            = g->Mat.NormalPath.AsString;
            fx.SpecularColorMapFile     = g->Mat.SpecularPath.AsString;
            fx.DiffuseAmbientMapFile    = "";
            fx.ParallaxMapFile          = "";
            if (fx.DiffuseMapFile.NotEmpty())        fx.DiffuseMapTexture        = Content.Load<Texture2D>(fx.DiffuseMapFile);
            if (fx.EmissiveMapFile.NotEmpty())       fx.EmissiveMapTexture       = Content.Load<Texture2D>(fx.EmissiveMapFile);
            if (fx.NormalMapFile.NotEmpty())         fx.NormalMapTexture         = Content.Load<Texture2D>(fx.NormalMapFile);
            if (fx.SpecularColorMapFile.NotEmpty())  fx.SpecularColorMapTexture  = Content.Load<Texture2D>(fx.SpecularColorMapFile);
            //if (fx.DiffuseAmbientMapFile.NotEmpty()) fx.DiffuseAmbientMapTexture = Content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
            //if (fx.ParallaxMapFile.NotEmpty())       fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, Content.Load<Texture2D>(fx.ParallaxMapFile));
            fx.Skinned         = false;
            fx.DoubleSided     = false;
            float transparency = 1.0f - g->Mat.Alpha;
            fx.SetTransparencyModeAndMap(TransparencyMode.None, transparency, fx.DiffuseMapTexture);
            fx.SpecularPower                 = 14.0f * g->Mat.Specular;
            fx.SpecularAmount                = 6.0f;
            fx.FresnelReflectBias            = 0.0f;
            fx.FresnelReflectOffset          = 0.0f;
            fx.FresnelMicrofacetDistribution = 0.0f;
            fx.ParallaxScale                 = 0.0f;
            fx.ParallaxOffset                = 0.0f;
            fx.DiffuseColor  = g->Mat.DiffuseColor;
            fx.EmissiveColor = g->Mat.EmissiveColor;
            fx.AddressModeU  = TextureAddressMode.Wrap;
            fx.AddressModeV  = TextureAddressMode.Wrap;
            fx.AddressModeW  = TextureAddressMode.Wrap;
            return fx;
        }
    }
}
