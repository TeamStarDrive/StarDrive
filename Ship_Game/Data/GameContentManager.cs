using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SgMotion;
using Ship_Game.Data.Mesh;
using Ship_Game.SpriteSystem;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Processors;
// ReSharper disable UnusedMember.Local

namespace Ship_Game.Data
{
    public sealed class GameContentManager : ContentManager, IEffectCache
    {
        // If non-null, a parent resource manager is checked first for existing resources
        // to avoid double loading resources into memory
        readonly GameContentManager Parent;
        Dictionary<string, object> LoadedAssets; // uses OrdinalIgnoreCase
        Map<string, Effect> LoadedEffects = new Map<string, Effect>();
        List<IDisposable> DisposableAssets;
        public string Name { get; }

        // Enables verbose logging for all asset loads and disposes
        public bool EnableLoadInfoLog { get; set; } = false && Debugger.IsAttached;

        public RawContentLoader RawContent { get; private set; }

        public IReadOnlyDictionary<string, object> Loaded => LoadedAssets;
        readonly object LoadSync = new object();

        public override string ToString() => $"Content:{Name} Assets:{LoadedAssets.Count} Root:{RootDirectory}";

        static GameContentManager()
        {
            FixSunBurnTypeLoader();
        }

        public GameContentManager(IServiceProvider services, string name, string rootDirectory = "Content") : base(services, rootDirectory)
        {
            Name = name;
            LoadedAssets     = (Dictionary<string, object>)GetField("loadedAssets");
            DisposableAssets = (List<IDisposable>)GetField("disposableAssets");
            RawContent       = new RawContentLoader(this);
        }

        public GameContentManager(GameContentManager parent, string name) : this(parent.ServiceProvider, name)
        {
            Parent     = parent;
            RawContent = new RawContentLoader(this);
        }

        object GetField(string field) => typeof(ContentManager).GetField(field, BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(this);

        bool TryGetAsset(string assetNameNoExt, out object asset)
        {
            GameContentManager mgr = this;
            do
            {
                lock (LoadSync)
                {
                    if (mgr.LoadedAssets.TryGetValue(assetNameNoExt, out asset))
                        return true;
                }
            }
            while ((mgr = mgr.Parent) != null);
            return false;
        }

        // Tries to get an existing asset.
        // Returns true if asset exists and type is correct
        // Returns false if asset does not exist
        // Throw ContentLoadException if asset type mismatches
        public bool TryGetAsset<T>(string assetNameNoExt, out T asset)
        {
            if (TryGetAsset(assetNameNoExt, out object existing))
            {
                if (existing is T assetObj)
                {
                    asset = assetObj;
                    return true;
                }
                throw new ContentLoadException($"Asset '{assetNameNoExt}' already loaded as '{existing.GetType()}' while Load requested type '{typeof(T)}'");
            }
            asset = default;
            return false;
        }

        public bool TryGetEffect<T>(string assetName, out T asset) where T : Effect
        {
            GameContentManager mgr = this;
            do
            {
                lock (LoadSync)
                {
                    if (mgr.LoadedEffects.TryGetValue(assetName, out Effect effect) && effect is T assetObj)
                    {
                        asset = assetObj;
                        return true;
                    }
                }
            }
            while ((mgr = mgr.Parent) != null);
            asset = null;
            return false;
        }

        public void AddEffect(string assetName, Effect effect) 
        {
            lock (LoadSync)
                LoadedEffects.Add(assetName, effect);
            RecordDisposableObject(effect);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            LoadedAssets     = null;
            DisposableAssets = null;
            RawContent       = null;
            LoadedEffects    = null;
        }

        static T GetField<T>(object obj, string name)
        {
            return (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        }

        public GraphicsDeviceManager Manager => (GraphicsDeviceManager)ServiceProvider.GetService(typeof(IGraphicsDeviceManager));
        public GraphicsDevice Device => Manager.GraphicsDevice;

        static int TextureSize(Texture2D tex)
        {
            if (tex == null || tex.IsDisposed)
                return 0;
            float mul = 1f;
            switch (tex.Format)
            {
                case SurfaceFormat.Dxt1: mul = 0.5f; break;
                case SurfaceFormat.Dxt3: mul = 1.0f; break;
                case SurfaceFormat.Dxt5: mul = 1.0f; break;
            }
            try { if (tex.LevelCount > 1) mul *= 1.75f; } // mip maps 
            catch (Exception) {}
            return (int)(tex.Width * tex.Height * mul) + 4096/*all the crap that manages this texture*/;
        }

        // Calculates the approximate size of the raw data in assets
        public int GetLoadedAssetBytes()
        {
            GraphicsDevice device = Manager.GraphicsDevice;
            if (device == null || device.IsDisposed)
                return 0;

            int numBytes = 0;
            foreach (object asset in LoadedAssets.Values.ToArray())
            {
                if (asset is Texture2D tex)
                {
                    numBytes += TextureSize(tex);
                }
                else if (asset is TextureAtlas atlas)
                {
                    numBytes += TextureSize(atlas.Atlas);
                }
                else if (asset is Video vid)
                {
                    numBytes += vid.Width * vid.Height * 3/*RGB*/ * 2/*double buffered*/;
                }
                else if (asset is Model mod)
                {
                    numBytes += mod.Bones.Count * 256;
                    foreach (ModelMesh mesh in mod.Meshes)
                        numBytes += mesh.IndexBuffer.SizeInBytes + mesh.VertexBuffer.SizeInBytes;
                }
                else if (asset is SpriteFont font)
                {
                    var fontTex = GetField<Texture2D>(font, "textureValue");
                    numBytes += TextureSize(fontTex);
                    numBytes += font.Characters.Count * 64;
                }
            }
            return numBytes;
        }

        public float GetLoadedAssetMegabytes() => GetLoadedAssetBytes() / (1024f * 1024f);

        // @warning Please be careful with this. Just let ScreenManager do the task of unloading.
        // Call ScreenManager.UnloadContent() to unload EVERYTHING
        public override void Unload()
        {
            if (LoadedAssets == null)
                throw new ObjectDisposedException(ToString());

            float totalMemSaved = GetLoadedAssetMegabytes();
            int count = LoadedAssets.Count;
            try
            {
                if (EnableLoadInfoLog)
                {
                    foreach (KeyValuePair<string,object> obj in LoadedAssets)
                    {
                        if      (obj.Value is Texture2D)          Log.Info(ConsoleColor.Magenta, "Disposing texture  "+obj.Key);
                        else if (obj.Value is TextureAtlas atlas) Log.Info(ConsoleColor.Magenta, "Disposing atlas    "+atlas);
                        else if (obj.Value is Model)              Log.Info(ConsoleColor.Magenta, "Disposing model    "+obj.Key);
                        else if (obj.Value is IDisposable)        Log.Info(ConsoleColor.Magenta, "Disposing asset    "+obj.Key);
                    }
                }
                foreach (IDisposable asset in DisposableAssets)
                    asset?.Dispose();
            }
            finally
            {
                LoadedAssets.Clear();
                DisposableAssets.Clear();
                LoadedEffects.Clear();
            }

            if (count > 0)
            {
                Log.Info($"Unloaded '{Name}' ({count} assets, {totalMemSaved:0.0}MB)");
            }
        }

        void RecordDisposableObject(IDisposable disposable)
        {
            lock (LoadSync)
                DisposableAssets.Add(disposable);
        }

        void DoNothingWithDisposable(IDisposable _)
        {
        }

        struct AssetName
        {
            public readonly string NoExt;     // XNA friendly name without an extension
            public readonly string Extension; // ".obj" or ".png" for raw resource loader

            public bool NonXnaAsset => Extension.Length > 0 && Extension != "xnb";

            public AssetName(string assetName)
            {
                if (assetName.IsEmpty())
                    throw new ArgumentNullException(nameof(assetName));

                NoExt = assetName;
                Extension = "";
                if (assetName[assetName.Length - 4] == '.')
                {
                    Extension = assetName.Substring(assetName.Length - 3).ToLower();
                    NoExt     = assetName.Substring(0, assetName.Length - 4);
                }

                NoExt = NoExt.NormalizedFilePath(); // normalize path

                if (NoExt.StartsWith("./"))
                    NoExt = NoExt.Substring(2);

                // starts with active mod, eg: "Mods/MyMod/" ?
                if (GlobalStats.HasMod && NoExt.StartsWith(GlobalStats.ModPath))
                {
                    // We only remove the Mod prefix for the active mod
                    NoExt = NoExt.Substring(GlobalStats.ModPath.Length);
                }
                //if (assetNoExt.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                //    assetNoExt = assetNoExt.Substring("Content/".Length);

            #if true // #if DEBUG
                // if we forbid relative paths, we can streamline our resource manager and eliminate almost all duplicate loading
                if (NoExt.Contains("..") || NoExt.Contains("./"))
                    throw new ArgumentException($"Asset name cannot contain relative paths: '{NoExt}'");

                // absolute paths would break all the modding support, so forbid that as well
                if (NoExt.Contains(":/"))
                    throw new ArgumentException($"Asset name cannot contain absolute paths: '{NoExt}'");
            #endif
            }
        }



        // Load the asset with the given name or path
        // Path must be relative to project root, such as:
        // "Textures/myTexture" or "Textures/myTexture.xnb"
        public override T Load<T>(string assetName)
        {
            return LoadAsset<T>(assetName, useCache:true);
        }

        public T LoadUncached<T>(string assetName)
        {
            return LoadAsset<T>(assetName, useCache: false);
        }

        public T LoadAsset<T>(string assetName, bool useCache)
        {
            var asset = new AssetName(assetName);
            if (LoadedAssets == null)
                throw new ObjectDisposedException(ToString());

            if (EnableLoadInfoLog)
                Log.Info(ConsoleColor.Cyan, $"Load<{typeof(T).Name}> {assetName}");

            Type assetType = typeof(T);
            if (assetType == typeof(SubTexture))   return (T)(object)LoadSubTexture(assetName);
            if (assetType == typeof(TextureAtlas)) return (T)(object)LoadTextureAtlas(assetName, useCache);



            if (useCache && TryGetAsset(assetName, out T existing))
                return existing;
            T loaded;

            var xnaPath = asset.NoExt;

            // hack to make sure the video asset is loaded with the mod WMV path
            if (GlobalStats.HasMod && assetType == typeof(Video))
            {
                var info = new FileInfo(GlobalStats.ModPath + assetName + ".xnb");
                if (info.Exists)
                {
                    xnaPath = "../" + GlobalStats.ModPath + assetName;
                }
            }

            if (asset.NonXnaAsset)
                loaded = (T) RawContent.LoadAsset(assetName, asset.Extension);
            else
                loaded = ReadAsset<T>(xnaPath, DoNothingWithDisposable);

            // detect possible resource leaks -- this is very slow, so only enable on demand
            #if false
                SlowCheckForResourceLeaks(assetName);
            #endif
            if (useCache)
            {
                RecordCacheObject(assetName, loaded);
                if (loaded is Texture2D texture)
                    texture.Name = assetName;
            }
            return loaded;
        }

        void RecordCacheObject<T>(string name, T obj)
        {
            lock (LoadSync)
            {
                // If same object already exists, we skip Add. We also test for concurrency bugs and type mismatches.
                if (LoadedAssets.TryGetValue(name, out object existing))
                {
                    if ((obj as object) != existing)
                        throw new ContentLoadException($"Duplicate asset '{name}' of type '{typeof(T)}' already loaded! This is a concurrency bug!");
                    if (!(existing is T))
                        throw new ContentLoadException($"Asset '{name}' already loaded as '{existing.GetType()}' while Load requested type '{typeof(T)}'");
                }
                else
                {
                    LoadedAssets.Add(name, obj);
                    if (obj is IDisposable disposable)
                        DisposableAssets.Add(disposable);
                }
            }
        }

        /// Loads a texture and DOES NOT store it inside GameContentManager
        public Texture2D LoadUncachedTexture(FileInfo file, string ext)
        {
            if (EnableLoadInfoLog)
                Log.Info(ConsoleColor.Cyan, $"Load<{typeof(Texture2D).Name}> {file.CleanResPath(false)}");

            if (ext != "xnb")
                return RawContent.LoadImageAsTexture(file);

            return ReadAsset<Texture2D>(file.CleanResPath(), DoNothingWithDisposable);
        }

        /// Loads a texture and DOES NOT store it inside GameContentManager
        public Texture2D LoadUncachedTexture(string name, string type, string folder)
        {
            if (EnableLoadInfoLog)
                Log.Info(ConsoleColor.Cyan, $"Load<{typeof(Texture2D).Name}> {folder}/{name}.{type}");

            if (type != "xnb")
                return RawContent.LoadImageAsTexture(folder+"/"+name+"."+type);

            return ReadAsset<Texture2D>(folder+"/"+name, DoNothingWithDisposable);
        }

        // @note Guaranteed to load an atlas with at least 1 texture
        public TextureAtlas LoadTextureAtlas(string folderWithTextures, bool useAssetCache = true)
        {
            if (useAssetCache && TryGetAsset(folderWithTextures, out TextureAtlas existing))
                return existing;

            TextureAtlas atlas = TextureAtlas.FromFolder(this, folderWithTextures);
            if (atlas != null && useAssetCache) RecordCacheObject(folderWithTextures, atlas);
            return atlas;
        }

        // @return null if texture not found
        // @example LoadSubTexture("Textures/NewUI/x_red");
        public SubTexture LoadSubTexture(string textureName)
        {
            int i = textureName.LastIndexOf('/');
            if (i == -1) i = textureName.LastIndexOf('\\');
            if (i == -1)
                return null;

            string folder = textureName.Substring(0, i);
            // @note LoadTextureAtlas useCache MUST be true, otherwise TextureAtlas will be destroyed
            TextureAtlas atlas = LoadTextureAtlas(folder, useAssetCache: true);
            if (atlas == null)
                return null;

            string name = Path.GetFileNameWithoutExtension(textureName);
            atlas.TryGetTexture(name, out SubTexture texture);
            return texture;
        }

        public SubTexture DefaultTexture() => LoadSubTexture("Textures/NewUI/x_red");

        // ex: texturePath="Textures/NewUI/x_red"
        public SubTexture LoadTextureOrDefault(string texturePath)
        {
            SubTexture texture = LoadSubTexture(texturePath);
            if (texture != null) return texture;
            Log.Warning(ConsoleColor.Red, $"{Name} failed to load texture '{texturePath}'");
            return DefaultTexture();
        }

        // Load texture for a specific mod, such as modName="Overdrive"
        public SubTexture LoadModTexture(string modName, string textureName)
        {
            string modTexPath = $"Mods/{modName}/Textures/{textureName}";
            if (!File.Exists(modTexPath) && !File.Exists(modTexPath+".xnb"))
                return DefaultTexture();
            var texture = Load<Texture2D>(modTexPath);
            return new SubTexture(texture.Name, texture);
        }

        public StaticMesh LoadStaticMesh(string modelName)
        {
            return Load<StaticMesh>(modelName);
        }

        public Model LoadModel(string modelName)
        {
            return Load<Model>(modelName);
        }

        public SkinnedModel LoadSkinnedModel(string modelName)
        {
            return Load<SkinnedModel>(modelName);
        }

        void SlowCheckForResourceLeaks(string assetNoExt)
        {
            string[] keys;
            lock (LoadSync) keys = LoadedAssets.Keys.ToArray();
            foreach (string key in keys)
            {
                if (key.EndsWith(assetNoExt, StringComparison.OrdinalIgnoreCase) ||
                    assetNoExt.EndsWith(key, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning($"Possible ResLeak: '{key}' may be duplicated by '{assetNoExt}'");
                }
            }
        }

        protected override Stream OpenStream(string assetName)
        {
            try
            {
                // NOTE: This will not create the path properly for XNB pointer files like video.

                // trying to do a direct Mod asset load, this may be different from currently active mod
                if (assetName.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase)) 
                {
                    var info = new FileInfo(assetName + ".xnb");
                    if (info.Exists) return info.OpenRead();
                }
                if (GlobalStats.HasMod)
                {
                    var info = new FileInfo(GlobalStats.ModPath + assetName + ".xnb");
                    if (info.Exists)
                    {
                        return info.OpenRead();
                    }
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

        static void FixSunBurnTypeLoader()
        {
            Type readerMgrType  = typeof(ContentTypeReaderManager);
            Type contentMgrType = typeof(GameContentManager);

            FieldInfo readerType = readerMgrType.GetField("readerTypeToReader", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo nameTo     = readerMgrType.GetField("nameToReader", BindingFlags.NonPublic | BindingFlags.Static);
            ReaderTypeToReader = readerType?.GetValue(null) as Dictionary<Type, ContentTypeReader>;
            NameToReader       = nameTo?.GetValue(null) as Dictionary<string, ContentTypeReader>;

            MethodInfo oldMethod = readerMgrType.GetMethod("InstantiateTypeReader", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo newMethod = contentMgrType.GetMethod("InstantiateTypeReader", BindingFlags.NonPublic | BindingFlags.Static);
            MethodUtil.ReplaceMethod(newMethod, oldMethod);

            XnaAssembly = readerMgrType.Assembly;
            SunburnAssemblyName = typeof(SceneInterface).Assembly.FullName;
        }

        static Dictionary<Type, ContentTypeReader> ReaderTypeToReader;
        static Dictionary<string, ContentTypeReader> NameToReader;
        static Assembly XnaAssembly;
        static string SunburnAssemblyName;

        // @note This IS used, but only through reflection. It's referenced by string in `FixSunBurnTypeLoader()`
        static bool InstantiateTypeReader(string readerTypeName, ContentReader contentReader, out ContentTypeReader reader)
        {
            try
            {
                Type type;
                if (readerTypeName.StartsWith("SynapseGaming."))
                {
                    string typeName = readerTypeName.Substring(0, readerTypeName.IndexOf(','));
                    string reroutedFullName = typeName + ", " + SunburnAssemblyName;
                    type = Type.GetType(reroutedFullName);
                }
                else
                {
                    type = XnaAssembly.GetType(readerTypeName) ?? Type.GetType(readerTypeName);
                }

                if (type == null)
                {
                    throw new ContentLoadException($"{contentReader.AssetName} load failed: TypeReader not found for {readerTypeName}");
                }
                if (ReaderTypeToReader.TryGetValue(type, out reader))
                {
                    NameToReader.Add(readerTypeName, reader);
                    return false;
                }
                reader = (ContentTypeReader)Activator.CreateInstance(type);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is TargetInvocationException || (ex is TypeLoadException || ex is NotSupportedException) || (ex is MemberAccessException || ex is InvalidCastException))
                    throw new ContentLoadException($"{contentReader.AssetName} load failed: TypeReader {readerTypeName} is invalid", ex);
                throw;
            }
        }

    }
}
