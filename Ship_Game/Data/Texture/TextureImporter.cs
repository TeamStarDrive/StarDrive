using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.Data.Texture
{
    using XGraphics = Microsoft.Xna.Framework.Graphics;

    public class TextureImporter : TextureInterface
    {
        public TextureImporter(GameContentManager content) : base(content)
        {
        }
        
        public Texture2D Load(string texturePath)
        {
            TextureCreationParameters parameters = XGraphics.Texture.GetCreationParameters(Device, texturePath);

            var tex = (Texture2D)XGraphics.Texture.FromFile(Device, texturePath, parameters);
            tex.Name = texturePath;
            return tex;
        }
        
        public Texture2D Load(FileInfo textureFile)
        {
            string fullPath = textureFile.FullName;
            TextureCreationParameters parameters = XGraphics.Texture.GetCreationParameters(Device, fullPath);

            var tex = (Texture2D)XGraphics.Texture.FromFile(Device, fullPath, parameters);
            tex.Name = textureFile.RelPath();
            return tex;
        }
    }
}
