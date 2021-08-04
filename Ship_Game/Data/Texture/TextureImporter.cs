using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

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
            TextureCreationParameters p = XGraphics.Texture.GetCreationParameters(Device, fullPath);
            var tex = (Texture2D)XGraphics.Texture.FromFile(Device, fullPath, p);
            return tex;
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
    }
}
