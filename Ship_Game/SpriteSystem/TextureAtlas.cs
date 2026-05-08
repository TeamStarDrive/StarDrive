using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Data;
using Ship_Game.Utils;

namespace Ship_Game.SpriteSystem
{
    /// <summary>
    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    /// </summary>
    public sealed partial class TextureAtlas : IDisposable
    {
        const int Version = 29; // changing this will force all caches to regenerate
        const string CacheVersionFile = "version.txt";

        // Run once per LoadAllResources, BEFORE any atlas loads. Compares the
        // sentinel `version.txt` at the cache root against the in-code Version
        // and wipes the whole cache folder on mismatch (or missing sentinel).
        // Belt-and-braces over per-atlas LoadCacheAtlas hash invalidation:
        // version bumps, removed atlases, renamed atlases, format flips
        // (.dds <-> .png) all leave orphan files that the per-atlas check
        // can't see. A clean rebuild is more expensive but only happens once
        // per Version bump, and removes the entire class of stale-companion
        // bugs.
        public static void PurgeCacheIfVersionChanged()
        {
            string cacheDir = AtlasPath.GetCacheRoot();
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
                File.WriteAllText(System.IO.Path.Combine(cacheDir, CacheVersionFile), Version.ToString());
                return;
            }

            string sentinelPath = System.IO.Path.Combine(cacheDir, CacheVersionFile);
            int storedVersion = -1;
            if (File.Exists(sentinelPath) &&
                int.TryParse(File.ReadAllText(sentinelPath).Trim(), out int parsed))
            {
                storedVersion = parsed;
            }

            if (storedVersion == Version)
                return;

            Log.Write(ConsoleColor.Cyan,
                $"[TextureAtlas] cache version {storedVersion} -> {Version}; purging {cacheDir}");

            foreach (string entry in Directory.EnumerateFileSystemEntries(cacheDir))
            {
                try
                {
                    FileAttributes attr = File.GetAttributes(entry);
                    if ((attr & FileAttributes.Directory) != 0)
                        Directory.Delete(entry, recursive: true);
                    else
                        File.Delete(entry);
                }
                catch (IOException ex)
                {
                    Log.Warning($"[TextureAtlas] could not delete {entry}: {ex.Message}");
                }
            }

            File.WriteAllText(sentinelPath, Version.ToString());
        }

        // DEBUG: export packed textures into     {cache}/{atlas}/{sprite}.png ?
        //        export non-packed textures into {cache}/{atlas}/NoPack/{sprite}.png
        public static readonly bool ExportTextures = false;
        public static readonly bool ExportPng = true;  // DEBUG: IF exporting, use PNG
        public static readonly bool ExportDds = false; // also use DDS?
        public static readonly bool DebugDrawBounds = false; // draw bounds over every SubTexture
        public static readonly bool DebugDrawFreeSpots = false; // draw remaining Free spots left during Packing
        public static readonly bool DebugDrawFreeSpotFills = false; // draw on free spots that were filled with SubTexture
        public static readonly bool DebugCheckOverlap = false; // whether to validate all Packed SubTextures to ensure no overlap
        public static readonly bool DebugPackerExpansion = false; // saves failed packer state for analysis

        ulong Hash;
        int NumPacked; // number of packed textures (not all textures are packed)
        int NonPacked; // non packed textures
        AtlasPath Path; // atlas path info
        Texture2D Atlas;

        // Usually name of the folder where this atlas is generated from
        // example: MMenu/
        public string Name { get; private set; }
        public int Width  { get; private set; }
        public int Height { get; private set; }

        TextureBinding[] Sorted = Empty<TextureBinding>.Array;
        readonly Map<string, TextureBinding> Lookup = new(StringComparer.OrdinalIgnoreCase);

        public override string ToString() => $"{Name,-32} {$"{Width}x{Height}",-9} n:{Lookup.Count,-3} pack:{NumPacked,-3} nopack:{NonPacked,-3}";

        public int Count => Sorted.Length;

        public SubTexture this[int index] => Sorted[index].GetOrLoadTexture();
        public SubTexture this[string name] => Lookup[name].GetOrLoadTexture();
        public TextureBinding GetBinding(int index) => Sorted[index];
        public TextureBinding GetBinding(string name) => Lookup[name];

        // Grabs a random texture from this texture atlas
        public SubTexture RandomTexture(RandomBase random) => random.Item(Sorted).GetOrLoadTexture();

        public TextureAtlas() {}
        ~TextureAtlas() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // the `Atlas` texture itself can be lazy-loaded so to consider TextureAtlas
        // completely disposed, the `Sorted` TextureBindings array must be empty
        public bool IsDisposed => Sorted.Length == 0;

        void Dispose(bool disposing)
        {
            Mem.Dispose(ref Atlas);
            for (int i = 0; i < Sorted.Length; ++i)
            {
                TextureBinding l = Sorted[i];
                Mem.Dispose(ref l.Texture);
            }
            Sorted = Empty<TextureBinding>.Array;
            Lookup.Clear();
            Path = null;
            LoadSync.Dispose();
        }

        // Try to get a texture out of this Atlas
        // @warning This MAY incur a sudden texture load
        public bool TryGetTexture(string name, out SubTexture texture)
        {
            if (Lookup.TryGetValue(name, out TextureBinding lookup))
            {
                texture = lookup.GetOrLoadTexture();
                return true;
            }
            texture = null;
            return false;
        }

        // @warning This MAY incur a sudden texture load
        public bool TryGetTexture(int index, out SubTexture texture)
        {
            if ((uint)index < Count)
            {
                texture = Sorted[index].GetOrLoadTexture();
                return true;
            }
            texture = null;
            return false;
        }

        // we lazy load the main Atlas texture on first reference
        // to avoid loading big textures which are not even used
        public Texture2D GetAtlasTexture()
        {
            if (Atlas != null)
                return Atlas;

            // Phase 2.3: Atlas cache files may land as .dds OR .png on disk.
            // CreateAtlasTexture chooses Compressed→DDS (via SDNative.ConvertToDDS) for
            // Dxt-flagged atlases and falls back to Texture2D.SaveAsPng for the
            // uncompressed RGBA path (Phase 1 left a TODO there because MonoGame
            // removed Texture2D.Save for DDS). Loader must check both extensions.
            string primary = Path.PrePackedTex ?? Path.CacheAtlasTex;
            var atlasTex = new FileInfo(primary);
            if (!atlasTex.Exists)
            {
                string pngFallback = System.IO.Path.ChangeExtension(primary, "png");
                var pngTex = new FileInfo(pngFallback);
                if (pngTex.Exists) atlasTex = pngTex;
            }

            if (atlasTex.Exists)
            {
                var atlas = ResourceManager.RootContent.LoadUncachedTexture(atlasTex);
                Width = atlas.Width;
                Height = atlas.Height;

                // signal a memory barrier to synchronize write to Atlas field across multiple threads
                Thread.MemoryBarrier();
                Atlas = atlas;
                return atlas;
            }

            Log.Error($"Atlas texture does not exist: {primary} (also tried .png)");
            return null;
        }

        // used memory in bytes
        public int GetUsedMemory()
        {
            int numBytes = GameContentManager.TextureSize(Atlas);
            for (int i = 0 ; i < Sorted.Length; ++i)
                numBytes += GameContentManager.TextureSize(Sorted[i].Texture);
            return numBytes;
        }

        static string Mod => $"[{GlobalStats.ModOrVanillaName}]";

        // To enable multi-threaded background pre-loading
        static readonly Map<string, TextureAtlas> Loading = new();
        readonly Mutex LoadSync = new();

        // atomically gets or inserts atlas
        // @return TRUE if an existing atlas was retrieved, FALSE if a new atlas was inserted
        static bool GetLoadedAtlas(string name, out TextureAtlas existingOrNew)
        {
            lock (Loading)
            {
                if (!Loading.TryGetValue(name, out existingOrNew))
                {
                    existingOrNew = new TextureAtlas { Name = name };
                    existingOrNew.LoadSync.WaitOne(); // lock it for upcoming load event
                    Loading.Add(name, existingOrNew);
                    return false;
                }
            }
            
            //Log.Write(ConsoleColor.Cyan, $"LoadAtlas blocked: {name}");
            existingOrNew.LoadSync.WaitOne(); // wait until loading completes
            return true;
        }

        // @note Guaranteed to load an atlas with at least 1 texture
        // @param useTextureCache if true try to load texture from existing texture cache folder
        // @return null if no textures in atlas {folder}
        public static TextureAtlas FromFolder(string folder, bool useTextureCache = true)
        {
            TextureAtlas atlas = null;
            try
            {
                GameLoadingScreen.SetStatus("LoadAtlas", folder);
                if (GetLoadedAtlas(folder, out atlas))
                    return atlas;

                var path = new AtlasPath(folder);
                atlas.Path = path;

                if (path.PrePackedFile != null)
                {
                    //Log.Info(ConsoleColor.White, $"PrePacked: {path.PrePackedFile}");
                    if (!atlas.LoadAtlasFile(path.PrePackedFile, path.PrePackedTex, checkVersionAndHash:false))
                    {
                        Log.Warning($"{Mod} TextureAtlas prepacked load failed: {path.PrePackedFile} ");
                        return null;
                    }
                    return atlas;
                }

                FileInfo[] files = GatherUniqueTextures(folder);
                if (files.Length == 0)
                {
                    Log.Warning($"{Mod} TextureAtlas create failed: {folder}  No textures.");
                    return null;
                }

                atlas.Hash = CreateHash(files);
                if (useTextureCache && atlas.LoadCacheAtlas())
                    return atlas;

                GameLoadingScreen.SetStatus("CreateAtlas", folder);
                atlas.CreateAtlas(files);
                HelperFunctions.CollectMemorySilent();
                return atlas;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Atlas.FromFolder failed: {folder}");
                throw;
            }
            finally
            {
                atlas?.LoadSync.ReleaseMutex();
                lock (Loading) Loading.Remove(folder);
            }
        }
    }
}
