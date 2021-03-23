using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Texture
{
    public class TextureExporter : TextureInterface
    {
        public TextureExporter(GameContentManager content) : base(content)
        {
        }

        public bool Save(Texture2D texture, string outPath, bool png = false)
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
                var d = Directory.CreateDirectory(dir);
                if (!d.Exists)
                    return false;

                var texInfo = new TextureInfo();
                texInfo.Texture = texture;
                if (png)
                    texInfo.SaveAsPng(outPath);
                else
                    texInfo.SaveAsDds(outPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
