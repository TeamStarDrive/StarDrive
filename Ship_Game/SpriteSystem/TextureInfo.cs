using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Texture;

namespace Ship_Game.SpriteSystem
{
    public class TextureInfo
    {
        public string Name;
        public string Type; // xnb, png, dds, ...
        public int X, Y;
        public int Width;
        public int Height;
        public Texture2D Texture;
        public bool NoPack; // This texture should not be packed

        public override string ToString() => $"X:{X} Y:{Y} W:{Width} H:{Height} Name:{Name} Type:{Type} Format:{Texture?.Format.ToString() ?? ""}";

        public Color[] GetColorData()
        {
            if (Texture == null)
                throw new ObjectDisposedException("TextureData Texture2D ref already disposed");
            Color[] colorData;
            if (Texture.Format == SurfaceFormat.Dxt5)
                colorData = ImageUtils.DecompressDxt5(Texture);
            else if (Texture.Format == SurfaceFormat.Dxt1)
                colorData = ImageUtils.DecompressDxt1(Texture);
            else if (Texture.Format == SurfaceFormat.Color) {
                colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
            } else {
                colorData = new Color[0];
                Log.Error($"Unsupported texture format: {Texture.Format}");
            }
            return colorData;
        }

        // @note this will destroy Texture after transferring it to atlas
        public void TransferTextureToAtlas(Color[] atlas, int atlasWidth, int atlasHeight)
        {
            Color[] colorData = GetColorData();
            ImageUtils.CopyPixelsWithPadding(atlas, atlasWidth, atlasHeight, X, Y, colorData, Width, Height);
        }

        public void DisposeTexture()
        {
            Texture.Dispose(); // save some memory
            Texture = null;
        }

        public void SaveAsPng(string filename)
        {
            Texture.Save(filename, ImageFileFormat.Png);
        }

        public void SaveAsDds(string filename)
        {
            if (Texture.Format == SurfaceFormat.Dxt5 || Texture.Format == SurfaceFormat.Dxt1)
            {
                Texture.Save(filename, ImageFileFormat.Dds); // already compressed
            }
            else if (Texture.Format == SurfaceFormat.Color)
            {
                var colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
                ImageUtils.SaveAsDds(filename, Width, Height, colorData);
            }
            else
            {
                Log.Error($"Unsupported texture format: {Texture.Format}");
            }
        }
    }
}
