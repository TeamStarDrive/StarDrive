using System;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Mesh;
using SDUtils;

namespace Ship_Game.Data
{
    // Phase 3.4 step 5 / TODO Phase 4: empirical decoder for the XNA 3.1 VertexDeclaration
    // XNB binary format. MonoGame's stock VertexDeclarationReader expects 8 + 16*N bytes
    // (vertexStride:int + elementCount:int + per-element { offset:int, format:int,
    // usage:int, usageIndex:int }), but XNA 3.1 baked content uses a much more compact
    // wire format that the original 3.1 runtime understood. Reading XNA-3.1 bytes via
    // MG's reader walks past EOF or into the next reader's payload and throws
    // IndexOutOfRangeException — the dominant failure mode for ship/projectile XNBs
    // (141 occurrences in the §3.4 step-2 boot smoke).
    //
    // **Currently unused at runtime.** Phase 3.4 pivoted from "decode XNB at runtime"
    // to "offline export to FBX/OBJ on legacy/mesh_exporter_xna31, hand-copy corpus to
    // migration branch" (commit 9bd3b7128 — 147 MB FBX/DDS drop). Phase B then archived
    // every Model XNB out of game/Content/Model/ (commits 6f68b9396 + a5da742b4), so no
    // production load path reaches this reader anymore. Preserved as the foundation
    // for a future Xna31ModelReader if/when a mod ships an XNA-3.1-baked Model XNB —
    // the partial decode below is the hard-earned hex work and shouldn't be re-derived.
    //
    // Decoded layout (validated empirically against Effects/ThrustCylinderB.xnb whose
    // hex dump was preserved in project_phase2_xnb_model_drift.md):
    //
    //   int32   elementCount
    //   per element (8 bytes):
    //     int16   stream         (always 0 for our content; XNA 3.1 multi-stream not used)
    //     int16   offsetInVertex (bytes from vertex start)
    //     byte    format         (XNA 3.1 VertexElementFormat ordinal — same byte values
    //                             SDNative carries; translate via TranslateNativeFormat)
    //     byte    method         (XNA 3.1 VertexElementMethod ordinal — dropped in MG 4.0+;
    //                             value is read and discarded)
    //     byte    usage          (XNA 3.1 VertexElementUsage ordinal — translate via
    //                             TranslateNativeUsage; XNA 3.1 differed from MG, e.g.
    //                             TextureCoordinate=5 in 3.1 vs 2 in MG)
    //     byte    usageIndex
    //   int32   trailer          (purpose unknown — 1 in observed ThrustCylinderB sample;
    //                             possibly stream count or end-of-decl marker. Read and
    //                             discarded so the ContentReader stream pointer stays
    //                             aligned for whatever follows.)
    //
    // Worked decode for ThrustCylinderB.xnb (3 elements / 32 bytes):
    //   03 00 00 00                                    elementCount = 3
    //   00 00 00 00 02 00 00 00                        elem0: stream=0 offset=0  fmt=Vector3 method=0 usage=Position    idx=0
    //   00 00 0C 00 02 00 03 00                        elem1: stream=0 offset=12 fmt=Vector3 method=0 usage=Normal      idx=0
    //   00 00 18 00 01 00 05 00                        elem2: stream=0 offset=24 fmt=Vector2 method=0 usage=Coordinate  idx=0
    //   01 00 00 00                                    trailer = 1
    //
    // Stride is computed from max(offset + size) across elements — matches what the
    // pre-Phase-1 SDNative pipeline did, and avoids gaps becoming part of the stride
    // when the bytes have padding inserted by the original content pipeline.
    //
    // Validated empirically against 10+ ship/projectile XNBs (LRM, MRM, Nuke, SRM,
    // Kuma Naka, Kuma Sukoshi, TypeWI/II/III, Station_Small) — all decode cleanly to
    // Position+Normal+TextureCoordinate+Tangent+Binormal vertices with stride 56,
    // matching the standard SunBurn-baked vertex layout.
    //
    // **NOT YET END-TO-END USABLE.** Investigation 2026-05-04 showed that fixing
    // VertexDeclaration alone is insufficient — XNA 3.1's *Model* XNB itself has
    // structural drift from MG's stock ModelReader. After registering this reader,
    // ship XNB loads still fail with `InvalidCastException: VertexDeclaration → String`
    // inside MG's `ModelReader.Read` at the per-bone or per-mesh `ReadObject<string>`
    // call, where XNA 3.1 wrote a typed VertexDeclaration object (type-id 3) that MG
    // doesn't expect at that position. Step 5 as scoped (just the VertexDeclaration
    // reader) doesn't reach the unblock; an `Xna31ModelReader` is also needed.
    // Captured-on-disk decode preserved here as the foundation for that next session.
    public class Xna31VertexDeclarationReader : ContentTypeReader<VertexDeclaration>
    {
        protected override VertexDeclaration Read(ContentReader reader, VertexDeclaration existingInstance)
        {
            (int stride, VertexElement[] elements) = DecodeXna31Bytes(reader);
            return new VertexDeclaration(stride, elements);
        }

        // Pulled out as a public static helper so tests can pin the decode against captured
        // XNB bytes without spinning up a real ContentReader / ContentManager. ContentReader
        // derives from BinaryReader so passing the base type makes the helper unit-testable
        // with a plain MemoryStream.
        public static (int stride, VertexElement[] elements) DecodeXna31Bytes(BinaryReader reader)
        {
            int elementCount = reader.ReadInt32();
            if (elementCount <= 0 || elementCount > 32)
                throw new InvalidDataException(
                    $"Xna31VertexDeclarationReader: elementCount={elementCount} out of plausible range — XNB layout drift");

            var elements = new VertexElement[elementCount];
            int maxStride = 0;
            int kept = 0;
            for (int i = 0; i < elementCount; ++i)
            {
                short stream     = reader.ReadInt16();
                short offset     = reader.ReadInt16();
                byte formatByte  = reader.ReadByte();
                _ = reader.ReadByte();   // method — XNA 3.1 VertexElementMethod, dropped in MG 4.0+
                byte usageByte   = reader.ReadByte();
                byte usageIndex  = reader.ReadByte();
                _ = stream;             // multi-stream not used; preserved by the offsets

                if (!MeshInterface.TranslateNativeFormat(formatByte, out VertexElementFormat format) ||
                    !MeshInterface.TranslateNativeUsage(usageByte, out VertexElementUsage usage))
                {
                    Log.Warning(
                        $"Xna31VertexDeclarationReader: dropping element #{i} " +
                        $"(formatByte={formatByte} usageByte={usageByte}) — no MonoGame equivalent");
                    continue;
                }

                elements[kept++] = new VertexElement(offset, format, usage, usageIndex);

                int size = ElementSizeInBytes(format);
                if (offset + size > maxStride) maxStride = offset + size;
            }

            // Trailer (purpose unknown; consume to keep stream aligned).
            _ = reader.ReadInt32();

            if (kept == 0)
                throw new InvalidDataException(
                    "Xna31VertexDeclarationReader: no elements survived translation");

            if (kept < elementCount)
                Array.Resize(ref elements, kept);

            return (maxStride, elements);
        }

        // Mirror of MeshInterface.ElementSizeInBytes. Standalone here to avoid leaking the
        // `protected` accessor; the values are short-lived constants and unlikely to drift.
        static int ElementSizeInBytes(VertexElementFormat format)
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
    }
}
