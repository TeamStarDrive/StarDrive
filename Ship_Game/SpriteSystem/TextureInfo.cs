using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game.SpriteSystem
{
    class TextureInfo
    {
        public string Name;
        public int X, Y;
        public int Width;
        public int Height;
        public Texture2D Texture;
        public bool NoPack; // This texture should not be packed

        // @note this will destroy Texture after transferring it to atlas
        public void TransferTextureToAtlas(Color[] atlas, int atlasWidth)
        {
            if (Texture == null)
                throw new ObjectDisposedException("TextureData Texture2D ref already disposed");

            Color[] colorData;
            if (Texture.Format == SurfaceFormat.Dxt5)
                colorData = ImageUtils.DecompressDxt5(Texture);
            else if (Texture.Format == SurfaceFormat.Dxt1)
                colorData = ImageUtils.DecompressDxt1(Texture);
            else if (Texture.Format == SurfaceFormat.Color)
            {
                colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
            }
            else
            {
                colorData = new Color[0];
                Log.Error($"Unsupported atlas texture format: {Texture.Format}");
            }
            Texture.Dispose(); // save some memory
            Texture = null;

            //ImageUtils.SaveAsPng($"{TextureCacheDir}/{texName}.png", Width, Height, ColorData);
            ImageUtils.CopyPixelsTo(atlas, atlasWidth, X, Y, colorData, Width, Height);
        }
    }
}
