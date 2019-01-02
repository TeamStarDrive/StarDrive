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
            byte[] pixels = DDSImage.DecompressData(tex.Width, tex.Height, dxtData, DDSImage.PixelFormat.DXT5);
            return BytesToColor(pixels);
        }

        public static Color[] DecompressDXT1(Texture2D tex)
        {
            var dxtData = new byte[(tex.Width * tex.Height) / 2];
            tex.GetData(dxtData);
            byte[] pixels = DDSImage.DecompressData(tex.Width, tex.Height, dxtData, DDSImage.PixelFormat.DXT1);
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
        private static extern unsafe IntPtr SaveBGRAImageAsPng(
            [MarshalAs(UnmanagedType.LPStr)] string filename, int width, int height, Color* bgraImage);

        public static unsafe void SaveAsPng(string filename, int width, int height, Color[] bgraImage)
        {
            fixed (Color* pColor = bgraImage)
            {
                // Color is a BGRA little-endian struct
                IntPtr error = SaveBGRAImageAsPng(filename, width, height, pColor);
                if (error != IntPtr.Zero)
                {
                    string message = Marshal.PtrToStringAnsi(error);
                    Log.Error($"Save PNG {filename} failed: {message}");
                }
            }
        }
    }
}
