using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ship_Game.Data.Mesh;
using SDGraphics;

namespace UnitTests.Data
{
    /// <summary>
    /// Phase 3.10.A.1 close-out: pin that per-vertex BlendIndices (Byte4) and
    /// BlendWeight (Vector4) round-trip cleanly through the
    /// C# -> SDVertexData -> Nano::MeshGroup -> SDVertexData -> C# pipe.
    /// This is the data foundation §3.10.A.2 (FbxSkin/FbxCluster writer) and
    /// §3.10.A.3 (animation curves) consume; without it, no skin weights make
    /// it from the XNA vertex buffer to the FBX exporter. The data plumbing
    /// is pre-existing (Nano::MeshGroup::BlendIndices/BlendWeights, the
    /// SDElementUsage::BlendWeight/BlendIndices cases in SetVertexDataFor /
    /// CreateCachedVertexData, and the C# enum translators in MeshInterface)
    /// — this test locks the surface in so A.2/A.3 build on solid ground.
    /// </summary>
    [TestClass]
    public class SkinnedVertexDataRoundtripTests : MeshInterface
    {
        public SkinnedVertexDataRoundtripTests() : base(null) { }

        [TestMethod]
        public unsafe void Roundtrip_BlendIndicesAndBlendWeight_PerVertex_Preserved()
        {
            SdMesh* mesh = SDMeshCreateEmpty("skin-roundtrip");
            Assert.IsTrue(mesh != null, "SDMeshCreateEmpty returned null");
            try
            {
                Matrix identity = Matrix.Identity;
                SdMeshGroup* group = SDMeshNewGroup(mesh, "g0", &identity);
                Assert.IsTrue(group != null, "SDMeshNewGroup returned null");

                // Layout: Position (Vector3, 12B) + BlendIndices (Byte4, 4B) + BlendWeight (Vector4, 16B).
                // NativeFormat ordinals from SdMeshGroup.h: 2=Vector3, 3=Vector4, 5=Byte4.
                // NativeUsage ordinals (XNA 3.1): 0=Position, 1=BlendWeight, 2=BlendIndices.
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
                var expectedBlendIndices = new byte[NumVerts][];
                var expectedBlendWeights = new float[NumVerts][];
                for (int v = 0; v < NumVerts; v++)
                {
                    int o = v * Stride;
                    BitConverter.GetBytes((float)v).CopyTo(verts, o + 0);
                    BitConverter.GetBytes(0f).CopyTo(verts, o + 4);
                    BitConverter.GetBytes(0f).CopyTo(verts, o + 8);

                    expectedBlendIndices[v] = new byte[] { (byte)(v*4), (byte)(v*4+1), (byte)(v*4+2), (byte)(v*4+3) };
                    Buffer.BlockCopy(expectedBlendIndices[v], 0, verts, o + 12, 4);

                    expectedBlendWeights[v] = new float[] { 0.4f + v*0.01f, 0.3f + v*0.01f, 0.2f + v*0.01f, 0.1f + v*0.01f };
                    Buffer.BlockCopy(expectedBlendWeights[v], 0, verts, o + 16, 16);
                }

                SdVertexData input;
                input.VertexStride = Stride;
                input.LayoutCount  = layout.Length;
                input.IndexCount   = NumIndices;
                input.VertexCount  = NumVerts;

                fixed (ushort* pIndex = indices)
                fixed (byte* pVerts = verts)
                fixed (SdVertexElement* pLayout = layout)
                {
                    input.IndexData  = pIndex;
                    input.VertexData = pVerts;
                    input.Layout     = pLayout;
                    SDMeshGroupSetData(group, input);
                }

                SdVertexData output = SDMeshGroupGetData(group);
                Assert.AreEqual(NumVerts,   output.VertexCount, "VertexCount round-trip mismatch");
                Assert.AreEqual(NumIndices, output.IndexCount,  "IndexCount round-trip mismatch");

                // Output layout order is determined by CreateCachedVertexData (canonical, not input order).
                int outBlendIndicesOffset = -1;
                int outBlendWeightOffset  = -1;
                for (int i = 0; i < output.LayoutCount; i++)
                {
                    if (output.Layout[i].NativeUsage == 2) outBlendIndicesOffset = output.Layout[i].Offset;
                    if (output.Layout[i].NativeUsage == 1) outBlendWeightOffset  = output.Layout[i].Offset;
                }
                Assert.AreNotEqual(-1, outBlendIndicesOffset, "Round-tripped layout missing BlendIndices element");
                Assert.AreNotEqual(-1, outBlendWeightOffset,  "Round-tripped layout missing BlendWeight element");

                for (int v = 0; v < NumVerts; v++)
                {
                    byte* vp = output.VertexData + v * output.VertexStride;
                    byte* idxPtr = vp + outBlendIndicesOffset;
                    for (int k = 0; k < 4; k++)
                    {
                        Assert.AreEqual(expectedBlendIndices[v][k], idxPtr[k],
                            $"BlendIndex[{v}].{k} round-trip mismatch");
                    }
                    float* wPtr = (float*)(vp + outBlendWeightOffset);
                    for (int k = 0; k < 4; k++)
                    {
                        Assert.AreEqual(expectedBlendWeights[v][k], wPtr[k], 0.0001f,
                            $"BlendWeight[{v}].{k} round-trip mismatch");
                    }
                }
            }
            finally
            {
                SDMeshClose(mesh);
            }
        }
    }
}
