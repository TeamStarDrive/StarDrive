using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Texture;

namespace Ship_Game.SpriteSystem
{
    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    public class TextureAtlas : IDisposable
    {
        const int Version = 21; // changing this will force all caches to regenerate

        // DEBUG: export packed textures into     {cache}/{atlas}/{sprite}.png ?
        //        export non-packed textures into {cache}/{atlas}/NoPack/{sprite}.png
        public static readonly bool ExportTextures = false;
        public static readonly bool ExportPng = true;  // DEBUG: IF exporting, use PNG
        public static readonly bool ExportDds = false; // also use DDS?
        public static readonly bool DebugDrawBounds = false; // draw bounds over every SubTexture
        public static readonly bool DebugDrawFreeSpots = false; // draw remaining Free spots left during Packing
        public static readonly bool DebugDrawFreeSpotFills = false; // draw on free spots that were filled with SubTexture
        public static readonly bool DebugCheckOverlap = true; // whether to validate all Packed SubTextures to ensure no overlap
        public static readonly bool DebugPackerExpansion = false; // saves failed packer state for analysis

        ulong Hash;
        int NumPacked; // number of packed textures (not all textures are packed)
        int NonPacked; // non packed textures

        public string Name { get; private set; }
        public int Width  { get; private set; }
        public int Height { get; private set; }
        public Texture2D Atlas { get; private set; }

        // To enable lazy loading non-packed textures
        class TextureLookup
        {
            public SubTexture SubTex;
            public Texture2D Texture;
            public string Name;
            public string UnpackedPath;
            public SubTexture GetOrLoadTexture(string folder)
            {
                if (SubTex != null)
                    return SubTex;

                // load the texture if we already didn't
                if (Texture == null)
                {
                    var file = new FileInfo(UnpackedPath);
                    Texture = ResourceManager.RootContent.LoadUncachedTexture(file, folder);
                }

                SubTex = new SubTexture(Name, Texture);
                return SubTex;
            }
        }

        TextureLookup[] Sorted = Empty<TextureLookup>.Array;
        readonly Map<string, TextureLookup> Lookup = new Map<string, TextureLookup>();

        ~TextureAtlas() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }

        void Destroy()
        {
            Atlas?.Dispose();
            for (int i = 0; i < Sorted.Length; ++i)
            {
                TextureLookup l = Sorted[i];
                l.Texture?.Dispose(ref l.Texture);
            }
            Sorted = Empty<TextureLookup>.Array;
            Lookup.Clear();
        }

        public string SizeString => $"{$"{Width}x{Height}",-9}";
        public override string ToString() => $"{Name,-24} {SizeString} refs:{Lookup.Count,-3} packed:{NumPacked,-3} non-packed:{NonPacked,-3}";

        public int Count => Sorted.Length;
        public SubTexture this[int index] => Sorted[index].GetOrLoadTexture(Name);
        public SubTexture this[string name] => Lookup[name].GetOrLoadTexture(Name);

        // Grabs a random texture from this texture atlas
        public SubTexture RandomTexture() => RandomMath.RandItem(Sorted).GetOrLoadTexture(Name);

        // Try to get a texture out of this Atlas
        // @warning This MAY incur a sudden texture load
        public bool TryGetTexture(string name, out SubTexture texture)
        {
            if (Lookup.TryGetValue(name, out TextureLookup lookup))
            {
                texture = lookup.GetOrLoadTexture(Name);
                return true;
            }
            texture = null;
            return false;
        }

        static string Mod => GlobalStats.HasMod ? $"[{GlobalStats.ActiveModInfo.ModName}]" : "[Vanilla]";

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

        static ulong Fnv1AHash(byte[] bytes)
        {
            ulong hash = 0xcbf29ce484222325;
            foreach (byte b in bytes)
            {
                hash = hash ^ b;
                hash = hash * 0x100000001b3;
            }
            return hash;
        }

        static ulong CreateHash(FileInfo[] textures)
        {
            // @note Had to roll back to a custom Fnv1AHash over text,
            //       since typical int hash-combine gave bad results.
            var ms = new MemoryStream(4096);
            var bw = new BinaryWriter(ms);
            bw.Write(textures.Length);
            bw.Write(Version);
            foreach (FileInfo info in textures)
            {
                bw.Write(info.Name);
                bw.Write(info.Length);
                bw.Write(info.LastWriteTimeUtc.Ticks);
            }
            return Fnv1AHash(ms.ToArray());
        }

        [Flags]
        enum AtlasFlags
        {
            None = 0,
            Alpha = (1 << 0),
            Compress = (1 << 1)
        }

        void CreateAtlasTexture(GameContentManager content, Color[] color, AtlasFlags flags, string texturePath)
        {
            if ((flags & AtlasFlags.Compress) != 0)
            {
                // We compress the DDS color into DXT5 and then reload it through XNA
                DDSFlags format = (flags&AtlasFlags.Alpha)!=0 ? DDSFlags.Dxt5BGRA : DDSFlags.Dxt1BGRA;
                ImageUtils.SaveAsDds(texturePath, Width, Height, color, format);

                // DXT5 size in mem after loading is 4x smaller than RGBA, but quality sucks!
                Atlas = Texture2D.FromFile(content.Device, texturePath);
            }
            else
            {
                // For this atlas, compression is forbidden, so we save with BGRA color
                // Although this will take 4x more memory
                Atlas = new Texture2D(content.Device, Width, Height, 1, TextureUsage.None, SurfaceFormat.Color);
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
            int transfer = 0, save = 0;
            Stopwatch total = Stopwatch.StartNew();
            Stopwatch perf = Stopwatch.StartNew();

            TextureInfo[] textures = CreateTextureInfos(content, path, textureFiles);
            int load = perf.NextMillis();

            var packer = new TexturePacker(path.Texture);
            NumPacked = packer.PackTextures(textures);
            NonPacked = textures.Length - NumPacked;
            Width = packer.Width;
            Height = packer.Height;
            var flags = AtlasFlags.None;
            int pack = perf.NextMillis();

            if (NonPacked > 0)
            {
                string compressedCacheDir = path.GetCompressedCacheDir();
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
                if (!ResourceManager.AtlasNoCompressFolders.Contains(path.OriginalName))
                {
                    flags |= AtlasFlags.Compress;
                }

                foreach (TextureInfo t in textures) // copy pixels
                {
                    if (ExportTextures) ExportTexture(t, path);
                    if (!t.NoPack)
                    {
                        if (t.HasAlpha)
                            flags |= AtlasFlags.Alpha;
                        t.TransferTextureToAtlas(atlasPixels, Width, Height);
                        if (DebugDrawBounds)
                            ImageUtils.DrawRectangle(atlasPixels, Width, Height, new Rectangle(t.X, t.Y, t.Width, t.Height), Color.YellowGreen);
                    }
                    t.DisposeTexture(); // dispose all, even nonpacked textures, we don't know if they will be used so need to free the mem
                }

                packer.DrawDebug(atlasPixels, Width, Height);
                transfer = perf.NextMillis();

                CreateAtlasTexture(content, atlasPixels, flags, path.Texture);
                save = perf.NextMillis();
            }

            CreateLookup(textures);
            SaveAtlasDescriptor(textures, path.Descriptor);

            int elapsed = total.NextMillis();
            Log.Write(ConsoleColor.Blue, $"{Mod} CreateAtlas {this} t:{elapsed,4}ms l:{load} p:{pack} t:{transfer} s:{save}");
            if ((flags & AtlasFlags.Compress) == 0)
                Log.Write(ConsoleColor.Blue, $"Compression Disabled: {path.OriginalName}");
        }

        void SaveAtlasDescriptor(TextureInfo[] textures, string descriptorPath)
        {
            using (var fs = new StreamWriter(descriptorPath))
            {
                fs.WriteLine(Hash);
                fs.WriteLine(Name);
                fs.WriteLine(NumPacked);
                fs.WriteLine(NonPacked);
                foreach (TextureInfo t in textures)
                {
                    string pack = t.NoPack ? "nopack" : "atlas";
                    fs.WriteLine($"{pack} {t.Type} {t.X} {t.Y} {t.Width} {t.Height} {t.Name}");
                }
            }
        }

        bool TryLoadCache(GameContentManager content, AtlasPath path)
        {
            Stopwatch s = Stopwatch.StartNew();
            if (!File.Exists(path.Descriptor)) return false; // regenerate!!

            using (var fs = new StreamReader(path.Descriptor))
            {
                ulong.TryParse(fs.ReadLine(), out ulong oldHash);
                if (oldHash != Hash)
                {
                    Log.Write(ConsoleColor.Cyan, $"{Mod} AtlasCache  {Name}  INVALIDATED");
                    return false; // hash mismatch, we need to regenerate cache
                }

                Lookup.Clear();
                Width  = 0;
                Height = 0;
                Name   = fs.ReadLine();
                int.TryParse(fs.ReadLine(), out NumPacked);
                int.TryParse(fs.ReadLine(), out NonPacked);
                if (NumPacked > 0)
                {
                    if (!File.Exists(path.Texture)) return false; // regenerate!!
                    Atlas = Texture2D.FromFile(content.Manager.GraphicsDevice, path.Texture);
                    Width  = Atlas.Width;
                    Height = Atlas.Height;
                }

                string compressedCacheDir = NonPacked > 0 ? path.GetCompressedCacheDir() : "";

                var textures = new Array<TextureInfo>();
                var separator = new[] { ' ' };
                string line;
                while ((line = fs.ReadLine()) != null)
                {
                    var t = new TextureInfo();
                    string[] entry = line.Split(separator, 7);
                    t.NoPack = (entry[0] == "nopack");
                    t.Type   = (entry[1]);
                    int.TryParse(entry[2], out t.X);
                    int.TryParse(entry[3], out t.Y);
                    int.TryParse(entry[4], out t.Width);
                    int.TryParse(entry[5], out t.Height);
                    t.Name = entry[6];
                    
                    t.UnpackedPath = $"{compressedCacheDir}{t.Name}.dds";
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
            {
                Lookup[t.Name] = new TextureLookup
                {
                    SubTex = t.NoPack ? null : new SubTexture(t.Name, t.X, t.Y, t.Width, t.Height, Atlas),
                    Texture = null,
                    Name = t.Name,
                    UnpackedPath = t.UnpackedPath,
                };
            }
            Sorted = Lookup.Values.ToArray();
            Array.Sort(Sorted, (a, b) => string.CompareOrdinal(a.Name, b.Name));
        }

        static TextureInfo[] CreateTextureInfos(GameContentManager content, AtlasPath path, FileInfo[] textureFiles)
        {
            var textures = new TextureInfo[textureFiles.Length];

            bool noPackAll = ResourceManager.AtlasExcludeFolder.Contains(path.OriginalName);
            HashSet<string> ignore = ResourceManager.AtlasExcludeTextures; // HACK

            for (int i = 0; i < textureFiles.Length; ++i)
            {
                FileInfo info = textureFiles[i];
                string texName = info.NameNoExt();
                string ext = info.Extension.Substring(1);
                Texture2D tex = content.LoadUncachedTexture(info, ext);
                bool noPack = noPackAll || ignore.Contains(texName);
                textures[i] = new TextureInfo
                {
                    Name    = texName,
                    Type    = ext,
                    Width   = tex.Width,
                    Height  = tex.Height,
                    Texture = tex,
                    NoPack  = noPack,
                };
            }
            return textures;
        }

        class AtlasPath
        {
            public readonly string OriginalName;
            public readonly string Texture;
            public readonly string Descriptor;
            readonly string AtlasName;
            readonly string CacheDir;
            public AtlasPath(string name)
            {
                OriginalName = Path.GetFileName(name);
                AtlasName = name.Replace('/', '_');
                CacheDir = Dir.StarDriveAppData + "/TextureCache";
                Directory.CreateDirectory(CacheDir);
                Texture    = $"{CacheDir}/{AtlasName}.dds";
                Descriptor = $"{CacheDir}/{AtlasName}.atlas";
            }
            public string GetExportPath(TextureInfo t)
            {
                string prefix = t.NoPack ? "NoPack/" : "";
                string folder = $"{CacheDir}/{AtlasName}/{prefix}";
                Directory.CreateDirectory(folder);
                return $"{folder}{t.Name}";
            }
            public string GetCompressedCacheDir()
            {
                string folder = $"{CacheDir}/{AtlasName}/";
                Directory.CreateDirectory(folder);
                return folder;
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

        // @note Guaranteed to load an atlas with at least 1 texture
        // @param useTextureCache if true try to load texture from existing texture cache folder
        // @return null if no textures in atlas {folder}
        public static TextureAtlas FromFolder(GameContentManager content, string folder, bool useTextureCache = true)
        {
            TextureAtlas atlas = null;
            try
            {
                if (GetLoadedAtlas(folder, out atlas))
                    return atlas;

                FileInfo[] files = GatherUniqueTextures(folder);
                if (files.Length == 0)
                {
                    Log.Warning($"{Mod} TextureAtlas create failed: {folder}  No textures.");
                    return null;
                }

                atlas.Hash = CreateHash(files);
                var path = new AtlasPath(folder);

                if (useTextureCache && atlas.TryLoadCache(content, path))
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
