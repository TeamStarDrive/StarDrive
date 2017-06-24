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
        private readonly GameContentManager Content;

        // This must be lazy init, because content manager is instantianted before
        // graphics device is initialized
        public GraphicsDevice Device => Content.Manager.GraphicsDevice;

        public RawContentLoader(GameContentManager content)
        {
            Content = content;
        }

        public static bool IsSupportedMeshExtension(string extension)
        {
            if (extension.Empty())
                return false;
            if (extension[0] == '.')
                return extension == ".fbx" || extension == ".obj";
            return extension == "fbx" || extension == "obj";
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
            return Texture2D.FromFile(Device, fileNameWithExt);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct SDMesh
        {
            public readonly CStrView Name;
            public readonly int NumGroups;
            public readonly int NumFaces;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct SDMaterial
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
        private struct SDVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Coords;
            public Vector3 Tangent;
            public Vector3 Binormal;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private unsafe struct SDMeshGroup
        {
            public readonly int GroupId;
            public readonly CStrView Name;
            public readonly SDMaterial Mat;
            public readonly int NumTriangles;
            public readonly int NumVertices;
            public readonly int NumIndices;
            public readonly SDVertex* Vertices;
            public readonly ushort*   Indices;
            public readonly BoundingSphere Bounds;

            public IndexBuffer CopyIndices(GraphicsDevice device)
            {
                ushort* src = Indices;
                var dst = new ushort[NumIndices];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new IndexBuffer(device, sizeof(ushort)*NumIndices, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                buf.SetData(dst);
                return buf;
            }

            public VertexBuffer CopyVertices(GraphicsDevice device)
            {
                SDVertex* src = Vertices;
                var dst = new SDVertex[NumVertices];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new VertexBuffer(device, sizeof(SDVertex)*NumVertices, BufferUsage.WriteOnly);
                buf.SetData(dst);
                return buf;
            }

            public LightingEffect CreateMaterialEffect(GraphicsDevice device, GameContentManager content)
            {
                var fx = new LightingEffect(device);
                fx.MaterialName             = Mat.Name.AsString;
                fx.MaterialFile             = Mat.MaterialFile.AsString;
                fx.ProjectFile              = "Ship_Game/Data/RawContentLoader.cs";
                fx.DiffuseMapFile           = Mat.DiffusePath.AsString;
                fx.EmissiveMapFile          = Mat.EmissivePath.AsString;
                fx.NormalMapFile            = Mat.NormalPath.AsString;
                fx.SpecularColorMapFile     = Mat.SpecularPath.AsString;
                fx.DiffuseAmbientMapFile    = "";
                fx.ParallaxMapFile          = "";
                if (fx.DiffuseMapFile.NotEmpty())        fx.DiffuseMapTexture        = content.Load<Texture2D>(fx.DiffuseMapFile);
                if (fx.EmissiveMapFile.NotEmpty())       fx.EmissiveMapTexture       = content.Load<Texture2D>(fx.EmissiveMapFile);
                if (fx.NormalMapFile.NotEmpty())         fx.NormalMapTexture         = content.Load<Texture2D>(fx.NormalMapFile);
                if (fx.SpecularColorMapFile.NotEmpty())  fx.SpecularColorMapTexture  = content.Load<Texture2D>(fx.SpecularColorMapFile);
                //if (fx.DiffuseAmbientMapFile.NotEmpty()) fx.DiffuseAmbientMapTexture = content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
                //if (fx.ParallaxMapFile.NotEmpty())       fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, content.Load<Texture2D>(fx.ParallaxMapFile));
                fx.Skinned         = false;
                fx.DoubleSided     = false;

                Texture2D alphaMap = Mat.AlphaPath.NotEmpty
                    ? content.Load<Texture2D>(Mat.AlphaPath.AsString)
                    : fx.DiffuseMapTexture;

                float transparency = 1.0f - Mat.Alpha;
                fx.SetTransparencyModeAndMap(TransparencyMode.None, transparency, alphaMap);

                fx.SpecularPower                 = 14.0f * Mat.Specular;
                fx.SpecularAmount                = 6.0f;
                fx.FresnelReflectBias            = 0.0f;
                fx.FresnelReflectOffset          = 0.0f;
                fx.FresnelMicrofacetDistribution = 0.0f;
                fx.ParallaxScale                 = 0.0f;
                fx.ParallaxOffset                = 0.0f;
                fx.DiffuseColor  = Mat.DiffuseColor;
                fx.EmissiveColor = Mat.EmissiveColor;
                fx.AddressModeU  = TextureAddressMode.Wrap;
                fx.AddressModeV  = TextureAddressMode.Wrap;
                fx.AddressModeW  = TextureAddressMode.Wrap;
                return fx;
            }

        }

        [DllImport("SDNative.dll")] private static extern unsafe SDMesh* SDMeshOpen([MarshalAs(UnmanagedType.LPWStr)] string filename);
        [DllImport("SDNative.dll")] private static extern unsafe void SDMeshClose(SDMesh* mesh);
        [DllImport("SDNative.dll")] private static extern unsafe SDMeshGroup* SDMeshGetGroup(SDMesh* mesh, int groupId);

        private static VertexDeclaration Layout;
        private static VertexDeclaration VertexLayout(GraphicsDevice device)
        {
            if (Layout == null)
            {
                Layout = new VertexDeclaration(device, new[]
                {
                    new VertexElement(0, 0,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                    new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                    new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                    new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                    new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0),
                });
            }
            return Layout;
        }

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

            GraphicsDevice device = Device;
            var staticMesh = new StaticMesh { Name = sdmesh->Name.AsString };

            for (int i = 0; i < sdmesh->NumGroups; ++i)
            {
                SDMeshGroup* g = SDMeshGetGroup(sdmesh, i);
                if (g->NumVertices == 0 || g->NumIndices == 0)
                    continue;

                Log.Info(ConsoleColor.Green, $"  group {g->GroupId}: {g->Name}  verts:{g->NumVertices}  ids:{g->NumIndices}");

                var meshData = new MeshData
                {
                    Name                      = g->Name.AsString,
                    Effect                    = g->CreateMaterialEffect(device, Content),
                    ObjectSpaceBoundingSphere = g->Bounds,
                    IndexBuffer               = g->CopyIndices(device),
                    VertexBuffer              = g->CopyVertices(device),
                    VertexDeclaration         = VertexLayout(device),
                    PrimitiveCount            = g->NumTriangles,
                    VertexCount               = g->NumVertices,
                    VertexStride              = sizeof(SDVertex)
                };
                staticMesh.Meshes.Add(meshData);
            }

            SDMeshClose(sdmesh);
            return staticMesh;
        }

        
        private SkinnedModel SkinnedMeshFromFile(string modelName)
        {
            return null;
        }

    }
}
