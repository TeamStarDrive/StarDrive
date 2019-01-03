using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public static class ImageUtils
    {
        // Color is a BGRA little-endian struct
        public static Color[] DecompressDXT5(Texture2D tex)
        {
            var dxtData = new byte[tex.Width * tex.Height];
            tex.GetData(dxtData);
            byte[] pixels = DDSReader.DecompressData(tex.Width, tex.Height, dxtData, DDSReader.PixelFormat.DXT5);
            return BytesToColor(pixels);
        }

        public static Color[] DecompressDXT1(Texture2D tex)
        {
            var dxtData = new byte[(tex.Width * tex.Height) / 2];
            tex.GetData(dxtData);
            byte[] pixels = DDSReader.DecompressData(tex.Width, tex.Height, dxtData, DDSReader.PixelFormat.DXT1);
            return BytesToColor(pixels);
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

        public static unsafe void SaveAsPNG(string filename, int width, int height, Color[] rgbaImage)
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

        public static unsafe void SaveAsDDS(string filename, int width, int height, Color[] rgbaImage)
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
    }
}
