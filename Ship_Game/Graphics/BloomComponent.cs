using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;

namespace Ship_Game
{
    // Phase 3.7 step 1: 4-pass post-process bloom on top of the §2.8 forward
    // renderer. The XNA 3.1 BloomExtract / BloomCombine / GaussianBlur XNBs
    // ship as `.mgfxo` siblings (compiled from `game/Content/Effects/*.fx`)
    // and load via the .xnb -> .mgfxo fallback in GameContentManager.
    //
    // Pipeline (canonical XNA Bloom Sample, 15-tap separable Gaussian):
    //   1. Extract   : sourceScene -> rt1   (above-threshold pixels only)
    //   2. Blur H    : rt1 -> rt2           (15-tap horizontal Gaussian)
    //   3. Blur V    : rt2 -> rt1           (15-tap vertical Gaussian)
    //   4. Combine   : rt1 + sourceScene -> destination
    //
    // Working RTs are half-resolution; the final combine writes back at
    // full destination size. SpriteBatch supplies the VS for each pass —
    // all three effects are PS-only, matching the desaturate/scale/etc.
    // PS-only convention used elsewhere in this project.
    public sealed class BloomComponent : IDisposable
    {
        public BloomSettings Settings { get; set; } = BloomSettings.PresetSettings[0];
        public IntermediateBuffer ShowBuffer { get; set; } = IntermediateBuffer.FinalResult;

        readonly GraphicsDevice Device;
        readonly GameContentManager Content;

        Effect BloomExtract;
        Effect BloomCombine;
        Effect GaussianBlur;

        RenderTarget2D RenderTarget1;   // half-res ping
        RenderTarget2D RenderTarget2;   // half-res pong

        public BloomComponent(GraphicsDevice device, GameContentManager content)
        {
            Device = device;
            Content = content;
        }

        public void LoadContent()
        {
            BloomExtract = Content.Load<Effect>("Effects/BloomExtract");
            BloomCombine = Content.Load<Effect>("Effects/BloomCombine");
            GaussianBlur = Content.Load<Effect>("Effects/GaussianBlur");

            PresentationParameters pp = Device.PresentationParameters;
            int width  = Math.Max(1, pp.BackBufferWidth  / 2);
            int height = Math.Max(1, pp.BackBufferHeight / 2);

            RenderTarget1 = new RenderTarget2D(Device, width, height, mipMap: false,
                                               SurfaceFormat.Color, DepthFormat.None);
            RenderTarget2 = new RenderTarget2D(Device, width, height, mipMap: false,
                                               SurfaceFormat.Color, DepthFormat.None);
        }

        // Run the 4-pass bloom pipeline. `source` is the scene RT; output
        // is written to `destination` (or back buffer if null). Both must
        // share the same dimensions; `destination` MUST NOT alias `source`
        // (the combine pass samples `source` as a parameter).
        public void Draw(SpriteBatch batch, RenderTarget2D source, RenderTarget2D destination)
        {
            if (BloomExtract == null) return;  // LoadContent never ran

            // Pass 1: bright-pass extract -> rt1
            BloomExtract.Parameters["BloomThreshold"]?.SetValue(Settings.BloomThreshold);
            SetMatrixTransform(BloomExtract, RenderTarget1.Width, RenderTarget1.Height);
            Device.SetRenderTarget(RenderTarget1);
            DrawFullscreenQuad(batch, source, RenderTarget1, BloomExtract);

            // Pass 2: horizontal blur rt1 -> rt2
            SetBlurEffectParameters(1f / RenderTarget1.Width, 0);
            SetMatrixTransform(GaussianBlur, RenderTarget2.Width, RenderTarget2.Height);
            Device.SetRenderTarget(RenderTarget2);
            DrawFullscreenQuad(batch, RenderTarget1, RenderTarget2, GaussianBlur);

            // Pass 3: vertical blur rt2 -> rt1
            SetBlurEffectParameters(0, 1f / RenderTarget1.Height);
            SetMatrixTransform(GaussianBlur, RenderTarget1.Width, RenderTarget1.Height);
            Device.SetRenderTarget(RenderTarget1);
            DrawFullscreenQuad(batch, RenderTarget2, RenderTarget1, GaussianBlur);

            // Pass 4: combine rt1 (bloom) + source (base) -> destination
            Device.SetRenderTarget(destination);
            BloomCombine.Parameters["BloomIntensity"]?.SetValue(Settings.BloomIntensity);
            BloomCombine.Parameters["BaseIntensity"] ?.SetValue(Settings.BaseIntensity);
            BloomCombine.Parameters["BloomSaturation"]?.SetValue(Settings.BloomSaturation);
            BloomCombine.Parameters["BaseSaturation"] ?.SetValue(Settings.BaseSaturation);
            BloomCombine.Parameters["BaseTexture"]?.SetValue((Texture2D)source);

            int destW = destination?.Width  ?? Device.PresentationParameters.BackBufferWidth;
            int destH = destination?.Height ?? Device.PresentationParameters.BackBufferHeight;
            SetMatrixTransform(BloomCombine, destW, destH);
            DrawFullscreenQuad(batch, RenderTarget1, destW, destH, BloomCombine);

            // Unbind the destination RT before returning. The downstream
            // draw flow (fog-of-war composite, borders, UI) targets the back
            // buffer; without this, MonoGame crashes at Present time with
            // "Cannot call Present when a render target is active."
            Device.SetRenderTarget(null);
        }

        // SpriteBatch in MonoGame 3.8.1.303 does NOT auto-populate the
        // `MatrixTransform` parameter on a custom effect passed to Begin —
        // it only sets `TransformMatrix` on a SpriteEffect-typed effect.
        // Without this, our VS multiplies pixel positions by a zero matrix
        // and the quad collapses to the origin (silent black output). We
        // build the same screen-pixel→clip-space ortho that SpriteEffect
        // would have built and push it ourselves.
        static void SetMatrixTransform(Effect effect, int viewportWidth, int viewportHeight)
        {
            EffectParameter mt = effect.Parameters["MatrixTransform"];
            if (mt == null) return;
            Matrix.CreateOrthographicOffCenter(0, viewportWidth, viewportHeight, 0, 0, 1, out Matrix projection);
            // Half-pixel offset matches XNA SpriteBatch's "fix" for D3D9
            // texel-center sampling. MonoGame on DirectX_11 uses D3D11
            // sampling rules and skips the offset; mirror that here.
            mt.SetValue(projection);
        }

        void DrawFullscreenQuad(SpriteBatch batch, Texture2D texture, RenderTarget2D rt, Effect effect)
            => DrawFullscreenQuad(batch, texture, rt.Width, rt.Height, effect);

        // The PS-only-with-SpriteBatch pattern (Begin without effect, Pass.Apply
        // before Draw) does not reliably override SpriteEffect's PS under MGFX
        // 3.8.1.303 / DirectX_11 — output is silently black. The bloom shaders
        // ship with explicit passthrough VS + custom PS so the effect can be
        // passed to SpriteBatch.Begin's `effect:` argument; SpriteBatch's
        // Apply uses our pass directly. This matches the canonical MonoGame
        // Bloom Sample.
        void DrawFullscreenQuad(SpriteBatch batch, Texture2D texture, int width, int height, Effect effect)
        {
            batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque,
                        SamplerState.LinearClamp, DepthStencilState.None,
                        RasterizerState.CullNone, effect);
            batch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            batch.End();
        }

        // Compute 15-tap separable Gaussian weights + offsets. Sample 0 is
        // the center; samples 1..14 are seven offset pairs at ±1, ±3, ±5,
        // ±7, ±9, ±11, ±13 pixels along (dx, dy), using the linear-sampling
        // trick to halve the tap count without quality loss.
        void SetBlurEffectParameters(float dx, float dy)
        {
            EffectParameter weightsParam = GaussianBlur.Parameters["SampleWeights"];
            EffectParameter offsetsParam = GaussianBlur.Parameters["SampleOffsets"];
            if (weightsParam == null || offsetsParam == null) return;

            int sampleCount = weightsParam.Elements.Count;
            var sampleWeights = new float[sampleCount];
            var sampleOffsets = new Vector2[sampleCount];

            sampleWeights[0] = ComputeGaussian(0);
            sampleOffsets[0] = Vector2.Zero;
            float totalWeights = sampleWeights[0];

            for (int i = 0; i < sampleCount / 2; i++)
            {
                float weight = ComputeGaussian(i + 1);
                sampleWeights[i * 2 + 1] = weight;
                sampleWeights[i * 2 + 2] = weight;
                totalWeights += weight * 2;

                // Linear-sampling pair offset (Sumeet Khanduja / GPU Gems 3).
                float sampleOffset = i * 2 + 1.5f;
                Vector2 delta = new Vector2(dx, dy) * sampleOffset;
                sampleOffsets[i * 2 + 1] =  delta;
                sampleOffsets[i * 2 + 2] = -delta;
            }

            for (int i = 0; i < sampleWeights.Length; i++)
                sampleWeights[i] /= totalWeights;

            weightsParam.SetValue(sampleWeights);
            offsetsParam.SetValue(sampleOffsets);
        }

        float ComputeGaussian(float n)
        {
            float theta = Settings.BlurAmount;
            return (float)(1.0 / Math.Sqrt(2 * Math.PI * theta) *
                           Math.Exp(-(n * n) / (2 * theta * theta)));
        }

        public enum IntermediateBuffer
        {
            PreBloom,
            BlurredHorizontally,
            BlurredBothWays,
            FinalResult
        }

        public sealed class BloomSettings
        {
            public readonly string Name;
            public readonly float BloomThreshold;
            public readonly float BlurAmount;
            public readonly float BloomIntensity;
            public readonly float BaseIntensity;
            public readonly float BloomSaturation;
            public readonly float BaseSaturation;
            public static BloomSettings[] PresetSettings;

            static BloomSettings()
            {
                BloomSettings[] bloomSetting =
                {
                    new BloomSettings("Default",     0.95f, 1f, 2f, 1f,   1f, 1f),
                    new BloomSettings("Intense",     0.9f,  1f, 3f, 1f,   1f, 1f),
                    new BloomSettings("Soft",        0f,    3f, 1f, 1f,   1f, 1f),
                    new BloomSettings("Desaturated", 0.5f,  8f, 2f, 1f,   0f, 1f),
                    new BloomSettings("Saturated",   0.25f, 4f, 2f, 1f,   2f, 0f),
                    new BloomSettings("Blurry",      0f,    2f, 1f, 0.1f, 1f, 1f),
                    new BloomSettings("Subtle",      0.5f,  2f, 1f, 1f,   1f, 1f)
                };
                PresetSettings = bloomSetting;
            }

            public BloomSettings(string name, float bloomThreshold, float blurAmount, float bloomIntensity, float baseIntensity, float bloomSaturation, float baseSaturation)
            {
                Name = name;
                BloomThreshold = bloomThreshold;
                BlurAmount = blurAmount;
                BloomIntensity = bloomIntensity;
                BaseIntensity = baseIntensity;
                BloomSaturation = bloomSaturation;
                BaseSaturation = baseSaturation;
            }
        }

        public void Dispose()
        {
            RenderTarget1?.Dispose();
            RenderTarget2?.Dispose();
            RenderTarget1 = null;
            RenderTarget2 = null;
            GC.SuppressFinalize(this);
        }
    }
}
