using System;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = SDGraphics.Rectangle;
using XnaVector2 = Microsoft.Xna.Framework.Vector2;

namespace Ship_Game
{
    public sealed class BloomComponent : IDisposable
    {
        readonly ScreenManager ScreenManager;
        readonly GraphicsDevice Device;
        Effect bloomExtractEffect;
        Effect bloomCombineEffect;
        Effect gaussianBlurEffect;
        ResolveTexture2D resolveTarget;
        RenderTarget2D renderTarget1;
        RenderTarget2D renderTarget2;
        DepthStencilBuffer buffer;

        public BloomSettings Settings { get; set; } = BloomSettings.PresetSettings[0];
        public IntermediateBuffer ShowBuffer { get; set; } = IntermediateBuffer.FinalResult;

        public BloomComponent(ScreenManager screenManager)
        {
            ScreenManager = screenManager;
            Device = screenManager.GraphicsDevice;
        }

        float ComputeGaussian(float n)
        {
            float theta = Settings.BlurAmount;
            return (float)(1 / Math.Sqrt(6.28318530717959 * theta) * Math.Exp(-(n * n) / (2f * theta * theta)));
        }

        public static DepthStencilBuffer CreateDepthStencil(RenderTarget2D target)
        {
            return new DepthStencilBuffer(target.GraphicsDevice, target.Width, target.Height, target.GraphicsDevice.DepthStencilBuffer.Format, target.MultiSampleType, target.MultiSampleQuality);
        }

        public static DepthStencilBuffer CreateDepthStencil(RenderTarget2D target, DepthFormat depth)
        {
            if (!GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, target.Format, depth))
            {
                return CreateDepthStencil(target);
            }
            return new DepthStencilBuffer(target.GraphicsDevice, target.Width, target.Height, depth, target.MultiSampleType, target.MultiSampleQuality);
        }

        public void Draw()
        {
            Device.ResolveBackBuffer(resolveTarget);
            bloomExtractEffect.Parameters["BloomThreshold"].SetValue(Settings.BloomThreshold);
            DrawFullscreenQuad(resolveTarget, renderTarget1, bloomExtractEffect, IntermediateBuffer.PreBloom);
            SetBlurEffectParameters(1f / renderTarget1.Width, 0f);
            DrawFullscreenQuad(renderTarget1.GetTexture(), renderTarget2, gaussianBlurEffect, IntermediateBuffer.BlurredHorizontally);
            SetBlurEffectParameters(0f, 1f / renderTarget1.Height);
            DrawFullscreenQuad(renderTarget2.GetTexture(), renderTarget1, gaussianBlurEffect, IntermediateBuffer.BlurredBothWays);
            Device.SetRenderTarget(0, null);
            EffectParameterCollection parameters = bloomCombineEffect.Parameters;
            parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
            parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
            parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
            parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);
            Device.Textures[1] = resolveTarget;
            Viewport viewport = GameBase.Viewport;
            DrawFullscreenQuad(renderTarget1.GetTexture(), viewport.Width, viewport.Height, bloomCombineEffect, IntermediateBuffer.FinalResult);
        }

        void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, Effect effect, IntermediateBuffer currentBuffer)
        {
            Device.SetRenderTarget(0, renderTarget);
            DepthStencilBuffer old = Device.DepthStencilBuffer;
            Device.DepthStencilBuffer = buffer;
            DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect, currentBuffer);
            Device.SetRenderTarget(0, null);
            Device.DepthStencilBuffer = old;
        }

        void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect, IntermediateBuffer currentBuffer)
        {
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
            if (ShowBuffer >= currentBuffer)
            {
                effect.Begin();
                effect.CurrentTechnique.Passes[0].Begin();
            }
            ScreenManager.SpriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
            ScreenManager.SpriteBatch.End();
            if (ShowBuffer >= currentBuffer)
            {
                effect.CurrentTechnique.Passes[0].End();
                effect.End();
            }
        }

        public void LoadContent()
        {
            bloomExtractEffect = GameBase.Base.Content.Load<Effect>("Effects/BloomExtract");
            bloomCombineEffect = GameBase.Base.Content.Load<Effect>("Effects/BloomCombine");
            gaussianBlurEffect = GameBase.Base.Content.Load<Effect>("Effects/GaussianBlur");
            PresentationParameters pp = Device.PresentationParameters;
            int width = pp.BackBufferWidth;
            int height = pp.BackBufferHeight;
            SurfaceFormat format = pp.BackBufferFormat;
            resolveTarget = new ResolveTexture2D(Device, width, height, 1, format);
            width = width / 2;
            height = height / 2;
            renderTarget1 = new RenderTarget2D(Device, width, height, 1, format);
            renderTarget2 = new RenderTarget2D(Device, width, height, 1, format);
            buffer = CreateDepthStencil(renderTarget1);
        }

        void SetBlurEffectParameters(float dx, float dy)
        {
            EffectParameter weightsParameter = gaussianBlurEffect.Parameters["SampleWeights"];
            EffectParameter offsetsParameter = gaussianBlurEffect.Parameters["SampleOffsets"];
            int sampleCount = weightsParameter.Elements.Count;
            float[] sampleWeights = new float[sampleCount];
            XnaVector2[] sampleOffsets = new XnaVector2[sampleCount];
            sampleWeights[0] = ComputeGaussian(0f);
            sampleOffsets[0] = new XnaVector2(0f);
            float totalWeights = sampleWeights[0];
            for (int i = 0; i < sampleCount / 2; i++)
            {
                float weight = ComputeGaussian(i + 1);
                sampleWeights[i * 2 + 1] = weight;
                sampleWeights[i * 2 + 2] = weight;
                totalWeights = totalWeights + weight * 2f;
                float sampleOffset = i * 2 + 1.5f;
                XnaVector2 delta = new XnaVector2(dx, dy) * sampleOffset;
                sampleOffsets[i * 2 + 1] = delta;
                sampleOffsets[i * 2 + 2] = -delta;
            }
            for (int i = 0; i < sampleWeights.Length; i++)
            {
                sampleWeights[i] = sampleWeights[i] / totalWeights;
            }
            weightsParameter.SetValue(sampleWeights);
            offsetsParameter.SetValue(sampleOffsets);
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
                    new BloomSettings("Default", 0.95f, 1f, 2f, 1f, 1f, 1f),
                    new BloomSettings("Intense", 0.9f, 1f, 3f, 1f, 1f, 1f),
                    new BloomSettings("Soft", 0f, 3f, 1f, 1f, 1f, 1f),
                    new BloomSettings("Desaturated", 0.5f, 8f, 2f, 1f, 0f, 1f),
                    new BloomSettings("Saturated", 0.25f, 4f, 2f, 1f, 2f, 0f),
                    new BloomSettings("Blurry", 0f, 2f, 1f, 0.1f, 1f, 1f),
                    new BloomSettings("Subtle", 0.5f, 2f, 1f, 1f, 1f, 1f)
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
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~BloomComponent() { Destroy(); }

        void Destroy()
        {
            resolveTarget?.Dispose(ref resolveTarget);
            renderTarget1?.Dispose(ref renderTarget1);
            renderTarget2?.Dispose(ref renderTarget2);
        }
    }
}