using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SDGraphics;
using SDUtils;
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
        public string Name { get; }

        // Enables verbose logging for all asset loads and disposes
        public bool DebugAssetLoading => GlobalStats.DebugAssetLoading;
        readonly Map<string, string> LoadStackTraces = new(); // for debugging asset loads

        public RawContentLoader RawContent { get; private set; }

        readonly object LoadSync = new();

        public override string ToString() => $"Content:{Name} Assets:{LoadedAssets.Count} Root:{RootDirectory}";

        static GameContentManager()
        {
            FixSunBurnTypeLoader();
        }

        public GameContentManager(IServiceProvider services, string name, string rootDirectory = "Content") : base(services, rootDirectory)
        {
            Name = name;
            LoadedAssets = (Dictionary<string, object>)GetField("loadedAssets");
            RawContent = new(this);
        }

        public GameContentManager(GameContentManager parent, string name) : this(parent.ServiceProvider, name)
        {
            Parent = parent;
            RawContent = new(this);
        }

        protected override void Dispose(bool disposing)
        {
            // note: this will call Unload() and will set base.loadedAssets to null
            base.Dispose(disposing);

            lock (LoadSync) // set our reference of LoadedAssets to null
                LoadedAssets = null;
            RawContent = null;
        }

        object GetField(string field)
            => typeof(ContentManager).GetField(field, BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(this);
        
        static T GetField<T>(object obj, string name)
        {
            return (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        }

        public GraphicsDeviceManager Manager => (GraphicsDeviceManager)ServiceProvider.GetService(typeof(IGraphicsDeviceManager));
        public GraphicsDevice Device => Manager.GraphicsDevice;

        bool TryGetAsset(string assetNameWithExt, out object asset)
        {
            GameContentManager mgr = this;
            do
            {
                lock (LoadSync)
                {
                    var assets = mgr.LoadedAssets;
                    if (assets != null && assets.TryGetValue(assetNameWithExt, out asset))
                    {
                        if (IsDisposed(asset))
                        {
                            Log.Error($"Cached Asset '{assetNameWithExt}' is Disposed, discarding cached asset");
                            assets.Remove(assetNameWithExt);
                        }
                        else
                        {
                            return true;
                        }
                    }
                }
            }
            while ((mgr = mgr.Parent) != null);

            asset = null;
            return false;
        }

        // Tries to get an existing asset.
        // Returns true if asset exists and type is correct
        // Returns false if asset does not exist
        // Throw ContentLoadException if asset type mismatches
        public bool TryGetAsset<T>(string assetNameWithExt, out T asset)
        {
            if (TryGetAsset(assetNameWithExt, out object existing))
            {
                if (existing is T assetObj)
                {
                    asset = assetObj;
                    return true;
                }
                Log.Error($"Asset '{assetNameWithExt}' already loaded as '{existing.GetType()}' while Load requested type '{typeof(T)}'");
            }
            asset = default;
            return false;
        }
        
        // SUNBURN COMPATIBILITY
        public bool TryGetEffect<T>(string assetName, out T asset) where T : Effect
        {
            if (TryGetAsset(assetName, out object assetObj) && assetObj is T fx)
            {
                asset = fx;
                return true;
            }
            asset = default;
            return false;
        }

        // SUNBURN COMPATIBILITY
        public void AddEffect(string assetName, Effect effect) 
        {
            lock (LoadSync)
                LoadedAssets.Add(assetName, effect);
        }

        public static int TextureSize(Texture2D tex)
        {
            if (tex == null || tex.IsDisposed)
                return 0;
            float mul = 1f;
            switch (tex.Format)
            {
                case SurfaceFormat.Dxt1: mul = 0.5f; break;
                case SurfaceFormat.Dxt3: mul = 1.0f; break;
                case SurfaceFormat.Dxt5: mul = 1.0f; break;
                case SurfaceFormat.Rgb32: mul = 4.0f; break;
                case SurfaceFormat.Rgba32: mul = 4.0f; break;
                case SurfaceFormat.Color: mul = 4.0f; break;
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
            object[] assets;
            lock (LoadSync) assets = LoadedAssets.Values.ToArr();

            foreach (object asset in assets)
            {
                if (asset is Texture2D tex)
                {
                    numBytes += TextureSize(tex);
                }
                else if (asset is TextureAtlas atlas)
                {
                    numBytes += atlas.GetUsedMemory();
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
                else if (asset is Graphics.Font font)
                {
                    var fontTex = GetField<Texture2D>(font, "textureValue");
                    numBytes += TextureSize(fontTex);
                    numBytes += font.NumCharacters * 64;
                }
            }
            return numBytes;
        }

        public float GetLoadedAssetMegabytes() => GetLoadedAssetBytes() / (1024f * 1024f);

        // @warning Please be careful with this. Just let ScreenManager do the task of unloading.
        // Call ScreenManager.UnloadContent() to unload EVERYTHING
        public override void Unload()
        {
            Dictionary<string, object> assets;
            lock (LoadSync) assets = LoadedAssets;
            if (assets == null)
                throw new ObjectDisposedException(ToString());

            float totalMemSaved = GetLoadedAssetMegabytes();
            int count = assets.Count;
            try
            {
                foreach (KeyValuePair<string,object> obj in assets)
                {
                    Dispose(obj.Key, obj.Value); // this will modify DisposableAssets
                }
            }
            finally
            {
                assets.Clear();
            }

            if (count > 0)
            {
                Log.Info($"Unloaded '{Name}' ({count} assets, {totalMemSaved:0.0}MB)");
            }
        }

        static void DoNothingWithDisposable(IDisposable _)
        {
        }

        /// <summary>
        /// Manually check and log all asset disposing to ensure we don't have accidental leaks
        /// some of the fonts and models can leak GPU resources.
        /// The asset is removed from DisposableAssets after being disposed.
        /// </summary>
        void Dispose(string assetName, object asset)
        {
            switch (asset)
            {
                case GraphicsResource g:
                    if (!g.IsDisposed)
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing texture  "+(assetName??g.Name));
                        g.Dispose();
                    }
                    break;
                case TextureAtlas atlas:
                    if (!atlas.IsDisposed)
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing atlas    "+(assetName??atlas.Name));
                        atlas.Dispose();
                    }
                    break;
                case StaticMesh mesh:
                    if (!mesh.IsDisposed)
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing mesh     "+(assetName??mesh.Name));
                        mesh.Dispose();
                    }
                    break;
                case Model model:
                    if (!StaticMesh.IsModelDisposed(model))
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing model    "+(assetName??model.Meshes[0].Name));
                        StaticMesh.DisposeModel(model);
                    }
                    break;
                case SkinnedModel skinnedModel:
                    if (!StaticMesh.IsModelDisposed(skinnedModel))
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing aniModel "+(assetName??skinnedModel.Model.Meshes[0].Name));
                        StaticMesh.DisposeModel(skinnedModel);
                    }
                    break;
                case SpriteFont font:
                    var texture = GetField<Texture2D>(font, "textureValue");
                    if (!texture.IsDisposed)
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing font     "+(assetName??texture.Name));
                        texture.Dispose();
                    }
                    break;
                case Effect fx:
                    if (!fx.IsDisposed)
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing effect   "+(assetName??"unknown"));
                        fx.Dispose();
                    }
                    break;
                case Video _: // video is just a reference object, nothing to dispose
                    break;
                case IDisposable disposable:
                    Log.Write(ConsoleColor.Magenta, "Disposing asset    "+(assetName ?? disposable.GetType().GetTypeName()));
                    disposable.Dispose();
                    break;
                default:
                    Log.Write(ConsoleColor.Red, "Cannot Dispose asset "+(assetName ?? asset.GetType().GetTypeName()));
                    break;
            }
        }

        public bool IsDisposed(object asset)
        {
            switch (asset)
            {
                // Texture, Texture2D, Texture3D, VertexBuffer, IndexBuffer, ...
                case GraphicsResource g: return g.IsDisposed;
                case TextureAtlas atlas: return atlas.IsDisposed;
                case StaticMesh mesh: return mesh.IsDisposed;
                case Model model: return StaticMesh.IsModelDisposed(model);
                case SkinnedModel sm: return StaticMesh.IsModelDisposed(sm);
                case Video _: return false; // nothing to dispose
                case SpriteFont font: return GetField<Texture2D>(font, "textureValue").IsDisposed;
            }
            // anything that falls here is of non-disposable type, such as `Video`
            return false;
        }

        /// <summary>
        /// Disposes an asset and removes it from the content manager
        /// </summary>
        public void Dispose<T>(ref T asset) where T : class
        {
            if (asset == null)
                return;
            lock (LoadSync)
            {
                // find the key of this asset (slow)
                // TODO: maybe add Asset-To-Key Mapping?
                foreach (KeyValuePair<string, object> kv in LoadedAssets)
                {
                    if (ReferenceEquals(asset, kv.Value))
                    {
                        Dispose(kv.Key, kv.Value);
                        asset = null;
                        LoadedAssets.Remove(kv.Key);
                        return;
                    }
                }
            }
            // we didn't find it in LoadedAssets, but lets dispose it anyways
            Dispose(null, asset);
            asset = null;
        }

        readonly struct AssetName
        {
            public readonly string RelPathWithExt; // "Textures/hqspace.xnb"
            public readonly string Extension; // ".obj" or ".png" for raw resource loader
            public readonly bool NonXnaAsset;
            public override string ToString() => RelPathWithExt;

            public AssetName(string assetName)
            {
                int extensionIndex = assetName.LastIndexOf('.', assetName.Length-1, 6);
                if (extensionIndex != -1)
                {
                    RelPathWithExt = Sanitized(assetName);
                    Extension = assetName.Substring(extensionIndex + 1).ToLower();
                    NonXnaAsset = Extension != "xnb" && Extension != "wmv";
                }
                else
                {
                    RelPathWithExt = Sanitized(assetName) + ".xnb";
                    Extension = "xnb"; // assume xnb
                    NonXnaAsset = false;
                }

            #if true // #if DEBUG
                // absolute paths would break all the modding support, so forbid that as well
                if (assetName.Contains(":/"))
                    throw new ArgumentException($"Asset name cannot contain absolute paths: '{assetName}'");
            #endif
            }
            public AssetName(FileInfo file)
            {
                string assetName = file.RelPath();
                RelPathWithExt = Sanitized(assetName);
                Extension = file.Extension.TrimStart('.').ToLower();
                NonXnaAsset = Extension != "xnb" && Extension != "wmv";
            }
            static string Sanitized(string assetName)
            {
                if (assetName.StartsWith("Content"))
                    assetName = assetName.Substring("Content/".Length);
                return assetName.Replace('\\', '/');
            }
        }

        // Load the asset with the given name or path
        // Path must be relative to project root, such as:
        // "Textures/myTexture" or "Textures/myTexture.xnb"
        // If a Mod file with the same relative path exists, the mod file is loaded instead
        public override T Load<T>(string assetName)
        {
            return LoadAsset<T>(assetName, useCache:true);
        }

        T LoadAsset<T>(string assetName, bool useCache)
        {
            if (LoadedAssets == null)
                throw new ObjectDisposedException(ToString());

            Type assetType = typeof(T);
            if (assetType == typeof(TextureAtlas))
                return (T)(object)LoadTextureAtlas(assetName, useCache);
            
            var asset = new AssetName(assetName);
            if (assetType == typeof(SubTexture))
                return (T)(object)LoadSubTexture(asset.RelPathWithExt);

            if (useCache && TryGetAsset(asset.RelPathWithExt, out T existing))
                return existing;

            if (DebugAssetLoading)
            {
                Log.Write(ConsoleColor.Cyan, $"Load<{typeof(T).Name}> {asset.RelPathWithExt}");
                // detect possible resource leaks -- this is very slow, so only enable on demand
                SlowCheckForResourceLeaks(asset.RelPathWithExt);
            }

            T loaded;
            if (asset.NonXnaAsset)
                loaded = (T)RawContent.LoadAsset(typeof(T), asset.RelPathWithExt, asset.Extension);
            else
                loaded = ReadXnaAsset<T>(asset.RelPathWithExt);

            if (useCache)
            {
                lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref loaded);
            }

            return loaded;
        }

        void SlowCheckForResourceLeaks(string assetNoExt)
        {
            lock (LoadSync)
            {
                foreach (KeyValuePair<string, object> asset in LoadedAssets)
                {
                    if (asset.Key.EndsWith(assetNoExt, StringComparison.OrdinalIgnoreCase) ||
                        assetNoExt.EndsWith(asset.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.WarningWithCallStack($"Possible ResLeak: existing {asset.Value.GetType().Name} '{asset.Key}' may be duplicated by new '{assetNoExt}'");
                        if (LoadStackTraces.TryGetValue(asset.Key, out string stacktrace))
                            Log.Warning($"  existing asset Load trace:\n{stacktrace}");
                        else
                            Log.Warning("  existing asset did NOT have an asset Load trace (was it loaded by Sunburn instead?)");
                    }
                }
            }
        }

        T ReadXnaAsset<T>(string assetName)
        {
            T loaded = ReadAsset<T>(assetName, DoNothingWithDisposable);
            if (loaded is Texture2D texture)
                texture.Name = assetName;
            return loaded;
        }

        /// <summary>
        /// Tries to record a cache object. If it already exists, then the existing one will be used and give `obj` is disposed.
        /// </summary>
        void RecordCacheObject<T>(string name, ref T obj)
        {
            // If same object already exists, we skip Add. We also test for concurrency bugs and type mismatches.
            if (LoadedAssets.TryGetValue(name, out object existing))
            {
                if (IsDisposed(existing))
                {
                    LoadedAssets[name] = obj;
                    if (DebugAssetLoading) LoadStackTraces[name] = Environment.StackTrace;
                    Log.Error($"Asset '{name}' was disposed and got replaced by the new instance");
                    return;
                }
                if (existing is not T)
                {
                    Log.Error($"Asset '{name}' already loaded as '{existing.GetType()}' while Load requested type '{typeof(T)}'");
                }
                else if (!ReferenceEquals(obj, existing))
                {
                    Log.Error($"Duplicate asset '{name}' of type '{typeof(T)}' already loaded!");
                    Dispose(name, obj);
                    obj = (T)existing; // use the existing one instead
                }
            }
            else
            {
                LoadedAssets.Add(name, obj);
                if (DebugAssetLoading) LoadStackTraces[name] = Environment.StackTrace;
            }
        }

        /// <summary>
        /// Loads a textures and DOES NOT cache it inside GameContentManager.
        /// WARNING: This method can easily cause memory leaks since there is no cache checks. Ensure it is always synchronized.
        /// </summary>
        public Texture2D LoadUncachedTexture(FileInfo file)
        {
            string ext = file.Extension.Substring(1);
            return LoadUncachedTexture(file, ext);
        }

        /// <summary>
        /// Loads a textures and DOES NOT cache it inside GameContentManager.
        /// WARNING: This method can easily cause memory leaks since there is no cache checks. Ensure it is always synchronized.
        /// </summary>
        public Texture2D LoadUncachedTexture(FileInfo file, string ext)
        {
            // the file path may be from AppData folder, in which case RelPath() doesn't work
            if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadUncachedTexture {file.FullName}  Thread={Thread.CurrentThread.Name}");
            if (ext != "xnb")
                return RawContent.LoadTexture(file);

            // needed for TextureExporter tool
            string assetPath = file.RelPath(); // XNB can only load from Content dir
            return ReadXnaAsset<Texture2D>(assetPath); 
        }

        // Loads a texture and caches it inside GameContentManager if useCache=true
        public Texture2D LoadTexture(FileInfo file)
        {
            AssetName asset = new(file);
            if (TryGetAsset(asset.RelPathWithExt, out Texture2D tex))
                return tex;
            
            if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadTexture {asset.RelPathWithExt}");

            string ext = file.Extension.Substring(1);
            if (ext != "xnb")
                tex = RawContent.LoadTexture(file);
            else
                tex = ReadXnaAsset<Texture2D>(asset.RelPathWithExt);

            lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref tex);
            return tex;
        }

        /// <summary>
        /// Guaranteed to load an atlas with at least 1 texture.
        /// This might be called by multiple threads, so additional synchronization is required
        /// </summary>
        public TextureAtlas LoadTextureAtlas(string folderWithTextures, bool useAssetCache = true)
        {
            if (useAssetCache)
            {
                lock (LoadSync) // this is a re-enterable lock
                {
                    if (TryGetAsset(folderWithTextures, out TextureAtlas existing))
                        return existing;

                    if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadTextureAtlas {folderWithTextures}  Thread={Thread.CurrentThread.Name}");

                    TextureAtlas atlas = TextureAtlas.FromFolder(folderWithTextures);
                    if (atlas != null)
                        RecordCacheObject(folderWithTextures, ref atlas);
                    return atlas;
                }
            }
            else
            {
                if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadUncachedTextureAtlas {folderWithTextures}  Thread={Thread.CurrentThread.Name}");
                TextureAtlas atlas = TextureAtlas.FromFolder(folderWithTextures);
                return atlas;
            }
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

        // Load texture for a specific mod
        public SubTexture LoadModTexture(string modPath, string textureName)
        {
            string modTexPath = Path.Combine(modPath, textureName);
            if (!File.Exists(modTexPath) && !File.Exists(modTexPath+".xnb"))
                return DefaultTexture();
            var texture = Load<Texture2D>(modTexPath);
            return new SubTexture(texture.Name, texture, modTexPath);
        }

        // Load and compile an .fx file
        public Effect LoadEffect(string effectFile)
        {
            AssetName asset = new(effectFile);
            if (TryGetAsset(asset.RelPathWithExt, out Effect existing))
                return existing;
            
            FileInfo file = ResourceManager.GetModOrVanillaFile(asset.RelPathWithExt);
            if (file == null)
                throw new FileNotFoundException($"LoadEffect {asset.RelPathWithExt} failed");

            if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadEffect {file.RelPath()}");

            string sourceCode = File.ReadAllText(file.FullName);
            CompiledEffect compiled = Effect.CompileEffectFromSource(sourceCode, Empty<CompilerMacro>.Array, null, 
                                                                     CompilerOptions.None, TargetPlatform.Windows);
            if (!compiled.Success)
            {
                throw new($"LoadEffect {asset.RelPathWithExt} failed: {compiled.ErrorsAndWarnings}");
            }
            
            var fx = new Effect(Device, compiled.GetEffectCode(), CompilerOptions.None, null);
            lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref fx);
            return fx;
        }

        public StaticMesh LoadStaticMesh(string meshName, bool animated = false)
        {
            AssetName asset = new(meshName);
            if (TryGetAsset(asset.RelPathWithExt, out StaticMesh mesh))
                return mesh;
            
            if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadStaticMesh {asset.RelPathWithExt}");

            if (RawContentLoader.IsSupportedMesh(asset.RelPathWithExt))
            {
                mesh = RawContent.LoadStaticMesh(asset.RelPathWithExt);
            }
            else if (animated)
            {
                // cannot cache these, otherwise we'll get a duplicate Model load
                SkinnedModel skinned = LoadAsset<SkinnedModel>(asset.RelPathWithExt, useCache:false);
                mesh = StaticMesh.FromSkinnedModel(asset.RelPathWithExt, skinned);
            }
            else
            {
                Model model = LoadAsset<Model>(asset.RelPathWithExt, useCache:false);
                mesh = StaticMesh.FromStaticModel(asset.RelPathWithExt, model);
            }

            lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref mesh);
            return mesh;
        }

        public Model LoadModel(string modelName)
        {
            return Load<Model>(modelName);
        }

        public SkinnedModel LoadSkinnedModel(string modelName)
        {
            return Load<SkinnedModel>(modelName);
        }

        protected override Stream OpenStream(string assetNameWithExt)
        {
            try
            {
                string assetPath = assetNameWithExt.NormalizedFilePath();

                // trying to do a direct Mod asset load, this may be different from currently active mod
                if (assetPath.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase)) 
                {
                    var info = new FileInfo(assetPath);
                    if (info.Exists)
                    {
                        //if (EnableLoadInfoLog) Log.Write(ConsoleColor.Cyan, $"OpenStream {assetPath}");
                        return info.OpenRead();
                    }
                    throw new FileNotFoundException(assetPath);
                }

                if (assetPath.StartsWith("Content"))
                    assetPath = assetPath.Substring("Content/".Length);

                // if Mod has file with the same name, use it instead of Vanilla file
                if (GlobalStats.HasMod)
                {
                    string modAssetPath = GlobalStats.ModPath + assetPath;
                    var info = new FileInfo(modAssetPath);
                    if (info.Exists)
                    {
                        //if (EnableLoadInfoLog) Log.Write(ConsoleColor.Cyan, $"OpenStream {modAssetPath}");
                        return info.OpenRead();
                    }
                }

                // Vanilla content load
                string vanillaAssetPath = "Content/" + assetPath;
                //if (EnableLoadInfoLog) Log.Write(ConsoleColor.Cyan, $"OpenStream {vanillaAssetPath}");
                return File.OpenRead(vanillaAssetPath);
            }
            catch (Exception ex)
            {
                if (ex is FileNotFoundException || ex is DirectoryNotFoundException)
                    throw new ContentLoadException($"Asset '{assetNameWithExt}' was not found", ex);
                if (ex is ArgumentException || ex is NotSupportedException || ex is IOException || ex is UnauthorizedAccessException)
                    throw new ContentLoadException($"Asset '{assetNameWithExt}' could not be opened", ex);
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
