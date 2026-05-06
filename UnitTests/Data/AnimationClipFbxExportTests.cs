using System;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Data.Mesh;
using SDGraphics;
using XnaQuaternion = Microsoft.Xna.Framework.Quaternion;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.A.3: pin that NanoMesh's FBX writer emits FbxAnimStack /
    /// FbxAnimLayer / FbxAnimCurve for clips populated via SDMeshCreateAnimationClip
    /// + SDMeshAddBoneAnimation + SDMeshAddAnimationKeyFrame. Without this, per-bone
    /// keyframe data is dropped on save; the FBX has no animation curves to evaluate
    /// at runtime.
    /// </summary>
    [TestClass]
    public class AnimationClipFbxExportTests : MeshInterface
    {
        const string OutDir = "MeshExport/Phase3_10A3";

        public AnimationClipFbxExportTests() : base(null) { }

        [TestMethod]
        public unsafe void Save_SkinnedMeshWithClip_FbxContainsAnimStackLayerCurve()
        {
            string outPath = PrepareOutPath("clip-test.fbx");

            SdMesh* mesh = SDMeshCreateEmpty("clip-test");
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

                // Attach a minimal mesh group so the FBX is structurally valid.
                Matrix groupTransform = Matrix.Identity;
                SdMeshGroup* group = SDMeshNewGroup(mesh, "g0", &groupTransform);
                Assert.IsTrue(group != null);
                AttachSkinnedQuad(group);

                // 1 clip, 1 bone-track, 2 keyframes spanning [0, 1] second.
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

                bool saved = SDMeshSave(mesh, outPath);
                Assert.IsTrue(saved, "SDMeshSave returned false");
                Assert.IsTrue(File.Exists(outPath));

                string ascii = ReadAscii(outPath);
                Assert.IsTrue(ascii.Contains("AnimStack"), "FBX missing AnimStack (clip not emitted)");
                Assert.IsTrue(ascii.Contains("AnimLayer"), "FBX missing AnimLayer (layer not emitted)");
                Assert.IsTrue(ascii.Contains("AnimCurve"), "FBX missing AnimCurve (per-axis keyframe curve not emitted)");
                Assert.IsTrue(ascii.Contains("TestClip"),  "FBX missing clip name 'TestClip' — stack name not propagated");
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        [TestMethod]
        public unsafe void Save_SkinnedMeshWithoutClip_FbxHasNoAnimStack()
        {
            string outPath = PrepareOutPath("noclip-test.fbx");

            SdMesh* mesh = SDMeshCreateEmpty("noclip-test");
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

                bool saved = SDMeshSave(mesh, outPath);
                Assert.IsTrue(saved);

                string ascii = ReadAscii(outPath);
                // FBX SDK emits a default "Take 001" AnimStack for any scene unconditionally,
                // so AnimStack alone is not a reliable user-clip diagnostic. AnimCurve only
                // appears when keyframes are written, which is what we're gating.
                Assert.IsFalse(ascii.Contains("AnimCurve"),
                    "Mesh with no clips emitted AnimCurve — animation path must be gated by AnimationClips presence");
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        // 4-vertex skinned quad, all weight on bone 0. Shared between A.3 tests so
        // both produce a structurally complete FBX even when only the clip path is
        // exercised.
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

        static string PrepareOutPath(string fileName)
        {
            Directory.CreateDirectory(OutDir);
            string outPath = Path.Combine(OutDir, fileName);
            if (File.Exists(outPath))
                File.Delete(outPath);
            return outPath;
        }

        static string ReadAscii(string path) => Encoding.ASCII.GetString(File.ReadAllBytes(path));
    }
}
