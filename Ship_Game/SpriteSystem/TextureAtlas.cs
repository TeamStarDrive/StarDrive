﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.SpriteSystem;

namespace Ship_Game
{
    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    public class TextureAtlas : IDisposable
    {
        const int Version = 6; // changing this will force all caches to regenerate

        // DEBUG: export packed textures into     {cache}/{atlas}/{sprite}.png ?
        //        export non-packed textures into {cache}/{atlas}/NoPack/{sprite}.png
        static bool ExportTextures = false;
        static bool ExportPng = true;  // DEBUG: IF exporting, use PNG
        static bool ExportDds = false; // also use DDS?

        int Hash;
        int NumPacked; // number of packed textures (not all textures are packed)

        public string Name { get; private set; }
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }

                 SubTexture[]            Sorted    = Empty<SubTexture>.Array;
        readonly Map<string, SubTexture> Lookup    = new Map<string, SubTexture>();
        readonly Array<Texture2D>        NonPacked = new Array<Texture2D>();

        ~TextureAtlas() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }

        void Destroy()
        {
            Atlas?.Dispose();
            for (int i = 0; i < NonPacked.Count; ++i) NonPacked[i].Dispose();
            NonPacked.Clear();
        }

        public string SizeString => $"{$"{Width}x{Height}",-9}";
        public override string ToString() => $"{Name,-24} {SizeString} refs:{Lookup.Count,-3} packed:{NumPacked,-3} non-packed:{NonPacked.Count,-3}";

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
                ImageUtils.SaveAsDds(texturePath, Width, Height, color); // save compressed!
                //ImageUtils.SaveAsPng(TexturePathPNG, Width, Height, atlasColorData); // DEBUG!

                // DXT5 size in mem after loading is 4x smaller than RGBA, but quality sucks!
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

        static void ExportTexture(TextureInfo t, AtlasPath path)
        {
            string filePathNoExt = path.GetExportPath(t);
            if (ExportPng) t.SaveAsPng($"{filePathNoExt}.png");
            if (ExportDds) t.SaveAsDds($"{filePathNoExt}.dds");
        }

        void CreateAtlas(GameContentManager content, FileInfo[] textureFiles, AtlasPath path)
        {
            int load = 0, pack = 0, transfer = 0, save = 0;
            Stopwatch total = Stopwatch.StartNew();
            Stopwatch perf = Stopwatch.StartNew();

            TextureInfo[] textures = LoadTextureInfo(content, path, textureFiles);
            load = perf.NextMillis();

            var packer = new TexturePacker();
            NumPacked = packer.PackTextures(textures);
            Width = packer.Width;
            Height = packer.Height;
            pack = perf.NextMillis();

            if (NumPacked > 0)
            {
                var atlasPixels = new Color[Width * Height];

                //foreach (Rectangle r in FreeSpots) // DEBUG only!
                //    ImageUtils.DrawRectangle(atlasPixels, Width, Height, r, Color.AliceBlue);

                foreach (TextureInfo t in textures) // copy pixels
                {
                    if (ExportTextures) ExportTexture(t, path);
                    if (t.NoPack) continue;
                    t.TransferTextureToAtlas(atlasPixels, Width, Height);
                    t.DisposeTexture();
                }
                transfer = perf.NextMillis();

                SaveAtlasTexture(content, atlasPixels, path.Texture);
                save = perf.NextMillis();
            }

            foreach (TextureInfo t in textures)
            {
                Lookup[t.Name] = new SubTexture(t.Name, t.X, t.Y, t.Width, t.Height, (t.NoPack ? t.Texture : Atlas));
                if (t.NoPack) NonPacked.Add(t.Texture);
            }

            SaveAtlasDescriptor(textures, path.Descriptor);
            CreateSortedList();

            int elapsed = total.NextMillis();
            Log.Info($"CreateAtlas {this} t:{elapsed,4}ms l:{load} p:{pack} t:{transfer} s:{save}");
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
            Stopwatch s = Stopwatch.StartNew();
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

                var textures = new Array<TextureInfo>();

                var separator = new[] { ' ' };
                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    var t = new TextureInfo();
                    string[] entry = line.Split(separator, 6);
                    t.NoPack = (entry[0] == "nopack");
                    int.TryParse(entry[1], out t.X);
                    int.TryParse(entry[2], out t.Y);
                    int.TryParse(entry[3], out t.Width);
                    int.TryParse(entry[4], out t.Height);
                    t.Name = entry[5];
                    t.Texture = Atlas;
                    textures.Add(t);
                }
                LoadTextures(content, textures);
                CreateSortedList();
            }

            int elapsed = s.NextMillis();
            Log.Info($"LoadAtlas {this} t:{elapsed,4}ms");
            return true; // we loaded everything
        }

        void LoadTextures(GameContentManager content, Array<TextureInfo> textures)
        {
            TextureInfo[] noPack = textures.Filter(t => t.NoPack);
            Parallel.For(noPack.Length, (start, end) =>
            {
                for (int i = start; i < end; ++i)
                {
                    TextureInfo t = noPack[i];
                    t.Texture = content.LoadUncached<Texture2D>($"{Name}/{t.Name}");
                }
            });

            foreach (TextureInfo t in textures)
            {
                var sub = new SubTexture(t.Name, t.X, t.Y, t.Width, t.Height, t.Texture);
                if (t.NoPack) NonPacked.Add(t.Texture);
                Lookup.Add(t.Name, sub);
            }
        }

        void CreateSortedList()
        {
            Sorted = Lookup.Values.ToArray();
            Array.Sort(Sorted, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        static TextureInfo[] LoadTextureInfo(GameContentManager content, AtlasPath path, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];

            bool noPackAll = ResourceManager.AtlasExcludeFolder.Contains(path.Name);
            HashSet<string> ignore = ResourceManager.AtlasExcludeTextures; // HACK

            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string assetName = info.CleanResPath(false);
                string texName = info.NameNoExt();
                var tex = content.LoadUncached<Texture2D>(assetName);
                bool noPack = noPackAll || ignore.Contains(texName);
                textures[i] = new TextureInfo
                {
                    Name = texName,
                    Width = tex.Width,
                    Height = tex.Height,
                    Texture = tex,
                    NoPack = noPack,
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
            }
            public string GetExportPath(TextureInfo t)
            {
                string prefix = t.NoPack ? "NoPack/" : "";
                string dir = $"{CacheDir}/{Name}/{prefix}{t.Name}";
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        // To enable multi-threaded background pre-loading
        static readonly Map<string, TextureAtlas> Loading = new Map<string, TextureAtlas>();
        readonly Mutex LoadSync = new Mutex();

        // atomically gets or inserts atlas
        // @return TRUE if an existing atlas was retrieved, FALSE if a new atlas was inserted
        static bool GetLoadedAtlas(string name, out TextureAtlas existingOrNew)
        {
            lock (Loading)
            {
                if (!Loading.TryGetValue(name, out existingOrNew))
                {
                    existingOrNew = new TextureAtlas { Name = name };
                    Loading.Add(name, existingOrNew);
                    existingOrNew.LoadSync.WaitOne(); // lock it for upcoming load event
                    return false;
                }
            }
            existingOrNew.LoadSync.WaitOne(); // wait until loading completes
            return true;
        }

        // @return null if no textures in atlas {folder}
        public static TextureAtlas FromFolder(GameContentManager content, string folder, bool useCache = true)
        {
            TextureAtlas atlas = null;
            try
            {
                if (GetLoadedAtlas(folder, out atlas))
                    return atlas;

                FileInfo[] files = GatherUniqueTextures(folder);
                if (files.Length == 0)
                    return null; // no textures!!

                atlas.Hash = CreateHash(files);
                var path = new AtlasPath(folder);

                if (useCache && atlas.TryLoadCache(content, path))
                    return atlas;

                atlas.CreateAtlas(content, files, path);
                HelperFunctions.CollectMemorySilent();
                return atlas;
            }
            finally
            {
                atlas?.LoadSync.ReleaseMutex();
                lock (Loading) Loading.Remove(folder);
            }
        }
    }
}
