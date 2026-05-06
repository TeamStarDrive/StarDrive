using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace EffectXnbDump
{
    // Phase 3.3 step 1. Empirical wire-format dump for XNA 3.1 effect XNBs:
    //
    //   1. Header (XNB target/version/flags/totalSize)
    //   2. Type-reader manifest (count, names, versions)
    //   3. Shared-resource count
    //   4. Primary-asset reader id (7-bit-encoded, 1-based index into the manifest)
    //   5. Hex dump of the remaining body bytes (the Effect payload itself)
    //
    // Run: dotnet run --project x64Migration/Tools/EffectXnbDump -- <out.txt> <xnb1> <xnb2> ...
    // Default targets: the 6 broken effects per migration-plan-phase3.md §3.3.
    static class Program
    {
        static int Main(string[] args)
        {
            // Sub-command 'extract' carves the inner FX blob out of an XNB so we can
            // feed it to fxc /dumpbin or future disassembly steps standalone.
            if (args.Length > 0 && args[0] == "extract")
                return ExtractFxBlob.Run(new ArraySegment<string>(args, 1, args.Length - 1).ToArray());

            // Sub-command 'dumptex' decodes a Texture2D XNB header far enough to
            // report SurfaceFormat / dimensions / first-level peek — used to triage
            // the alpha-premul-at-load fix scope.
            if (args.Length > 0 && args[0] == "dumptex")
                return DumpTextureFormat.Run(new ArraySegment<string>(args, 1, args.Length - 1).ToArray());

            string outPath = args.Length > 0
                ? args[0]
                : @"C:\Development\stardrive\BlackBoxPlus\x64Migration\phase3-logs\effect-xnb-dump.txt";

            string contentRoot = @"C:\Development\stardrive\BlackBoxPlus\game\Content\Effects";
            string[] targets = args.Length > 1
                ? new ArraySegment<string>(args, 1, args.Length - 1).ToArray()
                : new[]
                {
                    Path.Combine(contentRoot, "BeamFX.xnb"),
                    Path.Combine(contentRoot, "Thrust.xnb"),
                    Path.Combine(contentRoot, "scale.xnb"),
                    Path.Combine(contentRoot, "desaturate.xnb"),
                    Path.Combine(contentRoot, "BasicFogOfWar.xnb"),
                    Path.Combine(contentRoot, "PlanetHalo.xnb"),
                };

            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");
            using var w = new StreamWriter(outPath, append: false, Encoding.UTF8);

            foreach (string xnbPath in targets)
            {
                w.WriteLine();
                w.WriteLine(new string('=', 78));
                w.WriteLine($"FILE: {xnbPath}");
                w.WriteLine(new string('=', 78));
                Console.WriteLine($"Dumping {Path.GetFileName(xnbPath)} ...");
                try { Dump(xnbPath, w); }
                catch (Exception ex)
                {
                    w.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}");
                    w.WriteLine(ex.StackTrace ?? "");
                }
            }

            Console.WriteLine($"Wrote {outPath}");
            return 0;
        }

        static void Dump(string xnbPath, StreamWriter w)
        {
            using var fs = File.OpenRead(xnbPath);
            using var br = new BinaryReader(fs);

            if (br.ReadByte() != 'X' || br.ReadByte() != 'N' || br.ReadByte() != 'B')
                throw new InvalidDataException("Not an XNB");

            byte target = br.ReadByte();
            byte version = br.ReadByte();
            byte flags = br.ReadByte();
            int totalSize = br.ReadInt32();
            w.WriteLine($"target=0x{target:X2} ('{(char)target}')  version={version}  flags=0x{flags:X2}  totalSize={totalSize}");

            Stream body;
            bool needsClose = false;
            if ((flags & 0x80) != 0)
            {
                int decompressedSize = br.ReadInt32();
                int compressedSize = totalSize - 14;
                w.WriteLine($"LZX: compressedSize={compressedSize}  decompressedSize={decompressedSize}");
                body = LzxDecompress(br, compressedSize, decompressedSize);
                needsClose = true;
            }
            else
            {
                w.WriteLine("(uncompressed)");
                body = fs;
            }

            try
            {
                using var brBody = new BinaryReader(body, Encoding.UTF8, leaveOpen: needsClose);
                int numReaders = Read7BitEncodedInt(brBody);
                w.WriteLine($"readerCount={numReaders}");
                for (int i = 0; i < numReaders; i++)
                {
                    string name = brBody.ReadString();
                    int rdrVer = brBody.ReadInt32();
                    w.WriteLine($"  [{i}] v={rdrVer}: {name}");
                }

                int sharedRes = Read7BitEncodedInt(brBody);
                w.WriteLine($"sharedResourceCount={sharedRes}");

                long bodyOffset = body.Position;
                int primaryReaderId = Read7BitEncodedInt(brBody);
                long payloadOffset = body.Position;
                w.WriteLine($"primaryAssetReaderId={primaryReaderId} (1-based, so reader [{primaryReaderId - 1}])");
                w.WriteLine($"  payload starts at body offset 0x{payloadOffset:X4}");

                // Dump the remainder as hex.
                long remaining = body.Length - body.Position;
                w.WriteLine($"payload bytes remaining = {remaining}");
                byte[] payload = brBody.ReadBytes((int)remaining);
                w.WriteLine();
                w.WriteLine("--- payload hex (offset is from payload start) ---");
                HexDump(w, payload, lines: 64);

                // Optional: try to interpret as the documented XNA 3.1 Effect layout —
                //   Int32 bytecodeLen
                //   bytecodeLen bytes
                //   ... param metadata that we still need to figure out.
                if (payload.Length >= 4)
                {
                    int firstInt = BitConverter.ToInt32(payload, 0);
                    w.WriteLine();
                    w.WriteLine($"interp: payload[0..4] as Int32 = {firstInt} (0x{firstInt:X8})");
                    if (firstInt > 0 && firstInt + 4 <= payload.Length)
                    {
                        // Check for D3D9 shader-bytecode magic. fxc-emitted .fx blobs
                        // start with the D3DX10 effect FX-LVM header `CTAB`/`Fx10`/etc.
                        // For an XNA 3.1 EffectReader payload the natural first int
                        // should be the raw fx_2_0 shader bytecode length followed
                        // by the bytecode itself.
                        byte b0 = payload[4], b1 = payload[5], b2 = payload[6], b3 = payload[7];
                        w.WriteLine($"interp: first 4 bytes after the int = {b0:X2} {b1:X2} {b2:X2} {b3:X2} ('{Safe((char)b0)}{Safe((char)b1)}{Safe((char)b2)}{Safe((char)b3)}')");
                        w.WriteLine($"  D3DX fx_2_0 effects start with 0x01 0x09 0xFF 0xFE (vs/ps version token).");
                    }
                }
            }
            finally
            {
                if (needsClose) body.Dispose();
            }
        }

        static char Safe(char c) => (c >= 0x20 && c < 0x7F) ? c : '.';

        static void HexDump(StreamWriter w, byte[] data, int lines)
        {
            int len = Math.Min(data.Length, lines * 16);
            for (int i = 0; i < len; i += 16)
            {
                var sb = new StringBuilder();
                sb.Append($"{i:X4}  ");
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < len) sb.Append($"{data[i + j]:X2} ");
                    else sb.Append("   ");
                    if (j == 7) sb.Append(' ');
                }
                sb.Append(" |");
                for (int j = 0; j < 16 && (i + j) < len; j++)
                {
                    byte b = data[i + j];
                    sb.Append((b >= 0x20 && b < 0x7F) ? (char)b : '.');
                }
                sb.Append('|');
                w.WriteLine(sb.ToString());
            }
            if (data.Length > len)
                w.WriteLine($"... {data.Length - len} more bytes truncated");
        }

        static Stream LzxDecompress(BinaryReader br, int compressedSize, int decompressedSize)
        {
            Type lzxType = typeof(ContentManager).Assembly
                .GetType("Microsoft.Xna.Framework.Content.LzxDecoder", throwOnError: true);
            object decoder = Activator.CreateInstance(lzxType,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new object[] { 16 }, null);
            MethodInfo decompress = lzxType.GetMethod("Decompress",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            byte[] inBuf = br.ReadBytes(compressedSize);
            using var inStream = new MemoryStream(inBuf);
            var output = new MemoryStream(decompressedSize);

            int decodedBytes = 0;
            int defaultFrameSize = 0x8000;
            while (decodedBytes < decompressedSize)
            {
                int hi = inStream.ReadByte();
                int lo = inStream.ReadByte();
                if (hi < 0 || lo < 0) break;

                int blockSize, frameSize;
                if (hi == 0xFF)
                {
                    // XNA LZX long-form frame header is 5 bytes: FF, lo, b3, b4, b5.
                    // frame_size = (lo << 8) | b3 ; block_size = (b4 << 8) | b5.
                    // (Earlier `(b3 << 8) | lo` was backwards — it landed below
                    // decompressedSize for BeamFX and corrupted the decode; for
                    // the other 4 effects the wrong-endian value happened to
                    // exceed remaining and got clamped, masking the bug.)
                    int b3 = inStream.ReadByte();
                    frameSize = (lo << 8) | b3;
                    blockSize = (inStream.ReadByte() << 8) | inStream.ReadByte();
                    defaultFrameSize = frameSize;
                }
                else
                {
                    blockSize = (hi << 8) | lo;
                    frameSize = defaultFrameSize;
                }
                if (blockSize == 0 || frameSize == 0) break;

                int remaining = decompressedSize - decodedBytes;
                if (frameSize > remaining) frameSize = remaining;

                decompress.Invoke(decoder, new object[] { inStream, blockSize, output, frameSize });
                decodedBytes += frameSize;
            }

            output.Position = 0;
            return output;
        }

        static int Read7BitEncodedInt(BinaryReader r)
        {
            int result = 0, shift = 0;
            byte b;
            do
            {
                b = r.ReadByte();
                result |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return result;
        }
    }
}
