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
                if (!d.Exists)
                    return false;

                var texInfo = new TextureInfo(texture);
                switch (fmt)
                {
                    case TextureFileFormat.PNG: texInfo.SaveAsPng(outPath); break;
                    case TextureFileFormat.DDS: texInfo.SaveAsDds(outPath); break;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        // picks suitable format to represent the texture without losing quality
        // DXT1/5 compression: DDS
        // RGBA color: PNG
        public bool SaveAutoFormat(Texture2D texture, string outPath)
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
                if (!d.Exists)
                    return false;

                var texInfo = new TextureInfo(texture);
                if (f == SurfaceFormat.Dxt1 || f == SurfaceFormat.Dxt5)
                    texInfo.SaveAsDds(outPath);
                else
                    texInfo.SaveAsPng(outPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
