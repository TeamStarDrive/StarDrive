using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Gameplay;
using System;

namespace Ship_Game
{
	public class BloomComponent : IDisposable
	{
		private Effect bloomExtractEffect;

		private Effect bloomCombineEffect;

		private Effect gaussianBlurEffect;

		private ResolveTexture2D resolveTarget;

		private RenderTarget2D renderTarget1;

		private RenderTarget2D renderTarget2;

		private BloomSettings settings = BloomSettings.PresetSettings[0];

		private BloomComponent.IntermediateBuffer showBuffer = BloomComponent.IntermediateBuffer.FinalResult;

		private Ship_Game.ScreenManager ScreenManager;

		private Microsoft.Xna.Framework.Graphics.GraphicsDevice GraphicsDevice;

		private DepthStencilBuffer buffer;

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

		public BloomSettings Settings
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

		public BloomComponent.IntermediateBuffer ShowBuffer
		{
			get
			{
				return this.showBuffer;
			}
			set
			{
				this.showBuffer = value;
			}
		}

		public BloomComponent(Ship_Game.ScreenManager screenManager)
		{
			this.ScreenManager = screenManager;
			this.GraphicsDevice = screenManager.GraphicsDevice;
		}

		public static bool CheckTextureSize(int width, int height, out int newwidth, out int newheight)
		{
			bool retval = false;
			GraphicsDeviceCapabilities Caps = GraphicsAdapter.DefaultAdapter.GetCapabilities(DeviceType.Hardware);
			if (Caps.TextureCapabilities.RequiresPower2)
			{
				retval = true;
				double exp = Math.Ceiling(Math.Log((double)width) / Math.Log(2));
				width = (int)Math.Pow(2, exp);
				exp = Math.Ceiling(Math.Log((double)height) / Math.Log(2));
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
			float theta = this.Settings.BlurAmount;
			return (float)(1 / Math.Sqrt(6.28318530717959 * (double)theta) * Math.Exp((double)(-(n * n) / (2f * theta * theta))));
		}

		public static DepthStencilBuffer CreateDepthStencil(RenderTarget2D target)
		{
			return new DepthStencilBuffer(target.GraphicsDevice, target.Width, target.Height, target.GraphicsDevice.DepthStencilBuffer.Format, target.MultiSampleType, target.MultiSampleQuality);
		}

		public static DepthStencilBuffer CreateDepthStencil(RenderTarget2D target, DepthFormat depth)
		{
			if (!GraphicsAdapter.DefaultAdapter.CheckDepthStencilMatch(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format, target.Format, depth))
			{
				return BloomComponent.CreateDepthStencil(target);
			}
			return new DepthStencilBuffer(target.GraphicsDevice, target.Width, target.Height, depth, target.MultiSampleType, target.MultiSampleQuality);
		}

		public static RenderTarget2D CreateRenderTarget(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, int numberLevels, SurfaceFormat surface)
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
			BloomComponent.CheckTextureSize(device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight, out width, out height);
			return new RenderTarget2D(device, width, height, numberLevels, surface, type, 0);
		}

		public void Draw(GameTime gameTime)
		{
			this.GraphicsDevice.ResolveBackBuffer(this.resolveTarget);
			this.bloomExtractEffect.Parameters["BloomThreshold"].SetValue(this.Settings.BloomThreshold);
			this.DrawFullscreenQuad(this.resolveTarget, this.renderTarget1, this.bloomExtractEffect, BloomComponent.IntermediateBuffer.PreBloom);
			this.SetBlurEffectParameters(1f / (float)this.renderTarget1.Width, 0f);
			this.DrawFullscreenQuad(this.renderTarget1.GetTexture(), this.renderTarget2, this.gaussianBlurEffect, BloomComponent.IntermediateBuffer.BlurredHorizontally);
			this.SetBlurEffectParameters(0f, 1f / (float)this.renderTarget1.Height);
			this.DrawFullscreenQuad(this.renderTarget2.GetTexture(), this.renderTarget1, this.gaussianBlurEffect, BloomComponent.IntermediateBuffer.BlurredBothWays);
			this.GraphicsDevice.SetRenderTarget(0, null);
			EffectParameterCollection parameters = this.bloomCombineEffect.Parameters;
			parameters["BloomIntensity"].SetValue(this.Settings.BloomIntensity);
			parameters["BaseIntensity"].SetValue(this.Settings.BaseIntensity);
			parameters["BloomSaturation"].SetValue(this.Settings.BloomSaturation);
			parameters["BaseSaturation"].SetValue(this.Settings.BaseSaturation);
			this.GraphicsDevice.Textures[1] = this.resolveTarget;
			Viewport viewport = this.GraphicsDevice.Viewport;
			this.DrawFullscreenQuad(this.renderTarget1.GetTexture(), viewport.Width, viewport.Height, this.bloomCombineEffect, BloomComponent.IntermediateBuffer.FinalResult);
		}

		private void DrawFullscreenQuad(Texture2D texture, RenderTarget2D renderTarget, Effect effect, BloomComponent.IntermediateBuffer currentBuffer)
		{
			this.GraphicsDevice.SetRenderTarget(0, renderTarget);
			DepthStencilBuffer old = this.GraphicsDevice.DepthStencilBuffer;
			this.GraphicsDevice.DepthStencilBuffer = this.buffer;
			this.DrawFullscreenQuad(texture, renderTarget.Width, renderTarget.Height, effect, currentBuffer);
			this.GraphicsDevice.SetRenderTarget(0, null);
			this.GraphicsDevice.DepthStencilBuffer = old;
		}

		private void DrawFullscreenQuad(Texture2D texture, int width, int height, Effect effect, BloomComponent.IntermediateBuffer currentBuffer)
		{
			Ship.universeScreen.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.None, SpriteSortMode.Immediate, SaveStateMode.None);
			if (this.showBuffer >= currentBuffer)
			{
				effect.Begin();
				effect.CurrentTechnique.Passes[0].Begin();
			}
			Ship.universeScreen.ScreenManager.SpriteBatch.Draw(texture, new Rectangle(0, 0, width, height), Color.White);
			Ship.universeScreen.ScreenManager.SpriteBatch.End();
			if (this.showBuffer >= currentBuffer)
			{
				effect.CurrentTechnique.Passes[0].End();
				effect.End();
			}
		}

		public void LoadContent()
		{
			this.bloomExtractEffect = this.ScreenManager.Content.Load<Effect>("Effects/BloomExtract");
			this.bloomCombineEffect = this.ScreenManager.Content.Load<Effect>("Effects/BloomCombine");
			this.gaussianBlurEffect = this.ScreenManager.Content.Load<Effect>("Effects/GaussianBlur");
			PresentationParameters pp = this.GraphicsDevice.PresentationParameters;
			int width = pp.BackBufferWidth;
			int height = pp.BackBufferHeight;
			SurfaceFormat format = pp.BackBufferFormat;
			this.resolveTarget = new ResolveTexture2D(this.GraphicsDevice, width, height, 1, format);
			width = width / 2;
			height = height / 2;
			this.renderTarget1 = new RenderTarget2D(this.GraphicsDevice, width, height, 1, format);
			this.renderTarget2 = new RenderTarget2D(this.GraphicsDevice, width, height, 1, format);
			this.renderTarget1 = new RenderTarget2D(this.GraphicsDevice, width, height, 1, format);
			this.renderTarget2 = new RenderTarget2D(this.GraphicsDevice, width, height, 1, format);
			this.buffer = BloomComponent.CreateDepthStencil(this.renderTarget1);
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.resolveTarget != null)
                        this.resolveTarget.Dispose();
                    if (this.renderTarget1 != null)
                        this.renderTarget1.Dispose();
                    if (this.renderTarget2 != null)
                        this.renderTarget2.Dispose();
                }
                this.resolveTarget = null;
                this.renderTarget1 = null;
                this.renderTarget2 = null;
                this.disposed = true;
            }
        }
    }
}