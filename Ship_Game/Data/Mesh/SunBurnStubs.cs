// Phase 1 migration stubs for SynapseGaming SunBurn types.
// SDSunBurn project was excluded from the solution in Phase 1.9.
// All members are no-ops; existing call sites compile but render nothing.
// TODO Phase 2: replace with MonoGame-native lighting/rendering implementation.

using System;
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

    public class SceneEnvironment { }

    public class SceneState
    {
        public void BeginFrameRendering(ref Matrix view, ref Matrix proj, float elapsed,
                                        SceneEnvironment env, bool b) { }
        public void EndFrameRendering() { }
    }

    public class SceneInterface : IDisposable
    {
        public Lights.ILightManager LightManager { get; } = new Lights.LightManager();
        public Rendering.IObjectManager ObjectManager { get; } = new Rendering.ObjectManager();
        public Rendering.IRenderManager RenderManager { get; } = new Rendering.RenderManager();

        public SceneInterface(IGraphicsDeviceService _) { }
        public SceneInterface(GraphicsDeviceManager _) { }
        public void CreateDefaultManagers(bool useDeferredRendering, bool usePostProcessing) { }
        public void AddManager(IManagerService m) { }
        public void Update(float deltaTime) { }
        public void BeginFrameRendering(SceneState s) { }
        public void EndFrameRendering() { }
        public void Unload() { }
        public void ApplyPreferences(LightingSystemPreferences p) { }
        public void Dispose() { }
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
        public void Submit(ISceneObject obj) { }
        public bool Remove(ISceneObject obj) => false;
        public void Clear() { }
    }

    public class RenderManager : IRenderManager
    {
        public void Render() { }
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
        public bool HasMeshes => false;
        public void Add(ModelMesh m, Effect e) { }
        public void Add(RenderableMesh m) { }
        public void AffineTransform(Vector3 pos, Vector3 rotRads, Vector3 scale) { }
        public void UpdateAnimation(float deltaTime) { }
    }

    public class RenderableMesh
    {
        public RenderableMesh(SceneObject o, Effect e, Matrix world, BoundingSphere s,
                              IndexBuffer ib, VertexBuffer vb, VertexDeclaration vd,
                              int streamOffset, PrimitiveType pt,
                              int primitiveCount, int baseVertex, int numVertices,
                              int startIndex, int vertexStride) { }
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
        void Submit(LightRig rig);
        void Clear();
    }

    public class LightManager : ILightManager, SynapseGaming.LightingSystem.Core.IManagerService
    {
        public LightManager() { }
        public LightManager(IGraphicsDeviceService _) { }
        public void Submit(LightRig rig) { }
        public void Submit(ILight light) { }
        public bool Remove(ILight light) => false;
        public void Clear() { }
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
        public BaseMaterialEffect(GraphicsDevice device) : base(device) { }
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
        public LightingEffect(GraphicsDevice device) : base(device) { }
        public void SetTransparencyModeAndMap(TransparencyMode mode, float alpha, Texture2D map) { }
    }
}

// MonoGame ships its own Microsoft.Xna.Framework.Graphics.DirectionalLight (used by
// BasicEffect.DirectionalLight0/1/2). The XNA 3.1 name was BasicDirectionalLight; we
// updated call sites to use DirectionalLight directly rather than carrying a stub.
