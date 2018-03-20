using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using SgMotion;

namespace Ship_Game
{
    public sealed class GameContentManager : ContentManager
    {
        // If non-null, a parent resource manager is checked first for existing resources
        // to avoid double loading resources into memory
        private readonly GameContentManager Parent;
        private Dictionary<string, object> LoadedAssets; // uses OrdinalIgnoreCase
        private List<IDisposable> DisposableAssets;
        public string Name { get; }

        private RawContentLoader RawContent;

        public IReadOnlyDictionary<string, object> Loaded => LoadedAssets;
        private readonly object LoadSync = new object();

        public Map<string, Model> Models;
        public Map<string, StaticMesh> Meshes;
        public Map<string, SkinnedModel> SkinnedModels;
        public Array<Model> RoidsModels = new Array<Model>();
        public Array<Model> JunkModels = new Array<Model>();

        static GameContentManager()
        {
            FixSunBurnTypeLoader();
        }

        public GameContentManager(IServiceProvider services, string name) : base(services, "Content")
        {
            Name = name;
            LoadedAssets     = (Dictionary<string, object>)GetField("loadedAssets");
            DisposableAssets = (List<IDisposable>)GetField("disposableAssets");
            RawContent       = new RawContentLoader(this);
            SkinnedModels = new Map<string, SkinnedModel>();
            Meshes = new Map<string, StaticMesh>();
            Models = new Map<string, Model>();
            JunkModels = new Array<Model>();
            RoidsModels = new Array<Model>();
        }

        public GameContentManager(GameContentManager parent, string name) : this(parent.ServiceProvider, name)
        {
            Parent     = parent;
            RawContent = new RawContentLoader(this);
        }

        private object GetField(string field) => typeof(ContentManager).GetField(field, BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(this);

        public bool EnableLoadInfoLog { get; set; }

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
            LoadedAssets     = null;
            DisposableAssets = null;
            RawContent       = null;
            Meshes = null;
            SkinnedModels = null;
            Models = null;


        }

        private static T GetField<T>(object obj, string name)
        {
            return (T)obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);
        }

        public GraphicsDeviceManager Manager => (GraphicsDeviceManager)ServiceProvider.GetService(typeof(IGraphicsDeviceManager));

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
            try { if (tex.LevelCount > 1) mul *= 1.75f; } // mipmaps 
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
                    foreach (ModelMesh mesh in mod.Meshes)
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
            if (LoadedAssets == null)
                throw new ObjectDisposedException(ToString());

            float totalMemSaved = GetLoadedAssetMegabytes();
            int count = LoadedAssets.Count;
            try
            {
                foreach (IDisposable asset in DisposableAssets)
                    asset?.Dispose();
            }
            finally
            {
                LoadedAssets.Clear();
                DisposableAssets.Clear();
            }

            if (totalMemSaved > 0f)
            {
                Log.Info("Unloaded '{0}' ({1} assets, {2:0.0}MB)", Name, count, totalMemSaved);
            }
        }

        private void RecordDisposableObject(IDisposable disposable)
        {
            lock (LoadSync)
                DisposableAssets.Add(disposable);
        }

        // Load the asset with the given name or path
        // Path must be relative to project root, such as:
        // "Textures/mytexture" or "Textures/mytexture.xnb"
        public override T Load<T>(string assetName)
        {
            if (LoadedAssets == null) throw new ObjectDisposedException(ToString());
            if (assetName.IsEmpty())  throw new ArgumentNullException(nameof(assetName));

            string extension  = "";
            string assetNoExt = assetName;
            string assetWext = assetName;
            if (assetName[assetName.Length - 4] == '.')
            {
                extension = assetName.Substring(assetName.Length - 3).ToLower();
                assetNoExt = assetName.Substring(0, assetName.Length - 4);
            }
            else assetWext += ".xnb";

            assetNoExt = assetNoExt.Replace("\\", "/"); // normalize path

            if (assetNoExt.StartsWith("./"))
                assetNoExt = assetNoExt.Substring(2);

            // starts with active mod, eg: "Mods/MyMod/" ?
            if (GlobalStats.HasMod && assetNoExt.StartsWith(GlobalStats.ModPath))
            {
                // We only remove the Mod prefix for the active mod
                assetNoExt = assetNoExt.Substring(GlobalStats.ModPath.Length);
            }
            //if (assetNoExt.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            //    assetNoExt = assetNoExt.Substring("Content/".Length);

        #if true // #if DEBUG
            // if we forbid relative paths, we can streamline our resource manager and eliminate almost all duplicate loading
            if (assetNoExt.Contains("..") || assetNoExt.Contains("./"))
                throw new ArgumentException($"Asset name cannot contain relative paths: '{assetNoExt}'");

            // absolute paths would break all the modding support, so forbid that as well
            if (assetNoExt.Contains(":/"))
                throw new ArgumentException($"Asset name cannot contain absolute paths: '{assetNoExt}'");
        #endif

            if (EnableLoadInfoLog)
                Log.Info(ConsoleColor.Cyan, "Load<{0}> {1}", typeof(T).Name, assetNoExt);

            if (TryGetAsset(assetNoExt, out object existing))
            {
                if (existing is T assetObj)
                    return assetObj;
                throw new ContentLoadException($"Asset '{assetNoExt}' already loaded as '{existing.GetType()}' while Load requested type '{typeof(T)}");
            }

            T asset = (extension.Length > 0 && extension != "xnb") //(T)RawContent.LoadAsset(assetWext, extension); // 
                ? (T)RawContent.LoadAsset(assetName, extension) 
                : ReadAsset<T>(assetNoExt, RecordDisposableObject); //

            // detect possible resource leaks -- this is very slow, so only enable on demand
#if false
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
#endif
            lock (LoadSync) LoadedAssets.Add(assetNoExt, asset);            
            return asset;
        }

        protected override Stream OpenStream(string assetName)
        {
            try
            {
                // trying to do a direct Mod asset load, this may be different from currently active mod
                if (assetName.StartsWith("Mods/", StringComparison.OrdinalIgnoreCase)) 
                {
                    var info = new FileInfo(assetName + ".xnb");
                    if (info.Exists) return info.OpenRead();
                }
                if (GlobalStats.HasMod)
                {
                    var info = new FileInfo(GlobalStats.ModPath + assetName + ".xnb");
                    if (info.Exists) return info.OpenRead();
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

        private static void FixSunBurnTypeLoader()
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
            SunburnAssemblyName = typeof(SynapseGaming.LightingSystem.Core.SceneInterface).Assembly.FullName;
        }

        private static Dictionary<Type, ContentTypeReader> ReaderTypeToReader;
        private static Dictionary<string, ContentTypeReader> NameToReader;
        private static Assembly XnaAssembly;
        private static string SunburnAssemblyName;

        private static bool InstantiateTypeReader(string readerTypeName, ContentReader contentReader, out ContentTypeReader reader)
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
