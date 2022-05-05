using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = SDGraphics.Rectangle;

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

        public static Texture2D LoadPng(GraphicsDevice device, string filename)
        {
            Texture2D tex = null;
            void OnLoaded(Color[] color, int size, int width, int height)
            {
                tex = new Texture2D(device, width, height, 1, TextureUsage.None, SurfaceFormat.Color);
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


        [DllImport("SDNative.dll")]
        static extern unsafe IntPtr CopyPixelsPadded(Color* dst, int dstWidth, int dstHeight, 
                                                     int x, int y, Color* src, int w, int h);

        public static unsafe void CopyPixelsWithPadding(Color[] dst, int dstWidth, int dstHeight, 
                                                        int x, int y, Color[] src, int w, int h)
        {
            fixed (Color* pDst = dst)
            {
                fixed (Color* pSrc = src)
                {
                    CopyPixelsPadded(pDst, dstWidth, dstHeight, x, y, pSrc, w, h);
                }
            }
        }

        [DllImport("SDNative.dll")]
        static extern unsafe IntPtr FillPixels(Color* dst, int dstWidth, int dstHeight,
                                               int x, int y, Color color, int w, int h);

        // Fills pixels with an uniform color
        public static unsafe void FillPixels(Color[] dst, int dstWidth, int dstHeight, 
                                             int x, int y, Color color, int w, int h)
        {
            fixed (Color* pDst = dst)
            {
                FillPixels(pDst, dstWidth, dstHeight, x, y, color, w, h);
            }
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

        [DllImport("SDNative.dll")]
        static extern unsafe bool HasTransparentPixels(Color* img, int width, int height);

        // @return TRUE if image has at least 1 transparent pixel (A != 255)
        public static unsafe bool HasTransparentPixels(Color[] img, int width, int height)
        {
            fixed (Color* pImg = img)
            {
                return HasTransparentPixels(pImg, width, height);
            }
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
