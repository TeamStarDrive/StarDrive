using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = SDGraphics.Rectangle;
#pragma warning disable CA1060
#pragma warning disable CA2101

namespace Ship_Game.Data.Texture
{
    [Flags]
    public enum DDSFlags
    {
        //! Use DXT1 compression. Alpha channel will be discarded.
        Dxt1 = ( 1 << 0 ),
        //! Use DXT5 compression.
        Dxt5 = ( 1 << 1 ),
        //! Source is BGRA rather than RGBA
        SourceBGRA = ( 1 << 2 ),
        //! Source is BGRA rather than RGBA
        Dxt1BGRA = Dxt1 | SourceBGRA,
        Dxt5BGRA = Dxt5 | SourceBGRA,
    }

    public static class ImageUtils
    {
        // Color is a BGRA little-endian struct
        public static Color[] DecompressDxt5(Texture2D tex)
        {
            var dxtData = new byte[tex.Width * tex.Height];
            tex.GetData(dxtData);
            return DxtReader.DecompressData(tex.Width, tex.Height, dxtData, DxtReader.PixelFormat.DXT5);
        }

        public static Color[] DecompressDxt1(Texture2D tex)
        {
            var dxtData = new byte[(tex.Width * tex.Height) / 2];
            tex.GetData(dxtData);
            return DxtReader.DecompressData(tex.Width, tex.Height, dxtData, DxtReader.PixelFormat.DXT1);
        }

        static unsafe Color[] BytesToColor(byte[] pixels)
        {
            var colors = new Color[pixels.Length / 4];
            fixed (byte* pDecompressed = pixels)
            {
                byte* src = pDecompressed;
                for (int i = 0; i < colors.Length; ++i, src+=4)
                {
                    colors[i] = new Color(src[0], src[1], src[2], src[3]);
                }
            }
            return colors;
        }

        [DllImport("SDNative.dll")]
        static extern unsafe void ConvertBGRAtoRGBA(int width, int height, Color* rgbaImage);

        public static unsafe void ConvertToRGBA(int width, int height, Color[] bgraImage)
        {
            fixed (Color* pColor = bgraImage)
                ConvertBGRAtoRGBA(width, height, pColor);
        }

        [DllImport("SDNative.dll")]
        static extern unsafe void CopyBGRAtoRGBA(int width, int height, Color* src, Color* dst);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        delegate void OnImageLoaded([MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)] Color[] color,
                                    int size, int width, int height);

        [DllImport("SDNative.dll")]
        static extern IntPtr LoadPNGImage([MarshalAs(UnmanagedType.LPStr)] string filename,
                                          OnImageLoaded onLoaded);

        // Default `premultiplyAlpha=true` since Phase 3.3 (2026-05-03): MonoGame's
        // BlendState.AlphaBlend expects premul bytes, and most direct PNG callers
        // (YouLose/YouWin text panels, cursors, faction art, ad-hoc UI sprites)
        // need premul to avoid white-edge halos. PremultiplyAlpha is idempotent
        // (heuristic-skipped on already-premul buffers — see its body), so atlas-
        // pipeline reloads of premul-baked atlas-PNG files don't double-premul.
        // Atlas SOURCE PNG loads end up premul'd by this default, but downstream
        // CreateAtlasTexture and LoadDds calls to PremultiplyAlpha are now no-ops
        // for already-premul data. Net effect: every path converges to "premul'd
        // on GPU exactly once".
        public static Texture2D LoadPng(GraphicsDevice device, string filename, bool premultiplyAlpha = true)
        {
            Texture2D tex = null;
            void OnLoaded(Color[] color, int size, int width, int height)
            {
                if (premultiplyAlpha)
                    PremultiplyAlpha(color, size);
                tex = new Texture2D(device, width, height, false, SurfaceFormat.Color);
                tex.SetData(color);
            }

            IntPtr error = LoadPNGImage(filename, OnLoaded);
            if (error != IntPtr.Zero)
            {
                string message = Marshal.PtrToStringAnsi(error);
                throw new Exception($"Load PNG {filename} failed: {message}");
            }
            return tex;
        }

        public static Texture2D LoadDds(GraphicsDevice device, string filename)
        {
            using FileStream fs = File.OpenRead(filename);
            var dds = new DxtReader(fs);
            if (dds.DecodedImage.Length == 0 || dds.Width == 0 || dds.Height == 0)
                throw new Exception($"Load DDS {filename} failed: DxtReader produced no pixels");
            // DxtReader's internal buffer is over-sized; trim to base-level pixel count.
            int pixelCount = dds.Width * dds.Height;
            if (dds.DecodedImage.Length < pixelCount)
                throw new Exception($"Load DDS {filename} failed: decoded {dds.DecodedImage.Length} pixels, need {pixelCount}");
            // Pre-multiply RGB by alpha. DxtReader produces non-premultiplied data, but
            // MonoGame's SpriteBatch default (BlendState.AlphaBlend) is premultiplied.
            // Without this, A=0 pixels keep their RGB and the AlphaBlend blend formula
            // (result.rgb = src.rgb + dest.rgb*(1-src.a)) saturates to white wherever
            // the source is bright-and-transparent (e.g. Bridge.dds viewport area, which
            // is R=255 G=255 B=255 A=0 in the source DDS). XNA 3.1's Texture2D.FromFile
            // / MonoGame's Texture2D.FromStream both pre-multiply on PNG load; this
            // function (added in §2.3) needs to match that contract.
            PremultiplyAlpha(dds.DecodedImage, pixelCount);
            var tex = new Texture2D(device, dds.Width, dds.Height, false, SurfaceFormat.Color);
            tex.SetData(dds.DecodedImage, 0, pixelCount);
            return tex;
        }

        // Idempotent in-place premultiply.
        //
        // Phase 3.3 (2026-05-03): added heuristic skip so callers that may receive
        // already-premul data (LoadDds reloading a premul'd atlas, CreateAtlasTexture
        // composing pixels sourced from premul'd LoadPng output, etc.) don't double-
        // multiply. The heuristic mirrors Xna31Compat.PremultiplyAlphaIfNeeded /
        // Primitives2D.ToPremulIfNeeded: a pixel with RGB > A on any channel proves
        // the buffer is non-premul; if no such pixel exists the buffer is consistent
        // with premultiplied or fully-opaque data and is left untouched. False
        // positives (re-premul'ing a premul buffer) are mathematically impossible
        // because RGB > A is incompatible with `RGB = orig_RGB * A`.
        public static void PremultiplyAlpha(Color[] pixels, int count)
        {
            // Heuristic short-circuit: scan for proof of non-premul. Returns on the
            // first non-premul pixel — already-premul buffers pay an O(N) scan with
            // no writes, which is fine at load time.
            bool needsPremul = false;
            for (int i = 0; i < count; ++i)
            {
                Color c = pixels[i];
                if (c.R > c.A || c.G > c.A || c.B > c.A)
                {
                    needsPremul = true;
                    break;
                }
            }
            if (!needsPremul)
                return;

            for (int i = 0; i < count; ++i)
            {
                Color c = pixels[i];
                if (c.A == 255) continue; // opaque: no change
                pixels[i] = new Color((byte)((c.R * c.A) / 255),
                                      (byte)((c.G * c.A) / 255),
                                      (byte)((c.B * c.A) / 255),
                                      c.A);
            }
        }

        [DllImport("SDNative.dll")]
        static extern unsafe IntPtr SaveImageAsPNG(
            [MarshalAs(UnmanagedType.LPStr)] string filename, int width, int height, Color* rgbaImage);

        public static unsafe void SaveAsPng(string filename, int width, int height, Color[] rgbaImage)
        {
            fixed (Color* pColor = rgbaImage)
            {
                IntPtr error = SaveImageAsPNG(filename, width, height, pColor);
                if (error != IntPtr.Zero)
                {
                    string message = Marshal.PtrToStringAnsi(error);
                    Log.Error($"Save PNG {filename} failed: {message}");
                }
            }
        }


        [DllImport("SDNative.dll")]
        static extern unsafe IntPtr SaveImageAsDDS(
            [MarshalAs(UnmanagedType.LPStr)] string filename, int width, int height, Color* rgbaImage, DDSFlags flags);

        public static unsafe void ConvertToDDS(string filename, int width, int height, Color[] rgbaImage, DDSFlags flags)
        {
            if (width == 0 || height == 0)
                throw new ArgumentException($"DDS Width/Height cannot be zero: {width}x{height}");

            fixed (Color* pColor = rgbaImage)
            {
                IntPtr error = SaveImageAsDDS(filename, width, height, pColor, flags);
                if (error != IntPtr.Zero)
                {
                    string message = Marshal.PtrToStringAnsi(error);
                    Log.Error($"Save DDS {filename} failed: {message}");
                }
            }
        }


        // Phase 2.3: managed replacement for SDNative's CopyPixelsPadded. The native
        // version takes Image-by-value structs (dst, src) which use the x86 stack-passing
        // ABI; in x64 the ABI passes 16-byte structs by hidden pointer in RCX, so the
        // C#→C++ argument shape no longer matches and the C++ side dereferences pixel
        // data as a struct pointer (AccessViolationException). Atlas-build-time only,
        // perf cost negligible. Same semantics as the C++ version: copy src rect to
        // dst[x,y] PLUS replicate edge pixels into a 1-pixel padding gutter so atlas
        // sampling at rect boundaries doesn't bleed.
        public static void CopyPixelsWithPadding(Color[] dst, int dstWidth, int dstHeight,
                                                 int x, int y, Color[] src, int w, int h)
        {
            // Main rect: src(0..w, 0..h) -> dst(x..x+w, y..y+h)
            for (int sy = 0; sy < h; ++sy)
                Array.Copy(src, sy * w, dst, (y + sy) * dstWidth + x, w);

            // 1px padding gutter (replicates edge pixels of src into a border around the dst rect).
            bool padTop    = y > 0;
            bool padBottom = (y + h) < dstHeight;
            bool padLeft   = x > 0;
            bool padRight  = (x + w) < dstWidth;

            if (padTop)    Array.Copy(src, 0,           dst, (y - 1) * dstWidth + x, w);
            if (padBottom) Array.Copy(src, (h - 1) * w, dst, (y + h) * dstWidth + x, w);
            if (padLeft)
                for (int sy = 0; sy < h; ++sy)
                    dst[(y + sy) * dstWidth + (x - 1)] = src[sy * w];
            if (padRight)
                for (int sy = 0; sy < h; ++sy)
                    dst[(y + sy) * dstWidth + (x + w)] = src[sy * w + (w - 1)];

            // 4 corner pixels
            if (padTop    && padLeft)  dst[(y - 1) * dstWidth + (x - 1)] = src[0];
            if (padTop    && padRight) dst[(y - 1) * dstWidth + (x + w)] = src[w - 1];
            if (padBottom && padLeft)  dst[(y + h) * dstWidth + (x - 1)] = src[(h - 1) * w];
            if (padBottom && padRight) dst[(y + h) * dstWidth + (x + w)] = src[(h - 1) * w + (w - 1)];
        }

        // Fills pixels with an uniform color. Managed replacement for SDNative's
        // FillPixels (same x64-ABI bug as CopyPixelsPadded). Bounds-clamped per the
        // C++ semantics (endX/endY clamp to dst dimensions).
        public static void FillPixels(Color[] dst, int dstWidth, int dstHeight,
                                      int x, int y, Color color, int w, int h)
        {
            int endX = Math.Min(x + w - 1, dstWidth  - 1);
            int endY = Math.Min(y + h - 1, dstHeight - 1);
            for (int iy = y; iy <= endY; ++iy)
                for (int ix = x; ix <= endX; ++ix)
                    dst[iy * dstWidth + ix] = color;
        }

        // Draws a hollow rectangle (purely for debugging)
        public static void DrawRectangle(Color[] image, int width, int height, Rectangle r, Color color)
        {
            if (r.Height == 0) { Log.Error("DrawRectangle r.Height cannot be 0"); return; }
            if (r.Width == 0)  { Log.Error("DrawRectangle r.Width  cannot be 0"); return; }

            int x = r.X;
            int y = r.Y;
            int endX = x + (r.Width - 1);
            if (endX >= width) endX = width - 1;
            int endY = y + (r.Height - 1);
            if (endY >= height) endY = height - 1;

            for (int ix = x; ix <= endX; ++ix) // top and bottom ----
            {
                image[(y * width) + ix] = color;
                image[(endY * width) + ix] = color;
            }
            for (int iy = y; iy <= endY; ++iy) // | left and right |
            {
                image[(iy * width) + x] = color;
                image[(iy * width) + endX] = color;
            }
        }

        // @return TRUE if image has at least 1 transparent pixel (A != 255).
        // Phase 2.3: managed replacement for SDNative's HasTransparentPixels (same
        // x64-ABI bug as CopyPixelsPadded — Image-by-value mismatch with C# pointer
        // signature). Atlas-build-time path; perf cost negligible.
        public static bool HasTransparentPixels(Color[] img, int width, int height)
        {
            int n = Math.Min(img.Length, width * height);
            for (int i = 0; i < n; ++i)
                if (img[i].A != 255) return true;
            return false;
        }

        /// <summary>
        /// Converts the supplied 32-bit BGRA map into a non-multiplied OR pre-multiplied alpha map,
        /// from RGB Luminosity: A = (B + G + R)/3
        /// </summary>
        /// <param name="toPreMultipliedAlpha">If true, pixel=[A,A,A,A] if false, pixel=[255,255,255,A]</param>
        public static unsafe void ConvertToAlphaMap(Texture2D rgbMap, bool toPreMultipliedAlpha)
        {
            if (rgbMap.Format == SurfaceFormat.Color)
            {
                int numPixels = rgbMap.Width * rgbMap.Height * 4;
                var pixels = new byte[numPixels];
                rgbMap.GetData(pixels);

                fixed (byte* pPixels = pixels)
                {
                    if (toPreMultipliedAlpha)
                    {
                        for (int i = 0; i < numPixels; i += 4)
                        {
                            byte* pixel = pPixels + i; // note: XNA uses BGR
                            byte b = pixel[0];
                            byte g = pixel[1];
                            byte r = pixel[2];
                            byte a = (byte)((b + g + r) / 3);
                            pixel[0] = a; // B := A
                            pixel[1] = a; // G := A
                            pixel[2] = a; // R := A
                            pixel[3] = a; // A := A
                        }
                    }
                    else
                    {
                        for (int i = 0; i < numPixels; i += 4)
                        {
                            byte* pixel = pPixels + i; // note: XNA uses BGR
                            byte b = pixel[0];
                            byte g = pixel[1];
                            byte r = pixel[2];
                            byte a = (byte)((b + g + r) / 3);
                            pixel[0] = 255;
                            pixel[1] = 255;
                            pixel[2] = 255;
                            pixel[3] = a; // A := A
                        }
                    }
                }

                rgbMap.SetData(pixels);
            }
            else
            {
                throw new Exception("ConvertRGBToRGBAlphaMap failed: Texture is not an RGB Color texture");
            }
        }
    }
}
