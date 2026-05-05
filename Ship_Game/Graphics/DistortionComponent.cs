using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

namespace Ship_Game
{
    // Phase 3.7 step 2: screen-space shield-hit distortion as a single
    // SpriteBatch-driven post-process pass. Mirrors the BloomComponent shape:
    // load an Effect via .xnb -> .mgfxo fallback, expose a Draw(source -> dest)
    // entry point, and let the host renderer slot it into the post-process
    // chain.
    //
    // The legacy XNA pipeline rendered displacement-map sources (Distorters.fx
    // HeatHaze / PullIn quads) into a half-res RT, then ran Distort.fx to warp
    // the back buffer by sampling that RT. We collapse both stages into one PS
    // because MonoGame 3.8.1.303 can't read the back buffer directly (no
    // ResolveBackBuffer); the displacement RT would itself sit on a copy of
    // the scene RT, at which point it is just a per-pixel function of the
    // shield-hit uniforms and we can evaluate it inline. See Distort.fx
    // header for shader-side reasoning.
    public sealed class DistortionComponent : IDisposable
    {
        // Must match MAX_SHIELDS in Distort.fx. Eight is enough for combat
        // scenes — engagements with more simultaneously-hit shields fall
        // back to "the closest 8 hits get distorted"; the rest still draw
        // their visible shield bubble (ShieldManager) so the player sees
        // them, just without the screen-space ripple.
        public const int MaxShields = 8;

        // SpriteBatch-bound per-shield uniform.
        //   CenterUV : center of the shield in destination UV (0..1, 0=top-left)
        //   RadiusUV : shield bounding-circle radius in destination UV
        //   Intensity: 0..1, drives ripple amplitude (0 disables this slot)
        public struct DistortionSource
        {
            public Vector2 CenterUV;
            public float   RadiusUV;
            public float   Intensity;
        }

        readonly GraphicsDevice Device;
        readonly GameContentManager Content;

        Effect Distort;
        EffectParameter ShieldDataParam;
        EffectParameter TimeParam;
        EffectParameter MatrixTransformParam;

        // Reused per-frame to avoid allocating MaxShields Vector4s each Draw.
        readonly Vector4[] ShieldData = new Vector4[MaxShields];

        public DistortionComponent(GraphicsDevice device, GameContentManager content)
        {
            Device = device;
            Content = content;
        }

        public void LoadContent()
        {
            Distort = Content.Load<Effect>("Effects/Distort");
            if (Distort == null) return; // defense-in-depth: missing-file regression catch
            ShieldDataParam      = Distort.Parameters["ShieldData"];
            TimeParam            = Distort.Parameters["Time"];
            MatrixTransformParam = Distort.Parameters["MatrixTransform"];
        }

        // Run the distortion pass. `source` is the (post-bloom) scene RT;
        // output is written to `destination`. Both must share dimensions;
        // `destination` MUST NOT alias `source` (PS samples the source as
        // its texture argument).
        //
        // No-op if the effect failed to load or if no source has Intensity > 0
        // — the renderer should skip calling this entirely when the source
        // list is empty so the destination RT stays as scratch instead of
        // forcing a copy. The internal short-circuit is for safety only.
        public void Draw(SpriteBatch batch, Texture2D source, RenderTarget2D destination,
                         IReadOnlyList<DistortionSource> sources, float time)
        {
            if (Distort == null) return;

            int activeCount = PackShieldData(sources);
            if (activeCount == 0) return;

            ShieldDataParam?.SetValue(ShieldData);
            TimeParam?.SetValue(time);

            int destW = destination?.Width  ?? Device.PresentationParameters.BackBufferWidth;
            int destH = destination?.Height ?? Device.PresentationParameters.BackBufferHeight;

            // SpriteBatch in MonoGame 3.8.1.303 does NOT auto-populate the
            // `MatrixTransform` parameter on a custom effect passed to
            // Begin — see BloomComponent.SetMatrixTransform for the full
            // story. Build the screen-pixel→clip-space ortho ourselves.
            if (MatrixTransformParam != null)
            {
                Matrix.CreateOrthographicOffCenter(0, destW, destH, 0, 0, 1, out Matrix projection);
                MatrixTransformParam.SetValue(projection);
            }

            Device.SetRenderTarget(destination);
            batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, Distort);
            batch.Draw(source, new Rectangle(0, 0, destW, destH), Color.White);
            batch.End();
            Device.SetRenderTarget(null);
        }

        // Pack the source list into the fixed-size ShieldData uniform.
        // Returns the count of slots populated (intensity > 0). Zero-init
        // any unused tail slots so the shader's `intensity <= 0` early-out
        // skips them.
        int PackShieldData(IReadOnlyList<DistortionSource> sources)
        {
            int count = 0;
            int n = sources?.Count ?? 0;
            for (int i = 0; i < MaxShields; ++i)
            {
                if (i < n && sources[i].Intensity > 0f)
                {
                    var s = sources[i];
                    ShieldData[i] = new Vector4(s.CenterUV.X, s.CenterUV.Y, s.RadiusUV, s.Intensity);
                    ++count;
                }
                else
                {
                    ShieldData[i] = Vector4.Zero;
                }
            }
            return count;
        }

        public void Dispose()
        {
            // Effect is owned by GameContentManager; do not dispose.
            Distort = null;
            ShieldDataParam = null;
            TimeParam = null;
            MatrixTransformParam = null;
            GC.SuppressFinalize(this);
        }
    }
}
