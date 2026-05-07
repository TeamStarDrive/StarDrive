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
        // Raw XNB type-reader strings for XNA 3.1-baked content. These XNBs store
        // reader type names as BARE namespace-qualified names (no assembly, version,
        // culture, or public key token). Confirmed by Xna31Compat.DumpXnbTypeReaders
        // on Content/Fonts/Arial14Bold.xnb (target=w, ver=4). typeCreators is
        // matched exact-string against the XNB blob BEFORE PrepareType normalizes
        // anything, so these exact keys are what we must register.
        const string Texture2DReaderName = "Microsoft.Xna.Framework.Content.Texture2DReader";
        const string Texture3DReaderName = "Microsoft.Xna.Framework.Content.Texture3DReader";

        static bool Registered;

        public static void Register()
        {
            if (Registered) return;
            Registered = true;

            ContentTypeReaderManager.AddTypeCreator(Texture2DReaderName, () => new Xna31Texture2DReader());
            ContentTypeReaderManager.AddTypeCreator(Texture3DReaderName, () => new Xna31Texture3DReader());

            // Phase 3.4 step 5 / TODO Phase 4: XNA 3.1 VertexDeclaration binary format
            // reader. Empirically decoded — see Xna31VertexDeclarationReader. Originally
            // intended to unblock runtime XNB Model loads, but Phase 3.4 pivoted to an
            // offline FBX export pipeline (legacy/mesh_exporter_xna31 branch + commit
            // 9bd3b7128) and Phase B archived all Model XNBs (commits 6f68b9396 +
            // a5da742b4). Zero Model XNBs ship today; this registration is preserved
            // as defensive infrastructure for the unlikely case a mod ever ships an
            // XNA-3.1-baked Model XNB. Note: VertexDeclaration alone is not enough —
            // the Model XNB itself has structural drift, so a full path also needs an
            // Xna31ModelReader (deferred Phase 4).
            ContentTypeReaderManager.AddTypeCreator(
                "Microsoft.Xna.Framework.Content.VertexDeclarationReader",
                () => new Xna31VertexDeclarationReader());
        }

        // Shared translation table — XNA 3.1 SurfaceFormat int → MonoGame 3.8 SurfaceFormat.
        // Used by both Xna31Texture2DReader and Xna31Texture3DReader.
        internal static readonly Dictionary<int, SurfaceFormat> Xna31SurfaceFormatMap = new()
        {
            { 1,   SurfaceFormat.Color       },
            { 17,  SurfaceFormat.Bgr565      },
            { 18,  SurfaceFormat.Bgra5551    },
            { 19,  SurfaceFormat.Bgra4444    },
            { 28,  SurfaceFormat.Dxt1        },
            { 30,  SurfaceFormat.Dxt3        },
            { 32,  SurfaceFormat.Dxt5        },
            { 60,  SurfaceFormat.Alpha8      },
            { 110, SurfaceFormat.HalfSingle  },
            { 112, SurfaceFormat.HalfVector2 },
            { 113, SurfaceFormat.HalfVector4 },
            { 114, SurfaceFormat.Single      },
            { 115, SurfaceFormat.Vector2     },
            { 116, SurfaceFormat.Vector4     },
        };

        internal static readonly HashSet<int> WarnedSurfaceFormats = new();

        internal static SurfaceFormat TranslateSurfaceFormat(int raw, string readerName)
        {
            if (Xna31SurfaceFormatMap.TryGetValue(raw, out SurfaceFormat mapped))
                return mapped;
            lock (WarnedSurfaceFormats)
            {
                if (WarnedSurfaceFormats.Add(raw))
                    Log.Warning($"{readerName}: unknown XNA 3.1 SurfaceFormat={raw}, defaulting to Color");
            }
            return SurfaceFormat.Color;
        }

        // Phase 3.3 alpha-blend mismatch fix.
        //
        // XNA 3.1's default SpriteBlendMode.AlphaBlend used the non-premultiplied
        // formula `dst = src.rgb*src.a + dst*(1-src.a)`. MonoGame's BlendState.AlphaBlend
        // (despite the name) uses the premultiplied formula `dst = src.rgb + dst*(1-src.a)`.
        // XNA-3.1-baked textures with white-RGB-where-transparent — a common artist
        // convention — render as opaque white edges in MonoGame because the unpremul'd
        // (255,255,255) gets added in directly. Loading screens, win/lose UI panels,
        // and other 3.1-era XNB textures all hit this.
        //
        // Fix: premultiply at load time for raw RGBA. Block-compressed (DXT) and other
        // packed formats can't be trivially premultiplied without decode/re-encode and
        // are warned-once instead — most game art baked as DXT in the 3.1 era was
        // premultiplied at content-pipeline time anyway. Atlas PNGs are unaffected
        // (they go through TexImport.Load, not this reader, and were already
        // premul-baked in Phase 2.7.B per project_phase2_png_rb_swap.md).
        static readonly HashSet<SurfaceFormat> WarnedUnsupportedFormats = new();

        internal static void PremultiplyAlphaIfNeeded(byte[] data, SurfaceFormat format, string readerName)
        {
            switch (format)
            {
                case SurfaceFormat.Color:
                    // R/B byte-swap: XNA 3.1's SurfaceFormat.Color was D3DFMT_A8R8G8B8
                    // (memory byte order B G R A); MonoGame's SurfaceFormat.Color is
                    // memory byte order R G B A. Hand the bytes to SetData unchanged
                    // and the red and blue channels render swapped — confirmed visually
                    // on loading-screen XNBs (the in-game art rendered blue-tinted
                    // until this fix). Phase 2.7.B handled the DXT and PNG paths but
                    // never touched raw-RGBA XNBs. Run this before the premultiply so
                    // downstream byte[i]=R/byte[i+2]=B assumptions hold.
                    SwapRedBlue(data);

                    // Heuristic: only premultiply textures that look non-premul.
                    // In a premultiplied texture every pixel satisfies RGB ≤ A; finding
                    // a pixel with RGB > A on any channel proves the buffer is not yet
                    // premul'd. The XNA 3.1 content pipeline could be configured to
                    // premultiply at bake time (used by some font atlases and effects);
                    // we must skip those to avoid double-premultiplying and dulling the
                    // anti-aliased edges. Confirmed visually 2026-05-03 — without this
                    // guard, font glyphs render dim and "unclear".
                    if (LooksNonPremultiplied(data))
                        PremultiplyRgba8888(data);
                    return;

                // No alpha channel — nothing to do.
                case SurfaceFormat.Bgr565:
                case SurfaceFormat.Alpha8:
                case SurfaceFormat.HalfSingle:
                case SurfaceFormat.Single:
                case SurfaceFormat.HalfVector2:
                case SurfaceFormat.Vector2:
                    return;

                // DXT block formats and packed 16-bit alpha formats: premul would need
                // decode/re-encode. Skip with a one-shot warning so a regression in this
                // surface area is at least noisy. Note: Dxt3 fonts are intercepted upstream
                // in Xna31Texture2DReader.Read and decompressed to SurfaceFormat.Color
                // before reaching this method (MonoGame WindowsDX 3.8 BC2 alpha sampling
                // produces solid quads for the XNA 3.1 font atlas layout — squares-as-text
                // bug). This case still triggers for non-font Dxt3 textures if any exist.
                case SurfaceFormat.Dxt1:
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                case SurfaceFormat.Bgra5551:
                case SurfaceFormat.Bgra4444:
                case SurfaceFormat.HalfVector4:
                case SurfaceFormat.Vector4:
                    lock (WarnedUnsupportedFormats)
                    {
                        if (WarnedUnsupportedFormats.Add(format))
                            Log.Info($"{readerName}: skipping premultiply for {format} (block/packed format); rendering may show a white-edge ghost if the source XNB used non-premultiplied alpha.");
                    }
                    return;

                default:
                    lock (WarnedUnsupportedFormats)
                    {
                        if (WarnedUnsupportedFormats.Add(format))
                            Log.Warning($"{readerName}: no premultiply path for {format}; alpha behavior may be incorrect under MonoGame's premultiplied AlphaBlend.");
                    }
                    return;
            }
        }

        // Software BC2 (Dxt3) decompression. Workaround for the squares-as-text bug
        // documented in commit 10b35d779: MonoGame WindowsDX 3.8's GPU BC2 alpha
        // sampling produces solid white quads for XNA 3.1's font atlas layout (white
        // RGB, alpha encodes the glyph). Decoding to RGBA8888 at load time costs ~4×
        // VRAM (font atlases are tiny — Arial14Bold goes 128KB → 512KB) but routes
        // sampling through the well-tested SurfaceFormat.Color path instead.
        //
        // BC2 block layout (16 bytes per 4×4 block, row-major across the texture):
        //   bytes 0..7 : 16 explicit 4-bit alphas, row-major within block
        //                (byte 0 low nibble = pixel(0,0), high nibble = pixel(1,0))
        //   bytes 8..9 : color0 (RGB565, little-endian)
        //   bytes 10..11: color1 (RGB565, little-endian)
        //   bytes 12..15: 16 × 2-bit color indices, row-major within block
        //                 (byte 12 holds row 0, bits 0..1 = pixel(0,0))
        // BC2 always uses 4-color BC1 mode (color0 ≥ color1 path; transparent-black
        // mode is suppressed because alpha is carried explicitly).
        internal static byte[] DecompressDxt3ToRgba8888(byte[] dxt3, int width, int height)
        {
            int bx = (width + 3) >> 2;
            int by = (height + 3) >> 2;
            var rgba = new byte[width * height * 4];

            // Hoisted out of the per-block loop (CA2014: stackalloc inside a loop
            // can grow the stack frame across iterations on some CLRs).
            Span<byte> palette = stackalloc byte[12];

            for (int blockY = 0; blockY < by; blockY++)
            {
                for (int blockX = 0; blockX < bx; blockX++)
                {
                    int srcOff = (blockY * bx + blockX) * 16;
                    if (srcOff + 16 > dxt3.Length) return rgba; // truncated source — bail

                    ulong alpha64 = BitConverter.ToUInt64(dxt3, srcOff);
                    ushort c0 = BitConverter.ToUInt16(dxt3, srcOff + 8);
                    ushort c1 = BitConverter.ToUInt16(dxt3, srcOff + 10);
                    uint indices = BitConverter.ToUInt32(dxt3, srcOff + 12);

                    int c0r = ((c0 >> 11) & 0x1F) * 255 / 31;
                    int c0g = ((c0 >> 5)  & 0x3F) * 255 / 63;
                    int c0b = ( c0        & 0x1F) * 255 / 31;
                    int c1r = ((c1 >> 11) & 0x1F) * 255 / 31;
                    int c1g = ((c1 >> 5)  & 0x3F) * 255 / 63;
                    int c1b = ( c1        & 0x1F) * 255 / 31;

                    palette[0] = (byte)c0r; palette[1] = (byte)c0g; palette[2]  = (byte)c0b;
                    palette[3] = (byte)c1r; palette[4] = (byte)c1g; palette[5]  = (byte)c1b;
                    palette[6] = (byte)((2 * c0r + c1r) / 3);
                    palette[7] = (byte)((2 * c0g + c1g) / 3);
                    palette[8] = (byte)((2 * c0b + c1b) / 3);
                    palette[9]  = (byte)((c0r + 2 * c1r) / 3);
                    palette[10] = (byte)((c0g + 2 * c1g) / 3);
                    palette[11] = (byte)((c0b + 2 * c1b) / 3);

                    for (int py = 0; py < 4; py++)
                    {
                        int y = (blockY << 2) + py;
                        if (y >= height) break;
                        for (int px = 0; px < 4; px++)
                        {
                            int x = (blockX << 2) + px;
                            if (x >= width) break;

                            int texelIdx = (py << 2) + px;
                            int colorIdx = (int)((indices >> (texelIdx * 2)) & 0x3) * 3;
                            int a4 = (int)((alpha64 >> (texelIdx * 4)) & 0xF);

                            int dstOff = (y * width + x) * 4;
                            rgba[dstOff]     = palette[colorIdx];
                            rgba[dstOff + 1] = palette[colorIdx + 1];
                            rgba[dstOff + 2] = palette[colorIdx + 2];
                            rgba[dstOff + 3] = (byte)((a4 << 4) | a4);
                        }
                    }
                }
            }
            return rgba;
        }

        // In-place R↔B byte swap on an interleaved 32bpp buffer (4 bytes per pixel).
        // Used to translate XNA 3.1 BGRA byte layout to MonoGame's RGBA byte layout
        // for SurfaceFormat.Color XNB textures. Idempotent applies its own inverse —
        // never call twice on the same buffer.
        static void SwapRedBlue(byte[] rgba)
        {
            int n = rgba.Length & ~3;
            for (int i = 0; i < n; i += 4)
            {
                byte b = rgba[i];
                rgba[i]     = rgba[i + 2];
                rgba[i + 2] = b;
            }
        }

        // Returns true if the buffer is provably non-premultiplied (any pixel with
        // RGB > A on any channel). Returns false if the buffer is consistent with a
        // premultiplied or fully-opaque texture. Short-circuits on the first
        // non-premul pixel — typical art textures fail the check within a few pixels.
        static bool LooksNonPremultiplied(byte[] rgba)
        {
            int n = rgba.Length & ~3;
            for (int i = 0; i < n; i += 4)
            {
                byte a = rgba[i + 3];
                if (rgba[i] > a || rgba[i + 1] > a || rgba[i + 2] > a)
                    return true;
            }
            return false;
        }

        // In-place premultiply of an RGBA8888 buffer (matches SurfaceFormat.Color
        // memory layout: byte[i+3] holds alpha regardless of channel-order swap).
        // The +127 rounding term keeps round-trip behavior of pure-opaque (a=255)
        // and pure-transparent (a=0) pixels exact; intermediate alphas are slightly
        // biased toward the upper integer to match D3D's reference premul.
        static void PremultiplyRgba8888(byte[] rgba)
        {
            int n = rgba.Length & ~3;
            for (int i = 0; i < n; i += 4)
            {
                byte a = rgba[i + 3];
                if (a == 255) continue;
                if (a == 0)
                {
                    rgba[i] = 0; rgba[i + 1] = 0; rgba[i + 2] = 0;
                    continue;
                }
                rgba[i]     = (byte)((rgba[i]     * a + 127) / 255);
                rgba[i + 1] = (byte)((rgba[i + 1] * a + 127) / 255);
                rgba[i + 2] = (byte)((rgba[i + 2] * a + 127) / 255);
            }
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
    // Empirical verification (2026-05-01) on game/Content/Fonts/Arial14Bold.xnb:
    //   rawFmt=30 -> Dxt3, 256x512, byteCount=131072 (1.000 bpp).
    //   First-block bytes: `00 00 00 0F 00 F0 00 00 FF FF FF FF 00 00 00 00`
    //   = 8 bytes of 4-bit-per-pixel alpha (mostly transparent gutter + sparse
    //   opaque glyph pixels), then 0xFFFF/0xFFFF white color endpoints, then
    //   all-color0 indices. Classic Dxt3 font atlas layout. Mapping confirmed.
    // Other observed formats: rawFmt=28 -> Dxt1 (0.5 bpp), rawFmt=1 -> Color (4 bpp).
    public class Xna31Texture2DReader : ContentTypeReader<Texture2D>
    {
        protected override Texture2D Read(ContentReader reader, Texture2D existingInstance)
        {
            int formatRaw = reader.ReadInt32();
            SurfaceFormat format = Xna31Compat.TranslateSurfaceFormat(formatRaw, "Xna31Texture2DReader");

            int width      = reader.ReadInt32();
            int height     = reader.ReadInt32();
            int levelCount = reader.ReadInt32();

            GraphicsDevice device = reader.GetGraphicsDevice();

            // Dxt3 GPU BC2 alpha sampling is broken for XNA 3.1 font atlases under
            // MonoGame WindowsDX 3.8 (squares-as-text bug; see commit 10b35d779
            // and the Phase 2.3 rebake's commit message). Decode to RGBA8888 in
            // software at load time so the texture lives as SurfaceFormat.Color and
            // sampling goes through the known-good 32bpp path. Each mip is decoded
            // independently; the resulting Texture2D is created in Color format.
            if (format == SurfaceFormat.Dxt3)
            {
                var texture = new Texture2D(device, width, height, levelCount > 1, SurfaceFormat.Color);
                int mipW = width;
                int mipH = height;
                for (int level = 0; level < levelCount; level++)
                {
                    int byteCount = reader.ReadInt32();
                    byte[] dxt3 = reader.ReadBytes(byteCount);
                    byte[] rgba = Xna31Compat.DecompressDxt3ToRgba8888(dxt3, mipW, mipH);
                    Xna31Compat.PremultiplyAlphaIfNeeded(rgba, SurfaceFormat.Color, "Xna31Texture2DReader[Dxt3->Color]");
                    texture.SetData(level, null, rgba, 0, rgba.Length);
                    mipW = Math.Max(1, mipW >> 1);
                    mipH = Math.Max(1, mipH >> 1);
                }
                return texture;
            }

            {
                var texture = new Texture2D(device, width, height, levelCount > 1, format);
                for (int level = 0; level < levelCount; level++)
                {
                    int byteCount = reader.ReadInt32();
                    byte[] data = reader.ReadBytes(byteCount);
                    Xna31Compat.PremultiplyAlphaIfNeeded(data, format, "Xna31Texture2DReader");
                    texture.SetData(level, null, data, 0, byteCount);
                }
                return texture;
            }
        }
    }


    // 3D texture analog. XNB layout is also unchanged between 3.1 and 4.0; only the
    // SurfaceFormat int needs translating. Used for volume textures like the
    // `Effects/NoiseVolume` referenced by Thruster.
    public class Xna31Texture3DReader : ContentTypeReader<Texture3D>
    {
        protected override Texture3D Read(ContentReader reader, Texture3D existingInstance)
        {
            int formatRaw = reader.ReadInt32();
            SurfaceFormat format = Xna31Compat.TranslateSurfaceFormat(formatRaw, "Xna31Texture3DReader");

            int width      = reader.ReadInt32();
            int height     = reader.ReadInt32();
            int depth      = reader.ReadInt32();
            int levelCount = reader.ReadInt32();

            GraphicsDevice device = reader.GetGraphicsDevice();
            var texture = new Texture3D(device, width, height, depth, levelCount > 1, format);

            for (int level = 0; level < levelCount; level++)
            {
                int byteCount = reader.ReadInt32();
                byte[] data = reader.ReadBytes(byteCount);
                Xna31Compat.PremultiplyAlphaIfNeeded(data, format, "Xna31Texture3DReader");
                texture.SetData(level, 0, 0, width, height, 0, depth, data, 0, byteCount);
            }

            return texture;
        }
    }
}
