﻿using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data.Texture
{
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
            [MarshalAs(UnmanagedType.LPStr)] string filename, int width, int height, Color* rgbaImage);

        public static unsafe void SaveAsDds(string filename, int width, int height, Color[] rgbaImage)
        {
            fixed (Color* pColor = rgbaImage)
            {
                IntPtr error = SaveImageAsDDS(filename, width, height, pColor);
                if (error != IntPtr.Zero)
                {
                    string message = Marshal.PtrToStringAnsi(error);
                    Log.Error($"Save DDS {filename} failed: {message}");
                }
            }
        }


        [DllImport("SDNative.dll")]
        static extern unsafe IntPtr CopyPixelsPadded(
            Color* dst, int dstWidth, int dstHeight, int x, int y, Color* src, int w, int h);

        public static unsafe void CopyPixelsWithPadding(Color[] dst, int dstWidth, int dstHeight, int x, int y, Color[] src, int w, int h)
        {
            fixed (Color* pDst = dst)
            {
                fixed (Color* pSrc = src)
                {
                    CopyPixelsPadded(pDst, dstWidth, dstHeight, x, y, pSrc, w, h);
                }
            }
        }

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

    }
}
