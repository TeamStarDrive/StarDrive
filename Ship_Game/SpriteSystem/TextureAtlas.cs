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
        const int Version = 3; // changing this will force all caches to regenerate
        const bool ExportTextures = true; // export source textures into {cache}/{atlas}/{sprite}.png ?
        
        int Hash;
        int NumPacked; // number of packed textures (not all textures are packed)

        public string Name { get; private set; }
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }

                 SubTexture[]            Sorted = Empty<SubTexture>.Array;
        readonly Map<string, SubTexture> Lookup = new Map<string, SubTexture>();
        readonly Array<Texture2D>        Owned  = new Array<Texture2D>();

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
            => Lookup.TryGetValue(name, out texture);

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
                else uniqueTextures.Add(texName, info);
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

        void SaveAtlasTexture(GameContentManager content, Color[] color, string texturePath)
        {
            bool compress = Width > 1024 && Height > 1024;
            if (compress)
            {
                // We compress the DDS color into DXT5 and then reload it through XNA
                ImageUtils.ConvertToRGBA(Width, Height, color);
                ImageUtils.SaveAsDds(texturePath, Width, Height, color);
                //ImageUtils.SaveAsPng(TexturePathPNG, Width, Height, atlasColorData); // DEBUG!

                // DXT5 Compressed size in memory is good, but quality sucks!
                Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, texturePath);
            }
            else
            {
                // Uncompressed DDS, lossless quality, fast loading, big size in memory :(
                Atlas = new Texture2D(content.Manager.GraphicsDevice, Width, Height, 1, TextureUsage.None, SurfaceFormat.Color);
                Atlas.SetData(color);
                Atlas.Save(texturePath, ImageFileFormat.Dds);
            }
        }

        void CreateAtlasTexture(GameContentManager content, FileInfo[] textureFiles, AtlasPath path)
        {
            Stopwatch s = Stopwatch.StartNew();
            TextureInfo[] textures = LoadTextureInfo(content, textureFiles);

            var packer = new TexturePacker();
            NumPacked = packer.PackTextures(textures);
            Width = packer.Width;
            Height = packer.Height;

            if (NumPacked > 0)
            {
                var atlasPixels = new Color[Width * Height];

                //foreach (Rectangle r in FreeSpots) // DEBUG only!
                //    ImageUtils.DrawRectangle(atlasPixels, Width, Height, r, Color.AliceBlue);

                foreach (TextureInfo t in textures) // copy pixels
                {
                    if (t.NoPack) continue;
                    if (ExportTextures) t.Texture.Save($"{path.CacheDir}/{path.Name}/{t.Name}.png", ImageFileFormat.Png);
                    t.TransferTextureToAtlas(atlasPixels, Width, Height);
                }
                SaveAtlasTexture(content, atlasPixels, path.Texture);
            }

            foreach (TextureInfo t in textures)
            {
                Lookup[t.Name] = new SubTexture(t.Name, t.X, t.Y, t.Width, t.Height, (t.NoPack ? t.Texture : Atlas));
                if (t.NoPack) Owned.Add(t.Texture);
            }

            Log.Info($"CreateTexture    {SizeString} {Name} elapsed:{s.Elapsed.TotalMilliseconds}ms");
            SaveAtlasDescriptor(textures, path.Descriptor);
            CreateSortedList();
        }

        void SaveAtlasDescriptor(TextureInfo[] textures, string descriptorPath)
        {
            using (var fs = new StreamWriter(descriptorPath))
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

        bool TryLoadCache(GameContentManager content, AtlasPath path)
        {
            if (!File.Exists(path.Descriptor)) return false; // regenerate!!
            using (var fs = new StreamReader(path.Descriptor))
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
                    if (!File.Exists(path.Texture)) return false; // regenerate!!
                    Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, path.Texture);
                    Width  = Atlas.Width;
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

        //static Texture2D Resize1x1To4x4(GameContentManager content, Texture2D small)
        //{
        //    var old = new Color[small.Width*small.Height];
        //    small.GetData(old);
        //    small.Dispose();

        //    var big = new Texture2D(content.Device, 4, 4);
        //    var neu = new Color[4*4];
        //    for (int i = 0; i < neu.Length; ++i)
        //        neu[i] = old[0];
        //    big.SetData(neu);
        //    return big;
        //}

        static TextureInfo[] LoadTextureInfo(GameContentManager content, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];
            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetName = info.CleanResPath(false);
                string texName = info.NameNoExt();
                var tex = content.LoadUncached<Texture2D>(assetName);
                //if (tex.Width == 1 && tex.Height == 1)
                //    tex = Resize1x1To4x4(content, tex);
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

        class AtlasPath
        {
            public readonly string Texture;
            public readonly string Descriptor;
            public readonly string CacheDir;
            public readonly string Name;
            public AtlasPath(string name)
            {
                if (name.StartsWith("Textures/")) name = name.Substring("Textures/".Length);
                Name = name.Replace('/', '_');
                CacheDir = Dir.StarDriveAppData + "/TextureCache";
                Directory.CreateDirectory(CacheDir);
                Texture    = $"{CacheDir}/{Name}.dds";
                Descriptor = $"{CacheDir}/{Name}.atlas";
                if (ExportTextures) Directory.CreateDirectory($"{CacheDir}/{Name}");
            }
        }

        // @return null if no textures in atlas {folder}
        public static TextureAtlas FromFolder(GameContentManager content, string folder, bool useCache = true)
        {
            FileInfo[] textureFiles = GatherUniqueTextures(folder);
            if (textureFiles.Length == 0)
                return null; // no textures!!

            var atlas = new TextureAtlas { Name = folder, Hash = CreateHash(textureFiles) };
            var path = new AtlasPath(folder);

            if (useCache && atlas.TryLoadCache(content, path))
                return atlas;

            atlas.CreateAtlasTexture(content, textureFiles, path);
            HelperFunctions.CollectMemorySilent();
            return atlas;
        }
    }
}
