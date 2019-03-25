using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game
{
	public sealed class BloomPostProcessor : BaseRenderTargetPostProcessor, IDisposable
	{
		private List<SurfaceFormat> supportedSourceFormats;

		private Effect bloomExtractEffect;

		private Effect bloomCombineEffect;

		private Effect gaussianBlurEffect;

		private RenderTarget2D renderTarget1;

		private RenderTarget2D renderTarget2;

		private SpriteBatch spriteRenderer;

	    public BloomSettings Settings { get; set; } = BloomSettings.PresetSettings[5];

	    public override SurfaceFormat[] SupportedSourceFormats => supportedSourceFormats.ToArray();

	    public override SurfaceFormat[] SupportedTargetFormats => supportedSourceFormats.ToArray();

	    public BloomPostProcessor(GraphicsDeviceManager deviceManager) : base(deviceManager)
		{
		}

		public override void ApplyPreferences(ILightingSystemPreferences preferences)
		{
		}

		private float ComputeGaussian(float n)
		{
			float theta = Settings.BlurAmount;
			return (float)(1 / Math.Sqrt(6.28318530717959 * theta) * Math.Exp(-(n * n) / (2f * theta * theta)));
		}

		private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, Effect effect)
		{
			GraphicsDevice device = GraphicsDeviceManager.GraphicsDevice;
			device.SetRenderTarget(0, renderTarget);
			DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect);
			device.SetRenderTarget(0, null);
		}

		private void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect)
		{
			spriteRenderer.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
			effect.Begin();
			effect.CurrentTechnique.Passes[0].Begin();
			spriteRenderer.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
			spriteRenderer.End();
			effect.CurrentTechnique.Passes[0].End();
			effect.End();
		}

		public override Texture2D EndFrameRendering(Texture2D mastersource, Texture2D lastprocessorsource)
		{
			Texture2D source = base.EndFrameRendering(mastersource, lastprocessorsource);
			Rectangle rectangle = new Rectangle(0, 0, source.Width, source.Height);
			GraphicsDevice device = GraphicsDeviceManager.GraphicsDevice;
			bloomExtractEffect.Parameters["BloomThreshold"].SetValue(Settings.BloomThreshold);
			DrawFullscreenQuad(source, renderTarget1, bloomExtractEffect);
			SetBlurEffectParameters(1f / renderTarget1.Width, 0f);
			DrawFullscreenQuad(renderTarget1.GetTexture(), renderTarget2, gaussianBlurEffect);
			SetBlurEffectParameters(0f, 1f / renderTarget1.Height);
			DrawFullscreenQuad(renderTarget2.GetTexture(), renderTarget1, gaussianBlurEffect);
			device.SetRenderTarget(0, null);
			EffectParameterCollection parameters = bloomCombineEffect.Parameters;
			parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
			parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
			parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
			parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);
			device.Textures[1] = source;
			Viewport viewport = StarDriveGame.Instance.Viewport;
			DrawFullscreenQuad(renderTarget1.GetTexture(), viewport.Width, viewport.Height, bloomCombineEffect);
			return source;
		}

		public override bool Initialize(List<SurfaceFormat> availableformats)
		{
			supportedSourceFormats = availableformats;
			return base.Initialize(availableformats);
		}

		public void LoadContent(GameContentManager manager)
		{
			spriteRenderer = new SpriteBatch(GraphicsDeviceManager.GraphicsDevice);
			bloomExtractEffect = manager.Load<Effect>("Effects/BloomExtract");
			bloomCombineEffect = manager.Load<Effect>("Effects/BloomCombine");
			gaussianBlurEffect = manager.Load<Effect>("Effects/GaussianBlur");
			int width = GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2;
			int height = GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2;
			renderTarget1 = new RenderTarget2D(GraphicsDeviceManager.GraphicsDevice, width, height, 1, GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferFormat);
			renderTarget2 = new RenderTarget2D(GraphicsDeviceManager.GraphicsDevice, width, height, 1, GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferFormat);
		}

		private void SetBlurEffectParameters(float dx, float dy)
		{
			EffectParameter weightsParameter = gaussianBlurEffect.Parameters["SampleWeights"];
			EffectParameter offsetsParameter = gaussianBlurEffect.Parameters["SampleOffsets"];
			int sampleCount = weightsParameter.Elements.Count;
			float[] sampleWeights = new float[sampleCount];
			Vector2[] sampleOffsets = new Vector2[sampleCount];
			sampleWeights[0] = ComputeGaussian(0f);
			sampleOffsets[0] = new Vector2(0f);
			float totalWeights = sampleWeights[0];
			for (int i = 0; i < sampleCount / 2; i++)
			{
				float weight = ComputeGaussian(i + 1);
				sampleWeights[i * 2 + 1] = weight;
				sampleWeights[i * 2 + 2] = weight;
				totalWeights = totalWeights + weight * 2f;
				float sampleOffset = i * 2 + 1.5f;
				Vector2 delta = new Vector2(dx, dy) * sampleOffset;
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

		public void UnloadContent()
		{
			renderTarget1.Dispose();
			renderTarget2.Dispose();
		}

		public class BloomSettings
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
				BloomSettings[] bloomSetting = { new BloomSettings("Default", 0.25f, 4f, 1.25f, 1f, 1f, 1f), new BloomSettings("Soft", 0f, 3f, 1f, 1f, 1f, 1f), new BloomSettings("Desaturated", 0.5f, 8f, 2f, 1f, 0f, 1f), new BloomSettings("Saturated", 0.25f, 4f, 2f, 1f, 2f, 0f), new BloomSettings("Blurry", 0f, 2f, 1f, 0.1f, 1f, 1f), new BloomSettings("Subtle", 0.5f, 2f, 1f, 1f, 1f, 1f) };
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BloomPostProcessor() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            spriteRenderer?.Dispose(ref spriteRenderer);
            renderTarget1?.Dispose(ref renderTarget1);
            renderTarget2?.Dispose(ref renderTarget2);
        }
	}
}