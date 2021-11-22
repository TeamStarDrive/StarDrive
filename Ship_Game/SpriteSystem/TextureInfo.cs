﻿using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Texture;

namespace Ship_Game.SpriteSystem
{
    public class TextureInfo
    {
        public string Name;
        public string Type; // xnb, png, dds, ...
        public string UnpackedPath; // where we can load the unpacked texture
        public int X, Y;
        public int Width;
        public int Height;
        public Texture2D Texture;
        public bool NoPack; // This texture should not be packed

        public string SourcePath;

        public int Bottom => Y + Height;

        public TextureInfo()
        {
        }

        public TextureInfo(Texture2D texture)
        {
            Name = texture.Name;
            Texture = texture;
            Width = texture.Width;
            Height = texture.Height;
        }

        public override string ToString() => $"X:{X} Y:{Y} W:{Width} H:{Height} Name:{Name} Type:{Type} Format:{Texture?.Format.ToString() ?? ""}";

        // @note this will destroy Texture after transferring it to atlas
        public void TransferTextureToAtlas(Color[] atlas, int atlasWidth, int atlasHeight)
        {
            Color[] colorData;
            SurfaceFormat format = Texture.Format;
            if (format == SurfaceFormat.Dxt5)
            {
                colorData = ImageUtils.DecompressDxt5(Texture);
            }
            else if (format == SurfaceFormat.Dxt1)
            {
                colorData = ImageUtils.DecompressDxt1(Texture);
            }
            else if (format == SurfaceFormat.Color)
            {
                colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
            }
            else if (format == SurfaceFormat.Bgr32)
            {
                colorData = new Color[Texture.Width * Texture.Height];
                Texture.GetData(colorData);
            }
            else
            {
                Log.Error($"Unsupported format '{format}' from texture '{Name}.{Type}': "
                          +"Ensure you are using RGBA32 textures. Filling atlas rectangle with RED.");
                ImageUtils.FillPixels(atlas, atlasWidth, atlasHeight, X, Y, Color.Red, Width, Height);
                return;
            }

            ImageUtils.CopyPixelsWithPadding(atlas, atlasWidth, atlasHeight, X, Y, colorData, Width, Height);
        }

        public bool HasAlpha
        {
            get
            {
                SurfaceFormat format = Texture.Format;
                return format == SurfaceFormat.Color
                    || format == SurfaceFormat.Dxt5
                    || format == SurfaceFormat.Dxt3;
            }
        }

        public void DisposeTexture()
        {
            Texture.Dispose(); // save some memory
            Texture = null;
        }

        public void SaveAsPng(string filename)
        {
            string path = Path.ChangeExtension(filename, "png");
            Texture.Save(path, ImageFileFormat.Png);
        }

        public void SaveAsDds(string filename)
        {
            string path = Path.ChangeExtension(filename, "dds");
            SurfaceFormat format = Texture.Format;
            if (format == SurfaceFormat.Dxt5 || format == SurfaceFormat.Dxt1)
            {
                Texture.Save(path, ImageFileFormat.Dds); // already compressed
            }
            else if (format == SurfaceFormat.Color)
            {
                var color = new Color[Texture.Width * Texture.Height];
                Texture.GetData(color);

                bool alpha = ImageUtils.HasTransparentPixels(color, Width, Height);
                
                DDSFlags flags = alpha ? DDSFlags.Dxt5BGRA : DDSFlags.Dxt1BGRA;
                ImageUtils.ConvertToDDS(path, Width, Height, color, flags);
            }
            else if (format == SurfaceFormat.Bgr32)
            {
                var color = new Color[Texture.Width * Texture.Height];
                Texture.GetData(color);
                ImageUtils.ConvertToDDS(path, Width, Height, color, DDSFlags.Dxt1BGRA);
            }
            else
            {
                Log.Error($"Unsupported format '{format}' from texture '{Name}.{Type}': "
                          +"Ensure you are using BGRA32 or BGR32 textures.");
            }
        }
    }
}
