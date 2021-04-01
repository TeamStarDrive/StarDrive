using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data.Texture;

namespace Ship_Game.SpriteSystem
{
    public partial class TextureAtlas
    {
        [Flags]
        enum AtlasFlags
        {
            None = 0,
            Alpha = (1 << 0),
            Compress = (1 << 1)
        }

        void CreateAtlasTexture(Color[] color, AtlasFlags flags, string texturePath)
        {
            if ((flags & AtlasFlags.Compress) != 0)
            {
                // We compress the DDS color into DXT5 and then reload it later through XNA
                // DXT5 size in mem after loading is 4x smaller than RGBA32, but quality sucks!
                // DXT1 size in mem is 8x smaller than RGBA32
                DDSFlags format = (flags & AtlasFlags.Alpha) != 0 ? DDSFlags.Dxt5BGRA : DDSFlags.Dxt1BGRA;
                ImageUtils.ConvertToDDS(texturePath, Width, Height, color, format);
            }
            else
            {
                // For this atlas, compression is forbidden, so we save with BGRA color
                // Although this will take 4x more memory
                using (var atlas = new Texture2D(ResourceManager.RootContent.Device, 
                                                 Width, Height, 1, TextureUsage.None, SurfaceFormat.Color))
                {
                    atlas.SetData(color);
                    atlas.Save(texturePath, ImageFileFormat.Dds);
                    atlas.Dispose();
                }
            }
        }

        void ExportTexture(TextureInfo t)
        {
            string filePathNoExt = Path.GetExportPath(t);
            if (ExportPng) t.SaveAsPng($"{filePathNoExt}.png");
            if (ExportDds) t.SaveAsDds($"{filePathNoExt}.dds");
        }

        void CreateAtlas(FileInfo[] textureFiles)
        {
            int transfer = 0, save = 0;
            Stopwatch total = Stopwatch.StartNew();
            Stopwatch perf = Stopwatch.StartNew();

            TextureInfo[] textures = CreateTextureInfos(Path, textureFiles);
            int load = perf.NextMillis();

            var packer = new TexturePacker(Path.Texture);
            NumPacked = packer.PackTextures(textures);
            NonPacked = textures.Length - NumPacked;
            Width = packer.Width;
            Height = packer.Height;
            var flags = AtlasFlags.None;
            int pack = perf.NextMillis();

            if (NonPacked > 0)
            {
                string compressedCacheDir = Path.GetCompressedCacheDir();
                foreach (TextureInfo t in textures)
                {
                    if (t.NoPack)
                    {
                        t.UnpackedPath = $"{compressedCacheDir}{t.Name}.dds";
                        t.SaveAsDds(t.UnpackedPath);
                    }
                }
            }

            if (NumPacked > 0)
            {
                var atlasPixels = new Color[Width * Height];
                if (!ResourceManager.AtlasNoCompressFolders.Contains(Path.OriginalName))
                {
                    flags |= AtlasFlags.Compress;
                }

                foreach (TextureInfo t in textures) // copy pixels
                {
                    if (ExportTextures) ExportTexture(t);
                    if (!t.NoPack)
                    {
                        if (t.HasAlpha)
                            flags |= AtlasFlags.Alpha;
                        t.TransferTextureToAtlas(atlasPixels, Width, Height);
                        if (DebugDrawBounds)
                            ImageUtils.DrawRectangle(atlasPixels, Width, Height,
                                new Rectangle(t.X, t.Y, t.Width, t.Height), Color.YellowGreen);
                    }

                    t.DisposeTexture(); // dispose all, even nonpacked textures, we don't know if they will be used so need to free the mem
                }

                packer.DrawDebug(atlasPixels, Width, Height);
                transfer = perf.NextMillis();

                CreateAtlasTexture(atlasPixels, flags, Path.Texture);
                save = perf.NextMillis();
            }

            CreateLookup(textures);
            SaveAtlasDescriptor(textures, Path.Descriptor);

            int elapsed = total.NextMillis();
            Log.Write(ConsoleColor.Blue,
                $"{Mod} CreateAtlas {this} t:{elapsed,4}ms l:{load} p:{pack} t:{transfer} s:{save}");
            if (NumPacked > 0 && (flags & AtlasFlags.Compress) == 0)
                Log.Write(ConsoleColor.Blue, $"Compression Disabled: {Path.OriginalName}");
        }

        bool TryLoadCache()
        {
            Stopwatch s = Stopwatch.StartNew();
            if (!File.Exists(Path.Descriptor)) return false; // regenerate!!

            using (var fs = new StreamReader(Path.Descriptor))
            {
                int.TryParse(fs.ReadLine(), out int version);
                ulong.TryParse(fs.ReadLine(), out ulong oldHash);
                if (version != Version || oldHash != Hash)
                {
                    Log.Write(ConsoleColor.Cyan, $"{Mod} AtlasCache  {Name}  INVALIDATED");
                    return false; // hash mismatch, we need to regenerate cache
                }

                Lookup.Clear();
                Name = fs.ReadLine();
                int.TryParse(fs.ReadLine(), out int width);
                int.TryParse(fs.ReadLine(), out int height);
                int.TryParse(fs.ReadLine(), out NumPacked);
                int.TryParse(fs.ReadLine(), out NonPacked);
                Atlas = null; // we will lazy-load it later
                Width = width;
                Height = height;
                if (NumPacked > 0 && !File.Exists(Path.Texture))
                    return false; // regenerate!!

                string compressedCacheDir = NonPacked > 0 ? Path.GetCompressedCacheDir() : "";

                var textures = new Array<TextureInfo>();
                var separator = new[] {' '};
                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    var t = new TextureInfo();
                    string[] entry = line.Split(separator, 7);
                    t.NoPack = (entry[0] == "nopack");
                    t.Type = (entry[1]);
                    int.TryParse(entry[2], out t.X);
                    int.TryParse(entry[3], out t.Y);
                    int.TryParse(entry[4], out t.Width);
                    int.TryParse(entry[5], out t.Height);
                    t.Name = entry[6];
                    t.UnpackedPath = t.NoPack ? $"{compressedCacheDir}{t.Name}.dds" : null;
                    textures.Add(t);
                }

                CreateLookup(textures);
            }

            int elapsed = s.NextMillis();
            Log.Write(ConsoleColor.Blue, $"{Mod} LoadAtlas   {this} t:{elapsed,4}ms");
            return true; // we loaded everything
        }

        void CreateLookup(IReadOnlyList<TextureInfo> textures)
        {
            foreach (TextureInfo t in textures)
                Lookup[t.Name] = new TextureBinding(this, t);
            Sorted = Lookup.Values.ToArray();
            Array.Sort(Sorted, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        void SaveAtlasDescriptor(TextureInfo[] textures, string descriptorPath)
        {
            using (var fs = new StreamWriter(descriptorPath))
            {
                fs.WriteLine(Version);
                fs.WriteLine(Hash);
                fs.WriteLine(Name);
                fs.WriteLine(Width);
                fs.WriteLine(Height);
                fs.WriteLine(NumPacked);
                fs.WriteLine(NonPacked);
                foreach (TextureInfo t in textures)
                {
                    string pack = t.NoPack ? "nopack" : "atlas";
                    fs.WriteLine($"{pack} {t.Type} {t.X} {t.Y} {t.Width} {t.Height} {t.Name}");
                }
            }
        }
    }
}