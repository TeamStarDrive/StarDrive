using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Effects.Forward;
using SDGraphics;
using SDUtils;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;
using XnaVector3 = Microsoft.Xna.Framework.Vector3;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;
using BoundingSphere = Microsoft.Xna.Framework.BoundingSphere;
#pragma warning disable CA1060, CA1401

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

        [StructLayout(LayoutKind.Sequential)]
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

        [StructLayout(LayoutKind.Sequential)]
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

        // SDNative writes its own SDElementFormat/SDElementUsage byte enums into
        // these slots. The byte values match XNA 3.1's enum ordinals, NOT MonoGame's
        // (MG renumbered both enums entirely — e.g. XNA 3.1 TextureCoordinate=5 vs
        // MG TextureCoordinate=2; XNA 3.1 Sample=13 has no MG equivalent at all and
        // throws "Unknown vertex element usage!" if passed to D3D11). We hold the
        // raw bytes here and translate at SdVertexData.CreateDeclaration().
        [StructLayout(LayoutKind.Sequential)]
        [DebuggerDisplay("Offset={Offset} Size={Size} NativeFormat={NativeFormat} NativeUsage={NativeUsage}")]
        protected struct SdVertexElement
        {
            public byte Offset;       // element offset in vertex buffer data
            public byte Size;         // element size in bytes
            public byte NativeFormat; // SDElementFormat (Single..Rgba32, 0..8)
            public byte NativeUsage;  // SDElementUsage (Position..Sample, 0..13 with gap at 9)
        };
        
        [StructLayout(LayoutKind.Sequential)]
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

                var buf = new IndexBuffer(device, IndexElementSize.SixteenBits, IndexCount, BufferUsage.WriteOnly);
                buf.SetData(dst);
                return buf;
            }

            public VertexBuffer CopyVertices(GraphicsDevice device, VertexDeclaration declaration)
            {
                byte* src = VertexData;
                var dst = new byte[VertexStride*VertexCount];
                for (int i = 0; i < dst.Length; ++i) dst[i] = src[i];

                var buf = new VertexBuffer(device, declaration, VertexCount, BufferUsage.WriteOnly);
                buf.SetData(dst);
                return buf;
            }

            // MonoGame VertexElement ctor is 4-arg (offset, format, usage, usageIndex);
            // XNA 3.1's VertexElementMethod was dropped. VertexDeclaration takes the
            // element array directly — no GraphicsDevice arg.
            //
            // Translates SDNative's XNA-3.1-shaped byte enums to MonoGame's. Skips
            // any element whose usage doesn't map (e.g. SDElementUsage::Sample which
            // is out of MG's enum range entirely) — without translation, the unknown
            // byte propagates to D3D11 and throws "Unknown vertex element usage!"
            // mid-Draw, killing the game.
            public VertexDeclaration CreateDeclaration()
            {
                var elements = new Array<VertexElement>(LayoutCount);
                for (int i = 0; i < LayoutCount; ++i)
                {
                    if (TranslateNativeUsage(Layout[i].NativeUsage, out VertexElementUsage usage)
                     && TranslateNativeFormat(Layout[i].NativeFormat, out VertexElementFormat format))
                    {
                        elements.Add(new VertexElement(Layout[i].Offset, format, usage, 0));
                    }
                    else
                    {
                        Log.Warning($"SdVertexData.CreateDeclaration: dropping element #{i} " +
                                    $"(NativeFormat={Layout[i].NativeFormat} NativeUsage={Layout[i].NativeUsage}) — " +
                                    "no MonoGame equivalent");
                    }
                }
                return new VertexDeclaration(VertexStride, elements.ToArray());
            }
        };

        // SDElementUsage ordinals (SdMeshGroup.h):
        //   0:Position 1:BlendWeight 2:BlendIndices 3:Normal 4:PointSize 5:Coordinate
        //   6:Tangent 7:BiNormal 8:TessellateFactor (gap 9) 10:Color 11:Fog 12:Depth 13:Sample
        //
        // MonoGame VertexElementUsage ordinals (Microsoft.Xna.Framework.Graphics):
        //   0:Position 1:Color 2:TextureCoordinate 3:Normal 4:Binormal 5:Tangent
        //   6:BlendIndices 7:BlendWeight 8:Depth 9:Fog 10:PointSize 11:Sample 12:TessellateFactor
        internal static bool TranslateNativeUsage(byte native, out VertexElementUsage mg)
        {
            switch (native)
            {
                case 0:  mg = VertexElementUsage.Position;          return true;
                case 1:  mg = VertexElementUsage.BlendWeight;       return true;
                case 2:  mg = VertexElementUsage.BlendIndices;      return true;
                case 3:  mg = VertexElementUsage.Normal;            return true;
                case 4:  mg = VertexElementUsage.PointSize;         return true;
                case 5:  mg = VertexElementUsage.TextureCoordinate; return true;
                case 6:  mg = VertexElementUsage.Tangent;           return true;
                case 7:  mg = VertexElementUsage.Binormal;          return true;
                case 8:  mg = VertexElementUsage.TessellateFactor;  return true;
                case 10: mg = VertexElementUsage.Color;             return true;
                case 11: mg = VertexElementUsage.Fog;               return true;
                case 12: mg = VertexElementUsage.Depth;             return true;
                case 13: mg = VertexElementUsage.Sample;            return true;
                default: mg = VertexElementUsage.Position;          return false;
            }
        }

        // SDElementFormat ordinals (SdMeshGroup.h):
        //   0:Single 1:Vector2 2:Vector3 3:Vector4 4:Color 5:Byte4 6:Short2 7:Short4 8:Rgba32
        //
        // MonoGame VertexElementFormat ordinals (Microsoft.Xna.Framework.Graphics):
        //   0:Single 1:Vector2 2:Vector3 3:Vector4 4:Color 5:Byte4 6:Short2 7:Short4
        //   8:NormalizedShort2 9:NormalizedShort4 10:HalfVector2 11:HalfVector4
        // (no Rgba32 — MG removed it; map to Color which is also 4 bytes packed RGBA.)
        internal static bool TranslateNativeFormat(byte native, out VertexElementFormat mg)
        {
            switch (native)
            {
                case 0: mg = VertexElementFormat.Single;  return true;
                case 1: mg = VertexElementFormat.Vector2; return true;
                case 2: mg = VertexElementFormat.Vector3; return true;
                case 3: mg = VertexElementFormat.Vector4; return true;
                case 4: mg = VertexElementFormat.Color;   return true;
                case 5: mg = VertexElementFormat.Byte4;   return true;
                case 6: mg = VertexElementFormat.Short2;  return true;
                case 7: mg = VertexElementFormat.Short4;  return true;
                case 8: mg = VertexElementFormat.Color;   return true; // SDNative::Rgba32 → MG::Color
                default: mg = VertexElementFormat.Single; return false;
            }
        }

        // Inverse of TranslateNativeUsage: MonoGame VertexElementUsage → SDNative XNA-3.1 byte
        // ordinal. Used by the export path (CreateVertexElements). MG enums that have no XNA 3.1
        // counterpart (none in practice — every MG usage maps cleanly back) fall through to 0.
        protected static byte ToNativeUsage(VertexElementUsage mg)
        {
            switch (mg)
            {
                case VertexElementUsage.Position:          return 0;
                case VertexElementUsage.BlendWeight:       return 1;
                case VertexElementUsage.BlendIndices:      return 2;
                case VertexElementUsage.Normal:            return 3;
                case VertexElementUsage.PointSize:         return 4;
                case VertexElementUsage.TextureCoordinate: return 5;
                case VertexElementUsage.Tangent:           return 6;
                case VertexElementUsage.Binormal:          return 7;
                case VertexElementUsage.TessellateFactor:  return 8;
                case VertexElementUsage.Color:             return 10;
                case VertexElementUsage.Fog:               return 11;
                case VertexElementUsage.Depth:             return 12;
                case VertexElementUsage.Sample:            return 13;
                default:                                   return 0;
            }
        }

        // Inverse of TranslateNativeFormat: MonoGame VertexElementFormat → SDNative XNA-3.1 byte
        // ordinal. NormalizedShort2/4 and HalfVector2/4 have no XNA 3.1 counterpart and round to
        // the closest plain Short / Vector. The exporter doesn't expect to see them in baked
        // ship XNBs (XNA 3.1 didn't emit them) but the fallback keeps the byte stream parseable
        // by SDNative if mod content uses them.
        protected static byte ToNativeFormat(VertexElementFormat mg)
        {
            switch (mg)
            {
                case VertexElementFormat.Single:           return 0;
                case VertexElementFormat.Vector2:          return 1;
                case VertexElementFormat.Vector3:          return 2;
                case VertexElementFormat.Vector4:          return 3;
                case VertexElementFormat.Color:            return 4;
                case VertexElementFormat.Byte4:            return 5;
                case VertexElementFormat.Short2:           return 6;
                case VertexElementFormat.Short4:           return 7;
                case VertexElementFormat.NormalizedShort2: return 6; // → Short2 (not exact)
                case VertexElementFormat.NormalizedShort4: return 7; // → Short4 (not exact)
                case VertexElementFormat.HalfVector2:      return 1; // → Vector2 (not exact)
                case VertexElementFormat.HalfVector4:      return 3; // → Vector4 (not exact)
                default:                                   return 0;
            }
        }

        // Bytes occupied by one element of a MonoGame VertexElementFormat. Used by the export
        // path to fill SdVertexElement.Size; consumers on the SDNative side compare this against
        // the per-element format expected.
        protected static int ElementSizeInBytes(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Single:           return 4;
                case VertexElementFormat.Vector2:          return 8;
                case VertexElementFormat.Vector3:          return 12;
                case VertexElementFormat.Vector4:          return 16;
                case VertexElementFormat.Color:            return 4;
                case VertexElementFormat.Byte4:            return 4;
                case VertexElementFormat.Short2:           return 4;
                case VertexElementFormat.Short4:           return 8;
                case VertexElementFormat.NormalizedShort2: return 4;
                case VertexElementFormat.NormalizedShort4: return 8;
                case VertexElementFormat.HalfVector2:      return 4;
                case VertexElementFormat.HalfVector4:      return 8;
                default:                                   return 0;
            }
        }

        // Walks a MonoGame VertexDeclaration and emits SdVertexElement[] with SDNative's
        // XNA-3.1-shaped byte ordinals. This is the export-side analog of
        // SdVertexData.CreateDeclaration, which goes the other direction.
        protected static SdVertexElement[] CreateVertexElements(VertexDeclaration vd)
        {
            VertexElement[] mgElements = vd.GetVertexElements();
            var sd = new SdVertexElement[mgElements.Length];
            for (int i = 0; i < mgElements.Length; ++i)
            {
                VertexElement e = mgElements[i];
                sd[i] = new SdVertexElement
                {
                    Offset       = (byte)e.Offset,
                    Size         = (byte)ElementSizeInBytes(e.VertexElementFormat),
                    NativeFormat = ToNativeFormat(e.VertexElementFormat),
                    NativeUsage  = ToNativeUsage(e.VertexElementUsage)
                };
            }
            return sd;
        }

        [StructLayout(LayoutKind.Sequential)]
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
            public XnaQuaternion Orientation;
            public Vector3 Scale;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        protected struct SdModelBone
        {
            public CStrView Name;
            public int BoneIndex;
            public int ParentBone;
            public Matrix Transform;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
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
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdBoneAnimation
        {
            public readonly int Id;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdAnimationClip
        {
            public readonly int Id;
        }

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshAddBone(SdMesh* mesh,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                int boneIndex,
                int parentBone,
                in Matrix transform
            );

        [DllImport("SDNative.dll")] protected static extern unsafe
            void SDMeshAddBoneTRS(SdMesh* mesh,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                int boneIndex,
                int parentBone,
                in SdBonePose bindPose
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
            SdAnimationClip SDMeshCreateAnimationClip(SdMesh* mesh,
                [MarshalAs(UnmanagedType.LPWStr)] string name,
                float duration
            );

        [DllImport("SDNative.dll")]
        protected static extern unsafe
            SdBoneAnimation SDMeshAddBoneAnimation(SdMesh* mesh,
                SdAnimationClip clip,
                int skinnedBoneIndex
            );

        [DllImport("SDNative.dll")]
        protected static extern unsafe
            void SDMeshAddAnimationKeyFrame(SdMesh* mesh,
                SdAnimationClip clip,
                SdBoneAnimation anim,
                in SdAnimationKeyFrame keyFrame
            );

        /////////////////////////////////////////////////////////////////////////////
        // Phase 3.10.B.2: read-side surface for skinned bones + animation clips.
        // The legacy `SdBonePose` struct above uses XnaQuaternion (40 bytes); the
        // C++ Nano::BonePose is 36 bytes (Vector3 Euler degrees). The legacy
        // struct survives because the writer ignores SkinnedBone.Pose data after
        // the call (it derives bind-pose from the FbxNode hierarchy directly), so
        // the ABI mismatch is dormant on the write side. The read side has no
        // such cover — these new types match the C++ layout exactly.

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdBonePoseInfo
        {
            public Vector3 Translation;
            public Vector3 Rotation; // Euler XYZ DEGREES (matches Nano::BonePose)
            public Vector3 Scale;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct SdSkinnedBoneInfo
        {
            public CStrView Name;
            public int BoneIndex;
            public int ParentBone;
            public SdBonePoseInfo BindPose;
            public Matrix InverseBindPoseTransform;
        }

        [StructLayout(LayoutKind.Sequential)]
        protected struct SdAnimationClipInfo
        {
            public CStrView Name;
            public float Duration;
            public int NumAnimations;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdBoneAnimationInfo
        {
            public int SkinnedBoneIndex;
            public int NumFrames;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        protected struct SdAnimationKeyFrameInfo
        {
            public float Time;
            public SdBonePoseInfo Pose;
        }

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdSkinnedBoneInfo SDMeshGetSkinnedBone(SdMesh* mesh, int index);

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdAnimationClipInfo SDMeshGetAnimationClip(SdMesh* mesh, int clipIndex);

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdBoneAnimationInfo SDMeshGetBoneAnimation(SdMesh* mesh, int clipIndex, int animIndex);

        [DllImport("SDNative.dll")] protected static extern unsafe
            SdAnimationKeyFrameInfo SDMeshGetAnimationKeyFrame(SdMesh* mesh, int clipIndex, int animIndex, int frameIndex);

        /////////////////////////////////////////////////////////////////////////////

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
                XnaVector2 uv0 = vertices[in0].TextureCoordinate;
                XnaVector2 uv1 = vertices[in1].TextureCoordinate;
                XnaVector2 uv2 = vertices[in2].TextureCoordinate;
                float s1 = uv1.X - uv0.X;
                float t1 = uv1.Y - uv0.Y;
                float s2 = uv2.X - uv0.X;
                float t2 = uv2.Y - uv0.Y;
                float st = (s1 * t2 - s2 * t1);
                if (st != 0.0f)
                {
                    float tmp = 1f / st;
                    XnaVector3 p0 = vertices[in0].Position;
                    XnaVector3 p1 = vertices[in1].Position;
                    XnaVector3 p2 = vertices[in2].Position;
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
                vertices[i].Tangent  = XnaVector3.Normalize(vertices[i].Tangent);
                vertices[i].Binormal = XnaVector3.Normalize(vertices[i].Binormal);
            }
        }

        // Phase 3.10.B.6: optional `isSkinned` flag picks SkinnedLightingEffect
        // (matrix-palette skinning VS) over the static LightingEffect. Both share
        // the same property surface (DiffuseColor/SpecularPower/etc.) so the
        // material assignment below stays unified.
        protected static unsafe LightingEffect CreateMaterialEffect(
            SdMaterial* mat, GraphicsDevice device, GameContentManager content, string materialFile,
            bool isSkinned = false)
        {
            LightingEffect fx = isSkinned
                ? new SkinnedLightingEffect(device)
                : new LightingEffect(device);
            fx.MaterialName          = mat->Name.AsString;
            fx.MaterialFile          = materialFile;
            fx.ProjectFile           = "Ship_Game/Data/RawContentLoader.cs";
            fx.DiffuseMapFile        = mat->DiffusePath.AsString;
            fx.EmissiveMapFile       = mat->EmissivePath.AsString;
            fx.NormalMapFile         = mat->NormalPath.AsString;
            fx.SpecularColorMapFile  = mat->SpecularPath.AsString;
            //fx.DiffuseAmbientMapFile = "";
            //fx.ParallaxMapFile       = "";
            fx.DiffuseMapTexture = TryLoadTexture(content, fx.DiffuseMapFile);
            fx.EmissiveMapTexture = TryLoadTexture(content, fx.EmissiveMapFile);
            fx.NormalMapTexture = TryLoadTexture(content, fx.NormalMapFile);
            fx.SpecularColorMapTexture = TryLoadTexture(content, fx.SpecularColorMapFile);
            //if (fx.DiffuseAmbientMapFile.NotEmpty()) fx.DiffuseAmbientMapTexture = content.Load<Texture2D>(fx.DiffuseAmbientMapFile);
            //if (fx.ParallaxMapFile.NotEmpty())       fx.ParallaxMapTexture       = CoreUtils.ConvertToLuminance8(device, content.Load<Texture2D>(fx.ParallaxMapFile));
            fx.Skinned         = isSkinned;
            fx.DoubleSided     = false;

            Texture2D alphaMap = mat->AlphaPath.NotEmpty
                ? content.Load<Texture2D>(mat->AlphaPath.AsString)
                : fx.DiffuseMapTexture;

            fx.SetTransparencyModeAndMap(TransparencyMode.None, mat->Alpha, alphaMap);
            // Phase 3.7 step 4 (Phase C contrast pass): 14*Specular caps at 14
            // even at max material gloss — well below BasicEffect's default of
            // 16, which produced soft/wide highlights instead of the tight
            // metallic specular that pre-migration ship hulls had. Establish a
            // floor of 16 (BasicEffect default) and a ceiling of 64 (typical
            // hard-metal value), interpolated by the material's gloss factor.
            fx.SpecularPower                 = 16.0f + 48.0f * mat->Specular;
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

        static Texture2D TryLoadTexture(GameContentManager content, string texturePath)
        {
            if (texturePath.IsEmpty())
                return null;
            return content.Load<Texture2D>(texturePath);
        }
    }
}
