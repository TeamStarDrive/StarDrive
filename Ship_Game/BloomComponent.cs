using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class BloomComponent : IDisposable
	{
		private Effect bloomExtractEffect;

		private Effect bloomCombineEffect;

		private Effect gaussianBlurEffect;

		private ResolveTexture2D resolveTarget;

		private RenderTarget2D renderTarget1;

		private RenderTarget2D renderTarget2;

	    private ScreenManager ScreenManager;

		private GraphicsDevice GraphicsDevice;

		private DepthStencilBuffer buffer;


		public BloomSettings Settings { get; set; } = BloomSettings.PresetSettings[0];

	    public IntermediateBuffer ShowBuffer { get; set; } = IntermediateBuffer.FinalResult;

	    public BloomComponent(ScreenManager screenManager)
		{
			ScreenManager = screenManager;
			GraphicsDevice = screenManager.GraphicsDevice;
		}

		public static bool CheckTextureSize(int width, int height, out int newwidth, out int newheight)
		{
			bool retval = false;
			GraphicsDeviceCapabilities Caps = GraphicsAdapter.DefaultAdapter.GetCapabilities(DeviceType.Hardware);
			if (Caps.TextureCapabilities.RequiresPower2)
			{
				retval = true;
				double exp = Math.Ceiling(Math.Log(width) / Math.Log(2));
				width = (int)Math.Pow(2, exp);
				exp = Math.Ceiling(Math.Log(height) / Math.Log(2));
				height = (int)Math.Pow(2, exp);
			}
			if (Caps.TextureCapabilities.RequiresSquareOnly)
			{
				retval = true;
				width = Math.Max(width, height);
				height = width;
			}
			newwidth = Math.Min(Caps.MaxTextureWidth, width);
			newheight = Math.Min(Caps.MaxTextureHeight, height);
			return retval;
		}

		private float ComputeGaussian(float n)
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

		public static RenderTarget2D CreateRenderTarget(GraphicsDevice device, int numberLevels, SurfaceFormat surface)
		{
			int width;
			int height;
			MultiSampleType type = device.PresentationParameters.MultiSampleType;
			if (!GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, surface))
			{
				surface = device.DisplayMode.Format;
			}
			else if (!GraphicsAdapter.DefaultAdapter.CheckDeviceMultiSampleType(DeviceType.Hardware, surface, device.PresentationParameters.IsFullScreen, type))
			{
				type = MultiSampleType.None;
			}
			CheckTextureSize(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight, out width, out height);
			return new RenderTarget2D(device, width, height, numberLevels, surface, type, 0);
		}

		public void Draw(GameTime gameTime)
		{
			GraphicsDevice.ResolveBackBuffer(resolveTarget);
			bloomExtractEffect.Parameters["BloomThreshold"].SetValue(Settings.BloomThreshold);
			DrawFullscreenQuad(resolveTarget, renderTarget1, bloomExtractEffect, IntermediateBuffer.PreBloom);
			SetBlurEffectParameters(1f / renderTarget1.Width, 0f);
			DrawFullscreenQuad(renderTarget1.GetTexture(), renderTarget2, gaussianBlurEffect, IntermediateBuffer.BlurredHorizontally);
			SetBlurEffectParameters(0f, 1f / renderTarget1.Height);
			DrawFullscreenQuad(renderTarget2.GetTexture(), renderTarget1, gaussianBlurEffect, IntermediateBuffer.BlurredBothWays);
			GraphicsDevice.SetRenderTarget(0, null);
			EffectParameterCollection parameters = bloomCombineEffect.Parameters;
			parameters["BloomIntensity"].SetValue(Settings.BloomIntensity);
			parameters["BaseIntensity"].SetValue(Settings.BaseIntensity);
			parameters["BloomSaturation"].SetValue(Settings.BloomSaturation);
			parameters["BaseSaturation"].SetValue(Settings.BaseSaturation);
			GraphicsDevice.Textures[1] = resolveTarget;
			Viewport viewport = GameBase.Viewport;
			DrawFullscreenQuad(renderTarget1.GetTexture(), viewport.Width, viewport.Height, bloomCombineEffect, IntermediateBuffer.FinalResult);
		}

		private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, Effect effect, IntermediateBuffer currentBuffer)
		{
			GraphicsDevice.SetRenderTarget(0, renderTarget);
			DepthStencilBuffer old = GraphicsDevice.DepthStencilBuffer;
			GraphicsDevice.DepthStencilBuffer = buffer;
			DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect, currentBuffer);
			GraphicsDevice.SetRenderTarget(0, null);
			GraphicsDevice.DepthStencilBuffer = old;
		}

		private void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect, IntermediateBuffer currentBuffer)
		{
			Empire.Universe.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
			if (ShowBuffer >= currentBuffer)
			{
				effect.Begin();
				effect.CurrentTechnique.Passes[0].Begin();
			}
			Empire.Universe.ScreenManager.SpriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
			Empire.Universe.ScreenManager.SpriteBatch.End();
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
			PresentationParameters pp = GraphicsDevice.PresentationParameters;
			int width = pp.BackBufferWidth;
			int height = pp.BackBufferHeight;
			SurfaceFormat format = pp.BackBufferFormat;
			resolveTarget = new ResolveTexture2D(GraphicsDevice, width, height, 1, format);
			width = width / 2;
			height = height / 2;
			renderTarget1 = new RenderTarget2D(GraphicsDevice, width, height, 1, format);
			renderTarget2 = new RenderTarget2D(GraphicsDevice, width, height, 1, format);
			renderTarget1 = new RenderTarget2D(GraphicsDevice, width, height, 1, format);
			renderTarget2 = new RenderTarget2D(GraphicsDevice, width, height, 1, format);
			buffer = CreateDepthStencil(renderTarget1);
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

		public enum IntermediateBuffer
		{
			PreBloom,
			BlurredHorizontally,
			BlurredBothWays,
			FinalResult
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~BloomComponent() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            resolveTarget?.Dispose(ref resolveTarget);
            renderTarget1?.Dispose(ref renderTarget1);
            renderTarget2?.Dispose(ref renderTarget2);
        }
    }
}