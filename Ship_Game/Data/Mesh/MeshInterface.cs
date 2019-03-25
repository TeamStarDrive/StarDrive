using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;

// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local

namespace Ship_Game.Data.Mesh
{
    public abstract class MeshInterface
    {
        protected readonly GameContentManager Content;

        // This must be lazy init, because content manager is instantiated before
        // graphics device is initialized
        protected GraphicsDevice Device => Content.Manager.GraphicsDevice;

        protected MeshInterface(GameContentManager content)
        {
            Content = content;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdMesh
        {
            public readonly CStrView Name;
            public readonly int NumGroups;
            public readonly int NumFaces;
            public readonly int NumMaterials;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdMaterial
        {
            public readonly CStrView Name; // name of the material instance
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
        protected struct SdVertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 Coords;
            public Vector3 Tangent;
            public Vector3 BiNormal;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected unsafe struct SdMeshGroup
        {
            public readonly int GroupId;
            public readonly CStrView Name;
            public readonly SdMaterial* Mat;
            public readonly int NumTriangles;
            public readonly int NumVertices;
            public readonly int NumIndices;
            public readonly SdVertex* Vertices;
            public readonly ushort*   Indices;
            public readonly BoundingSphere Bounds;
            public readonly Matrix Transform;

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
                SdVertex* src = Vertices;
                var dst = new SdVertex[NumVertices];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new VertexBuffer(device, sizeof(SdVertex)*NumVertices, BufferUsage.WriteOnly);
                buf.SetData(dst);
                return buf;
            }
        }

        protected static unsafe LightingEffect CreateMaterialEffect(
            SdMaterial* mat, GraphicsDevice device, GameContentManager content, string materialFile)
        {
            var fx = new LightingEffect(device);
            fx.MaterialName          = mat->Name.AsString;
            fx.MaterialFile          = materialFile;
            fx.ProjectFile           = "Ship_Game/Data/RawContentLoader.cs";
            fx.DiffuseMapFile        = mat->DiffusePath.AsString;
            fx.EmissiveMapFile       = mat->EmissivePath.AsString;
            fx.NormalMapFile         = mat->NormalPath.AsString;
            fx.SpecularColorMapFile  = mat->SpecularPath.AsString;
            fx.DiffuseAmbientMapFile = "";
            fx.ParallaxMapFile       = "";
            if (fx.DiffuseMapFile.NotEmpty())        fx.DiffuseMapTexture        = content.Load<Texture2D>(fx.DiffuseMapFile);
            if (fx.EmissiveMapFile.NotEmpty())       fx.EmissiveMapTexture       = content.Load<Texture2D>(fx.EmissiveMapFile);
            if (fx.NormalMapFile.NotEmpty())         fx.NormalMapTexture         = content.Load<Texture2D>(fx.NormalMapFile);
            if (fx.SpecularColorMapFile.NotEmpty())  fx.SpecularColorMapTexture  = content.Load<Texture2D>(fx.SpecularColorMapFile);
            //if (fx.DiffuseAmbientMapFile.NotEmpty()) fx.DiffuseAmbientMapTexture = content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
            //if (fx.ParallaxMapFile.NotEmpty())       fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, content.Load<Texture2D>(fx.ParallaxMapFile));
            fx.Skinned         = false;
            fx.DoubleSided     = false;

            Texture2D alphaMap = mat->AlphaPath.NotEmpty
                ? content.Load<Texture2D>(mat->AlphaPath.AsString)
                : fx.DiffuseMapTexture;

            fx.SetTransparencyModeAndMap(TransparencyMode.None, mat->Alpha, alphaMap);
            fx.SpecularPower                 = 14.0f * mat->Specular;
            fx.SpecularAmount                = 6.0f * mat->Specular;
            fx.FresnelReflectBias            = 0.0f;
            fx.FresnelReflectOffset          = 0.0f;
            fx.FresnelMicrofacetDistribution = 0.0f;
            fx.ParallaxScale                 = 0.0f;
            fx.ParallaxOffset                = 0.0f;
            fx.DiffuseColor  = mat->DiffuseColor;
            //fx.EmissiveColor = mat->EmissiveColor;
            fx.AddressModeU  = TextureAddressMode.Wrap;
            fx.AddressModeV  = TextureAddressMode.Wrap;
            fx.AddressModeW  = TextureAddressMode.Wrap;
            return fx;
        }


        [DllImport("SDNative.dll")] protected static extern unsafe
            SdMesh* SDMeshOpen([MarshalAs(UnmanagedType.LPWStr)] string fileName);

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshClose(SdMesh* mesh);

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdMeshGroup* SDMeshGetGroup(SdMesh* mesh, int groupId);

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdMesh* SDMeshCreateEmpty([MarshalAs(UnmanagedType.LPWStr)] string meshName);

        [DllImport("SDNative.dll")] protected static extern unsafe
            bool SDMeshSave(SdMesh* mesh, [MarshalAs(UnmanagedType.LPWStr)] string fileName);

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdMeshGroup* SDMeshNewGroup(SdMesh* mesh, 
                [MarshalAs(UnmanagedType.LPWStr)] string groupName,
                Matrix* transform);

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshGroupSetData(SdMeshGroup* group, 
                Vector3* vertices, Vector3* normals, Vector2* coords, 
                int numVertices, ushort* indices, int numIndices);

        [DllImport("SDNative.dll")] protected static extern unsafe 
            SdMaterial* SDMeshCreateMaterial(SdMesh* mesh, 
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                [MarshalAs(UnmanagedType.LPWStr)] string diffusePath,
                [MarshalAs(UnmanagedType.LPWStr)] string alphaPath,
                [MarshalAs(UnmanagedType.LPWStr)] string specularPath,
                [MarshalAs(UnmanagedType.LPWStr)] string normalPath,
                [MarshalAs(UnmanagedType.LPWStr)] string emissivePath,
                Vector3 ambientColor,
                Vector3 diffuseColor,
                Vector3 specularColor,
                Vector3 emissiveColor,
                float specular,
                float alpha);

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshGroupSetMaterial(SdMeshGroup* group, SdMaterial* material);


        protected static VertexDeclaration Layout;
        protected static VertexDeclaration VertexLayout(GraphicsDevice device)
        {
            return Layout ?? (Layout = new VertexDeclaration(device, new[]
            {
                new VertexElement(0, 0,  VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Position, 0),
                new VertexElement(0, 12, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Normal, 0),
                new VertexElement(0, 24, VertexElementFormat.Vector2, VertexElementMethod.Default, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(0, 32, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Tangent, 0),
                new VertexElement(0, 44, VertexElementFormat.Vector3, VertexElementMethod.Default, VertexElementUsage.Binormal, 0)
            }));
        }
    }
}
