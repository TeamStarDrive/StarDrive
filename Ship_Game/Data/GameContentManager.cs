using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Content;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace Ship_Game
{
    public sealed class GameContentManager : ContentManager
    {
        // If non-null, a parent resource manager is checked first for existing resources
        // to avoid double loading resources into memory
        private readonly GameContentManager Parent;
        private Dictionary<string, object> LoadedAssets;
        public string Name { get; }

        public GameContentManager(IServiceProvider service, string name) : base(service, "Content")
        {
            Name = name;
            LoadedAssets = (Dictionary<string, object>)
                typeof(ContentManager).GetField("loadedAssets", BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(this);
        }

        public GameContentManager(GameContentManager parent, string name) : this(parent.ServiceProvider, name)
        {
            Parent = parent;
        }

        private bool TryGetAsset(string assetNameNoExt, out object asset)
        {
            GameContentManager mgr = this;
            do
            {
                if (mgr.LoadedAssets.TryGetValue(assetNameNoExt, out asset))
                    return true;
            }
            while ((mgr = mgr.Parent) != null);
            return false;
        }

        // Tries to get an existing asset. Only returns true if the asset is already loaded and matches typeof(T)
        public bool TryGetAsset<T>(string assetNameNoExt, out T asset)
        {
            if (TryGetAsset(assetNameNoExt, out object obj) && obj is T assetObj)
            {
                asset = assetObj;
                return true;
            }
            asset = default(T);
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            LoadedAssets = null;
        }

        private static T GetField<T>(object obj, string name)
        {
            return (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        }

        private static int TextureSize(Texture2D tex)
        {
            if (tex.IsDisposed)
                return 0;
            float mul = 1f;
            switch (tex.Format)
            {
                case SurfaceFormat.Dxt1: mul = 0.5f; break;
                case SurfaceFormat.Dxt3: mul = 1.0f; break;
                case SurfaceFormat.Dxt5: mul = 1.0f; break;
            }
            if (tex.LevelCount > 1) mul *= 1.75f; // mipmaps

            return (int)(tex.Width * tex.Height * mul) + 4096/*all the crap that manages this texture*/;
        }

        // Calculates the approximate size of the raw data in assets
        public int GetLoadedAssetBytes()
        {
            int numBytes = 0;
            foreach (object asset in LoadedAssets.Values)
            {
                if (asset is Texture2D tex)
                {
                    numBytes += TextureSize(tex);
                }
                else if (asset is Video vid)
                {
                    numBytes += vid.Width * vid.Height * 3/*RGB*/ * 2/*doublebuffered*/;
                }
                else if (asset is Model mod)
                {
                    numBytes += mod.Bones.Count * 256;
                    foreach (var mesh in mod.Meshes)
                        numBytes += mesh.IndexBuffer.SizeInBytes + mesh.VertexBuffer.SizeInBytes;
                }
                else if (asset is SpriteFont font)
                {
                    var fontTex = GetField<Texture2D>(font, "textureValue");
                    numBytes += TextureSize(fontTex);
                    numBytes += font.Characters.Count * 64;
                }
                else
                {
                    //Debugger.Break();
                }
            }
            return numBytes;
        }

        public float GetLoadedAssetMegabytes() => GetLoadedAssetBytes() / (1024f * 1024f);

        public override void Unload()
        {
            float totalMemSaved = GetLoadedAssetMegabytes();
            int count = LoadedAssets.Count;
            base.Unload();

            if (totalMemSaved > 0f)
            {
                Log.Info("Unloaded '{0}' ({1} assets, {2:0.0}MB)", Name, count, totalMemSaved);
            }
        }

        // Load the asset with the given name or path
        // Path must be relative to project root, such as:
        // "Textures/mytexture" or "Textures/mytexture.xnb"
        public override T Load<T>(string assetName)
        {
            if (LoadedAssets == null) throw new ObjectDisposedException(ToString());
            if (string.IsNullOrEmpty(assetName)) throw new ArgumentNullException(nameof(assetName));

            string assetNoExt = assetName.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase) 
                ? assetName.Substring(0, assetName.Length - 4) : assetName;
            assetNoExt = assetNoExt.Replace("\\", "/"); // normalize path

            if (assetNoExt.StartsWith("./"))
                assetNoExt = assetNoExt.Substring(2);

            //if (assetNoExt.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            //    assetNoExt = assetNoExt.Substring("Content/".Length);

        #if DEBUG
            // if we forbid relative paths, we can streamline our resource manager and eliminate almost all duplicate loading
            if (assetNoExt.Contains("..") || assetNoExt.Contains("./"))
                throw new ArgumentException($"Asset name cannot contain relative paths: '{assetNoExt}'");

            // absolute paths would break all the modding support, so forbid that as well
            if (assetNoExt.Contains(":/"))
                throw new ArgumentException($"Asset name cannot contain absolute paths: '{assetNoExt}'");
        #endif

            if (TryGetAsset(assetNoExt, out object existing))
            {
                if (existing is T assetObj)
                    return assetObj;
                throw new ContentLoadException($"Asset '{assetNoExt}' already loaded as '{existing.GetType()}' while Load requested type '{typeof(T)}");
            }

            T asset = ReadAsset<T>(assetNoExt, null);
            LoadedAssets.Add(assetNoExt, asset);
            return asset;
        }

        protected override Stream OpenStream(string assetName)
        {
            try
            {
                if (GlobalStats.HasMod)
                {
                    var info = new FileInfo(GlobalStats.ModPath + assetName + ".xnb");
                    if (info.Exists)
                        return info.OpenRead();
                }
                return File.OpenRead("Content/" + assetName + ".xnb");
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                    throw new ContentLoadException($"Asset '{assetName}' was not found", ex);
                if (ex is ArgumentException || ex is NotSupportedException || ex is IOException || ex is UnauthorizedAccessException)
                    throw new ContentLoadException($"Asset '{assetName}' could not be opened", ex);
                throw;
            }
        }
    }
}
