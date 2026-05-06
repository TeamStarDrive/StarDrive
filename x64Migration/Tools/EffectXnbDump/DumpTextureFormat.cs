// Phase 3.3 alpha-bug probe. Reads an XNB texture far enough to dump:
//   header, type-reader manifest, primary-asset id,
//   then the Texture2D body header: format(int), width(int), height(int), mipCount(int),
//   first-level byte count, and a small RGBA peek of the first 4 pixels.
//
// Used to confirm whether the on-disk loading-screen / win-lose XNBs are
// SurfaceFormat.Color (raw RGBA, premultiplyable in-place) or DXT-block
// compressed (would need a decode-multiply-encode pass).
//
// Run: dotnet run --project x64Migration/Tools/EffectXnbDump --no-build -- dumptex <out.txt> <xnb1> <xnb2> ...
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework.Content;

namespace EffectXnbDump
{
    static class DumpTextureFormat
    {
        // XNA 3.1 SurfaceFormat ordinals — same map as Xna31Compat.
        static readonly Dictionary<int, string> Xna31Names = new()
        {
            { 1,   "Color" },
            { 17,  "Bgr565" },
            { 18,  "Bgra5551" },
            { 19,  "Bgra4444" },
            { 28,  "Dxt1" },
            { 30,  "Dxt3" },
            { 32,  "Dxt5" },
            { 60,  "Alpha8" },
            { 110, "HalfSingle" },
            { 112, "HalfVector2" },
            { 113, "HalfVector4" },
            { 114, "Single" },
            { 115, "Vector2" },
            { 116, "Vector4" },
        };

        public static int Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("usage: dumptex <out.txt> <xnb> [<xnb> ...]");
                return 2;
            }
            string outPath = args[0];
            Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? ".");
            using var w = new StreamWriter(outPath, append: false, Encoding.UTF8);
            for (int i = 1; i < args.Length; i++)
            {
                w.WriteLine();
                w.WriteLine(new string('=', 78));
                w.WriteLine($"FILE: {args[i]}");
                w.WriteLine(new string('=', 78));
                Console.WriteLine($"Dumping {Path.GetFileName(args[i])} ...");
                try { Dump(args[i], w); }
                catch (Exception ex)
                {
                    w.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}");
                }
            }
            Console.WriteLine($"Wrote {outPath}");
            return 0;
        }

        static void Dump(string path, StreamWriter w)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs);
            if (br.ReadByte() != 'X' || br.ReadByte() != 'N' || br.ReadByte() != 'B')
                throw new InvalidDataException("Not an XNB");
            br.ReadByte(); br.ReadByte();              // target, version
            byte flags = br.ReadByte();
            int totalSize = br.ReadInt32();

            Stream body;
            bool needsClose = false;
            if ((flags & 0x80) != 0)
            {
                int decompressed = br.ReadInt32();
                int compressed = totalSize - 14;
                body = LzxDecompress(br, compressed, decompressed);
                needsClose = true;
            }
            else body = fs;

            try
            {
                using var brB = new BinaryReader(body, Encoding.UTF8, leaveOpen: needsClose);
                int numReaders = Read7Bit(brB);
                w.WriteLine($"readerCount={numReaders}");
                for (int i = 0; i < numReaders; i++)
                {
                    string name = brB.ReadString();
                    int v = brB.ReadInt32();
                    w.WriteLine($"  [{i}] v={v}: {name}");
                }
                Read7Bit(brB);   // sharedResourceCount
                Read7Bit(brB);   // primaryAssetReaderId

                int formatRaw = brB.ReadInt32();
                int width     = brB.ReadInt32();
                int height    = brB.ReadInt32();
                int levels    = brB.ReadInt32();
                Xna31Names.TryGetValue(formatRaw, out string fname);
                w.WriteLine($"format={formatRaw} ({fname ?? "?"})  {width}x{height}  levels={levels}");

                int firstByteCount = brB.ReadInt32();
                w.WriteLine($"level[0] byteCount={firstByteCount}  (bpp={(double)(firstByteCount * 8) / (width * height):F3})");

                if (firstByteCount > 0 && firstByteCount <= 64)
                {
                    byte[] peek = brB.ReadBytes(firstByteCount);
                    var sb = new StringBuilder("level[0] bytes: ");
                    foreach (byte b in peek) sb.Append($"{b:X2} ");
                    w.WriteLine(sb.ToString());
                }
                else if (firstByteCount > 0)
                {
                    byte[] peek = brB.ReadBytes(64);
                    var sb = new StringBuilder("level[0] first 64 bytes: ");
                    foreach (byte b in peek) sb.Append($"{b:X2} ");
                    w.WriteLine(sb.ToString());
                    if (formatRaw == 1)  // raw RGBA: peek 4 pixels
                    {
                        w.WriteLine($"  pixel[0] = ({peek[0]:X2}, {peek[1]:X2}, {peek[2]:X2}, A={peek[3]:X2})");
                        w.WriteLine($"  pixel[1] = ({peek[4]:X2}, {peek[5]:X2}, {peek[6]:X2}, A={peek[7]:X2})");
                    }
                }
            }
            finally { if (needsClose) body.Dispose(); }
        }

        static Stream LzxDecompress(BinaryReader br, int compressedSize, int decompressedSize)
        {
            Type lzxType = typeof(ContentManager).Assembly.GetType("Microsoft.Xna.Framework.Content.LzxDecoder", true);
            object decoder = Activator.CreateInstance(lzxType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { 16 }, null);
            MethodInfo decompress = lzxType.GetMethod("Decompress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            byte[] inBuf = br.ReadBytes(compressedSize);
            using var inStream = new MemoryStream(inBuf);
            var output = new MemoryStream(decompressedSize);
            int decoded = 0; int defaultFrame = 0x8000;
            while (decoded < decompressedSize)
            {
                int hi = inStream.ReadByte(); int lo = inStream.ReadByte();
                if (hi < 0 || lo < 0) break;
                int blockSize, frameSize;
                if (hi == 0xFF) { int b3 = inStream.ReadByte(); frameSize = (b3 << 8) | lo; blockSize = (inStream.ReadByte() << 8) | inStream.ReadByte(); defaultFrame = frameSize; }
                else { blockSize = (hi << 8) | lo; frameSize = defaultFrame; }
                if (blockSize == 0 || frameSize == 0) break;
                int remaining = decompressedSize - decoded;
                if (frameSize > remaining) frameSize = remaining;
                decompress.Invoke(decoder, new object[] { inStream, blockSize, output, frameSize });
                decoded += frameSize;
            }
            output.Position = 0;
            return output;
        }

        static int Read7Bit(BinaryReader r)
        {
            int result = 0, shift = 0; byte b;
            do { b = r.ReadByte(); result |= (b & 0x7F) << shift; shift += 7; } while ((b & 0x80) != 0);
            return result;
        }
    }
}
