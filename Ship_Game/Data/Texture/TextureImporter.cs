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
            tex.Name = FileSystemExtensions.GetAppRootRelPath(texturePath);
            return tex;
        }
        
        public Texture2D Load(FileInfo textureFile)
        {
            return Load(textureFile.FullName);
        }

        Texture2D LoadTexture(string fullPath)
        {
            if (Type == ImporterType.ImageUtilsPNG_XnaDDS)
                return ImageUtilsPNG_XnaDDS(fullPath);
            return LoadXna(fullPath);
        }

        Texture2D LoadXna(string fullPath)
        {
            // TODO Phase 4: XNA 3.1's Texture.GetCreationParameters/FromFile removed in MonoGame.
            // Texture2D.FromStream is the working substitute; revisit if format-specific
            // metadata recovery is ever needed (currently no consumers ask for it).
            try
            {
                using FileStream fs = File.OpenRead(fullPath);
                return Texture2D.FromStream(Device, fs);
            }
            catch (Exception e)
            {
                throw new($"LoadTexture XNA failed: {fullPath}", e);
            }
        }

        Texture2D ImageUtilsPNG_XnaDDS(string fullPath)
        {
            if (fullPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                return ImageUtils.LoadPng(Device, fullPath);
            if (fullPath.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                return ImageUtils.LoadDds(Device, fullPath);
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

                // Phase 3.7 step 3: write rgb = alpha (premultiplied storage)
                // so the loaded FogMap composites correctly under the LightsTarget
                // path's premul AlphaBlend. Pre-fix this stored rgb=255 always,
                // which made every saved-FogMap pixel sample as fully-bright
                // (the premul blend's dst*(1-srcA) term overshot and clamped),
                // so loading any save lifted the entire fog overlay to 100%.
                fixed (byte* pAlphas = alphas)
                fixed (byte* pPixels = pixels)
                {
                    for (int i = 0; i < numPixels; ++i)
                    {
                        byte a = pAlphas[i];
                        pPixels[i*4]     = a;
                        pPixels[i*4 + 1] = a;
                        pPixels[i*4 + 2] = a;
                        pPixels[i*4 + 3] = a;
                    }
                }

                // finally create the texture and set the image pixels
                var t = new Texture2D(Device, width, width, false, SurfaceFormat.Color);
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
