// Phase 1 migration stubs for SynapseGaming SunBurn types.
// SDSunBurn project was excluded from the solution in Phase 1.9.
// All members are no-ops; existing call sites compile but render nothing.
// TODO Phase 2: replace with MonoGame-native lighting/rendering implementation.

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SynapseGaming.LightingSystem.Core
{
    public enum DetailPreference { Low, Medium, High }
    public enum SamplingPreference { Bilinear, Trilinear, Anisotropic }

    [Flags]
    public enum ObjectVisibility
    {
        None = 0,
        Rendered = 1,
        CastShadows = 2,
        ReceiveShadows = 4,
        RenderedAndCastShadows = Rendered | CastShadows,
        RenderedAndReceiveShadows = Rendered | ReceiveShadows,
        All = Rendered | CastShadows | ReceiveShadows,
    }

    public struct VertexPositionNormalTextureBump
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;
        public Vector3 Tangent;
        public Vector3 Binormal;
        public static int SizeInBytes => 56;
    }

    public interface IManagerService { }

    public class LightingSystemManager : IDisposable
    {
        public LightingSystemManager(GameServiceContainer _) { }
        public void Dispose() { }
    }

    public class LightingSystemPreferences
    {
        public int MaxAnisotropy { get; set; }
        public float ShadowQuality { get; set; }
        public DetailPreference ShadowDetail { get; set; }
        public DetailPreference EffectDetail { get; set; }
        public DetailPreference TextureQuality { get; set; }
        public SamplingPreference TextureSampling { get; set; }
        public DetailPreference PostProcessingDetail { get; set; }
    }

    public class SceneEnvironment
    {
        public Vector3 AmbientLightColor = new(0.2f, 0.2f, 0.2f);
        public bool FogEnabled;
        public Vector3 FogColor = Vector3.Zero;
        public float FogStart = 1000f;
        public float FogEnd = 10000f;
    }

    // Phase 2.8 sub-phase B1: SceneState stores the per-frame view/proj/env/elapsed
    // values that the renderer needs. ScreenManager.BeginFrameRendering populates
    // it; SceneInterface.RenderScene (B3) reads from it.
    public class SceneState
    {
        public Matrix View { get; private set; } = Matrix.Identity;
        public Matrix Projection { get; private set; } = Matrix.Identity;
        public float ElapsedSeconds { get; private set; }
        public SceneEnvironment Environment { get; private set; }

        public void BeginFrameRendering(ref Matrix view, ref Matrix proj, float elapsed,
                                        SceneEnvironment env, bool b)
        {
            View = view;
            Projection = proj;
            ElapsedSeconds = elapsed;
            Environment = env;
        }

        public void EndFrameRendering() { }
    }

    public class SceneInterface : IDisposable
    {
        public Lights.ILightManager LightManager { get; }
        public Rendering.IObjectManager ObjectManager { get; }
        public Rendering.IRenderManager RenderManager { get; }

        // Phase 2.8 sub-phase B2: SceneInterface owns the GraphicsDevice + a
        // lazy SharedLightingEffect that's re-bound from submitted lights every
        // frame. RenderScene (B3) is the single forward pass.
        public GraphicsDevice GraphicsDevice { get; }
        SceneState LastFrameState;
        Effects.Forward.LightingEffect SharedFx;

        public SceneInterface(IGraphicsDeviceService gfx)
        {
            GraphicsDevice = gfx?.GraphicsDevice;
            LightManager = new Lights.LightManager();
            ObjectManager = new Rendering.ObjectManager();
            RenderManager = new Rendering.RenderManager(this);
        }

        public SceneInterface(GraphicsDeviceManager gfx)
        {
            GraphicsDevice = gfx?.GraphicsDevice;
            LightManager = new Lights.LightManager();
            ObjectManager = new Rendering.ObjectManager();
            RenderManager = new Rendering.RenderManager(this);
        }

        public void CreateDefaultManagers(bool useDeferredRendering, bool usePostProcessing) { }
        public void AddManager(IManagerService m) { }
        public void Update(float deltaTime) { }
        public void BeginFrameRendering(SceneState s) { LastFrameState = s; }
        public void EndFrameRendering() { }
        public void ApplyPreferences(LightingSystemPreferences p) { }

        public void Unload()
        {
            SharedFx?.Dispose();
            SharedFx = null;
        }

        public void Dispose()
        {
            Unload();
            (LightManager as IDisposable)?.Dispose();
            (ObjectManager as IDisposable)?.Dispose();
        }

        // Phase 2.8 sub-phase B3: actual forward-render pass.
        // Iterates ObjectManager.ActiveObjects, applies submitted lights to the
        // shared LightingEffect, and draws each SceneObject's RenderableMeshes
        // and AddedModelMeshes. Per-mesh `Effect` overrides the shared effect
        // (e.g., planet-halo custom shader paths). No-op when device or state
        // unavailable, or no objects submitted.
        public void RenderScene()
        {
            if (GraphicsDevice == null || LastFrameState == null) return;
            if (ObjectManager is not Rendering.ObjectManager om) return;
            if (om.ActiveObjects.Count == 0) return;

            SharedFx ??= new Effects.Forward.LightingEffect(GraphicsDevice);

            // Apply lights + ambient + fog from submitted state ONCE per frame.
            Ship_Game.Data.Mesh.LightingEffectBinder.Apply(
                SharedFx, LightManager.ActiveLights, LastFrameState.Environment);
            SharedFx.View = LastFrameState.View;
            SharedFx.Projection = LastFrameState.Projection;

            foreach (Rendering.ISceneObject iso in om.ActiveObjects)
            {
                if (iso is not Rendering.SceneObject so || !so.HasMeshes) continue;
                if (so.Visibility == ObjectVisibility.None) continue;

                DrawRenderables(so);
                DrawModelMeshes(so);
            }
        }

        void DrawRenderables(Rendering.SceneObject so)
        {
            foreach (Rendering.RenderableMesh rm in so.RenderableMeshes)
            {
                if (rm.PrimitiveCount == 0 || rm.VertexBuffer == null || rm.IndexBuffer == null)
                    continue;

                // Per-mesh effect override; LightingEffect-typed effects are
                // bound to the same shared light set; non-LightingEffect (e.g.,
                // planet-halo custom shader) are passed through with caller-set
                // matrices (stays the legacy contract).
                var fx = (rm.Effect as Effects.Forward.LightingEffect) ?? SharedFx;
                fx.World = so.World * rm.World;

                // Phase 2.8.C hotfix #5: per-mesh effects (PlanetType.Material,
                // ship Materials, etc.) need View/Projection from the current
                // frame too — only SharedFx gets them in RenderScene at lines
                // 154-155. Without these, the per-mesh effect renders with
                // stale/default Identity matrices, putting the geometry far
                // outside the camera frustum (planets disappeared this way
                // even with textures bound and lighting enabled).
                //
                // Phase 2.8.C hotfix #6: also push the per-SO primary light
                // (sun direction). Without this, per-mesh effects rely on
                // BasicEffect.EnableDefaultLighting's hardcoded angles and
                // light from a fixed wrong direction regardless of where the
                // system's sun actually is.
                if (fx != SharedFx)
                {
                    fx.View = SharedFx.View;
                    fx.Projection = SharedFx.Projection;
                    if (so.PrimaryLightEnabled)
                    {
                        fx.LightingEnabled = true;
                        fx.DirectionalLight0.Enabled = true;
                        fx.DirectionalLight0.Direction = so.PrimaryLightDirection;
                        fx.DirectionalLight0.DiffuseColor = so.PrimaryLightColor;
                        fx.DirectionalLight0.SpecularColor = so.PrimaryLightColor * 0.5f;
                        fx.DirectionalLight1.Enabled = false;
                        fx.DirectionalLight2.Enabled = false;
                        fx.AmbientLightColor = so.PrimaryLightColor * 0.15f; // soft fill
                    }
                }

                // Phase 2.8.C hotfix #3: planet bodies (and other SO consumers
                // that wire textures via the LightingEffect's own DiffuseMapTexture
                // — see PlanetType.CreateMaterial) had their textures stranded on
                // the LightingEffect's `new`-shadowed slot because ApplyToBasicEffect
                // was only called when the renderable carried its own
                // rm.DiffuseTexture override. Result: BasicEffect.Texture stayed
                // null → planet rendered as an unlit dark sphere against the
                // dark space background → invisible. Always pull from the shadow
                // slots; rm.DiffuseTexture (when set) overrides only the texture.
                if (rm.DiffuseTexture != null)
                    fx.DiffuseMapTexture = rm.DiffuseTexture;
                fx.ApplyToBasicEffect();

                GraphicsDevice.SetVertexBuffer(rm.VertexBuffer);
                GraphicsDevice.Indices = rm.IndexBuffer;
                foreach (EffectPass pass in fx.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    GraphicsDevice.DrawIndexedPrimitives(rm.PrimitiveType,
                        rm.BaseVertex, rm.StartIndex, rm.PrimitiveCount);
                }
            }
        }

        void DrawModelMeshes(Rendering.SceneObject so)
        {
            foreach ((ModelMesh mesh, Effect effect) in so.AddedModelMeshes)
            {
                var fx = (effect as Effects.Forward.LightingEffect) ?? SharedFx;
                // Note: parent-bone hierarchy walk omitted for Phase 2 simplicity.
                // Submeshes inherit SceneObject.World only; bone-aware composite
                // is a Phase 3 cleanup if/when skeletal hierarchies return.
                fx.World = so.World;
                if (fx != SharedFx) // see DrawRenderables for hotfixes #5 and #6
                {
                    fx.View = SharedFx.View;
                    fx.Projection = SharedFx.Projection;
                    if (so.PrimaryLightEnabled)
                    {
                        fx.LightingEnabled = true;
                        fx.DirectionalLight0.Enabled = true;
                        fx.DirectionalLight0.Direction = so.PrimaryLightDirection;
                        fx.DirectionalLight0.DiffuseColor = so.PrimaryLightColor;
                        fx.DirectionalLight0.SpecularColor = so.PrimaryLightColor * 0.5f;
                        fx.DirectionalLight1.Enabled = false;
                        fx.DirectionalLight2.Enabled = false;
                        fx.AmbientLightColor = so.PrimaryLightColor * 0.15f;
                    }
                }
                fx.ApplyToBasicEffect(); // see DrawRenderables for the why

                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    if (part.PrimitiveCount == 0) continue;
                    GraphicsDevice.SetVertexBuffer(part.VertexBuffer);
                    GraphicsDevice.Indices = part.IndexBuffer;
                    foreach (EffectPass pass in fx.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                            part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                    }
                }
            }
        }
    }
}

namespace SynapseGaming.LightingSystem.Shadows
{
    public enum ShadowType { None, AllObjects, DynamicOnly, StaticOnly }
}

namespace SynapseGaming.LightingSystem.Rendering
{
    public enum ObjectType { Static, Dynamic }

    public interface ISceneObject { }

    public interface ISubmit<T>
    {
        void Submit(T item);
        bool Remove(T item);
    }

    public interface IObjectManager : ISubmit<ISceneObject>
    {
        void Clear();
    }

    public interface IRenderManager
    {
        void Render();
    }

    public class ObjectManager : IObjectManager
    {
        readonly List<ISceneObject> Objects = new();
        public IReadOnlyList<ISceneObject> ActiveObjects => Objects;
        public void Submit(ISceneObject obj) { if (obj != null) Objects.Add(obj); }
        public bool Remove(ISceneObject obj) => obj != null && Objects.Remove(obj);
        public void Clear() => Objects.Clear();
    }

    public class RenderManager : IRenderManager
    {
        // Phase 2.8 sub-phase B3: back-ref to SceneInterface so the parameterless
        // Render() method can pull GraphicsDevice / lights / objects / state from
        // a single owner. The legacy parameterless ctor stays for any tooling that
        // constructs the manager standalone (defensive — there's no live caller
        // doing this today).
        Core.SceneInterface Owner;
        public RenderManager() { }
        public RenderManager(Core.SceneInterface owner) { Owner = owner; }
        public void Render() => Owner?.RenderScene();
    }

    public class AnimationStub
    {
        public AnimationStub() { }
        public AnimationStub(object skeleton) { }
        public float Speed { get; set; }
        public object TranslationInterpolation { get; set; }
        public object OrientationInterpolation { get; set; }
        public object ScaleInterpolation { get; set; }
        public void StartClip(object clip) { }
    }

    public class SceneObject : ISceneObject
    {
        public SceneObject() { }
        public SceneObject(string name) { Name = name; }
        public string Name { get; set; }
        public ObjectType ObjectType { get; set; }
        public BoundingBox WorldBoundingBox { get; set; }
        public BoundingSphere ObjectBoundingSphere { get; set; }
        public Matrix World { get; set; } = Matrix.Identity;
        public SynapseGaming.LightingSystem.Core.ObjectVisibility Visibility { get; set; }
        public AnimationStub Animation { get; set; }

        // Phase 2.8.C hotfix #6: per-object primary light (the system's sun for
        // planets/ships/asteroids). DrawRenderables binds this onto the per-mesh
        // LightingEffect's DirectionalLight0 — without it, per-mesh effects fall
        // back to BasicEffect.EnableDefaultLighting's hardcoded directions and
        // light from the wrong angle (visible regression: Jupiter lit from below
        // when the sun was upper-right). LightingEffectBinder.Apply only hits
        // SharedFx in RenderScene, not per-mesh material effects, so this is
        // the per-object hook for those.
        public Vector3 PrimaryLightDirection { get; set; }
        public Vector3 PrimaryLightColor { get; set; }
        public bool PrimaryLightEnabled { get; set; }

        readonly List<RenderableMesh> Renderables = new();
        readonly List<(ModelMesh Mesh, Effect Effect)> ModelMeshes = new();

        public IReadOnlyList<RenderableMesh> RenderableMeshes => Renderables;
        public IReadOnlyList<(ModelMesh Mesh, Effect Effect)> AddedModelMeshes => ModelMeshes;
        public bool HasMeshes => Renderables.Count > 0 || ModelMeshes.Count > 0;

        public void Add(ModelMesh m, Effect e) { if (m != null) ModelMeshes.Add((m, e)); }
        public void Add(RenderableMesh m) { if (m != null) Renderables.Add(m); }
        public void AffineTransform(Vector3 pos, Vector3 rotRads, Vector3 scale) { }
        public void UpdateAnimation(float deltaTime) { }
    }

    public class RenderableMesh
    {
        // Phase 2.8 sub-phase A1: ctor args promoted from /dev/null to real storage
        // so the forward renderer can iterate SceneObject.RenderableMeshes and feed
        // the bound VB/IB into device.DrawIndexedPrimitives.
        public SceneObject Owner { get; }
        public Effect Effect { get; set; }   // setter so renderer can swap to a shared LightingEffect
        public Matrix World { get; set; }
        public BoundingSphere BoundingSphere { get; }
        public IndexBuffer IndexBuffer { get; }
        public VertexBuffer VertexBuffer { get; }
        public VertexDeclaration VertexDeclaration { get; }
        public int StreamOffset { get; }
        public PrimitiveType PrimitiveType { get; }
        public int PrimitiveCount { get; }
        public int BaseVertex { get; }
        public int NumVertices { get; }
        public int StartIndex { get; }
        public int VertexStride { get; }

        // Material data — Phase 2 only uses DiffuseTexture; reserve MaterialOverrides
        // dict for Phase 3 normal-map / specular / emissive additions.
        public Texture2D DiffuseTexture { get; set; }
        public Dictionary<string, object> MaterialOverrides { get; } = new();

        public RenderableMesh(SceneObject o, Effect e, Matrix world, BoundingSphere s,
                              IndexBuffer ib, VertexBuffer vb, VertexDeclaration vd,
                              int streamOffset, PrimitiveType pt,
                              int primitiveCount, int baseVertex, int numVertices,
                              int startIndex, int vertexStride)
        {
            Owner = o;
            Effect = e;
            World = world;
            BoundingSphere = s;
            IndexBuffer = ib;
            VertexBuffer = vb;
            VertexDeclaration = vd;
            StreamOffset = streamOffset;
            PrimitiveType = pt;
            PrimitiveCount = primitiveCount;
            BaseVertex = baseVertex;
            NumVertices = numVertices;
            StartIndex = startIndex;
            VertexStride = vertexStride;
        }
    }

    public sealed class MeshData : IDisposable
    {
        public string Name { get; set; }
        public Matrix MeshToObject { get; set; } = Matrix.Identity;
        public bool InfiniteBounds { get; set; }
        public int PrimitiveCount { get; set; }
        public int VertexCount { get; set; }
        public int VertexStride { get; set; }
        public BoundingSphere ObjectSpaceBoundingSphere { get; set; }
        public VertexDeclaration VertexDeclaration { get; set; }
        public VertexBuffer VertexBuffer { get; set; }
        public IndexBuffer IndexBuffer { get; set; }
        public Effect Effect { get; set; }
        public void Dispose()
        {
            VertexDeclaration?.Dispose(); VertexDeclaration = null;
            VertexBuffer?.Dispose(); VertexBuffer = null;
            IndexBuffer?.Dispose(); IndexBuffer = null;
            Effect?.Dispose(); Effect = null;
        }
    }
}

namespace SynapseGaming.LightingSystem.Lights
{
    using Rendering;
    using Shadows;

    // LightRigIdentity is defined in Ship_Game.GameScreens (predates this stub file)
    // and is unrelated to SunBurn — kept there.

    public interface ILight
    {
        Vector3 CompositeColorAndIntensity { get; }
    }

    public interface ILightManager : ISubmit<ILight>
    {
        IReadOnlyList<ILight> ActiveLights { get; }
        void Submit(LightRig rig);
        void Clear();
    }

    public class LightManager : ILightManager, SynapseGaming.LightingSystem.Core.IManagerService
    {
        readonly List<ILight> Lights = new();
        public IReadOnlyList<ILight> ActiveLights => Lights;
        public LightManager() { }
        public LightManager(IGraphicsDeviceService _) { }
        public void Submit(LightRig rig) { } // LightRig is a data-less stub; nothing to extract
        public void Submit(ILight light) { if (light != null) Lights.Add(light); }
        public bool Remove(ILight light) => light != null && Lights.Remove(light);
        public void Clear() => Lights.Clear();
    }

    public class LightRig { }

    public class DirectionalLight : ILight
    {
        public string Name { get; set; }
        public Vector3 DiffuseColor { get; set; }
        public float Intensity { get; set; }
        public ObjectType ObjectType { get; set; }
        public Vector3 Direction { get; set; }
        public bool Enabled { get; set; }
        public bool ShadowPerSurfaceLOD { get; set; }
        public float ShadowQuality { get; set; }
        public ShadowType ShadowType { get; set; }
        public Matrix World { get; set; }
        public Vector3 CompositeColorAndIntensity => DiffuseColor * Intensity;
    }

    public class PointLight : ILight
    {
        public string Name { get; set; }
        public Vector3 DiffuseColor { get; set; }
        public float Intensity { get; set; }
        public ObjectType ObjectType { get; set; }
        public bool FillLight { get; set; }
        public float Radius { get; set; }
        public Vector3 Position { get; set; }
        public bool Enabled { get; set; }
        public float FalloffStrength { get; set; }
        public bool ShadowPerSurfaceLOD { get; set; }
        public float ShadowQuality { get; set; }
        public ShadowType ShadowType { get; set; }
        public Matrix World { get; set; }
        public Vector3 CompositeColorAndIntensity => DiffuseColor * Intensity;
    }

    public class AmbientLight : ILight
    {
        public string Name { get; set; }
        public Vector3 DiffuseColor { get; set; }
        public float Intensity { get; set; }
        public Vector3 CompositeColorAndIntensity => DiffuseColor * Intensity;
    }
}

namespace SynapseGaming.LightingSystem.Processors
{
    public interface IEffectCache
    {
        bool TryGetEffect<T>(string assetName, out T asset) where T : Effect;
        void AddEffect(string assetName, Effect effect);
    }
}

namespace SynapseGaming.LightingSystem.Effects
{
    // Subclassing BasicEffect avoids the Effect-bytecode-required problem.
    // It pulls in BasicEffect's own DiffuseColor/SpecularPower; we shadow with `new`.
    public class BaseMaterialEffect : BasicEffect
    {
        // Phase 2.8.C hotfix #4: the shadowed DiffuseColor / SpecularPower auto-
        // properties default to Vector3.Zero / 0f. When ApplyToBasicEffect runs,
        // it copies those defaults down to BasicEffect's slots, killing the
        // material (texture × 0 = black; SpecularPower=0 NaN-pollutes lighting).
        // Initialize to BasicEffect's documented defaults so callers that don't
        // set these explicitly (PlanetType.CreateMaterial, etc.) get a working
        // material out of the box. Callers that DO set them keep their values.
        public BaseMaterialEffect(GraphicsDevice device) : base(device)
        {
            DiffuseColor = Vector3.One; // BasicEffect default = (1,1,1)
            SpecularPower = 16f;        // BasicEffect default = 16
        }
        public string MaterialName { get; set; }
        public string MaterialFile { get; set; }
        public string ProjectFile { get; set; }
        public string DiffuseMapFile { get; set; }
        public string EmissiveMapFile { get; set; }
        public string NormalMapFile { get; set; }
        public string SpecularColorMapFile { get; set; }
        public Texture2D DiffuseMapTexture { get; set; }
        public Texture2D EmissiveMapTexture { get; set; }
        public Texture2D NormalMapTexture { get; set; }
        public Texture2D SpecularColorMapTexture { get; set; }
        public bool Skinned { get; set; }
        public bool DoubleSided { get; set; }
        public new float SpecularPower { get; set; }
        public new Vector3 DiffuseColor { get; set; }
        public float SpecularAmount { get; set; }
        public float FresnelReflectBias { get; set; }
        public float FresnelReflectOffset { get; set; }
        public float FresnelMicrofacetDistribution { get; set; }
        public float ParallaxScale { get; set; }
        public float ParallaxOffset { get; set; }
        public TextureAddressMode AddressModeU { get; set; }
        public TextureAddressMode AddressModeV { get; set; }
        public TextureAddressMode AddressModeW { get; set; }
        public float Transparency { get; set; }
    }
}

namespace SynapseGaming.LightingSystem.Effects.Forward
{
    public enum TransparencyMode { None, Solid, Standard, Refractive }

    public class LightingEffect : BaseMaterialEffect
    {
        public LightingEffect(GraphicsDevice device) : base(device)
        {
            // BasicEffect default: no lights enabled, no texture. Renderer flips
            // these via ApplyMaterial / ApplyLights / ApplyToBasicEffect.
            EnableDefaultLighting();
        }

        // Phase 2.8 sub-phase A3: bridge `new`-shadowed material properties on
        // BaseMaterialEffect down to the underlying BasicEffect. Without this,
        // `effect.DiffuseColor = X` writes to BaseMaterialEffect.DiffuseColor
        // (the `new` slot) and BasicEffect.DiffuseColor stays at default — the
        // GPU never sees the value. Probe 2 confirmed this is real C# `new`
        // shadowing semantics.
        //
        // Renderer flow: caller sets material/transparency/texture properties
        // on the LightingEffect (or BaseMaterialEffect) view, then calls
        // ApplyToBasicEffect() once before pass.Apply() to push values down.
        public void ApplyToBasicEffect()
        {
            // Material — read the shadowed `new` slots, write to BasicEffect base.
            base.DiffuseColor = DiffuseColor;
            base.SpecularPower = SpecularPower;

            // Texture: prefer DiffuseMapTexture if assigned; else BasicEffect's own.
            if (DiffuseMapTexture != null)
            {
                Texture = DiffuseMapTexture;
                TextureEnabled = true;
            }
        }

        // Phase 2.8 sub-phase A3: was a stub. Now implements the SunBurn
        // call-site contract by mapping mode/alpha/map onto BasicEffect's
        // Alpha + TextureEnabled. Refractive/Standard are mapped to Standard
        // (no refraction in Phase 2's BasicEffect-backed renderer).
        public void SetTransparencyModeAndMap(TransparencyMode mode, float alpha, Texture2D map)
        {
            Alpha = alpha;
            if (map != null)
            {
                Texture = map;
                TextureEnabled = true;
            }
            else if (mode == TransparencyMode.None)
            {
                // No transparency — restore opaque alpha
                Alpha = 1f;
            }
        }
    }
}

// MonoGame ships its own Microsoft.Xna.Framework.Graphics.DirectionalLight (used by
// BasicEffect.DirectionalLight0/1/2). The XNA 3.1 name was BasicDirectionalLight; we
// updated call sites to use DirectionalLight directly rather than carrying a stub.
