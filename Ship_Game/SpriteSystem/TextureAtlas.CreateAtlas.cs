using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDUtils;
using Ship_Game.Data.Texture;
using Rectangle = SDGraphics.Rectangle;

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
                DDSFlags format = (flags & AtlasFlags.Alpha) != 0 ? DDSFlags.Dxt5 : DDSFlags.Dxt1;
                ImageUtils.ConvertToDDS(texturePath, Width, Height, color, format);
            }
            else
            {
                // PNG fallback for AtlasNoCompress atlases (EmpireTopBar, NewUI, Popup).
                // SpriteBatch's AlphaBlend uses the premultiplied formula. The atlas-DDS
                // path gets premul applied at LoadDds time; the PNG path's LoadPng does
                // not premultiply, so we bake premultiplication into the saved PNG to
                // keep the same on-GPU contract. Without this, bright-and-transparent
                // pixels in NewUI/EmpireTopBar/Popup atlases saturate to white at draw.
                ImageUtils.PremultiplyAlpha(color, color.Length);
                ImageUtils.SaveAsPng(System.IO.Path.ChangeExtension(texturePath, "png"), Width, Height, color);
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
            Stopwatch total = Stopwatch.StartNew();
            TextureInfo[] textures = CreateTextureInfos(Path, textureFiles);

            var packer = new TexturePacker(Path.CacheAtlasTex);
            NumPacked = packer.PackTextures(textures);
            NonPacked = textures.Length - NumPacked;
            Width = packer.Width;
            Height = packer.Height;
            var flags = AtlasFlags.None;

            if (NonPacked > 0)
            {
                string compressedCacheDir = Path.GetCompressedCacheDir();
                foreach (TextureInfo t in textures)
                {
                    if (t.NoPack)
                    {
                        // §4.6 #7: SaveAsDds may return a .png path for Color+alpha
                        // textures (DXT5 alpha quantization is lossy on smooth
                        // gradients — see TextureInfo.SaveAsDds). Use the actual
                        // saved path so UnpackedPath matches the file on disk.
                        t.UnpackedPath = t.SaveAsDds($"{compressedCacheDir}{t.Name}.dds");
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
                CreateAtlasTexture(atlasPixels, flags, Path.CacheAtlasTex);
            }

            CreateLookup(textures);
            SaveAtlasDescriptor(textures, Path.CacheAtlasFile);

            int elapsed = total.NextMillis();
            Log.Write(ConsoleColor.Blue, $"{Mod} Create {this.ToString()} t:{elapsed,4}ms");
        }

        bool LoadCacheAtlas()
        {
            if (!File.Exists(Path.CacheAtlasFile))
                return false; // regenerate!!
            return LoadAtlasFile(Path.CacheAtlasFile, Path.CacheAtlasTex, checkVersionAndHash:true);
        }

        bool LoadAtlasFile(string atlasFile, string atlasTex, bool checkVersionAndHash)
        {
            using (var fs = new StreamReader(atlasFile))
            {
                int.TryParse(fs.ReadLine(), out int version);
                ulong.TryParse(fs.ReadLine(), out ulong oldHash);
                if (checkVersionAndHash && version != Version)
                {
                    if (ResourceManager.Verbose)
                        Log.Write(ConsoleColor.Cyan, $"{Mod} AtlasCache  {Name}  INVALIDATED  (version-mismatch)");
                    return false;
                }
                if (checkVersionAndHash && oldHash != Hash)
                {
                    if (ResourceManager.Verbose)
                        Log.Write(ConsoleColor.Cyan, $"{Mod} AtlasCache  {Name}  INVALIDATED  (hash-mismatch)");
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

                // §4.6 #7: AtlasNoCompress atlases write .png instead of the
                // .dds path stored in CacheAtlasTex. GetAtlasTexture probes
                // both extensions on load, so the existence check here must
                // do the same — otherwise every AtlasNoCompress atlas
                // invalidates on every launch (texture-missing) even though
                // the real file is sitting next to it as .png.
                if (NumPacked > 0 && !File.Exists(atlasTex)
                                 && !File.Exists(System.IO.Path.ChangeExtension(atlasTex, "png")))
                {
                    if (ResourceManager.Verbose)
                        Log.Write(ConsoleColor.Cyan, $"{Mod} AtlasCache  {Name}  INVALIDATED  (texture-missing)");
                    return false; // regenerate!!
                }

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
                    // §4.6 #7: nopack path may be .dds OR .png on disk (Color+alpha
                    // textures save as PNG to preserve smooth alpha gradients —
                    // DXT5 quantization is lossy and read as visible banding/dark
                    // fringes at the FOW sensor circle edge in the original cache).
                    if (t.NoPack)
                    {
                        string ddsPath = $"{compressedCacheDir}{t.Name}.dds";
                        string pngPath = $"{compressedCacheDir}{t.Name}.png";
                        t.UnpackedPath = File.Exists(pngPath) ? pngPath : ddsPath;
                    }
                    else
                    {
                        t.UnpackedPath = null;
                    }
                    t.SourcePath = Path.OriginalName + "/" + t.Name + "." + t.Type;
                    textures.Add(t);
                }

                CreateLookup(textures);
            }

            if (ResourceManager.Verbose)
                Log.Write(ConsoleColor.Blue, $"{Mod} Load   {this.ToString()}");
            return true; // we loaded everything
        }

        void CreateLookup(IReadOnlyList<TextureInfo> textures)
        {
            foreach (TextureInfo t in textures)
                Lookup[t.Name] = new TextureBinding(this, t);
            Sorted = Lookup.Values.ToArr();
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