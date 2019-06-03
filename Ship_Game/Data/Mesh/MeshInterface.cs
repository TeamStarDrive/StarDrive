using System;
using System.Diagnostics;
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

            public readonly int NumModelBones;
            public readonly int NumSkinnedBones;
            public readonly int NumAnimClips;
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
        [DebuggerDisplay("Offset={Offset} Size={Size} Format={Format} Usage={Usage}")]
        protected struct SdVertexElement
        {
            public byte Offset; // element offset in vertex buffer data
            public byte Size;   // element size in bytes
            public VertexElementFormat Format;
            public VertexElementUsage  Usage;
        };
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected unsafe struct SdVertexData
        {
            public int VertexStride;
            public int LayoutCount;
            public int IndexCount;
            public int VertexCount;
            public SdVertexElement* Layout;
            public ushort* IndexData;
            public byte* VertexData;

            public IndexBuffer CopyIndices(GraphicsDevice device)
            {
                ushort* src = IndexData;
                var dst = new ushort[IndexCount];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new IndexBuffer(device, sizeof(ushort)*IndexCount, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                buf.SetData(dst);
                return buf;
            }

            public VertexBuffer CopyVertices(GraphicsDevice device)
            {
                byte* src = VertexData;
                var dst = new byte[VertexStride*VertexCount];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new VertexBuffer(device, dst.Length, BufferUsage.WriteOnly);
                buf.SetData(dst);
                return buf;
            }

            public VertexDeclaration CreateDeclaration(GraphicsDevice device)
            {
                var elements = new Array<VertexElement>();
                for (int i = 0; i < LayoutCount; ++i)
                {
                    var e = new VertexElement(0, Layout[i].Offset, Layout[i].Format,
                        VertexElementMethod.Default, Layout[i].Usage, 0);
                    elements.Add(e);
                }
                return new VertexDeclaration(device, elements.ToArray());
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected unsafe struct SdMeshGroup
        {
            public readonly int GroupId;
            public readonly CStrView Name;
            public readonly SdMaterial* Mat;
            public readonly BoundingSphere Bounds;
            public readonly Matrix Transform;
        }

        /////////////////////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////////////////////

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshGroupSetData(SdMeshGroup* group, SdVertexData vertexData);
        
        [DllImport("SDNative.dll")] protected static extern unsafe
            SdVertexData SDMeshGroupGetData(SdMeshGroup* group);

        /////////////////////////////////////////////////////////////////////////////

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

        /////////////////////////////////////////////////////////////////////////////
            
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdBonePose
        {
            public Vector3 Translation;
            public Quaternion Orientation;
            public Vector3 Scale;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        protected struct SdModelBone
        {
            public CStrView Name;
            public int BoneIndex;
            public int ParentBone;
            public Matrix Transform;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        protected struct SdSkinnedBone
        {
            public CStrView Name;
            public int BoneIndex;
            public int ParentBone;
            public SdBonePose BindPose;
            public Matrix InverseBindPoseTransform;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdAnimationKeyFrame
        {
            public float Time;
            public SdBonePose Pose;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected unsafe struct SdBoneAnimation
        {
            public readonly int BoneIndex;
            public readonly int NumKeyFrames;
            public readonly SdAnimationKeyFrame* KeyFrames;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected unsafe struct SdAnimationClip
        {
            public CStrView Name;
            public float Duration;
            public readonly int NumAnimations;
            public readonly SdBoneAnimation* Animations;
        };

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshAddBone(SdMesh* mesh,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                int boneIndex,
                int parentBone,
                in Matrix transform
            );

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshAddSkinnedBone(SdMesh* mesh,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                int boneIndex,
                int parentBone,
                in SdBonePose bindPose,
                in Matrix inverseBindPoseTransform
            );

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdAnimationClip* SDMeshCreateAnimationClip(SdMesh* mesh,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                float duration
            );

        [DllImport("SDNative.dll")]
        protected static extern unsafe
            SdBoneAnimation* SDMeshAddBoneAnimation(SdAnimationClip* clip,
                int skinnedBoneIndex
            );

        [DllImport("SDNative.dll")]
        protected static extern unsafe
            void SDMeshAddAnimationKeyFrame(SdBoneAnimation* anim, 
                in SdAnimationKeyFrame keyFrame
            );

        /////////////////////////////////////////////////////////////////////////////

        protected static SdVertexElement[] CreateVertexElements(VertexDeclaration vd)
        {
            VertexElement[] vertexElements = vd.GetVertexElements();
            var elements = new SdVertexElement[vertexElements.Length];
            for (int i = 0; i < vertexElements.Length; ++i)
            {
                VertexElement e = vertexElements[i];
                elements[i] = new SdVertexElement
                {
                    Offset = (byte)e.Offset,
                    Size   = (byte)ElementSizeInBytes(e.VertexElementFormat),
                    Format = e.VertexElementFormat,
                    Usage  = e.VertexElementUsage
                };
            }
            return elements;
        }

        protected static int ElementSizeInBytes(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Single:  return sizeof(float);
                case VertexElementFormat.Vector2: return sizeof(float)*2;
                case VertexElementFormat.Vector3: return sizeof(float)*3;
                case VertexElementFormat.Vector4: return sizeof(float)*4;
                case VertexElementFormat.Color:   return sizeof(int); // packed color RGBA
                case VertexElementFormat.Byte4:   return 4;
                case VertexElementFormat.Short2:  return 4;
                case VertexElementFormat.Short4:  return 8;
                case VertexElementFormat.Rgba32:  return 4;
            }
            return 4;
        }

        /// <summary>
        /// Generates tangent space data (used for bump and specular mapping) from the provided vertex information.
        /// </summary>
        /// <param name="triangleIndices">Indices that describe a list of triangles to generate tangent space
        /// information for.  WARNING: this method requires triangle lists (not fans or strips).</param>
        /// <param name="vertices">Array of vertices.</param>
        public static void BuildTangentSpaceDataForTriangleList(
              short[] triangleIndices, VertexPositionNormalTextureBump[] vertices)
        {
            for (int i = 0; i < triangleIndices.Length; i += 3)
            {
                int in0 = triangleIndices[i];
                int in1 = triangleIndices[i + 1];
                int in2 = triangleIndices[i + 2];
                Vector2 uv0 = vertices[in0].TextureCoordinate;
                Vector2 uv1 = vertices[in1].TextureCoordinate;
                Vector2 uv2 = vertices[in2].TextureCoordinate;
                float s1 = uv1.X - uv0.X;
                float t1 = uv1.Y - uv0.Y;
                float s2 = uv2.X - uv0.X;
                float t2 = uv2.Y - uv0.Y;
                float st = (s1 * t2 - s2 * t1);
                if (st != 0.0f)
                {
                    float tmp = 1f / st;
                    Vector3 p0 = vertices[in0].Position;
                    Vector3 p1 = vertices[in1].Position;
                    Vector3 p2 = vertices[in2].Position;
                    float Px = p1.X - p0.X;
                    float Py = p1.Y - p0.Y;
                    float Pz = p1.Z - p0.Z;
                    float Qx = p2.X - p0.X;
                    float Qy = p2.Y - p0.Y;
                    float Qz = p2.Z - p0.Z;
                    var tangent = new Vector3(
                        (t2 * Px - t1 * Qx) * tmp,
                        (t2 * Py - t1 * Qy) * tmp,
                        (t2 * Pz - t1 * Qz) * tmp);
                    var biNormal = new Vector3(
                        (s1 * Qx - s2 * Px) * tmp,
                        (s1 * Qy - s2 * Py) * tmp,
                        (s1 * Qz - s2 * Pz) * tmp);
                    vertices[in0].Tangent += tangent;
                    vertices[in1].Tangent += tangent;
                    vertices[in2].Tangent += tangent;
                    vertices[in0].Binormal += biNormal;
                    vertices[in1].Binormal += biNormal;
                    vertices[in2].Binormal += biNormal;
                }
            }
            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i].Tangent  = Vector3.Normalize(vertices[i].Tangent);
                vertices[i].Binormal = Vector3.Normalize(vertices[i].Binormal);
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

    }
}
