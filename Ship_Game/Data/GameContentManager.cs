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
using SDGraphics.Shaders;
using SDUtils;
using Ship_Game.Data.Mesh;
using Ship_Game.SpriteSystem;
using SynapseGaming.LightingSystem.Processors;
// SynapseGaming.LightingSystem.Core/Processors usings were removed in Phase 1.8.12
// (SunBurn type loader stubbed). IEffectCache restored via SunBurnStubs.cs in Phase 1.9.
// Historical context only — no further action.
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

        // Phase 3.3: each XNA 3.1 D3DX-compiled Effect XNB still in this set is
        // hand-rewritten as HLSL and shipped as a .mgfxo sibling — preferred via the
        // .xnb -> .mgfxo fallback in LoadAsset before this stub fires. As entries are
        // restored (mgfxo built + verified), remove them here. The set itself can go
        // once it's empty. See memory: project_phase2_effect_xnb_drift.md
        //
        // 2026-05-02: Effects/desaturate.xnb restored via desaturate.mgfxo (probe).
        // 2026-05-02: BasicFogOfWar attempted + reverted — 4-instruction PS rewrite did
        //   not produce the right fog mask. Failure was the manual Pass.Apply()-
        //   after-SpriteBatch-Begin pattern (silent black under MGFX 3.8.1.303 /
        //   DX11), not the shader logic. Re-restored 2026-05-06 (§3.7 step 3) via
        //   SpriteBatch.Begin(effect:) + parameter-driven LightsTexture sampler-
        //   state, mirroring the BloomCombine pattern.
        // 2026-05-03: Effects/PlanetHalo.xnb restored via PlanetHalo.mgfxo (vs_2_0+ps_2_0
        //   atmospheric ring rewrite; no textures so no sampler-binding pitfalls).
        // 2026-05-04: Effects/scale.xnb restored via scale.mgfxo (vs_1_1+ps_2_0 shield
        //   gradient rewrite — UV-zoom VS, alpha-mask PS).
        // 2026-05-04: Effects/Thrust.xnb restored via Thrust.mgfxo (vs_3_0+ps_3_0 thruster
        //   cone rewrite — animated volume noise + cone falloff + silhouette term).
        // 2026-05-05: Effects/BeamFX.xnb restored via BeamFX.mgfxo (vs_1_1+ps_2_0 beam
        //   weapon rewrite — WVP + UV-scroll VS, single tex2D PS). Decode unblocked
        //   by fixing the LZX framing byte-order bug in EffectXnbDump.
        static readonly HashSet<string> Phase2BrokenEffectXnbs = new(StringComparer.OrdinalIgnoreCase)
        {
        };
        static readonly HashSet<string> Phase2WarnedEffects = new(StringComparer.OrdinalIgnoreCase);
        static void WarnPhase2BrokenEffectOnce(string assetName)
        {
            lock (Phase2WarnedEffects)
            {
                if (Phase2WarnedEffects.Add(assetName))
                    Log.Warning($"Phase 2.2 stub: returning null for Effect '{assetName}' (XNA 3.1 D3DX bytecode incompatible with MGFX)");
            }
        }

        public override string ToString() => $"Content:{Name} Assets:{LoadedAssets.Count} Root:{RootDirectory}";

        static GameContentManager()
        {
            FixSunBurnTypeLoader();
            Xna31Compat.Register();
            Mesh.SunBurnReaderStubs.Register();
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
        
        // MonoGame's SpriteFont stores the underlying Texture2D in private field `_texture`
        // (XNA 3.1 used `textureValue`). Reflection lookup tolerates absence so the
        // disposal/size paths don't NRE on partially-constructed or stub fonts.
        static Texture2D GetSpriteFontTexture(SpriteFont font)
            => font == null ? null : GetField<Texture2D>(font, "_texture");

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
                            // the asset was Disposed and should be removed
                            // this could be intentional to force asset reload
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
                // MonoGame removed Rgb32/Rgba32; only Color/HalfVector4/etc. remain.
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
                    // Note: ModelMesh.IndexBuffer/VertexBuffer moved to ModelMeshPart in MonoGame
                    // — already accounted for via the inner MeshParts loop below.
                    foreach (ModelMesh mesh in mod.Meshes)
                        foreach (ModelMeshPart part in mesh.MeshParts)
                            numBytes += (part.IndexBuffer?.IndexCount ?? 0) * 2
                                      + (part.VertexBuffer?.VertexCount ?? 0) * part.VertexBuffer.VertexDeclaration.VertexStride;
                }
                else if (asset is Graphics.Font font)
                {
                    var fontTex = GetSpriteFontTexture(font.XnaFont);
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
                // Skinned meshes load as StaticMesh (with IsSkinned=true and a BoneAnimationPlayer)
                // through the §3.10 FBX pipeline, so the historical XNA SkinnedModel switch arm
                // is no longer needed.
                case SpriteFont font:
                    var texture = GetSpriteFontTexture(font);
                    if (texture != null && !texture.IsDisposed)
                    {
                        if (DebugAssetLoading) Log.Write(ConsoleColor.Magenta, "Disposing font     "+(assetName??texture.Name));
                        texture.Dispose();
                    }
                    break;
                // Effect and Shader cases removed — both inherit from GraphicsResource in MonoGame
                // and are already handled by the case above. (XNA 3.1 had a different hierarchy.)
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
                // Skinned meshes share the StaticMesh case above; no separate SkinnedModel arm.
                case Video _: return false; // nothing to dispose
                case SpriteFont font: { var t = GetSpriteFontTexture(font); return t == null || t.IsDisposed; }
                // Effect/Shader cases removed — both inherit GraphicsResource (handled above).
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
        
        /// <summary>
        /// Disposes an asset and removes it from the content manager
        /// </summary>
        public void Dispose<T>(T asset) where T : class
        {
            Dispose<T>(ref asset);
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

            // Phase 3.3: prefer a .mgfxo sibling over the (often broken) D3DX fx_2_0
            // .xnb. The .mgfxo is a hand-rewritten HLSL effect compiled by mgfxc to
            // MonoGame-compatible MGFX. Mod-friendly: GetContentPath resolves through
            // Mods/Vanilla so a mod can ship its own .fx (compiled to .mgfxo) and have
            // it win over the vendored one. Falls through to the legacy stub list when
            // no sibling exists.
            if (assetType == typeof(Effect) && asset.RelPathWithExt.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
            {
                if (useCache && TryGetAsset(asset.RelPathWithExt, out T cachedFx))
                    return cachedFx;

                string mgfxoRel = asset.RelPathWithExt.Substring(0, asset.RelPathWithExt.Length - 4) + ".mgfxo";
                string mgfxoPath = RawContentLoader.GetContentPath(mgfxoRel);
                if (File.Exists(mgfxoPath))
                {
                    byte[] mgfxBytes = File.ReadAllBytes(mgfxoPath);
                    var fx = (T)(object)new Effect(Device, mgfxBytes) { Name = asset.RelPathWithExt };
                    if (useCache)
                        lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref fx);
                    return fx;
                }

                if (Phase2BrokenEffectXnbs.Contains(asset.RelPathWithExt))
                {
                    WarnPhase2BrokenEffectOnce(asset.RelPathWithExt);
                    return default;
                }
            }

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

        // Load an Effect from either a precompiled .mgfx or a sibling .fx (lookup
        // resolves to .mgfx if the .fx-named asset isn't present, then falls through
        // to a .mgfx of the same base name).
        // TODO: replace LoadEffect calls with LoadShader
        public Effect LoadEffect(string effectFile)
        {
            AssetName asset = new(effectFile);
            if (TryGetAsset(asset.RelPathWithExt, out Effect existing))
                return existing;

            // Phase 2.2: XNA 3.1 runtime HLSL compilation API was removed in MonoGame.
            // Effects must now be precompiled to MGFX via the MonoGame Effect Compiler
            // (mgfxc) at build time, then loaded as raw bytes through the Effect ctor.
            // For .fx requests, look for a sibling .mgfx with the same base name.
            string mgfxPath = asset.RelPathWithExt.EndsWith(".fx", StringComparison.OrdinalIgnoreCase)
                ? asset.RelPathWithExt.Substring(0, asset.RelPathWithExt.Length - 3) + ".mgfx"
                : asset.RelPathWithExt;

            FileInfo file = ResourceManager.GetModOrVanillaFile(mgfxPath);
            if (file == null)
                throw new FileNotFoundException($"LoadEffect {asset.RelPathWithExt}: no precompiled MGFX at '{mgfxPath}'");

            if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadEffect {file.RelPath()}");

            byte[] bytes = File.ReadAllBytes(file.FullName);
            var effect = new Effect(Device, bytes);
            lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref effect);
            return effect;
        }

        // Load and compile an .fx file as an SDGraphics Shader
        public Shader LoadShader(string effectFile)
        {
            AssetName asset = new(effectFile);
            return LoadShader(in asset, null);
        }
        
        // Load and compile an .fx file as an SDGraphics Shader via an existing FileInfo
        public Shader LoadShader(FileInfo file)
        {
            AssetName asset = new(file ?? throw new NullReferenceException(nameof(file)));
            return LoadShader(in asset, file);
        }

        Shader LoadShader(in AssetName asset, FileInfo file)
        {
            if (TryGetAsset(asset.RelPathWithExt, out Shader existing))
                return existing;
                        
            file ??= ResourceManager.GetModOrVanillaFile(asset.RelPathWithExt);
            if (file == null)
                throw new FileNotFoundException($"LoadShader {asset.RelPathWithExt} does not exist!");

            Shader shader = Shader.FromFile(Device, file.FullName);
            lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref shader);
            return shader;
        }

        public StaticMesh LoadStaticMesh(string meshName, bool animated = false)
        {
            AssetName asset = new(meshName);
            if (TryGetAsset(asset.RelPathWithExt, out StaticMesh mesh))
                return mesh;

            // Phase 3.2: prefer .fbx/.obj sibling over stubbed .xnb. The XNB Model
            // ContentTypeReader chain is still stubbed (§3.4 work); routing to the
            // raw asset unblocks the visual restore for content shipping both
            // source (.fbx/.obj) and baked (.xnb) forms — currently the 9 asteroids.
            string loadPath = asset.RelPathWithExt;
            if (loadPath.EndsWith(".xnb", StringComparison.OrdinalIgnoreCase))
            {
                string baseName = loadPath.Substring(0, loadPath.Length - 4);
                // Prefer .fbx over .obj — FBX preserves per-group transforms and
                // material parameters (alpha, specular, normal/specular paths) that
                // the OBJ MTL format can't carry. OBJ stays as a fallback for content
                // that ships without an FBX sibling.
                foreach (string ext in new[] { ".fbx", ".obj" })
                {
                    string candidate = baseName + ext;
                    if (File.Exists(RawContentLoader.GetContentPath(candidate)))
                    {
                        loadPath = candidate;
                        break;
                    }
                }
            }

            if (DebugAssetLoading) Log.Write(ConsoleColor.Cyan, $"LoadStaticMesh {loadPath}");

            if (RawContentLoader.IsSupportedMesh(loadPath))
            {
                mesh = RawContent.LoadStaticMesh(loadPath);
            }
            else
            {
                // No .fbx/.obj sidecar found. Skinned content normally arrives via the
                // §3.10 FBX pipeline (offline export on legacy/mesh_exporter_xna31 →
                // SkinnedMesh + BoneAnimationPlayer at load), so reaching this branch
                // with `animated=true` means a mod shipped an XNB without a sibling
                // .fbx/.obj — the static-Model fallback below loses the skin data.
                if (animated)
                    Log.Warning($"Skinned model '{asset.RelPathWithExt}' has no .fbx sidecar; loading as static (skin data lost)");

                // Defensive XNB Model fallback. Phase 3.4 pivoted from "decode 3.1 XNB
                // Models at runtime" to an offline FBX/OBJ export pipeline (see
                // legacy/mesh_exporter_xna31 branch + commit 9bd3b7128); Phase B then
                // archived every Model XNB out of game/Content/Model/ (commits
                // 6f68b9396 + a5da742b4). The .fbx-first preference above means this
                // branch only runs if a mod ships an XNB Model that has neither an
                // .fbx nor .obj sibling. The Phase 1 Xna31VertexDeclarationReader
                // decodes part of the XNA-3.1 wire format but not enough on its own —
                // the Model XNB itself has structural drift (TODO Phase 4: write
                // Xna31ModelReader). Stub-StaticMesh fallback keeps the runtime alive.
                // See memory: project_phase2_xnb_model_drift.md for the original hex.
                try
                {
                    Model model = LoadAsset<Model>(asset.RelPathWithExt, useCache:false);
                    mesh = StaticMesh.FromStaticModel(asset.RelPathWithExt, model);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Phase 2.2 stub: XNB Model '{asset.RelPathWithExt}' load failed ({ex.GetType().Name}: {ex.Message}); returning empty StaticMesh");
                    var stubBounds = new BoundingBox(-Microsoft.Xna.Framework.Vector3.One, Microsoft.Xna.Framework.Vector3.One);
                    mesh = new StaticMesh(asset.RelPathWithExt, stubBounds);
                }
            }

            lock (LoadSync) RecordCacheObject(asset.RelPathWithExt, ref mesh);
            return mesh;
        }

        public Model LoadModel(string modelName)
        {
            return Load<Model>(modelName);
        }

        // Skinned-mesh playback now goes through StaticMesh + BoneAnimationPlayer
        // (§3.10), so the XNA SkinnedModel surface stays retired.

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

        // TODO Phase 4: restore SunBurn ContentTypeReader routing if SunBurn-derived
        // types ever need to load via XNB. Phase 1.8.12 stubbed this because:
        //   1) SDSunBurn is excluded from the solution in Phase 1.9, so
        //      `typeof(SceneInterface)` is no longer resolvable.
        //   2) MonoGame's ContentTypeReaderManager has different private fields
        //      (`readerTypeToReader`/`nameToReader`) than XNA 3.1, so the
        //      MethodUtil.ReplaceMethod monkey-patch would silently no-op.
        // No live callers depend on this rerouting today; the stubbed type readers
        // (SunBurnReaderStubs.cs, Xna31* readers) cover the actual XNB load paths.
        static void FixSunBurnTypeLoader()
        {
        }

        // TODO Phase 4: restore SunBurn type rerouting (see FixSunBurnTypeLoader)
        static bool InstantiateTypeReader(string readerTypeName, ContentReader contentReader, out ContentTypeReader reader)
        {
            reader = null;
            return false;
        }

    }
}
