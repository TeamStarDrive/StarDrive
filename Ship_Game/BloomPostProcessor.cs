using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class BloomPostProcessor : BaseRenderTargetPostProcessor, IDisposable
	{
		private List<SurfaceFormat> supportedSourceFormats;

		private Effect bloomExtractEffect;

		private Effect bloomCombineEffect;

		private Effect gaussianBlurEffect;

		private RenderTarget2D renderTarget1;

		private RenderTarget2D renderTarget2;

		private SpriteBatch spriteRenderer;

		private BloomPostProcessor.BloomSettings settings = BloomPostProcessor.BloomSettings.PresetSettings[5];

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public BloomPostProcessor.BloomSettings Settings
		{
			get
			{
				return this.settings;
			}
			set
			{
				this.settings = value;
			}
		}

		public override SurfaceFormat[] SupportedSourceFormats
		{
			get
			{
				return this.supportedSourceFormats.ToArray();
			}
		}

		public override SurfaceFormat[] SupportedTargetFormats
		{
			get
			{
				return this.supportedSourceFormats.ToArray();
			}
		}

		public BloomPostProcessor(Microsoft.Xna.Framework.GraphicsDeviceManager deviceManager) : base(deviceManager)
		{
		}

		public override void ApplyPreferences(ILightingSystemPreferences preferences)
		{
		}

		private float ComputeGaussian(float n)
		{
			float theta = this.Settings.BlurAmount;
			return (float)(1 / Math.Sqrt(6.28318530717959 * (double)theta) * Math.Exp((double)(-(n * n) / (2f * theta * theta))));
		}

		private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, Effect effect)
		{
			GraphicsDevice device = base.GraphicsDeviceManager.GraphicsDevice;
			device.SetRenderTarget(0, renderTarget);
			this.DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect);
			device.SetRenderTarget(0, null);
		}

		private void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect)
		{
			this.spriteRenderer.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
			effect.Begin();
			effect.CurrentTechnique.Passes[0].Begin();
			this.spriteRenderer.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
			this.spriteRenderer.End();
			effect.CurrentTechnique.Passes[0].End();
			effect.End();
		}

		public override Texture2D EndFrameRendering(Texture2D mastersource, Texture2D lastprocessorsource)
		{
			Texture2D source = base.EndFrameRendering(mastersource, lastprocessorsource);
			Rectangle rectangle = new Rectangle(0, 0, source.Width, source.Height);
			GraphicsDevice device = base.GraphicsDeviceManager.GraphicsDevice;
			this.bloomExtractEffect.Parameters["BloomThreshold"].SetValue(this.Settings.BloomThreshold);
			this.DrawFullscreenQuad(source, this.renderTarget1, this.bloomExtractEffect);
			this.SetBlurEffectParameters(1f / (float)this.renderTarget1.Width, 0f);
			this.DrawFullscreenQuad(this.renderTarget1.GetTexture(), this.renderTarget2, this.gaussianBlurEffect);
			this.SetBlurEffectParameters(0f, 1f / (float)this.renderTarget1.Height);
			this.DrawFullscreenQuad(this.renderTarget2.GetTexture(), this.renderTarget1, this.gaussianBlurEffect);
			device.SetRenderTarget(0, null);
			EffectParameterCollection parameters = this.bloomCombineEffect.Parameters;
			parameters["BloomIntensity"].SetValue(this.Settings.BloomIntensity);
			parameters["BaseIntensity"].SetValue(this.Settings.BaseIntensity);
			parameters["BloomSaturation"].SetValue(this.Settings.BloomSaturation);
			parameters["BaseSaturation"].SetValue(this.Settings.BaseSaturation);
			device.Textures[1] = source;
			Viewport viewport = device.Viewport;
			this.DrawFullscreenQuad(this.renderTarget1.GetTexture(), viewport.Width, viewport.Height, this.bloomCombineEffect);
			return source;
		}

		public override bool Initialize(List<SurfaceFormat> availableformats)
		{
			this.supportedSourceFormats = availableformats;
			return base.Initialize(availableformats);
		}

		public void LoadContent(ContentManager manager)
		{
			this.spriteRenderer = new SpriteBatch(base.GraphicsDeviceManager.GraphicsDevice);
			this.bloomExtractEffect = manager.Load<Effect>("Effects/BloomExtract");
			this.bloomCombineEffect = manager.Load<Effect>("Effects/BloomCombine");
			this.gaussianBlurEffect = manager.Load<Effect>("Effects/GaussianBlur");
			int width = base.GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2;
			int height = base.GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2;
			this.renderTarget1 = new RenderTarget2D(base.GraphicsDeviceManager.GraphicsDevice, width, height, 1, base.GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferFormat);
			this.renderTarget2 = new RenderTarget2D(base.GraphicsDeviceManager.GraphicsDevice, width, height, 1, base.GraphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferFormat);
		}

		private void SetBlurEffectParameters(float dx, float dy)
		{
			EffectParameter weightsParameter = this.gaussianBlurEffect.Parameters["SampleWeights"];
			EffectParameter offsetsParameter = this.gaussianBlurEffect.Parameters["SampleOffsets"];
			int sampleCount = weightsParameter.Elements.Count;
			float[] sampleWeights = new float[sampleCount];
			Vector2[] sampleOffsets = new Vector2[sampleCount];
			sampleWeights[0] = this.ComputeGaussian(0f);
			sampleOffsets[0] = new Vector2(0f);
			float totalWeights = sampleWeights[0];
			for (int i = 0; i < sampleCount / 2; i++)
			{
				float weight = this.ComputeGaussian((float)(i + 1));
				sampleWeights[i * 2 + 1] = weight;
				sampleWeights[i * 2 + 2] = weight;
				totalWeights = totalWeights + weight * 2f;
				float sampleOffset = (float)(i * 2) + 1.5f;
				Vector2 delta = new Vector2(dx, dy) * sampleOffset;
				sampleOffsets[i * 2 + 1] = delta;
				sampleOffsets[i * 2 + 2] = -delta;
			}
			for (int i = 0; i < (int)sampleWeights.Length; i++)
			{
				sampleWeights[i] = sampleWeights[i] / totalWeights;
			}
			weightsParameter.SetValue(sampleWeights);
			offsetsParameter.SetValue(sampleOffsets);
		}

		public void UnloadContent()
		{
			this.renderTarget1.Dispose();
			this.renderTarget2.Dispose();
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

			public static BloomPostProcessor.BloomSettings[] PresetSettings;

			static BloomSettings()
			{
				BloomPostProcessor.BloomSettings[] bloomSetting = new BloomPostProcessor.BloomSettings[] { new BloomPostProcessor.BloomSettings("Default", 0.25f, 4f, 1.25f, 1f, 1f, 1f), new BloomPostProcessor.BloomSettings("Soft", 0f, 3f, 1f, 1f, 1f, 1f), new BloomPostProcessor.BloomSettings("Desaturated", 0.5f, 8f, 2f, 1f, 0f, 1f), new BloomPostProcessor.BloomSettings("Saturated", 0.25f, 4f, 2f, 1f, 2f, 0f), new BloomPostProcessor.BloomSettings("Blurry", 0f, 2f, 1f, 0.1f, 1f, 1f), new BloomPostProcessor.BloomSettings("Subtle", 0.5f, 2f, 1f, 1f, 1f, 1f) };
				BloomPostProcessor.BloomSettings.PresetSettings = bloomSetting;
			}

			public BloomSettings(string name, float bloomThreshold, float blurAmount, float bloomIntensity, float baseIntensity, float bloomSaturation, float baseSaturation)
			{
				this.Name = name;
				this.BloomThreshold = bloomThreshold;
				this.BlurAmount = blurAmount;
				this.BloomIntensity = bloomIntensity;
				this.BaseIntensity = baseIntensity;
				this.BloomSaturation = bloomSaturation;
				this.BaseSaturation = baseSaturation;
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.spriteRenderer != null)
                        this.spriteRenderer.Dispose();
                    if (this.renderTarget1 != null)
                        this.renderTarget1.Dispose();
                    if (this.renderTarget2 != null)
                        this.renderTarget2.Dispose();
                }
                this.renderTarget2 = null;
                this.spriteRenderer = null;
                this.renderTarget1 = null;
                this.disposed = true;
            }
        }
	}
}