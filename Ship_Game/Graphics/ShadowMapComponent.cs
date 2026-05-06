using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

namespace Ship_Game.Graphics
{
    // Phase 3.8.A — depth-pass infrastructure for the basic shadow-map feature.
    //
    // Owns a single 1024×1024 R32F shadow render target plus a depth buffer
    // for occlusion ordering during the depth-only pass. Callers feed the
    // dominant directional light's world-space direction and a bounding
    // sphere covering the scene's casters; the component builds an
    // orthographic light view/projection that tightly wraps that sphere and
    // streams caster geometry through Shadow.fx.
    //
    // §3.8.A scope: produce the depth RT + LightView/LightProjection so
    // ShadowMapTests can verify depth correctness in isolation. The lit
    // shader and live RenderScene wiring land in §3.8.B; until then this
    // component is constructed only by tests and is not on the live render
    // path. Falling back to "no shadows" is therefore the existing
    // unshadowed forward path verbatim — strict-additive feature.
    //
    // RT size choice: 1024² balanced against Phase 3.7's existing
    // half-res post-process buffers; raised to 2048²/4096² in §3.8.C only
    // if shadow acne / projective aliasing forces it. The test fixture
    // uses 64² to keep GetData round-trips quick.
    //
    // Depth encoding: R32F holds normalised NDC z (clip.z / clip.w), so a
    // surface at the light's near plane reads ~0 and one at the far plane
    // reads ~1. §3.8.B's lit shader will compare the receiver's
    // light-space depth against this; bias is added on the receiver side
    // so this RT is the canonical occluder depth.
    public sealed class ShadowMapComponent : IDisposable
    {
        public const int DefaultSize = 1024;

        readonly GraphicsDevice Device;
        readonly GameContentManager Content;
        readonly int Size;

        Effect ShadowFx;
        EffectParameter WorldParam;
        EffectParameter LightViewParam;
        EffectParameter LightProjectionParam;

        public RenderTarget2D ShadowMap { get; private set; }

        // Light-space transforms exposed for §3.8.B (lit pass samples the
        // shadow map at the surface's projected light-space UV) and for
        // tests that want to project ground-truth depths to compare.
        public Matrix LightView { get; private set; } = Matrix.Identity;
        public Matrix LightProjection { get; private set; } = Matrix.Identity;
        public Vector3 LightDirection { get; private set; } = -Vector3.UnitZ;

        // Track the device's previous render target so EndShadowPass can
        // restore it. SunBurnStubs.SceneInterface.RenderScene runs against
        // the back buffer, but UI / post-process passes may have stacked
        // RTs — preserve them defensively.
        RenderTargetBinding[] PreviousTargets;
        bool PassActive;

        public ShadowMapComponent(GraphicsDevice device, GameContentManager content, int size = DefaultSize)
        {
            Device = device;
            Content = content;
            Size = size;
        }

        public void LoadContent()
        {
            // Effects/Shadow.xnb does not exist on disk; the .mgfxo fallback in
            // GameContentManager.LoadAsset picks up the sibling we ship at
            // game/Content/Effects/Shadow.mgfxo (compiled by mgfxc, identical
            // path to Bloom/Distortion).
            ShadowFx = Content.Load<Effect>("Effects/Shadow");
            if (ShadowFx == null) return;

            WorldParam           = ShadowFx.Parameters["World"];
            LightViewParam       = ShadowFx.Parameters["LightView"];
            LightProjectionParam = ShadowFx.Parameters["LightProjection"];

            // Single-channel depth target. SurfaceFormat.Single is R32F on
            // DirectX_11 — the only format that gives us the full FP range
            // we need to compare against per-pixel scene depth without
            // banding the receiver-side test in §3.8.B.
            ShadowMap = new RenderTarget2D(Device, Size, Size, mipMap: false,
                                           SurfaceFormat.Single, DepthFormat.Depth24);
        }

        // Build a view/projection that wraps `sceneBounds` from the sun's
        // POV and bind the shadow RT for depth-only rendering. Caller
        // streams casters via DrawCaster, then calls EndShadowPass.
        //
        // sceneBounds: world-space bounding sphere covering all casters.
        // The ortho box is sized to 2*radius on each axis with the light
        // camera placed one diameter behind the sphere center so the near
        // plane sits just in front of the closest caster.
        public void BeginShadowPass(Vector3 lightDirection, BoundingSphere sceneBounds)
        {
            if (ShadowFx == null || ShadowMap == null)
                throw new InvalidOperationException("ShadowMapComponent.LoadContent must be called before BeginShadowPass.");
            if (PassActive)
                throw new InvalidOperationException("ShadowMapComponent: BeginShadowPass called while a pass is already active.");

            float lenSq = lightDirection.LengthSquared();
            LightDirection = lenSq > 1e-6f ? Vector3.Normalize(lightDirection) : -Vector3.UnitY;

            float radius = Math.Max(sceneBounds.Radius, 1e-3f);
            Vector3 center = sceneBounds.Center;
            // Light camera sits one diameter behind the sphere along the
            // negative light direction so casters fully fit between near
            // and far planes (range = 2*radius * 2 = 4*radius).
            Vector3 lightPos = center - LightDirection * (radius * 2f);

            // Build a stable up vector that is not collinear with the light
            // direction. Picking world-up first matches the SunBurn
            // convention; if the sun shines straight down we fall back to
            // world-forward.
            Vector3 up = Math.Abs(Vector3.Dot(LightDirection, Vector3.Up)) > 0.99f
                ? Vector3.Forward
                : Vector3.Up;

            LightView = Matrix.CreateLookAt(lightPos, center, up);
            // Ortho box: 2*radius wide in light-space X/Y, near=0 / far=4r.
            LightProjection = Matrix.CreateOrthographic(radius * 2f, radius * 2f, 0f, radius * 4f);

            PreviousTargets = Device.GetRenderTargets();
            Device.SetRenderTarget(ShadowMap);
            // Far plane = white; closer surfaces overwrite with smaller z.
            Device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, depth: 1f, stencil: 0);

            // Standard depth-only configuration: opaque blend (we only
            // care about z, not alpha), depth read+write, back-face cull.
            Device.BlendState        = BlendState.Opaque;
            Device.DepthStencilState = DepthStencilState.Default;
            Device.RasterizerState   = RasterizerState.CullCounterClockwise;

            LightViewParam      ?.SetValue(LightView);
            LightProjectionParam?.SetValue(LightProjection);

            PassActive = true;
        }

        // Submit one indexed mesh to the shadow pass. Mirrors the parameter
        // shape of SunBurnStubs.DrawRenderables so the §3.8.B wiring in
        // SceneInterface.RenderScene can call this in lockstep with the
        // existing iteration over RenderableMeshes / AddedModelMeshes —
        // the caster set must stay identical to the lit-pass set or
        // shadows project from objects that aren't visible (and vice versa).
        public void DrawCaster(Matrix world, VertexBuffer vertexBuffer, IndexBuffer indexBuffer,
                               PrimitiveType primitiveType, int baseVertex, int startIndex, int primitiveCount)
        {
            if (!PassActive || ShadowFx == null) return;
            if (vertexBuffer == null || indexBuffer == null || primitiveCount == 0) return;

            WorldParam?.SetValue(world);

            Device.SetVertexBuffer(vertexBuffer);
            Device.Indices = indexBuffer;
            foreach (EffectPass pass in ShadowFx.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawIndexedPrimitives(primitiveType, baseVertex, startIndex, primitiveCount);
            }
        }

        public void EndShadowPass()
        {
            if (!PassActive) return;
            if (PreviousTargets != null && PreviousTargets.Length > 0)
                Device.SetRenderTargets(PreviousTargets);
            else
                Device.SetRenderTarget(null);
            PreviousTargets = null;
            PassActive = false;
        }

        public void Dispose()
        {
            ShadowMap?.Dispose();
            ShadowMap = null;
            // Effect is owned by GameContentManager; do not dispose.
            ShadowFx = null;
            WorldParam = null;
            LightViewParam = null;
            LightProjectionParam = null;
            GC.SuppressFinalize(this);
        }
    }
}
