using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Data.Mesh;
using SDGraphics;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.B.3 close-out: pin the full FBX -> C# data pipe for skinned
    /// meshes. Saves a synthetic FBX with bones + skin weights + a clip with
    /// 2 keyframes via the legacy write API, re-opens via SDMeshOpen (which
    /// triggers NanoMesh LoadFBX -> B.0 skin reader + B.1 anim reader), then
    /// reads back via the B.2 SDNative getters and asserts every field
    /// matches what we wrote. This is the round-trip gate the user asked for
    /// at the B.3 stop point.
    /// </summary>
    [TestClass]
    public class SkinnedMeshRoundtripTests : MeshInterface
    {
        const string OutDir = "MeshExport/Phase3_10B3";

        public SkinnedMeshRoundtripTests() : base(null) { }

        [TestMethod]
        public unsafe void Roundtrip_SkinnedMeshWithClip_DataObservableInCSharp()
        {
            string outPath = PrepareOutPath("roundtrip-skinned.fbx");

            // === Write phase ===========================================
            SdMesh* mesh = SDMeshCreateEmpty("roundtrip-skinned");
            Assert.IsTrue(mesh != null);
            try
            {
                Matrix identity = Matrix.Identity;
                SDMeshAddBone(mesh, "RootBone", 0, -1, in identity);
                var bindPose = new SdBonePose
                {
                    Translation = Vector3.Zero,
                    Orientation = XnaQuaternion.Identity,
                    Scale = Vector3.One,
                };
                SDMeshAddSkinnedBone(mesh, "RootBone", 0, -1, in bindPose, in identity);

                Matrix groupTransform = Matrix.Identity;
                SdMeshGroup* group = SDMeshNewGroup(mesh, "g0", &groupTransform);
                AttachSkinnedQuad(group);

                SdAnimationClip clip = SDMeshCreateAnimationClip(mesh, "TestClip", 1.0f);
                SdBoneAnimation anim = SDMeshAddBoneAnimation(mesh, clip, 0);

                var key0 = new SdAnimationKeyFrame
                {
                    Time = 0f,
                    Pose = new SdBonePose { Translation = Vector3.Zero, Orientation = XnaQuaternion.Identity, Scale = Vector3.One },
                };
                var key1 = new SdAnimationKeyFrame
                {
                    Time = 1f,
                    Pose = new SdBonePose { Translation = new Vector3(10f, 0f, 0f), Orientation = XnaQuaternion.Identity, Scale = Vector3.One },
                };
                SDMeshAddAnimationKeyFrame(mesh, clip, anim, in key0);
                SDMeshAddAnimationKeyFrame(mesh, clip, anim, in key1);

                Assert.IsTrue(SDMeshSave(mesh, outPath), "SDMeshSave returned false");
            }
            finally
            {
                SDMeshClose(mesh);
            }

            // === Read phase ============================================
            SdMesh* loaded = SDMeshOpen(outPath);
            Assert.IsTrue(loaded != null, "SDMeshOpen returned null on the file we just saved");
            try
            {
                Assert.IsTrue(loaded->NumSkinnedBones >= 1,
                    $"Expected at least 1 skinned bone, got {loaded->NumSkinnedBones}");
                Assert.IsTrue(loaded->NumAnimClips >= 1,
                    $"Expected at least 1 animation clip, got {loaded->NumAnimClips}");

                SdSkinnedBoneInfo sb = SDMeshGetSkinnedBone(loaded, 0);
                Assert.AreEqual("RootBone", sb.Name.AsString, "skinned bone name round-trip");
                Assert.AreEqual(-1, sb.ParentBone, "root bone has no parent");

                SdAnimationClipInfo ci = SDMeshGetAnimationClip(loaded, 0);
                Assert.AreEqual("TestClip", ci.Name.AsString, "clip name round-trip");
                Assert.AreEqual(1.0f, ci.Duration, 0.01f, "clip duration round-trip");
                Assert.AreEqual(1, ci.NumAnimations, "clip should have 1 bone-track animation");

                SdBoneAnimationInfo ba = SDMeshGetBoneAnimation(loaded, 0, 0);
                Assert.AreEqual(0, ba.SkinnedBoneIndex, "track binds to bone 0");
                Assert.AreEqual(2, ba.NumFrames, "two keyframes round-trip");

                SdAnimationKeyFrameInfo k0 = SDMeshGetAnimationKeyFrame(loaded, 0, 0, 0);
                SdAnimationKeyFrameInfo k1 = SDMeshGetAnimationKeyFrame(loaded, 0, 0, 1);
                Assert.AreEqual(0f, k0.Time, 0.01f, "keyframe 0 time");
                Assert.AreEqual(1f, k1.Time, 0.01f, "keyframe 1 time");
                Assert.AreEqual(0f,  k0.Pose.Translation.X, 0.01f, "keyframe 0 X = 0");
                Assert.AreEqual(10f, k1.Pose.Translation.X, 0.01f, "keyframe 1 X = 10");
            }
            finally
            {
                SDMeshClose(loaded);
            }
        }

        [TestMethod]
        public unsafe void Roundtrip_StaticMesh_NoSkinnedBonesOrClips()
        {
            string outPath = PrepareOutPath("roundtrip-static.fbx");

            SdMesh* mesh = SDMeshCreateEmpty("roundtrip-static");
            Assert.IsTrue(mesh != null);
            try
            {
                Matrix groupTransform = Matrix.Identity;
                SdMeshGroup* group = SDMeshNewGroup(mesh, "g0", &groupTransform);
                AttachStaticQuad(group);
                Assert.IsTrue(SDMeshSave(mesh, outPath));
            }
            finally
            {
                SDMeshClose(mesh);
            }

            SdMesh* loaded = SDMeshOpen(outPath);
            Assert.IsTrue(loaded != null);
            try
            {
                Assert.AreEqual(0, loaded->NumSkinnedBones,
                    "Static mesh load should not invent skinned bones");
                Assert.AreEqual(0, loaded->NumAnimClips,
                    "Static mesh load should not invent animation clips");
            }
            finally
            {
                SDMeshClose(loaded);
            }
        }

        static unsafe void AttachSkinnedQuad(SdMeshGroup* group)
        {
            const int Stride = 32;
            const int NumVerts = 4;
            const int NumIndices = 6;
            var layout = new SdVertexElement[]
            {
                new() { Offset = 0,  Size = 12, NativeFormat = 2, NativeUsage = 0 },
                new() { Offset = 12, Size = 4,  NativeFormat = 5, NativeUsage = 2 },
                new() { Offset = 16, Size = 16, NativeFormat = 3, NativeUsage = 1 },
            };
            var indices = new ushort[NumIndices] { 0, 1, 2, 0, 2, 3 };
            var verts = new byte[NumVerts * Stride];
            for (int v = 0; v < NumVerts; v++)
            {
                int o = v * Stride;
                BitConverter.GetBytes((float)v).CopyTo(verts, o + 0);
                BitConverter.GetBytes(0f).CopyTo(verts, o + 4);
                BitConverter.GetBytes(0f).CopyTo(verts, o + 8);
                BitConverter.GetBytes(1f).CopyTo(verts, o + 16);
            }

            SdVertexData data;
            data.VertexStride = Stride;
            data.LayoutCount  = layout.Length;
            data.IndexCount   = NumIndices;
            data.VertexCount  = NumVerts;
            fixed (ushort* pIndex = indices)
            fixed (byte* pVerts = verts)
            fixed (SdVertexElement* pLayout = layout)
            {
                data.IndexData  = pIndex;
                data.VertexData = pVerts;
                data.Layout     = pLayout;
                SDMeshGroupSetData(group, data);
            }
        }

        static unsafe void AttachStaticQuad(SdMeshGroup* group)
        {
            const int Stride = 12;
            const int NumVerts = 4;
            var layout = new SdVertexElement[]
            {
                new() { Offset = 0, Size = 12, NativeFormat = 2, NativeUsage = 0 },
            };
            var indices = new ushort[] { 0, 1, 2, 0, 2, 3 };
            var verts = new byte[NumVerts * Stride];
            for (int v = 0; v < NumVerts; v++)
            {
                int o = v * Stride;
                BitConverter.GetBytes((float)v).CopyTo(verts, o + 0);
                BitConverter.GetBytes(0f).CopyTo(verts, o + 4);
                BitConverter.GetBytes(0f).CopyTo(verts, o + 8);
            }

            SdVertexData data;
            data.VertexStride = Stride;
            data.LayoutCount  = layout.Length;
            data.IndexCount   = indices.Length;
            data.VertexCount  = NumVerts;
            fixed (ushort* pIndex = indices)
            fixed (byte* pVerts = verts)
            fixed (SdVertexElement* pLayout = layout)
            {
                data.IndexData  = pIndex;
                data.VertexData = pVerts;
                data.Layout     = pLayout;
                SDMeshGroupSetData(group, data);
            }
        }

        static string PrepareOutPath(string fileName)
        {
            Directory.CreateDirectory(OutDir);
            string outPath = Path.Combine(OutDir, fileName);
            if (File.Exists(outPath))
                File.Delete(outPath);
            return outPath;
        }
    }
}
