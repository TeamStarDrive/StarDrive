using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace Ship_Game.Data
{
    public static class Xna31Compat
    {
        // Raw XNB type-reader strings for XNA 3.1-baked Texture2D readers. These XNBs
        // store the reader type as a BARE namespace-qualified name (no assembly,
        // version, culture, or public key token). Confirmed by Xna31Compat.DumpXnbTypeReaders
        // on Content/Fonts/Arial14Bold.xnb (target=w, ver=4). typeCreators is matched
        // exact-string against the XNB blob BEFORE PrepareType normalizes anything,
        // so this exact key is what we must register.
        static readonly string[] Texture2DReaderVariants =
        {
            "Microsoft.Xna.Framework.Content.Texture2DReader",
        };

        static bool Registered;

        public static void Register()
        {
            if (Registered) return;
            Registered = true;

            foreach (string s in Texture2DReaderVariants)
                ContentTypeReaderManager.AddTypeCreator(s, () => new Xna31Texture2DReader());
        }

        // Diagnostic: decompresses the given XNB and dumps its type-reader strings
        // and primary-asset reader id. Use to discover the exact strings the XNBs
        // contain when the variant list above misses. Calls into MonoGame's internal
        // LzxDecoder via reflection (3.8 keeps it `internal`).
        public static void DumpXnbTypeReaders(string xnbPath, string outputLogPath = null)
        {
            var lines = new List<string>();
            try
            {
                using var fs = File.OpenRead(xnbPath);
                using var br = new BinaryReader(fs);

                if (br.ReadByte() != 'X' || br.ReadByte() != 'N' || br.ReadByte() != 'B')
                {
                    lines.Add($"DumpXnbTypeReaders: '{xnbPath}' is not an XNB file");
                    return;
                }
                byte target = br.ReadByte();
                byte version = br.ReadByte();
                byte flags = br.ReadByte();
                int totalSize = br.ReadInt32();

                lines.Add($"=== XNB '{xnbPath}' target={(char)target} ver={version} flags=0x{flags:X2} totalSize={totalSize} ===");

                Stream body;
                if ((flags & 0x80) != 0)
                {
                    int decompressedSize = br.ReadInt32();
                    int compressedSize = totalSize - 14;
                    body = LzxDecompress(br, compressedSize, decompressedSize);
                }
                else
                {
                    body = fs;
                }

                using var brBody = new BinaryReader(body);
                int numReaders = Read7BitEncodedInt(brBody);
                lines.Add($"readerCount={numReaders}");
                for (int i = 0; i < numReaders; i++)
                {
                    string name = brBody.ReadString();
                    int rdrVersion = brBody.ReadInt32();
                    lines.Add($"  [{i}] v={rdrVersion}: {name}");
                }
            }
            catch (Exception ex)
            {
                lines.Add($"DumpXnbTypeReaders failed for '{xnbPath}': {ex.GetType().Name}: {ex.Message}");
                lines.Add(ex.StackTrace ?? "");
            }
            finally
            {
                foreach (string line in lines) Log.Info(line);
                if (outputLogPath != null)
                {
                    try { File.WriteAllLines(outputLogPath, lines); } catch { /* best-effort */ }
                }
            }
        }

        static Stream LzxDecompress(BinaryReader br, int compressedSize, int decompressedSize)
        {
            // MonoGame 3.8 has internal class Microsoft.Xna.Framework.Content.LzxDecoder
            // with ctor LzxDecoder(int windowBits) and method
            // int Decompress(Stream input, int inLen, Stream output, int outLen).
            Type lzxType = typeof(ContentManager).Assembly
                .GetType("Microsoft.Xna.Framework.Content.LzxDecoder", throwOnError: true);
            object decoder = Activator.CreateInstance(lzxType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { 16 }, null);
            MethodInfo decompress = lzxType.GetMethod("Decompress", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var output = new MemoryStream(decompressedSize);
            // The XNB body is split into LZX frames; each frame has a 5-byte header:
            //   byte hi, byte lo, byte hi, byte lo, byte flag (0xFF marker, optional)
            // and is decoded one frame at a time. Mirror MonoGame's outer ContentReader logic.
            int pos = 0;
            int outPos = 0;
            int bytesRemaining = compressedSize;
            byte[] inBuf = br.ReadBytes(bytesRemaining);
            using var inStream = new MemoryStream(inBuf);
            int frameOutSize = 0x8000;
            int frameInSize = 0;

            while (inStream.Position < inStream.Length)
            {
                int hi = inStream.ReadByte();
                int lo = inStream.ReadByte();
                if (hi == 0xFF)
                {
                    frameOutSize = (lo << 8) | inStream.ReadByte();
                    frameInSize  = (inStream.ReadByte() << 8) | inStream.ReadByte();
                }
                else
                {
                    frameOutSize = 0x8000;
                    frameInSize  = (hi << 8) | lo;
                }
                if (frameOutSize == 0 || frameInSize == 0) break;

                long beforeIn = inStream.Position;
                long beforeOut = output.Position;
                decompress.Invoke(decoder, new object[] { inStream, frameInSize, output, frameOutSize });
                pos += (int)(inStream.Position - beforeIn);
                outPos += (int)(output.Position - beforeOut);
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


    // Phase 2.2 step 2: XNA 3.1-baked Texture2D XNBs encode SurfaceFormat using XNA 3.1's
    // D3D9-aligned enum (Color=1, Dxt5=32, Alpha8=60, ...). MonoGame 3.8's enum is a
    // smaller, contiguous subset (Color=0, Dxt5=6, Alpha8=12, ...). When MonoGame's
    // built-in Texture2DReader reads the int and casts to SurfaceFormat directly, an
    // XNA 3.1 Color (=1) becomes MonoGame Bgr565, a 3.1 Alpha8 (=60) becomes an
    // out-of-range value and falls through to a 4 byte/pixel default — the source of
    // the "elementCount * sizeof(T) is X, but data size is 4*X" SpriteFont crash.
    //
    // The XNB layout itself is unchanged between 3.1 and 4.0 (int format, int W, int H,
    // int mipCount, then per-mip {int byteCount, byte[] bytes}). Only the format int
    // needs translating. Register this reader via Xna31Compat.Register() at startup
    // so it intercepts both standalone Texture2D XNBs and the embedded Texture2D
    // inside SpriteFont XNBs (SpriteFontReader → InnerReadObject<Texture2D>).
    public class Xna31Texture2DReader : ContentTypeReader<Texture2D>
    {
        // XNA 3.1 SurfaceFormat (D3D9 D3DFORMAT-aligned) → MonoGame 3.8 SurfaceFormat
        static readonly Dictionary<int, SurfaceFormat> Xna31FormatMap = new()
        {
            { 1,   SurfaceFormat.Color       }, // XNA 3.1 Color (32bpp R8G8B8A8)
            { 17,  SurfaceFormat.Bgr565      },
            { 18,  SurfaceFormat.Bgra5551    },
            { 19,  SurfaceFormat.Bgra4444    },
            { 28,  SurfaceFormat.Dxt1        },
            { 30,  SurfaceFormat.Dxt3        },
            { 32,  SurfaceFormat.Dxt5        },
            { 60,  SurfaceFormat.Alpha8      }, // SpriteFont fonts are typically Alpha8
            { 110, SurfaceFormat.HalfSingle  },
            { 112, SurfaceFormat.HalfVector2 },
            { 113, SurfaceFormat.HalfVector4 },
            { 114, SurfaceFormat.Single      },
            { 115, SurfaceFormat.Vector2     },
            { 116, SurfaceFormat.Vector4     },
        };

        static readonly HashSet<int> WarnedFormats = new();

        protected override Texture2D Read(ContentReader reader, Texture2D existingInstance)
        {
            int formatRaw = reader.ReadInt32();
            SurfaceFormat format = TranslateFormat(formatRaw);

            int width      = reader.ReadInt32();
            int height     = reader.ReadInt32();
            int levelCount = reader.ReadInt32();

            lock (WarnedFormats)
            {
                if (WarnedFormats.Add(formatRaw))
                    Log.Info($"Xna31Texture2DReader: rawFmt={formatRaw} -> {format} ({width}x{height}, mips={levelCount})");
            }

            GraphicsDevice device = reader.GetGraphicsDevice();
            var texture = new Texture2D(device, width, height, levelCount > 1, format);

            for (int level = 0; level < levelCount; level++)
            {
                int byteCount = reader.ReadInt32();
                byte[] data = reader.ReadBytes(byteCount);
                texture.SetData(level, null, data, 0, byteCount);
            }

            return texture;
        }

        // XNA 3.1 SurfaceFormat ints DO NOT collide cleanly with MonoGame's enum:
        // XNA 3.1 Dxt5 = 32 == MonoGame Bgra32SRgb (also 32). XNA 3.1 Color = 1
        // == MonoGame Bgr565 (also 1). The naive `(SurfaceFormat)raw` cast or
        // `Enum.IsDefined` shortcut would mis-map silently. Always go through
        // the translation table; only fall through if the value is unknown.
        static SurfaceFormat TranslateFormat(int raw)
        {
            if (Xna31FormatMap.TryGetValue(raw, out SurfaceFormat mapped))
                return mapped;

            lock (WarnedFormats)
            {
                if (WarnedFormats.Add(raw))
                    Log.Warning($"Xna31Texture2DReader: unknown XNA 3.1 SurfaceFormat={raw}, defaulting to Color");
            }
            return SurfaceFormat.Color;
        }
    }
}
