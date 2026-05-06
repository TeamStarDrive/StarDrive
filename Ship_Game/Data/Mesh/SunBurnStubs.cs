// Phase 1 migration stubs for SynapseGaming SunBurn types.
// SDSunBurn project was excluded from the solution in Phase 1.9.
//
// Phase 2.8 sub-phases A1–C built a MonoGame-native forward renderer on top of
// these stubs (LightingEffect over BasicEffect; LightingEffectBinder pushes
// submitted lights onto BasicEffect's slots; SceneInterface.RenderScene is a
// real per-frame pass), so most of this file is now functional rather than no-op.
// What remains stub-shaped is the SunBurn type *surface* (PointLight vs DirectionalLight,
// ShadowType, etc.) — these compile-time types are kept so existing call sites build,
// while the actual rendering decisions live in SunBurnStubs.RenderScene + LightingEffectBinder.
// Full SunBurn behavior parity (PointLight falloff, deferred path, real shadow maps)
// is Phase 3.9 (shadow maps) + Phase 4 (deferred renderer if ever needed).

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

        // Phase 3.7 step 4 (Phase C): VertexDeclaration so this struct can
        // back tangent-bearing VertexBuffers used by code that doesn't go
        // through SDNative's SdMeshGroup layout builder (test scaffolding,
        // procedurally-generated geometry, etc.). Layout matches the
        // SD-emitted decoration in MeshInterface.TranslateNativeUsage:
        // Position(0) → Normal(12) → TexCoord(24) → Tangent(32) → Binormal(44).
        public static readonly VertexElement[] VertexElements =
        {
            new VertexElement(0,  VertexElementFormat.Vector3, VertexElementUsage.Position,          0),
            new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal,            0),
            new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Tangent,           0),
            new VertexElement(44, VertexElementFormat.Vector3, VertexElementUsage.Binormal,          0),
        };
        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(56, VertexElements);
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
        // Phase 3.7: was (0.2, 0.2, 0.2) which leaks neutral-gray ambient onto
        // every scene that doesn't explicitly override it — including MainMenu's
        // submitted violet AmbientLight, which turned the carefully-authored cool
        // ambient into a washed-out blue-gray. Default to zero; rely on
        // scene-submitted AmbientLights for the actual ambient term.
        public Vector3 AmbientLightColor = Vector3.Zero;
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

        // Phase 3.8.B: optional per-frame shadow map. Owner (UniverseScreen
        // / future ShipDesigner) constructs the component, calls
        // LoadContent, and assigns here. RenderScene runs the depth pre-
        // pass against this component before binder.Apply, then leaves the
        // shadow texture + light-clip matrix on SharedFx so per-mesh
        // effects pick them up via CopySharedLighting. Null = unshadowed
        // forward path (the existing Phase 2.8/3.7 behavior).
        public Ship_Game.Graphics.ShadowMapComponent ShadowMap { get; set; }

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

            // Match the original SunBurn RenderManager default (Forward/RenderManager.cs:271).
            // Without this, ships and stations inherit whatever rasterizer state the previous
            // pass (background, sprite batch, BasicBlendMode-with-CullNone) left behind, and
            // back-facing polygons render visibly — making meshes look "inside-out".
            Ship_Game.Graphics.RenderStates.SetCullMode(
                GraphicsDevice, CullMode.CullCounterClockwiseFace);
            Ship_Game.Graphics.RenderStates.EnableDepthWrite(GraphicsDevice);
            // Phase 3.7: same hazard for BlendState — the MainMenu's BackAdditive
            // sprite pass leaves SoftAdditive (InverseDestColor + One) bound, which
            // would make ships/stations/asteroids render with a screen blend and
            // appear translucent over the planet. Force opaque before the 3D pass.
            GraphicsDevice.BlendState = BlendState.Opaque;

            // Apply lights + ambient + fog from submitted state ONCE per frame.
            // Camera position is needed so the binder can pick the PointLight
            // closest to the active view as the scene "sun" (per-system Key
            // suns each submit their own PointLight; globally-brightest can
            // pick a sun from a different system across the universe and
            // light the ship from the wrong direction).
            Vector3 cameraPos = Matrix.Invert(LastFrameState.View).Translation;

            // Phase 3.8.B: shadow pre-pass. Runs BEFORE binder.Apply so the
            // resulting RT + light-clip matrix flow through the binder onto
            // SharedFx alongside the lights, and CopySharedLighting picks
            // them up for per-mesh effects. The pre-pass dirties the
            // device's render target + raster/blend/depth state, but the
            // lit pass below re-sets all three explicitly so we don't need
            // to restore here.
            Texture2D shadowTex = null;
            Matrix    shadowVP  = Matrix.Identity;
            RunShadowPrePass(om, cameraPos, ref shadowTex, ref shadowVP);

            Ship_Game.Data.Mesh.LightingEffectBinder.Apply(
                SharedFx, LightManager.ActiveLights, LastFrameState.Environment, cameraPos,
                shadowTex, shadowVP);
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

        // Phase 3.8.B: depth-only pass into ShadowMap.ShadowMap from the
        // dominant directional / sun-anchor light's POV. Iterates the
        // SAME caster set as the lit pass so shadows project from the
        // exact geometry the receiver sees lit. No-op when:
        //   - no ShadowMap component is wired up (most non-universe scenes),
        //   - the ObjectManager has no meshed casters with non-zero bounds,
        //   - no usable directional light could be derived from the
        //     submitted light list.
        void RunShadowPrePass(Rendering.ObjectManager om, Vector3 cameraPos,
                              ref Texture2D shadowTex, ref Matrix shadowVP)
        {
            if (ShadowMap == null || ShadowMap.ShadowMap == null) return;

            BoundingSphere bounds = ComputeCasterBounds(om);
            if (bounds.Radius <= 0f) return;

            if (!TryPickShadowDirection(LightManager.ActiveLights, cameraPos, bounds.Center, out Vector3 lightDir))
                return;

            ShadowMap.BeginShadowPass(lightDir, bounds);
            try
            {
                foreach (Rendering.ISceneObject iso in om.ActiveObjects)
                {
                    if (iso is not Rendering.SceneObject so || !so.HasMeshes) continue;
                    if (so.Visibility == ObjectVisibility.None) continue;

                    foreach (Rendering.RenderableMesh rm in so.RenderableMeshes)
                    {
                        if (rm.PrimitiveCount == 0 || rm.VertexBuffer == null || rm.IndexBuffer == null)
                            continue;
                        ShadowMap.DrawCaster(so.World * rm.World, rm.VertexBuffer, rm.IndexBuffer,
                            rm.PrimitiveType, rm.BaseVertex, rm.StartIndex, rm.PrimitiveCount);
                    }

                    foreach ((ModelMesh mesh, Effect _) in so.AddedModelMeshes)
                    {
                        foreach (ModelMeshPart part in mesh.MeshParts)
                        {
                            if (part.PrimitiveCount == 0 || part.VertexBuffer == null || part.IndexBuffer == null)
                                continue;
                            ShadowMap.DrawCaster(so.World, part.VertexBuffer, part.IndexBuffer,
                                PrimitiveType.TriangleList,
                                part.VertexOffset, part.StartIndex, part.PrimitiveCount);
                        }
                    }
                }
            }
            finally
            {
                ShadowMap.EndShadowPass();
            }

            shadowTex = ShadowMap.ShadowMap;
            shadowVP  = ShadowMap.LightView * ShadowMap.LightProjection;
        }

        // Merge the world-space bounding spheres of every meshed,
        // non-hidden caster. Falls back to BoundingSphere(zero, 0) when no
        // SO contributes — the caller's `bounds.Radius <= 0` early-out
        // handles that case. Uses ObjectBoundingSphere over WorldBoundingBox
        // because most StarDrive SOs (ships, planets, asteroids) populate
        // the sphere via SceneObject.UpdateAnimation but not the box.
        static BoundingSphere ComputeCasterBounds(Rendering.ObjectManager om)
        {
            BoundingSphere result = default;
            bool any = false;
            foreach (Rendering.ISceneObject iso in om.ActiveObjects)
            {
                if (iso is not Rendering.SceneObject so || !so.HasMeshes) continue;
                if (so.Visibility == ObjectVisibility.None) continue;

                BoundingSphere s = so.ObjectBoundingSphere;
                if (s.Radius <= 0f) continue;
                // Translate object-space sphere into world space. SceneObject
                // doesn't currently apply scale to ObjectBoundingSphere, so
                // we just translate by the world position.
                s.Center += so.World.Translation;

                if (!any) { result = s; any = true; }
                else      { result = BoundingSphere.CreateMerged(result, s); }
            }
            return any ? result : default;
        }

        // Mirror LightingEffectBinder.Apply's dominant-light selection
        // BEFORE the binder runs (the binder hasn't populated SharedFx yet
        // when we need the light direction for the depth pass). Priority:
        //   1. First enabled DirectionalLight.
        //   2. Closest sun-anchor PointLight (XY-distance to camera, with
        //      the same radius bounds the binder uses to filter scene
        //      lights from global ambient proxies).
        // Returns false when neither is available — caller skips the pass.
        static bool TryPickShadowDirection(IReadOnlyList<Lights.ILight> lights, Vector3 cameraPos,
                                           Vector3 sceneCenter, out Vector3 direction)
        {
            if (lights != null)
            {
                for (int i = 0; i < lights.Count; ++i)
                {
                    if (lights[i] is Lights.DirectionalLight d && d.Enabled
                        && d.Direction.LengthSquared() > 1e-6f)
                    {
                        direction = Vector3.Normalize(d.Direction);
                        return true;
                    }
                }

                Lights.PointLight bestSun = null;
                float bestSunDist2 = float.MaxValue;
                for (int i = 0; i < lights.Count; ++i)
                {
                    if (lights[i] is Lights.PointLight p && p.Enabled
                        && p.Radius >= 1000f && p.Radius < 1_000_000f)
                    {
                        float dx = p.Position.X - cameraPos.X;
                        float dy = p.Position.Y - cameraPos.Y;
                        float dist2 = dx * dx + dy * dy;
                        if (dist2 < bestSunDist2) { bestSun = p; bestSunDist2 = dist2; }
                    }
                }
                if (bestSun != null)
                {
                    Vector3 toCenter = sceneCenter - bestSun.Position;
                    if (toCenter.LengthSquared() > 1e-6f)
                    {
                        direction = Vector3.Normalize(toCenter);
                        return true;
                    }
                }
            }

            direction = -Vector3.UnitY;
            return false;
        }

        // Phase B refactor: per-mesh LightingEffects are constructed with
        // EnableDefaultLighting (BasicEffect's hardcoded 3-light setup), and
        // LightingEffectBinder.Apply only touches SharedFx. Copy SharedFx's
        // resolved lights onto the per-mesh effect so all SOs (ships, planets,
        // asteroids, launching ships, debris) get the same scene-wide lighting
        // — replaces the per-SO PrimaryLight* hotfix that only fixed visible
        // ships/planets and missed everything else.
        void CopySharedLighting(Effects.Forward.LightingEffect fx)
        {
            fx.LightingEnabled = SharedFx.LightingEnabled;
            fx.AmbientLightColor = SharedFx.AmbientLightColor;
            CopyDirectional(fx.DirectionalLight0, SharedFx.DirectionalLight0);
            CopyDirectional(fx.DirectionalLight1, SharedFx.DirectionalLight1);
            CopyDirectional(fx.DirectionalLight2, SharedFx.DirectionalLight2);
            fx.PointLight0 = SharedFx.PointLight0;
            fx.PointLight1 = SharedFx.PointLight1;
            fx.PointLight2 = SharedFx.PointLight2;
            // Phase 3.8.B: propagate shadow state alongside the lights so
            // per-mesh effects (planet halos, ship Materials, etc.) sample
            // the same shadow RT the SharedFx is bound to. ShadowBias is
            // also copied because §3.8.C may end up tuning it per-scene.
            fx.ShadowMapEnabled    = SharedFx.ShadowMapEnabled;
            fx.ShadowMap           = SharedFx.ShadowMap;
            fx.LightViewProjection = SharedFx.LightViewProjection;
            fx.ShadowBias          = SharedFx.ShadowBias;
        }

        static void CopyDirectional(Microsoft.Xna.Framework.Graphics.DirectionalLight dst,
                                    Microsoft.Xna.Framework.Graphics.DirectionalLight src)
        {
            dst.Enabled = src.Enabled;
            dst.Direction = src.Direction;
            dst.DiffuseColor = src.DiffuseColor;
            dst.SpecularColor = src.SpecularColor;
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
                if (fx != SharedFx)
                {
                    fx.View = SharedFx.View;
                    fx.Projection = SharedFx.Projection;
                    CopySharedLighting(fx);
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

                // Phase 3.10.B.6: matrix-palette upload for skinned hulls.
                // SkinnedLightingEffect : LightingEffect, so the per-mesh effect
                // assignment in MeshImporter (when mesh->NumSkinnedBones > 0)
                // already lands on the skin VS. The only remaining piece is
                // pushing the BoneAnimationPlayer's palette per draw.
                if (fx is Effects.Forward.SkinnedLightingEffect skinned
                    && so.AnimationPlayer != null && so.AnimationPlayer.HasBones)
                {
                    skinned.SetBoneTransforms(so.AnimationPlayer.SkinningPalette);
                }

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
                if (fx != SharedFx) // see DrawRenderables for hotfix #5
                {
                    fx.View = SharedFx.View;
                    fx.Projection = SharedFx.Projection;
                    CopySharedLighting(fx);
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

        // Phase 3.10.B.6/B.7: matrix-palette skinning state. Created by
        // StaticMesh.CreateSceneObject when the source mesh has skin data.
        // Static SOs leave this null and pay zero cost. UpdateAnimation
        // ticks the active clip; the renderer downcasts the per-mesh effect
        // to SkinnedLightingEffect and pushes the palette before each draw.
        public Ship_Game.Data.Mesh.BoneAnimationPlayer AnimationPlayer { get; set; }
        public bool IsSkinned => AnimationPlayer != null && AnimationPlayer.HasBones;

        readonly List<RenderableMesh> Renderables = new();
        readonly List<(ModelMesh Mesh, Effect Effect)> ModelMeshes = new();

        public IReadOnlyList<RenderableMesh> RenderableMeshes => Renderables;
        public IReadOnlyList<(ModelMesh Mesh, Effect Effect)> AddedModelMeshes => ModelMeshes;
        public bool HasMeshes => Renderables.Count > 0 || ModelMeshes.Count > 0;

        public void Add(ModelMesh m, Effect e) { if (m != null) ModelMeshes.Add((m, e)); }
        public void Add(RenderableMesh m) { if (m != null) Renderables.Add(m); }
        public void UpdateAnimation(float deltaTime)
        {
            AnimationPlayer?.Update(deltaTime);
        }
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
    // Phase 3.7 step 4 (Phase A): material data carrier. Pre-§3.7 this
    // extended BasicEffect to piggy-back its lighting; LightingEffect now
    // extends Effect directly via the MeshLighting.mgfxo shader so this
    // class is reduced to a pure data-carrier base. Properties are still
    // populated by SunBurnReaderStubs.MaterialReader at content-load time
    // and consumed by LightingEffect.ApplyToBasicEffect — Phase B will
    // wire NormalMapTexture / SpecularColorMapTexture / EmissiveMapTexture
    // through to dedicated MGFX samplers.
    public abstract class BaseMaterialEffect : Effect
    {
        protected BaseMaterialEffect(GraphicsDevice device, byte[] effectCode) : base(device, effectCode)
        {
        }

        // Material identity (read from XNB, mostly diagnostic).
        public string MaterialName { get; set; }
        public string MaterialFile { get; set; }
        public string ProjectFile { get; set; }
        public string DiffuseMapFile { get; set; }
        public string EmissiveMapFile { get; set; }
        public string NormalMapFile { get; set; }
        public string SpecularColorMapFile { get; set; }

        // Map-texture data carriers. Phase A samples DiffuseMapTexture only;
        // the others land in Phase B.
        public Texture2D DiffuseMapTexture { get; set; }
        public Texture2D EmissiveMapTexture { get; set; }
        public Texture2D NormalMapTexture { get; set; }
        public Texture2D SpecularColorMapTexture { get; set; }

        public bool Skinned { get; set; }
        public bool DoubleSided { get; set; }

        // SunBurn material constants — Phase A pushes DiffuseColor and SpecularPower
        // to MGFX uniforms; the rest are accepted from XNB / placeholders only.
        public Vector3 DiffuseColor   { get; set; } = Vector3.One;
        public Vector3 EmissiveColor  { get; set; } = Vector3.Zero;
        public Vector3 SpecularColor  { get; set; } = Vector3.One;
        public float   SpecularPower  { get; set; } = 16f;
        public float   Alpha          { get; set; } = 1f;

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

        // Diffuse texture — exposed BasicEffect-style so call sites that did
        // `effect.Texture = X; effect.TextureEnabled = true;` keep compiling
        // (Projectile.DrawMesh, etc.). Setters cache; OnApply pushes.
        public Texture2D Texture { get; set; }
        public bool TextureEnabled { get; set; }
    }
}

namespace SynapseGaming.LightingSystem.Effects.Forward
{
    public enum TransparencyMode { None, Solid, Standard, Refractive }

    // Phase 3.7 step 4 (Phase A): replaces the BasicEffect-backed stub with
    // a custom MGFX (game/Content/Effects/MeshLighting.fx → .mgfxo) that
    // mirrors BasicEffect's per-pixel-lighting model. Phase A is a wiring
    // change only — visually a no-op for ships/planets/stations. Phase B
    // adds normal/specular/emissive map sampling.
    //
    // The public surface preserves BasicEffect's shape (DirectionalLight0/1/2,
    // AmbientLightColor, FogEnabled, Texture, etc.) so LightingEffectBinder,
    // SunBurnStubs.RenderScene, and other consumers keep compiling unchanged.
    public class LightingEffect : SynapseGaming.LightingSystem.Effects.BaseMaterialEffect
    {
        // ── Shared shader bytes, loaded once at startup ─────────────────────
        // ResourceManager.CreateCoreGfxResources calls TryLoadShared to populate
        // this. If the .mgfxo is missing we fall back to BasicEffect-equivalent
        // bytes (set by TryLoadShared) so existing tests don't crash.
        static byte[] s_meshLightingBytes;
        static readonly object s_loadLock = new object();

        public static bool TryLoadShared(string contentPath)
        {
            lock (s_loadLock)
            {
                if (s_meshLightingBytes != null) return true;
                try
                {
                    if (System.IO.File.Exists(contentPath))
                    {
                        s_meshLightingBytes = System.IO.File.ReadAllBytes(contentPath);
                        return true;
                    }
                }
                catch { /* fall through; ctor will throw */ }
                return false;
            }
        }

        static byte[] GetEffectBytes()
        {
            if (s_meshLightingBytes == null)
                throw new InvalidOperationException(
                    "LightingEffect: MeshLighting.mgfxo bytes not loaded. Call " +
                    "LightingEffect.TryLoadShared() during startup (e.g. from " +
                    "ResourceManager.CreateCoreGfxResources).");
            return s_meshLightingBytes;
        }

        // ── EffectParameter handles (cached at ctor) ────────────────────────
        readonly EffectParameter pWorld, pView, pProjection;
        readonly EffectParameter pDiffuseColor, pEmissiveColor, pSpecularColor;
        readonly EffectParameter pSpecularPower, pAlpha, pEyePosition;
        readonly EffectParameter pLightingEnabled, pTextureEnabled, pFogEnabled;
        readonly EffectParameter pEmissiveMapEnabled, pSpecularMapEnabled, pNormalMapEnabled;
        readonly EffectParameter pAmbientLightColor;
        readonly EffectParameter pFogColor, pFogStart, pFogEnd;
        readonly EffectParameter pTexture, pEmissiveMap, pSpecularMap, pNormalMap;

        readonly EffectParameter pDl0Direction, pDl0Diffuse, pDl0Specular;
        readonly EffectParameter pDl1Direction, pDl1Diffuse, pDl1Specular;
        readonly EffectParameter pDl2Direction, pDl2Diffuse, pDl2Specular;

        // 3 per-pixel PointLight slots. Each shaded pixel computes its own
        // direction + smooth-quadratic radius falloff against PointLightN*.
        // Replaces SunBurn's deferred per-system Key/OverSaturationKey/
        // LocalFill — each light keeps its native radius so the small-radius
        // OverSaturationKey only over-brightens hulls near the sun while
        // Key + LocalFill light the whole orbit.
        readonly EffectParameter pPl0Enabled, pPl0Position, pPl0Diffuse, pPl0Specular, pPl0Radius;
        readonly EffectParameter pPl1Enabled, pPl1Position, pPl1Diffuse, pPl1Specular, pPl1Radius;
        readonly EffectParameter pPl2Enabled, pPl2Position, pPl2Diffuse, pPl2Specular, pPl2Radius;

        // Phase 3.8.B: receiver-side shadow uniforms. Bound by
        // LightingEffectBinder.Apply when the renderer drove a depth pre-
        // pass; default-disabled (ShadowMapEnabled=false) so meshes drawn
        // outside a shadow-aware scene fall through to the unshadowed
        // path without paying any sampling cost.
        readonly EffectParameter pShadowParams, pShadowMap, pLightViewProjection;

        // ── World/View/Projection ───────────────────────────────────────────
        public Matrix World      { get; set; } = Matrix.Identity;
        public Matrix Projection { get; set; } = Matrix.Identity;

        // View setter caches the inverse-view eye position so OnApply doesn't
        // re-invert per drawn mesh. View typically changes once per frame
        // (camera) while OnApply fires per mesh per pass — without the cache,
        // a 1000-mesh scene paid ~1000 redundant Matrix.Invert calls.
        Matrix _view = Matrix.Identity;
        Vector3 _eyePosition = Vector3.Zero;
        public Matrix View
        {
            get => _view;
            set
            {
                _view = value;
                _eyePosition = Matrix.Invert(value).Translation;
            }
        }

        // ── Lighting state ──────────────────────────────────────────────────
        public bool    LightingEnabled  { get; set; }
        public Vector3 AmbientLightColor { get; set; }
        public DirectionalLight DirectionalLight0 { get; }
        public DirectionalLight DirectionalLight1 { get; }
        public DirectionalLight DirectionalLight2 { get; }

        // PointLight slots — see field declaration above.
        public struct PointLightSlot
        {
            public bool    Enabled;
            public Vector3 Position;
            public Vector3 DiffuseColor;
            public Vector3 SpecularColor;
            public float   Radius;
        }
        public PointLightSlot PointLight0;
        public PointLightSlot PointLight1;
        public PointLightSlot PointLight2;

        // Phase 3.8.B: receiver-side shadow state. The renderer drives the
        // depth pre-pass into ShadowMapComponent and then calls
        // LightingEffectBinder.Apply, which pushes the resulting RT +
        // light-clip matrix here; CopySharedLighting then propagates to
        // per-mesh effects in lockstep with the existing light slots.
        public bool      ShadowMapEnabled    { get; set; }
        public Texture2D ShadowMap           { get; set; }
        public Matrix    LightViewProjection { get; set; } = Matrix.Identity;
        public float     ShadowBias          { get; set; } = 0.001f;

        // ── Fog state ───────────────────────────────────────────────────────
        public bool    FogEnabled { get; set; }
        public Vector3 FogColor   { get; set; }
        public float   FogStart   { get; set; }
        public float   FogEnd     { get; set; }

        public LightingEffect(GraphicsDevice device) : this(device, GetEffectBytes()) { }

        // Phase 3.10.B.5: subclass hook so SkinnedEffect can hand its own
        // .mgfxo bytes to the base Effect ctor while inheriting the full
        // parameter-cache + lighting-state surface.
        protected LightingEffect(GraphicsDevice device, byte[] effectBytes) : base(device, effectBytes)
        {
            // Cache parameter handles. .Parameters[] returns null for missing
            // parameters; we don't null-guard on every push because the FX
            // declares each name explicitly — a missing one means the .mgfxo
            // is out of date and that's a build error, not a runtime hazard.
            pWorld              = Parameters["World"];
            pView               = Parameters["View"];
            pProjection         = Parameters["Projection"];
            pDiffuseColor       = Parameters["DiffuseColor"];
            pEmissiveColor      = Parameters["EmissiveColor"];
            pSpecularColor      = Parameters["SpecularColor"];
            pSpecularPower      = Parameters["SpecularPower"];
            pAlpha              = Parameters["Alpha"];
            pEyePosition        = Parameters["EyePosition"];
            pLightingEnabled    = Parameters["LightingEnabled"];
            pTextureEnabled     = Parameters["TextureEnabled"];
            pEmissiveMapEnabled = Parameters["EmissiveMapEnabled"];
            pSpecularMapEnabled = Parameters["SpecularMapEnabled"];
            pNormalMapEnabled   = Parameters["NormalMapEnabled"];
            pFogEnabled         = Parameters["FogEnabled"];
            pAmbientLightColor  = Parameters["AmbientLightColor"];
            pFogColor           = Parameters["FogColor"];
            pFogStart           = Parameters["FogStart"];
            pFogEnd             = Parameters["FogEnd"];
            pTexture            = Parameters["Texture"];
            pEmissiveMap        = Parameters["EmissiveMap"];
            pSpecularMap        = Parameters["SpecularMap"];
            pNormalMap          = Parameters["NormalMap"];

            pDl0Direction = Parameters["DirLight0Direction"];
            pDl0Diffuse   = Parameters["DirLight0DiffuseColor"];
            pDl0Specular  = Parameters["DirLight0SpecularColor"];
            pDl1Direction = Parameters["DirLight1Direction"];
            pDl1Diffuse   = Parameters["DirLight1DiffuseColor"];
            pDl1Specular  = Parameters["DirLight1SpecularColor"];
            pDl2Direction = Parameters["DirLight2Direction"];
            pDl2Diffuse   = Parameters["DirLight2DiffuseColor"];
            pDl2Specular  = Parameters["DirLight2SpecularColor"];

            pPl0Enabled  = Parameters["PointLight0Enabled"];
            pPl0Position = Parameters["PointLight0Position"];
            pPl0Diffuse  = Parameters["PointLight0DiffuseColor"];
            pPl0Specular = Parameters["PointLight0SpecularColor"];
            pPl0Radius   = Parameters["PointLight0Radius"];

            pPl1Enabled  = Parameters["PointLight1Enabled"];
            pPl1Position = Parameters["PointLight1Position"];
            pPl1Diffuse  = Parameters["PointLight1DiffuseColor"];
            pPl1Specular = Parameters["PointLight1SpecularColor"];
            pPl1Radius   = Parameters["PointLight1Radius"];

            pPl2Enabled  = Parameters["PointLight2Enabled"];
            pPl2Position = Parameters["PointLight2Position"];
            pPl2Diffuse  = Parameters["PointLight2DiffuseColor"];
            pPl2Specular = Parameters["PointLight2SpecularColor"];
            pPl2Radius   = Parameters["PointLight2Radius"];

            // Phase 3.8.B shadow uniforms (see field declarations above).
            pShadowParams        = Parameters["ShadowParams"];
            pShadowMap           = Parameters["ShadowMap"];
            pLightViewProjection = Parameters["LightViewProjection"];

            // DirectionalLight binds direction/diffuse/specular parameters and
            // auto-pushes on property assignment. Enabled is a C# bool that we
            // honor manually in OnApply (writing zeros for disabled lights).
            DirectionalLight0 = new DirectionalLight(pDl0Direction, pDl0Diffuse, pDl0Specular, null);
            DirectionalLight1 = new DirectionalLight(pDl1Direction, pDl1Diffuse, pDl1Specular, null);
            DirectionalLight2 = new DirectionalLight(pDl2Direction, pDl2Diffuse, pDl2Specular, null);

            EnableDefaultLighting();
        }

        // BasicEffect's hardcoded 3-light setup, ported byte-for-byte. Used
        // by every per-mesh effect ctor that doesn't get explicit lighting
        // overrides from the renderer.
        public void EnableDefaultLighting()
        {
            LightingEnabled = true;
            AmbientLightColor = new Vector3(0.05333332f, 0.09882354f, 0.1819608f);
            SpecularColor = new Vector3(1, 1, 1);
            SpecularPower = 16;

            DirectionalLight0.Enabled = true;
            DirectionalLight0.Direction     = new Vector3(-0.5265408f, -0.5735765f, -0.6275069f);
            DirectionalLight0.DiffuseColor  = new Vector3(1f, 0.9607844f, 0.8078432f);
            DirectionalLight0.SpecularColor = new Vector3(1f, 0.9607844f, 0.8078432f);

            DirectionalLight1.Enabled = true;
            DirectionalLight1.Direction     = new Vector3(0.7198464f, 0.3420201f, 0.6040227f);
            DirectionalLight1.DiffuseColor  = new Vector3(0.9647059f, 0.7607844f, 0.4078432f);
            DirectionalLight1.SpecularColor = Vector3.Zero;

            DirectionalLight2.Enabled = true;
            DirectionalLight2.Direction     = new Vector3(0.4545195f, -0.7660444f, 0.4545195f);
            DirectionalLight2.DiffuseColor  = new Vector3(0.3231373f, 0.3607844f, 0.3937255f);
            DirectionalLight2.SpecularColor = Vector3.Zero;
        }

        // Push every CPU-side cached value onto the GPU's constant buffers.
        // Called explicitly by SunBurnStubs.RenderScene before pass.Apply,
        // and also by OnApply as a safety net (so callers who set properties
        // and pass-Apply directly get a consistent push).
        public void ApplyToBasicEffect()
        {
            // Texture preference: DiffuseMapTexture (data carrier from XNB
            // material reader) wins over the explicit Texture override. Pre-
            // §3.7 logic: DrawRenderables sets DiffuseMapTexture first, then
            // calls ApplyToBasicEffect which used to bridge to BasicEffect.Texture.
            if (DiffuseMapTexture != null)
            {
                Texture = DiffuseMapTexture;
                TextureEnabled = true;
            }
        }

        // Phase 2.8 carryover: SunBurn material reader call-site that maps
        // (TransparencyMode + alpha + texture) onto our Alpha + Texture state.
        public void SetTransparencyModeAndMap(TransparencyMode mode, float alpha, Texture2D map)
        {
            Alpha = alpha;
            if (map != null)
            {
                Texture = map;
                TextureEnabled = true;
            }
        }

        // Push all CPU state to GPU just before each pass renders. Called by
        // EffectPass.Apply() through the base implementation.
        protected override void OnApply()
        {
            base.OnApply();

            pWorld?.SetValue(World);
            pView?.SetValue(View);
            pProjection?.SetValue(Projection);

            pDiffuseColor?.SetValue(DiffuseColor);
            pEmissiveColor?.SetValue(EmissiveColor);
            pSpecularColor?.SetValue(SpecularColor);
            pSpecularPower?.SetValue(SpecularPower);
            pAlpha?.SetValue(Alpha);

            // EyePosition is cached on the View setter — see _view/_eyePosition
            // for why we don't re-invert per OnApply.
            pEyePosition?.SetValue(_eyePosition);

            pLightingEnabled?.SetValue(LightingEnabled);
            pAmbientLightColor?.SetValue(AmbientLightColor);

            // Disabled lights contribute zero — push (0,0,0) for diffuse + specular
            // to mirror BasicEffect's zero-when-disabled convention. Direction is
            // left at the user's value (not used when both colors are zero).
            ApplyDirectional(DirectionalLight0, pDl0Diffuse, pDl0Specular);
            ApplyDirectional(DirectionalLight1, pDl1Diffuse, pDl1Specular);
            ApplyDirectional(DirectionalLight2, pDl2Diffuse, pDl2Specular);

            ApplyPointLight(PointLight0, pPl0Enabled, pPl0Position, pPl0Diffuse, pPl0Specular, pPl0Radius);
            ApplyPointLight(PointLight1, pPl1Enabled, pPl1Position, pPl1Diffuse, pPl1Specular, pPl1Radius);
            ApplyPointLight(PointLight2, pPl2Enabled, pPl2Position, pPl2Diffuse, pPl2Specular, pPl2Radius);

            pFogEnabled?.SetValue(FogEnabled);
            pFogColor?.SetValue(FogColor);
            pFogStart?.SetValue(FogStart);
            pFogEnd?.SetValue(FogEnd);

            pTextureEnabled?.SetValue(TextureEnabled && Texture != null);
            if (Texture != null)
                pTexture?.SetValue(Texture);

            // Phase 3.7 step 4 (Phase B): emissive (`_g`) and specular (`_s`)
            // map sampling. The flag tells the shader whether to sample at all
            // — bound but null/disabled textures fall back to the per-material
            // EmissiveColor / unit specular respectively.
            bool emissiveMapBound = EmissiveMapTexture != null;
            pEmissiveMapEnabled?.SetValue(emissiveMapBound);
            if (emissiveMapBound)
                pEmissiveMap?.SetValue(EmissiveMapTexture);

            bool specularMapBound = SpecularColorMapTexture != null;
            pSpecularMapEnabled?.SetValue(specularMapBound);
            if (specularMapBound)
                pSpecularMap?.SetValue(SpecularColorMapTexture);

            // Phase 3.7 step 4 (Phase C): tangent-space normal (`_n`) map.
            // Enables per-pixel hull surface detail — recessed panels, rivets,
            // weld seams. Vertex format must carry Tangent + Binormal for the
            // TBN basis; SdMeshGroup writes these for any mesh with UVs.
            bool normalMapBound = NormalMapTexture != null;
            pNormalMapEnabled?.SetValue(normalMapBound);
            if (normalMapBound)
                pNormalMap?.SetValue(NormalMapTexture);

            // Phase 3.8.B: shadow uniforms. ShadowParams.x is the
            // enable flag (0/1); .y carries the depth bias. Packed into
            // a float4 instead of two separate scalars to dodge an MGFX
            // 3.8.1 constant-folding quirk that drops solo-float uniforms
            // declared with a 0.0 default. See MeshLighting.fx header.
            bool shadowBound = ShadowMapEnabled && ShadowMap != null;
            pShadowParams?.SetValue(new Vector4(shadowBound ? 1f : 0f, ShadowBias, 0f, 0f));
            if (shadowBound)
                pShadowMap?.SetValue(ShadowMap);
            pLightViewProjection?.SetValue(LightViewProjection);
        }

        static void ApplyDirectional(DirectionalLight light, EffectParameter diffuseParam, EffectParameter specularParam)
        {
            if (light.Enabled) return;   // user-set diffuse/specular already pushed via property setters
            diffuseParam?.SetValue(Vector3.Zero);
            specularParam?.SetValue(Vector3.Zero);
        }

        static void ApplyPointLight(PointLightSlot slot,
            EffectParameter enabledParam, EffectParameter positionParam,
            EffectParameter diffuseParam, EffectParameter specularParam, EffectParameter radiusParam)
        {
            enabledParam?.SetValue(slot.Enabled);
            if (!slot.Enabled) return;
            positionParam?.SetValue(slot.Position);
            diffuseParam?.SetValue(slot.DiffuseColor);
            specularParam?.SetValue(slot.SpecularColor);
            radiusParam?.SetValue(slot.Radius);
        }
    }
}

// MonoGame ships its own Microsoft.Xna.Framework.Graphics.DirectionalLight (used by
// LightingEffect.DirectionalLight0/1/2). The XNA 3.1 name was BasicDirectionalLight; we
// updated call sites to use DirectionalLight directly rather than carrying a stub.
