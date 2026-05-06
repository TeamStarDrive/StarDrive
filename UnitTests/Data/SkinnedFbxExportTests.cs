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
    /// Phase 3.10.A.2: pin that NanoMesh's FBX writer emits FbxSkin / FbxCluster /
    /// LimbNode deformers for a skinned mesh, and emits NO cluster nodes for a
    /// static mesh. Without these clusters in the FBX, vertices have no
    /// link back to bones, and a downstream consumer can't deform the geometry.
    /// </summary>
    [TestClass]
    public class SkinnedFbxExportTests : MeshInterface
    {
        const string OutDir = "MeshExport/Phase3_10A2";

        public SkinnedFbxExportTests() : base(null) { }

        [TestMethod]
        public unsafe void Save_SkinnedSyntheticMesh_FbxContainsDeformerSkinCluster()
        {
            string outPath = PrepareOutPath("skin-test.fbx");

            SdMesh* mesh = SDMeshCreateEmpty("skin-test");
            Assert.IsTrue(mesh != null, "SDMeshCreateEmpty returned null");
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
                Assert.IsTrue(group != null, "SDMeshNewGroup returned null");

                // 4-vertex skinned stream: Position + BlendIndices(all=0) + BlendWeight(1,0,0,0).
                // All weight on bone 0 keeps the test trivially correct under eNormalize.
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
                    // BlendIndices: all 0 (single bone)
                    // BlendWeight: (1, 0, 0, 0)
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

                bool saved = SDMeshSave(mesh, outPath);
                Assert.IsTrue(saved, "SDMeshSave returned false");
                Assert.IsTrue(File.Exists(outPath), $"Expected FBX at {outPath}");

                string ascii = ReadAscii(outPath);
                // Single-bone test produces an eRoot Skeleton (not eLimbNode); both share
                // the "Skeleton" object class. eLimbNode coverage is incidental in
                // multi-bone exports like ship17a-f.
                Assert.IsTrue(ascii.Contains("Skeleton"), "FBX missing Skeleton (skeleton emission regressed)");
                Assert.IsTrue(ascii.Contains("Deformer"), "FBX missing Deformer object class (FbxSkin not emitted)");
                Assert.IsTrue(ascii.Contains("Cluster"),  "FBX missing Cluster object class (FbxCluster not emitted)");
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }

        [TestMethod]
        public unsafe void Save_StaticSyntheticMesh_FbxHasNoCluster()
        {
            string outPath = PrepareOutPath("static-test.fbx");

            SdMesh* mesh = SDMeshCreateEmpty("static-test");
            Assert.IsTrue(mesh != null);
            try
            {
                Matrix groupTransform = Matrix.Identity;
                SdMeshGroup* group = SDMeshNewGroup(mesh, "g0", &groupTransform);
                Assert.IsTrue(group != null);

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

                bool saved = SDMeshSave(mesh, outPath);
                Assert.IsTrue(saved, "SDMeshSave returned false");

                string ascii = ReadAscii(outPath);
                Assert.IsFalse(ascii.Contains("Cluster"),
                    "Static mesh emitted Cluster — skin path must be gated by BlendIndices/BlendWeights presence");
            }
            finally
            {
                SDMeshClose(mesh);
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

        // FBX binary stores object class names ("Cluster", "Deformer", "LimbNode") as
        // length-prefixed ASCII inside node records. A plain ASCII substring scan is
        // sufficient to detect their presence — the names are 7-bit ASCII either way.
        static string ReadAscii(string path)
        {
            return Encoding.ASCII.GetString(File.ReadAllBytes(path));
        }
    }
}
