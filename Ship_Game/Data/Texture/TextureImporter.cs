using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data.Texture
{
    public class TextureImporter : TextureInterface
    {
        public TextureImporter(GameContentManager content) : base(content)
        {
        }
        
        public Texture2D Load(string texturePath)
        {
            using (var fs = new FileStream(texturePath, FileMode.Open))
            {
                Texture2D tex = Texture2D.FromFile(Device, fs);
                tex.Name = texturePath;
                return tex;
            }
        }
        
        public Texture2D Load(FileInfo textureFile)
        {
            using (FileStream fs = textureFile.OpenRead())
            {
                Texture2D tex = Texture2D.FromFile(Device, fs);
                tex.Name = textureFile.RelPath();
                return tex;
            }
        }
    }
}
