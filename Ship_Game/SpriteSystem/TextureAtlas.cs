using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    public class TextureAtlas : IDisposable
    {
        const int Version = 2; // changing this will force all caches to regenerate

        public   string Name { get; private set; }
        readonly string CacheName;
        int Hash;
        int NumPacked; // number of packed textures (not all textures are packed)

        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }

                 SubTexture[]            Sorted = Empty<SubTexture>.Array;
        readonly Map<string, SubTexture> Lookup = new Map<string, SubTexture>();
        readonly Array<Texture2D>        Owned  = new Array<Texture2D>();

        protected TextureAtlas(string name)
        {
            Name = name;

            CacheName = name;
            if (CacheName.StartsWith("Textures/"))
                CacheName = CacheName.Substring("Textures/".Length);
            CacheName = CacheName.Replace('/', '_');
        }

        ~TextureAtlas() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }

        void Destroy()
        {
            Atlas?.Dispose();
            for (int i = 0; i < Owned.Count; ++i) Owned[i].Dispose();
            Owned.Clear();
        }

        public string SizeString => $"{$"{Width}x{Height}",9}";
        public override string ToString() => $"atlas {Count} {SizeString} {Name}";

        public IReadOnlyList<SubTexture> Textures => Sorted;
        public int Count => Sorted.Length;
        public SubTexture this[int index] => Sorted[index];
        public SubTexture this[string name] => Lookup[name];

        public bool TryGetTexture(string name, out SubTexture texture)
        {
            return Lookup.TryGetValue(name, out texture);
        }

        static string PrepareTextureCacheDir()
        {
            string dir = Dir.StarDriveAppData + "/TextureCache";
            Directory.CreateDirectory(dir);
            return dir;
        }
        static readonly string TextureCacheDir = PrepareTextureCacheDir();

        static FileInfo[] GatherUniqueTextures(string folder)
        {
            FileInfo[] textureFiles = ResourceManager.GatherTextureFiles(folder, recursive:false);
            var uniqueTextures = new Map<string, FileInfo>();
            foreach (FileInfo info in textureFiles)
            {
                string texName = info.NameNoExt();
                if (uniqueTextures.TryGetValue(texName, out FileInfo existing))
                {
                    if (existing.Extension == "xnb") // only replace if old was xnb
                        uniqueTextures[texName] = info;
                }
                else
                {
                    uniqueTextures.Add(texName, info);
                }
            }
            return uniqueTextures.Values.ToArray();
        }

        static int CreateHash(FileInfo[] textures)
        {
            // @note This is very fast, looks like FileInfo is cached somehow.
            int hash = 5381;
            void Combine(int hashCode) => hash = ((hash << 5) + hash) ^ hashCode;
            Combine(textures.Length.GetHashCode());
            Combine(Version);
            foreach (FileInfo info in textures)
            {
                Combine(info.LastWriteTimeUtc.GetHashCode());
                Combine(info.Name.GetHashCode());
            }
            return hash;
        }


        string DescriptorPath => $"{TextureCacheDir}/{CacheName}.atlas";
        string TexturePath    => $"{TextureCacheDir}/{CacheName}.dds";

        void SaveAtlasTexture(GameContentManager content, Color[] color)
        {
            bool compress = Width > 1024 && Height > 1024;
            if (compress)
            {
                // We compress the DDS color into DXT5 and then reload it through XNA
                ImageUtils.ConvertToRGBA(Width, Height, color);
                ImageUtils.SaveAsDds(TexturePath, Width, Height, color);
                //ImageUtils.SaveAsPng(TexturePathPNG, Width, Height, atlasColorData); // DEBUG!

                // DXT5 Compressed size in memory is good, but quality sucks!
                Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);
            }
            else
            {
                // Uncompressed DDS, lossless quality, fast loading, big size in memory :(
                Atlas = new Texture2D(content.Manager.GraphicsDevice, Width, Height, 1, TextureUsage.Linear, SurfaceFormat.Color);
                Atlas.SetData(color);
                Atlas.Save(TexturePath, ImageFileFormat.Dds);
            }
        }

        void CreateAtlasTexture(GameContentManager content, TextureInfo[] textures)
        {
            Stopwatch s = Stopwatch.StartNew();

            var packer = new TexturePacker();
            NumPacked = packer.PackTextures(textures);
            Width = packer.Width;
            Height = packer.Height;

            if (NumPacked > 0)
            {
                var atlasPixels = new Color[Width * Height];

                // DEBUG only!
                //foreach (Rectangle r in FreeSpots)
                //    ImageUtils.DrawRectangle(atlasPixels, Width, Height, r, Color.AliceBlue);

                foreach (TextureInfo t in textures) // copy pixels
                    if (!t.NoPack) t.TransferTextureToAtlas(atlasPixels, Width);
                SaveAtlasTexture(content, atlasPixels);
            }

            foreach (TextureInfo t in textures)
            {
                Lookup[t.Name] = new SubTexture(t.Name, t.X, t.Y, t.Width, t.Height, (t.NoPack ? t.Texture : Atlas));
                if (t.NoPack) Owned.Add(t.Texture);
            }

            Log.Info($"CreateTexture    {SizeString} {Name} elapsed:{s.Elapsed.TotalMilliseconds}ms");
            SaveAtlasDescriptor(textures);
            CreateSortedList();
        }

        void SaveAtlasDescriptor(TextureInfo[] textures)
        {
            using (var fs = new StreamWriter(DescriptorPath))
            {
                fs.WriteLine(Hash);
                fs.WriteLine(Name);
                fs.WriteLine(NumPacked);
                foreach (TextureInfo t in textures)
                {
                    string pack = t.NoPack ? "nopack" : "atlas";
                    fs.WriteLine($"{pack} {t.X} {t.Y} {t.Width} {t.Height} {t.Name}");
                }
            }
        }

        bool TryLoadCache(GameContentManager content)
        {
            if (!File.Exists(DescriptorPath)) return false; // regenerate!!
            using (var fs = new StreamReader(DescriptorPath))
            {
                int.TryParse(fs.ReadLine(), out int hash);
                if (hash != Hash)
                    return false; // hash mismatch, we need to regenerate cache

                Lookup.Clear();
                Width  = 0;
                Height = 0;
                Name = fs.ReadLine();
                int.TryParse(fs.ReadLine(), out NumPacked);
                if (NumPacked > 0)
                {
                    if (!File.Exists(TexturePath)) return false; // regenerate!!
                    Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, TexturePath);
                    Width = Atlas.Width;
                    Height = Atlas.Height;
                }

                var separator = new[] { ' ' };
                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    string[] entry = line.Split(separator, 6);
                    string what = entry[0];
                    int.TryParse(entry[1], out int x);
                    int.TryParse(entry[2], out int y);
                    int.TryParse(entry[3], out int w);
                    int.TryParse(entry[4], out int h);
                    string sprite = entry[5];
                    Texture2D tex = Atlas;
                    if (what == "nopack")
                    {
                        tex = content.LoadUncached<Texture2D>($"{Name}/{sprite}");
                        Owned.Add(tex);
                    }
                    Lookup.Add(sprite, new SubTexture(sprite, x, y, w, h, tex));
                }
                CreateSortedList();
            }
            Log.Info($"LoadAtlas    {Lookup.Count,3} {SizeString} {Name}");
            return true; // we loaded everything
        }

        void CreateSortedList()
        {
            Sorted = Lookup.Values.ToArray();
            Array.Sort(Sorted, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        static TextureInfo[] LoadTextureInfo(GameContentManager content, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];
            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetName = info.CleanResPath(false);
                string texName = info.NameNoExt();
                var tex = content.LoadUncached<Texture2D>(assetName);
                textures[i] = new TextureInfo
                {
                    Name = texName,
                    Width = tex.Width,
                    Height = tex.Height,
                    Texture = tex,
                };
            }
            return textures;
        }

        // @return null if no textures in atlas {folder}
        public static TextureAtlas FromFolder(GameContentManager content, string folder, bool useCache = true)
        {
            FileInfo[] textureFiles = GatherUniqueTextures(folder);
            if (textureFiles.Length == 0)
                return null; // no textures!!

            var atlas = new TextureAtlas(folder) { Hash = CreateHash(textureFiles) };
            if (useCache && atlas.TryLoadCache(content))
                return atlas;

            TextureInfo[] textures = LoadTextureInfo(content, textureFiles);
            atlas.CreateAtlasTexture(content, textures);
            HelperFunctions.CollectMemorySilent();
            return atlas;
        }
    }
}
