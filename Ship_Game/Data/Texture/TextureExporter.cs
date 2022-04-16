using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;
using System;
using System.IO;

namespace Ship_Game.Data.Texture
{
    public enum TextureFileFormat
    {
        PNG,
        DDS,
    }

    public class TextureExporter : TextureInterface
    {
        public TextureExporter(GameContentManager content) : base(content)
        {
        }

        public bool Save(Texture2D texture, string outPath, TextureFileFormat fmt = TextureFileFormat.DDS)
        {
            try
            {
                SurfaceFormat f = texture.Format;
                if (f == SurfaceFormat.NormalizedByte2)
                {
                    Log.Warning($"Unsupported pixel format {f} for {texture.Name} -> {outPath}");
                    return false;
                }

                string dir = Path.GetDirectoryName(outPath);
                DirectoryInfo d = Directory.CreateDirectory(dir);
                if (d.Exists)
                {
                    var texInfo = new TextureInfo(texture);
                    switch (fmt)
                    {
                        case TextureFileFormat.PNG: texInfo.SaveAsPng(outPath); break;
                        case TextureFileFormat.DDS: texInfo.SaveAsDds(outPath); break;
                    }
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public bool Save(Texture3D texture, string outPath)
        {
            try
            {
                SurfaceFormat f = texture.Format;
                string dir = Path.GetDirectoryName(outPath);
                DirectoryInfo d = Directory.CreateDirectory(dir);
                if (d.Exists)
                {
                    texture.Save(outPath, ImageFileFormat.Dds);
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        // picks suitable format to represent the texture without losing quality
        // DXT1/5 compression: DDS
        // RGBA color: PNG
        public bool SaveAutoFormat(Texture2D texture, string outPath)
        {
            return SaveAutoFormat(texture, outPath, out string _);
        }

        public bool SaveAutoFormat(Texture2D texture, string outPath, out string savedPath)
        {
            try
            {
                SurfaceFormat f = texture.Format;
                if (f == SurfaceFormat.NormalizedByte2)
                {
                    Log.Warning($"Unsupported pixel format {f} for {texture.Name} -> {outPath}");
                    savedPath = null;
                    return false;
                }

                string dir = Path.GetDirectoryName(outPath);
                DirectoryInfo d = Directory.CreateDirectory(dir);
                if (d.Exists)
                {
                    var texInfo = new TextureInfo(texture);
                    if (f == SurfaceFormat.Dxt1 || f == SurfaceFormat.Dxt5)
                        savedPath = texInfo.SaveAsDds(outPath);
                    else
                        savedPath = texInfo.SaveAsPng(outPath);
                    return true;
                }
            }
            catch
            {
            }
            savedPath = null;
            return false;
        }

        // previews which extension the texture will be saved as
        public string GetSaveAutoFormatPath(Texture2D texture, string outPath)
        {
            SurfaceFormat f = texture.Format;
            if (f == SurfaceFormat.NormalizedByte2)
                return null;

            if (f == SurfaceFormat.Dxt1 || f == SurfaceFormat.Dxt5)
                return Path.ChangeExtension(outPath, "dds");

            return Path.ChangeExtension(outPath, "png");
        }

        // Takes only the A channel from a Texture2D and converts it into a byte array
        // The texture must be with 1:1 aspect ratio
        public unsafe byte[] ToAlphaBytes(Texture2D texture)
        {
            if (texture == null || texture.IsDisposed)
                return null; // sigh

            if (texture.Width != texture.Height)
            {
                Log.Error($"TextureExporter ToAlphaBytes only supports 1:1 aspect ratio but got: {texture.Width}x{texture.Height}");
                return null;
            }

            if (texture.Format != SurfaceFormat.Color)
            {
                Log.Error("TextureExporter ToAlphaBytes only supports RGBA textures");
                return null;
            }

            try
            {
                // grab a copy of the pixels
                int numPixels = texture.Width * texture.Height;
                byte[] rgba = new byte[numPixels * 4];
                texture.GetData(rgba);

                // allocate temporary buffer for alphas
                byte[] alphas = new byte[numPixels];

                // copy pixels
                fixed (byte* pSource = rgba)
                fixed (byte* pDest = alphas)
                {
                    for (int i = 0; i < numPixels; ++i)
                        pDest[i] = pSource[i*4 + 3]; // dest[i] = color.A
                }

                // finally to base64, if we had a byte* utility, conversion wouldn't be necessary
                return alphas;
            }
            catch (Exception e)
            {
                Log.Error(e, "TextureExporter ToAlphaBytes failed");
                return null;
            }
        }
    }
}
