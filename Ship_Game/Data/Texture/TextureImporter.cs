using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;

namespace Ship_Game.Data.Texture
{
    using XGraphics = Microsoft.Xna.Framework.Graphics;

    public class TextureImporter : TextureInterface
    {
        enum ImporterType
        {
            XNA,
            ImageUtilsPNG_XnaDDS, // PNG-s from Image Utils
        }

        ImporterType Type = ImporterType.ImageUtilsPNG_XnaDDS;

        public TextureImporter(GameContentManager content) : base(content)
        {
        }
        
        public Texture2D Load(string texturePath)
        {
            Texture2D tex = LoadTexture(texturePath);
            tex.Name = texturePath;
            return tex;
        }
        
        public Texture2D Load(FileInfo textureFile)
        {
            Texture2D tex = LoadTexture(textureFile.FullName);
            tex.Name = textureFile.RelPath();
            return tex;
        }

        Texture2D LoadTexture(string fullPath)
        {
            if (Type == ImporterType.ImageUtilsPNG_XnaDDS)
                return ImageUtilsPNG_XnaDDS(fullPath);
            return LoadXna(fullPath);
        }

        Texture2D LoadXna(string fullPath)
        {
            try
            {
                TextureCreationParameters p = XGraphics.Texture.GetCreationParameters(Device, fullPath);
                var tex = (Texture2D)XGraphics.Texture.FromFile(Device, fullPath, p);
                return tex;
            }
            catch (Exception e)
            {
                throw new($"LoadTexture XNA failed: {fullPath}", e);
            }
        }

        Texture2D ImageUtilsPNG_XnaDDS(string fullPath)
        {
            if (fullPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                Texture2D tex = ImageUtils.LoadPng(Device, fullPath);
                return tex;
            }
            return LoadXna(fullPath);
        }

        // Converts a AlphaOnly byte[] into a 1:1 aspect ratio RGBA texture
        // The base value for pixels will be 255
        public unsafe Texture2D FromAlphaOnly(byte[] alphas)
        {
            try
            {
                // convert from Base64 to raw bytes
                int width = (int)Math.Sqrt(alphas.Length);

                // allocate temporary buffer for pixels
                int numPixels = width*width;
                var pixels = new byte[numPixels*4];

                // copy alphas
                fixed (byte* pAlphas = alphas)
                fixed (byte* pPixels = pixels)
                {
                    for (int i = 0; i < numPixels; ++i)
                    {
                        pPixels[i*4]     = 255;
                        pPixels[i*4 + 1] = 255;
                        pPixels[i*4 + 2] = 255;
                        pPixels[i*4 + 3] = pAlphas[i];
                    }
                }

                // finally create the texture and set the image pixels
                var t = new Texture2D(Device, width, width, 0, TextureUsage.Linear, SurfaceFormat.Color);
                t.SetData(pixels);
                //t.Save(Dir.StarDriveAppData + "/Saved Games/fog.debug.png", ImageFileFormat.Png);
                return t;
            }
            catch (Exception e)
            {
                Log.Error(e, "TextureImporter FromAlphaOnly failed");
                return null;
            }
        }
    }
}
