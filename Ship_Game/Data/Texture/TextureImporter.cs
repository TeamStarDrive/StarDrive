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
                return Texture2D.FromFile(Device, fs);
            }
        }
        
        public Texture2D Load(FileInfo textureFile)
        {
            using (FileStream fs = textureFile.OpenRead())
            {
                return Texture2D.FromFile(Device, fs);
            }
        }
    }
}
