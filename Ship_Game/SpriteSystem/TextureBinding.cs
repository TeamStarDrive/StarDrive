using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.SpriteSystem
{
    public class TextureBinding
    {
        public SubTexture SubTex;
        public Texture2D Texture;
        public int Width;
        public int Height;
        public string Name;
        public string UnpackedPath;
        public string AtlasFolder;

        public TextureBinding(SubTexture tex, int w, int h, string name, string unpackedPath, string atlasFolder)
        {
            SubTex = tex;
            Width = w;
            Height = h;
            Name = name;
            UnpackedPath = unpackedPath;
            AtlasFolder = atlasFolder;
        }

        public SubTexture GetOrLoadTexture()
        {
            if (SubTex != null)
                return SubTex;

            // load the texture if we already didn't
            if (Texture == null)
            {
                var file = new FileInfo(UnpackedPath);
                Texture = ResourceManager.RootContent.LoadUncachedTexture(file, AtlasFolder);
            }

            SubTex = new SubTexture(Name, Texture);
            return SubTex;
        }
    }
}