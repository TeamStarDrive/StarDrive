using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

namespace UnitTests.Content
{
    /// <summary>
    /// Phase 3.4 step 5 / Phase 4 carryover — byte-level pin for
    /// <see cref="Xna31VertexDeclarationReader.DecodeXna31Bytes"/>.
    /// Captured XNB byte sequences from real ship/projectile XNBs are the source of truth;
    /// if the wire-format hypothesis ever drifts (e.g. a future MG version reorders the
    /// translation tables, or someone tweaks the trailer-skip), these regressions catch it
    /// without needing the runtime smoke loop.
    ///
    /// Note: the runtime path is unused today (Phase 3.4 pivoted to offline FBX export);
    /// these tests preserve the decode work for the eventual Phase 4 Xna31ModelReader.
    /// Captured 2026-05-04 from the live boot diagnostic over the §3.1 inventory's
    /// static-sunburn set (see commit log for the ground-truth dump).
    /// </summary>
    [TestClass]
    public class Xna31VertexDeclarationDecodeTests
    {
        // Effects/ThrustCylinderB.xnb — 3 elements / 32 bytes. Position+Normal+TexCoord
        // (the §1.10/§2.2 reference sample preserved in project_phase2_xnb_model_drift.md).
        static readonly byte[] ThrustCylinderBBytes =
        {
            0x03, 0x00, 0x00, 0x00,                          // count=3
            0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,  // elem 0: stream=0 offset=0  fmt=Vector3 method=0 usage=Position    idx=0
            0x00, 0x00, 0x0C, 0x00, 0x02, 0x00, 0x03, 0x00,  // elem 1: stream=0 offset=12 fmt=Vector3 method=0 usage=Normal      idx=0
            0x00, 0x00, 0x18, 0x00, 0x01, 0x00, 0x05, 0x00,  // elem 2: stream=0 offset=24 fmt=Vector2 method=0 usage=Coordinate  idx=0
            0x01, 0x00, 0x00, 0x00,                          // trailer=1
        };

        // Model/Projectiles/custom/LRM.xnb — 5 elements / 48 bytes.
        // Position+Normal+TexCoord+Tangent+Binormal — the canonical SunBurn-baked layout.
        static readonly byte[] LRMBytes =
        {
            0x05, 0x00, 0x00, 0x00,                          // count=5
            0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00,  // elem 0: offset=0  fmt=Vector3 usage=Position   idx=0
            0x00, 0x00, 0x0C, 0x00, 0x02, 0x00, 0x03, 0x00,  // elem 1: offset=12 fmt=Vector3 usage=Normal     idx=0
            0x00, 0x00, 0x18, 0x00, 0x01, 0x00, 0x05, 0x00,  // elem 2: offset=24 fmt=Vector2 usage=Coordinate idx=0
            0x00, 0x00, 0x20, 0x00, 0x02, 0x00, 0x06, 0x00,  // elem 3: offset=32 fmt=Vector3 usage=Tangent    idx=0
            0x00, 0x00, 0x2C, 0x00, 0x02, 0x00, 0x07, 0x00,  // elem 4: offset=44 fmt=Vector3 usage=Binormal   idx=0
            0x01, 0x00, 0x00, 0x00,                          // trailer=1
        };

        [TestMethod]
        public void DecodeThrustCylinderB_ProducesPositionNormalTexCoord()
        {
            using var ms = new MemoryStream(ThrustCylinderBBytes);
            using var br = new BinaryReader(ms);
            (int stride, VertexElement[] elements) = Xna31VertexDeclarationReader.DecodeXna31Bytes(br);

            Assert.AreEqual(32, stride, "stride should be max(offset+size) = 24+8 = 32");
            Assert.AreEqual(3, elements.Length, "expected 3 elements");

            Assert.AreEqual(VertexElementUsage.Position,          elements[0].VertexElementUsage);
            Assert.AreEqual(VertexElementFormat.Vector3,          elements[0].VertexElementFormat);
            Assert.AreEqual(0,                                    elements[0].Offset);

            Assert.AreEqual(VertexElementUsage.Normal,            elements[1].VertexElementUsage);
            Assert.AreEqual(VertexElementFormat.Vector3,          elements[1].VertexElementFormat);
            Assert.AreEqual(12,                                   elements[1].Offset);

            Assert.AreEqual(VertexElementUsage.TextureCoordinate, elements[2].VertexElementUsage);
            Assert.AreEqual(VertexElementFormat.Vector2,          elements[2].VertexElementFormat);
            Assert.AreEqual(24,                                   elements[2].Offset);

            // The decoder must consume the entire 32-byte section so the next type-reader
            // in the XNB stream sees its expected start byte.
            Assert.AreEqual(ms.Length, ms.Position, "decoder should consume all 32 bytes (count + elements + trailer)");
        }

        [TestMethod]
        public void DecodeLRM_ProducesPositionNormalTexCoordTangentBinormal()
        {
            using var ms = new MemoryStream(LRMBytes);
            using var br = new BinaryReader(ms);
            (int stride, VertexElement[] elements) = Xna31VertexDeclarationReader.DecodeXna31Bytes(br);

            Assert.AreEqual(56, stride, "stride should be max(offset+size) = 44+12 = 56");
            Assert.AreEqual(5, elements.Length, "expected 5 elements");

            Assert.AreEqual(VertexElementUsage.Position,          elements[0].VertexElementUsage);
            Assert.AreEqual(VertexElementUsage.Normal,            elements[1].VertexElementUsage);
            Assert.AreEqual(VertexElementUsage.TextureCoordinate, elements[2].VertexElementUsage);
            Assert.AreEqual(VertexElementUsage.Tangent,           elements[3].VertexElementUsage);
            Assert.AreEqual(VertexElementUsage.Binormal,          elements[4].VertexElementUsage);

            Assert.AreEqual(0,  elements[0].Offset);
            Assert.AreEqual(12, elements[1].Offset);
            Assert.AreEqual(24, elements[2].Offset);
            Assert.AreEqual(32, elements[3].Offset);
            Assert.AreEqual(44, elements[4].Offset);

            Assert.AreEqual(ms.Length, ms.Position, "decoder should consume all 48 bytes");
        }

        [TestMethod]
        public void DecodeBogusElementCount_Throws()
        {
            // Sanity floor: a corrupted XNB shouldn't allocate gigabytes of elements.
            byte[] bogus = { 0xFF, 0xFF, 0xFF, 0xFF };
            using var ms = new MemoryStream(bogus);
            using var br = new BinaryReader(ms);
            Assert.ThrowsException<InvalidDataException>(() => Xna31VertexDeclarationReader.DecodeXna31Bytes(br));
        }
    }
}
