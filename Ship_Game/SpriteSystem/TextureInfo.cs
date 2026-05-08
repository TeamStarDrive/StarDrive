using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
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
        // §4.6 #7: when true and the texture is Color+alpha, SaveAsDds writes
        // a PNG (lossless) instead of DXT5 (banded). Set per-folder via
        // ResourceManager.AtlasLosslessAlphaFolders during CreateTextureInfos.
        public bool LosslessAlpha;

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

        public string SaveAsPng(string filename)
        {
            string path = Path.ChangeExtension(filename, "png");
            using FileStream fs = File.Create(path);
            Texture.SaveAsPng(fs, Texture.Width, Texture.Height);
            return path;
        }

        public string SaveAsDds(string filename)
        {
            string ddsPath = Path.ChangeExtension(filename, "dds");
            string pngPath = Path.ChangeExtension(filename, "png");
            SurfaceFormat format = Texture.Format;
            if (format == SurfaceFormat.Dxt5 || format == SurfaceFormat.Dxt1)
            {
                // Round-trip already-compressed DXT bytes back to a real DDS file
                // (header + raw blocks, no re-encoding). MonoGame removed XNA's
                // Texture2D.Save(path, ImageFileFormat.Dds); without this writer
                // the cache fallback was SaveAsPng — decode DXT to RGBA then PNG
                // encode, ~300ms per nopack texture vs sub-ms here. The big
                // nopack atlases (Suns 33 nopack, PlanetTiles 23 nopack, Nebulas
                // 20 nopack) dominated full-rebuild time pre this fix.
                bool isDxt5 = format == SurfaceFormat.Dxt5;
                int blockBytes = isDxt5 ? 16 : 8;
                int blocksWide = Math.Max(1, (Texture.Width + 3) / 4);
                int blocksHigh = Math.Max(1, (Texture.Height + 3) / 4);
                var blocks = new byte[blocksWide * blocksHigh * blockBytes];
                Texture.GetData(blocks);
                ImageUtils.WriteCompressedDds(ddsPath, Texture.Width, Texture.Height, blocks, isDxt5);
                return ddsPath;
            }
            if (format == SurfaceFormat.Color)
            {
                var color = new Color[Texture.Width * Texture.Height];
                Texture.GetData(color);

                bool alpha = ImageUtils.HasTransparentPixels(color, Width, Height);

                if (alpha && LosslessAlpha)
                {
                    // §4.6 #7: avoid DXT5 alpha-quantization artifacts on smooth
                    // alpha gradients (UI/node's circular alpha mask read as a
                    // visible dark ring at the sensor-circle edge in the FOW
                    // composite). DXT5 alpha is 8-level-per-4x4-block; for
                    // gradient textures this produces banding that bilinear
                    // sampling doesn't fully smooth. Folder must be in
                    // ResourceManager.AtlasLosslessAlphaFolders to opt in;
                    // game-art atlases (Suns, Nebulas, PlanetTiles, etc.) take
                    // the DXT5 fast path below — banding is invisible against
                    // the noisy art content, and PNG encoding is ~20× slower
                    // per nopack texture on full-rebuild.
                    ImageUtils.SaveAsPng(pngPath, Width, Height, color);
                    return pngPath;
                }

                DDSFlags flags = alpha ? DDSFlags.Dxt5 : DDSFlags.Dxt1;
                ImageUtils.ConvertToDDS(ddsPath, Width, Height, color, flags);
                return ddsPath;
            }
            if (format == SurfaceFormat.Bgr32)
            {
                var color = new Color[Texture.Width * Texture.Height];
                Texture.GetData(color);
                ImageUtils.ConvertToDDS(ddsPath, Width, Height, color, DDSFlags.Dxt1);
                return ddsPath;
            }
            Log.Error($"Unsupported format '{format}' from texture '{Name}.{Type}': "
                      +"Ensure you are using BGRA32 or BGR32 textures.");
            return ddsPath;
        }
    }
}
