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
    /// Generic TextureAtlas which is used as a container
    /// for related textures and animation sequences
    public partial class TextureAtlas : IDisposable
    {
        const int Version = 24; // changing this will force all caches to regenerate

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
        public SubTexture RandomTexture() => RandomMath.RandItem(Sorted).GetOrLoadTexture();
        public SubTexture RandomTexture(RandomBase random) => random.RandItem(Sorted).GetOrLoadTexture();

        public TextureAtlas() {}
        ~TextureAtlas() { Destroy(); }
        public void Dispose() { Destroy(); GC.SuppressFinalize(this); }

        void Destroy()
        {
            Atlas?.Dispose(ref Atlas);
            for (int i = 0; i < Sorted.Length; ++i)
            {
                TextureBinding l = Sorted[i];
                l.Texture?.Dispose(ref l.Texture);
            }
            Sorted = Empty<TextureBinding>.Array;
            Lookup.Clear();
            Path = null;
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
            if (Atlas == null)
            {
                string path = Path.PrePackedTex ?? Path.CacheAtlasTex;
                var atlasTex = new FileInfo(path);
                if (atlasTex.Exists)
                {
                    Atlas = ResourceManager.RootContent.LoadUncachedTexture(atlasTex, "dds");
                    Width = Atlas.Width;
                    Height = Atlas.Height;
                }
                else
                {
                    Log.Error($"Atlas texture does not exist: {path}");
                }
            }
            return Atlas;
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
