using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NAudio.Wave;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using SgMotion;
using SgMotion.Controllers;

namespace Ship_Game
{
	public sealed class MainMenuScreen : GameScreen, IDisposable
	{
		public static readonly string Version = "BlackBox " + GlobalStats.ExtendedVersion;
		private IWavePlayer waveOut;
		private Mp3FileReader mp3FileReader;
		public BatchRemovalCollection<Comet> CometList = new BatchRemovalCollection<MainMenuScreen.Comet>();
		private Rectangle StarFieldRect = new Rectangle(0, 0, 1920, 1080);
		private Texture2D StarField;
		private readonly List<Texture2D> LogoAnimation = new List<Texture2D>();

		private UIButton PlayGame;
        private UIButton Load;
        private UIButton Adventure;
        private UIButton Tutorials;
        private UIButton Mods;
		private UIButton Options;
		private UIButton Exit;

		private SceneObject planetSO;
		private Vector2 MoonPosition;
        private Vector3 MoonRotation = new Vector3(-79.5f, -19f, 93.5f);
        private Rectangle Portrait;
		private float zshift = 0.0f;    //was uninitialized, set to float default
		private float scale = 0.7f;

		private Matrix view;
		private Matrix projection;

		private SceneObject shipSO;
        private AnimationController ShipAnim;
        private MouseState currentMouse;
		private MouseState previousMouse;

		private Vector2 ShipPosition = new Vector2(5400f, -2000f);
        private Vector3 ShipRotation = new Vector3(-137f, -190f, -20f);
        private Vector2 FlareAdd = new Vector2(0f, 0f);
		private Rectangle LogoRect = new Rectangle(256, 256, 512, 128);
		private float rotate = 3.85f;

		private int AnimationFrame;
		private bool flip;
		private bool StayOn;
		private int flareFrames;
        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;

        private Texture2D TexComet = ResourceManager.TextureDict["GameScreens/comet"];

		public MainMenuScreen()
		{
            TransitionOnTime  = TimeSpan.FromSeconds(1);
			TransitionOffTime = TimeSpan.FromSeconds(0.5);
        }

		public override void Draw(GameTime gameTime)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			MainMenuScreen mainMenuScreen = this;
			mainMenuScreen.rotate = mainMenuScreen.rotate + elapsedTime / 350f;
			if (RandomMath.RandomBetween(0f, 100f) > 99.75)
			{
				Comet c = new Comet()
				{
					Position = new Vector2(RandomMath.RandomBetween(-100f, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100), 0f),
					Velocity = new Vector2(RandomMath.RandomBetween(-1f, 1f), 1f)
				};
				c.Velocity = Vector2.Normalize(c.Velocity);
				c.Rotation = c.Position.RadiansToTarget(c.Position + c.Velocity);
				this.CometList.Add(c);
			}
			Vector2 cometOrigin = new Vector2(TexComet.Width, TexComet.Height) / 2f;
			if (SplashScreen.DisplayComplete )
			{
				base.ScreenManager.splashScreenGameComponent.Visible = false;
				base.ScreenManager.sceneState.BeginFrameRendering(this.view, this.projection, gameTime, base.ScreenManager.environment, true);
				base.ScreenManager.editor.BeginFrameRendering(base.ScreenManager.sceneState);
                try
                {
                    base.ScreenManager.inter.BeginFrameRendering(base.ScreenManager.sceneState);
                }
                catch { }
				this.DrawNew(gameTime);
				base.ScreenManager.inter.RenderManager.Render();
				base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
				base.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;
				base.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
				base.ScreenManager.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
				Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 mp = viewport.Project(this.planetSO.WorldBoundingSphere.Center, this.projection, this.view, Matrix.Identity);
				Vector2 MoonFlarePos = new Vector2(mp.X - 40f - 2f, mp.Y - 40f + 24f) + this.FlareAdd;
				Vector2 Origin = new Vector2(184f, 184f);
				Rectangle? nullable = null;
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_flare"], MoonFlarePos, nullable, Color.White, 0f, Origin, 0.95f, SpriteEffects.None, 1f);
				base.ScreenManager.SpriteBatch.End();
				base.ScreenManager.SpriteBatch.Begin();
				if (this.AnimationFrame >= 41 && this.AnimationFrame < 52)
				{
					float alphaStep = (float)(255 / 12);
					float Alpha = (float)(this.AnimationFrame - 41) * alphaStep;
					if (Alpha > 220f)
					{
						Alpha = 220f;
					}
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X - 220, (int)MoonFlarePos.Y - 130, 201, 78);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_1"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame >= 52 && this.AnimationFrame <= 67)
				{
					float Alpha = 220f;
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X - 220, (int)MoonFlarePos.Y - 130, 201, 78);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_1"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame > 67 && this.AnimationFrame <= 95)
				{
					float alphaStep = (float)(255 / 28);
					float Alpha = 255f - (float)(this.AnimationFrame - 67) * alphaStep;
					if (Alpha < 0f)
					{
						Alpha = 0f;
					}
					if (Alpha > 220f)
					{
						Alpha = 220f;
					}
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X - 220, (int)MoonFlarePos.Y - 130, 201, 78);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_1"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame >= 161 && this.AnimationFrame < 172)
				{
					float alphaStep = (float)(255 / 12);
					float Alpha = (float)(this.AnimationFrame - 161) * alphaStep;
					if (Alpha > 220f)
					{
						Alpha = 220f;
					}
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X - 250, (int)MoonFlarePos.Y + 60, 254, 82);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_2"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame >= 172 && this.AnimationFrame <= 187)
				{
					float Alpha = 220f;
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X - 250, (int)MoonFlarePos.Y + 60, 254, 82);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_2"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame > 187 && this.AnimationFrame <= 215)
				{
					float alphaStep = (float)(255 / 28);
					float Alpha = 255f - (float)(this.AnimationFrame - 187) * alphaStep;
					if (Alpha < 0f)
					{
						Alpha = 0f;
					}
					if (Alpha > 220f)
					{
						Alpha = 220f;
					}
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X - 250, (int)MoonFlarePos.Y + 60, 254, 82);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_2"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame >= 232 && this.AnimationFrame < 243)
				{
					float alphaStep = (float)(255 / 12);
					float Alpha = (float)(this.AnimationFrame - 232) * alphaStep;
					if (Alpha > 220f)
					{
						Alpha = 220f;
					}
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X + 60, (int)MoonFlarePos.Y + 80, 156, 93);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_3"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame >= 243 && this.AnimationFrame <= 258)
				{
					float Alpha = 220f;
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X + 60, (int)MoonFlarePos.Y + 80, 156, 93);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_3"], moon1, new Color(Color.White, (byte)Alpha));
				}
				if (this.AnimationFrame > 258 && this.AnimationFrame <= 286)
				{
					float alphaStep = (float)(255 / 28);
					float Alpha = 255f - (float)(this.AnimationFrame - 258) * alphaStep;
					if (Alpha < 0f)
					{
						Alpha = 0f;
					}
					if (Alpha > 220f)
					{
						Alpha = 220f;
					}
					Rectangle moon1 = new Rectangle((int)MoonFlarePos.X + 60, (int)MoonFlarePos.Y + 80, 156, 93);
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/moon_3"], moon1, new Color(Color.White, (byte)Alpha));
				}
				base.ScreenManager.SpriteBatch.End();
				base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.Additive);
				foreach (MainMenuScreen.Comet c in this.CometList)
				{
					float Alpha = 255f;
					if (c.Position.Y > 100f)
					{
						Alpha = 25500f / c.Position.Y;
						if (Alpha > 255f)
						{
							Alpha = 255f;
						}
					}
					Rectangle? nullable1 = null;
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["GameScreens/comet2"], c.Position, nullable1, new Color(255, 255, 255, (byte)Alpha), c.Rotation, cometOrigin, 0.45f, SpriteEffects.None, 1f);
					MainMenuScreen.Comet position = c;
					position.Position = position.Position + ((c.Velocity * 2400f) * elapsedTime);
					if (c.Position.Y <= 1050f)
					{
						continue;
					}
					this.CometList.QueuePendingRemoval(c);
				}
				this.CometList.ApplyPendingRemovals();
				base.ScreenManager.SpriteBatch.End();
				base.ScreenManager.SpriteBatch.Begin();
				int numEntries = 5;
				int k = 5;
				foreach (UIButton b in this.Buttons)
				{
					Rectangle r = b.Rect;
					float transitionOffset = MathHelper.Clamp((base.TransitionPosition - 0.5f * (float)k / (float)numEntries) / 0.5f, 0f, 1f);
					k--;
					if (base.ScreenState != Ship_Game.ScreenState.TransitionOn)
					{
						r.X = r.X + (int)transitionOffset * 512;
					}
					else
					{
						r.X = r.X + (int)(transitionOffset * 512f);
						if (transitionOffset == 0f)
						{
							AudioManager.PlayCue("blip_click");
						}
					}
					b.Draw(base.ScreenManager.SpriteBatch, r);
				}
				if (GlobalStats.ActiveMod != null)
				{
					base.ScreenManager.SpriteBatch.Draw(GlobalStats.ActiveMod.MainMenuTex, this.Portrait, Color.White);
				}
				base.ScreenManager.SpriteBatch.Draw(this.LogoAnimation[0], this.LogoRect, Color.White);
				if (this.LogoAnimation.Count > 1)
				{
					this.LogoAnimation.RemoveAt(0);
				}
				base.ScreenManager.SpriteBatch.End();
				base.ScreenManager.inter.EndFrameRendering();
				base.ScreenManager.editor.EndFrameRendering();
				base.ScreenManager.sceneState.EndFrameRendering();
			}
		}

		public void DrawNew(GameTime gameTime)
		{
			if (!this.flip)
			{
				this.flip = true;
			}
			else
			{
				MainMenuScreen animationFrame = this;
				animationFrame.AnimationFrame = animationFrame.AnimationFrame + 1;
				this.flip = false;
			}

            // @todo What the hell is this bloody thing?? REFACTOR
			double totalSeconds = gameTime.ElapsedGameTime.TotalSeconds;
			base.ScreenManager.SpriteBatch.Begin();
			Rectangle screenRect = new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
			base.ScreenManager.SpriteBatch.Draw(this.StarField, this.StarFieldRect, Color.White);
			Rectangle PlanetRect = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 680, 1016, 680);
			base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet"], PlanetRect, Color.White);
			if (this.AnimationFrame >= 127 && this.AnimationFrame < 145)
			{
				float alphaStep = (float)(255 / 18);
				float Alpha = (float)(this.AnimationFrame - 127) * alphaStep;
				Rectangle PlanetGridRect = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 640, 972, 640);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid"], PlanetGridRect, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame >= 145 && this.AnimationFrame <= 148)
			{
				Rectangle PlanetGridRect = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 640, 972, 640);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid"], PlanetGridRect, Color.White);
			}
			if (this.AnimationFrame > 148 && this.AnimationFrame <= 180)
			{
				float alphaStep = (float)(255 / 31);
				float Alpha = 255f - (float)(this.AnimationFrame - 148) * alphaStep;
				if (Alpha < 0f)
				{
					Alpha = 0f;
				}
				Rectangle PlanetGridRect = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 640, 972, 640);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid"], PlanetGridRect, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame >= 141 && this.AnimationFrame <= 149)
			{
				float alphaStep = (float)(255 / 9);
				float Alpha = (float)(this.AnimationFrame - 141) * alphaStep;
				Rectangle Grid1Hex = new Rectangle(277, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 592, 77, 33);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid_hex_1"], Grid1Hex, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame > 149 && this.AnimationFrame <= 165)
			{
				float alphaStep = (float)(255 / 16);
				float Alpha = 255f - (float)(this.AnimationFrame - 149) * alphaStep;
				Rectangle Grid1Hex = new Rectangle(277, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 592, 77, 33);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid_hex_1"], Grid1Hex, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame >= 159 && this.AnimationFrame <= 168)
			{
				float alphaStep = (float)(255 / 10);
				float Alpha = (float)(this.AnimationFrame - 159) * alphaStep;
				Rectangle Grid1Hex = new Rectangle(392, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 418, 79, 60);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid_hex_2"], Grid1Hex, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame > 168 && this.AnimationFrame <= 183)
			{
				float alphaStep = (float)(255 / 15);
				float Alpha = 255f - (float)(this.AnimationFrame - 168) * alphaStep;
				Rectangle Grid1Hex = new Rectangle(392, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 418, 79, 60);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid_hex_2"], Grid1Hex, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame >= 150 && this.AnimationFrame <= 158)
			{
				float alphaStep = (float)(255 / 9);
				float Alpha = (float)(this.AnimationFrame - 150) * alphaStep;
				Rectangle Grid1Hex = new Rectangle(682, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 295, 63, 67);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid_hex_3"], Grid1Hex, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame > 158 && this.AnimationFrame <= 174)
			{
				float alphaStep = (float)(255 / 16);
				float Alpha = 255f - (float)(this.AnimationFrame - 158) * alphaStep;
				Rectangle Grid1Hex = new Rectangle(682, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 295, 63, 67);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_grid_hex_3"], Grid1Hex, new Color(Color.White, (byte)Alpha));
			}
			if (this.AnimationFrame >= 7 || this.StayOn)
			{
				float alphaStep = (float)(255 / 30);
				float Alpha = (float)(this.AnimationFrame - 7) * alphaStep;
				Alpha = MathHelper.SmoothStep((float)(this.AnimationFrame - 1 - 7) * alphaStep, (float)(this.AnimationFrame - 7) * alphaStep, 0.9f);
				if (Alpha > 225f || this.StayOn)
				{
					Alpha = 225f;
					this.StayOn = true;
				}
				Rectangle CornerTL = new Rectangle(31, 30, 608, 340);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/corner_TL"], CornerTL, new Color(Color.White, (byte)Alpha));
				Rectangle CornerBR = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 551, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 562, 520, 532);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/corner_BR"], CornerBR, new Color(Color.White, (byte)Alpha));
				Rectangle Version = new Rectangle(205, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 37, 318, 12);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/version_bar"], Version, new Color(Color.White, (byte)Alpha));
				Vector2 TextPos = new Vector2(20f, (float)(Version.Y + 6 - Fonts.Pirulen12.LineSpacing / 2 - 1));
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, string.Concat("StarDrive"," 15B"), TextPos, Color.White);

                 Version = new Rectangle(20+ (int)Fonts.Pirulen12.MeasureString(MainMenuScreen.Version).X , base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 85, 318, 12);
                base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/version_bar"], Version, new Color(Color.White, (byte)Alpha));
                 TextPos = new Vector2(20f, (float)(Version.Y  +6 - Fonts.Pirulen12.LineSpacing / 2 - 1));
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, string.Concat(MainMenuScreen.Version), TextPos, Color.White);

				if (GlobalStats.ActiveModInfo != null)
                {
                    string title = GlobalStats.ActiveModInfo.ModName;
                    //if (GlobalStats.ActiveModInfo.Version != null && GlobalStats.ActiveModInfo.Version != "" && !title.Contains(GlobalStats.ActiveModInfo.Version))
                    if (!string.IsNullOrEmpty(GlobalStats.ActiveModInfo.Version) && !title.Contains(GlobalStats.ActiveModInfo.Version))
                        title = string.Concat(title, " - ", GlobalStats.ActiveModInfo.Version);
                    Version = new Rectangle(20 + (int)Fonts.Pirulen12.MeasureString(title).X, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 60, 318, 12);
                    base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/version_bar"], Version, new Color(Color.White, (byte)Alpha));
                    TextPos = new Vector2(20f, (float)(Version.Y + 6 - Fonts.Pirulen12.LineSpacing / 2 - 1));
                    base.ScreenManager.SpriteBatch.DrawString(Fonts.Pirulen12, title, TextPos, Color.White);
                }
			}
			if (this.AnimationFrame > 300)
			{
				this.AnimationFrame = 0;
			}
			base.ScreenManager.SpriteBatch.End();
			base.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;
			base.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
			base.ScreenManager.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
			base.ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
			base.ScreenManager.GraphicsDevice.RenderState.SourceBlend = Blend.InverseDestinationColor;
			base.ScreenManager.GraphicsDevice.RenderState.DestinationBlend = Blend.One;
			base.ScreenManager.GraphicsDevice.RenderState.BlendFunction = BlendFunction.Add;
			if (this.flareFrames >= 0 && this.flareFrames <= 31)
			{
				float alphaStep = 35f / 32f;
				float Alpha = 255f - (float)this.flareFrames * alphaStep;
				Rectangle SolarFlare = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 784, 1024, 784);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_solarflare"], SolarFlare, new Color((byte)Alpha, (byte)Alpha, (byte)Alpha, 255));
			}
			if (this.flareFrames > 31 && this.flareFrames <= 62)
			{
				float alphaStep = 35f / 31f;
				float Alpha = 220f + (float)(this.flareFrames - 31) * alphaStep;
				Rectangle SolarFlare = new Rectangle(0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight - 784, 1024, 784);
				base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/planet_solarflare"], SolarFlare, new Color((byte)Alpha, (byte)Alpha, (byte)Alpha, 255));
			}
			if (this.flip)
			{
				MainMenuScreen mainMenuScreen = this;
				mainMenuScreen.flareFrames = mainMenuScreen.flareFrames + 1;
			}
			if (this.flareFrames >= 62)
			{
				this.flareFrames = 0;
			}
			base.ScreenManager.SpriteBatch.End();
			base.ScreenManager.SpriteBatch.Begin();
			base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["MainMenu/vignette"], screenRect, Color.White);
			base.ScreenManager.SpriteBatch.End();
		}

		private void ExitMessageBoxAccepted(object sender, EventArgs e)
		{
			Game1.Instance.Exit();
		}

		public override void HandleInput(InputState input)
		{
            // Use these controls to reorient the ship and planet in the menu. The new rotation
            // is logged into debug console and can be set as default values later
        #if false
            if (input.CurrentKeyboardState.IsKeyDown(Keys.W)) ShipRotation.X += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.S)) ShipRotation.X -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.A)) ShipRotation.Y += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.D)) ShipRotation.Y -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Q)) ShipRotation.Z += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.E)) ShipRotation.Z -= 0.5f;

            if (input.CurrentKeyboardState.IsKeyDown(Keys.I)) MoonRotation.X += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.K)) MoonRotation.X -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.J)) MoonRotation.Y += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.L)) MoonRotation.Y -= 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.U)) MoonRotation.Z += 0.5f;
            if (input.CurrentKeyboardState.IsKeyDown(Keys.O)) MoonRotation.Z -= 0.5f;

            System.Diagnostics.Debug.WriteLine("rot {0}   {1}", ShipRotation, MoonRotation);
        #endif

            if (input.InGameSelect)
			{
				Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 nearPoint = viewport.Unproject(new Vector3(input.CursorPosition, 0f), this.projection, this.view, Matrix.Identity);
				Viewport viewport1 = base.ScreenManager.GraphicsDevice.Viewport;
				Vector3 farPoint = viewport1.Unproject(new Vector3(input.CursorPosition, 1f), this.projection, this.view, Matrix.Identity);
				Vector3 direction = farPoint - nearPoint;
				direction.Normalize();
				Ray pickRay = new Ray(nearPoint, direction);
				float k = -pickRay.Position.Z / pickRay.Direction.Z;
				Vector3 pickedPosition = new Vector3(pickRay.Position.X + k * pickRay.Direction.X, pickRay.Position.Y + k * pickRay.Direction.Y, 0f);
				if (Vector3.Distance(pickedPosition, this.planetSO.WorldBoundingSphere.Center) < this.planetSO.WorldBoundingSphere.Radius)
				{
					AudioManager.PlayCue("sd_bomb_impact_01");
					Vector3 VectorToCenter = pickedPosition - this.planetSO.WorldBoundingSphere.Center;
					VectorToCenter = Vector3.Normalize(VectorToCenter);
					VectorToCenter = this.planetSO.WorldBoundingSphere.Center + (VectorToCenter * this.planetSO.WorldBoundingSphere.Radius);
				}
			}
			this.currentMouse = input.CurrentMouseState;
			Vector2 MousePos = new Vector2((float)this.currentMouse.X, (float)this.currentMouse.Y);
			bool okcomet = true;
			foreach (UIButton b in this.Buttons)
			{
				if (!HelperFunctions.CheckIntersection(b.Rect, MousePos))
				{
					b.State = UIButton.PressState.Default;
				}
				else
				{
					okcomet = false;
					if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
						AudioManager.PlayCue("mouse_over4");

					b.State = UIButton.PressState.Hover;
					if (currentMouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Pressed)
						b.State = UIButton.PressState.Pressed;

					if (currentMouse.LeftButton != ButtonState.Pressed || previousMouse.LeftButton != ButtonState.Released)
					{
						continue;
					}
					switch (b.Launches)
					{
					    case "New Campaign":
					        AudioManager.PlayCue("sd_ui_tactical_pause");
					        OnPlayGame();
					        break;
					    case "Tutorials":
					        AudioManager.PlayCue("sd_ui_tactical_pause");
					        ScreenManager.AddScreen(new TutorialScreen());
					        break;
					    case "Load Game":
					        AudioManager.PlayCue("sd_ui_tactical_pause");
					        ScreenManager.AddScreen(new LoadSaveScreen(this));
					        break;
					    case "Options":
					        ScreenManager.AddScreen(new OptionsScreen(this, new Rectangle(0, 0, 600, 600))
                            {
                                TitleText  = Localizer.Token(4),
                                MiddleText = Localizer.Token(4004)
                            });
					        break;
					    case "Mods":
					        ScreenManager.AddScreen(new ModManager(this));
					        break;
					    case "Exit":
					        Game1.Instance.Exit();
					        break;
					}
				}
			}
			if (input.C && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
			{
				ScreenManager.AddScreen(new ShipToolScreen());
				ExitScreen();
			}
			if (okcomet && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
			{
			    Comet c = new Comet
			    {
			        Position = new Vector2(RandomMath.RandomBetween(-100f,
			                    ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100), 0f)
			    };
			    c.Velocity = Vector2.Normalize(HelperFunctions.FindVectorToTarget(c.Position, input.CursorPosition));
				c.Rotation = c.Position.RadiansToTarget(c.Position + c.Velocity);
				CometList.Add(c);
			}
			previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}


		public override void LoadContent()
		{
            base.LoadContent();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //if (ConfigurationManager.AppSettings["ActiveMod"] != "")
            if (!string.IsNullOrEmpty(config.AppSettings.Settings["ActiveMod"].Value))
			{
                if (!File.Exists("Mods/" + config.AppSettings.Settings["ActiveMod"].Value + ".xml"))
				{
					//Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
					config.AppSettings.Settings["ActiveMod"].Value = "";
					config.Save();
					ResourceManager.WhichModPath = "Content";
					ResourceManager.Reset();
					ResourceManager.Initialize(base.ScreenManager.Content);
				}
				else
				{
                    //Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    FileInfo info = new FileInfo("Mods/" + config.AppSettings.Settings["ActiveMod"].Value + ".xml");
                    ModInformation data = ResourceManager.ModSerializer.Deserialize<ModInformation>(info);

					ModEntry me = new ModEntry(ScreenManager, data, info.NameNoExt());
					GlobalStats.ActiveMod = me;
					GlobalStats.ActiveModInfo = me.mi;
					ResourceManager.LoadMods("Mods/" + config.AppSettings.Settings["ActiveMod"].Value);
				}
			}
			ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
            ScreenManager.racialMusic.SetVolume(GlobalStats.Config.MusicVolume);
            ScreenManager.combatMusic.SetVolume(GlobalStats.Config.MusicVolume);
			ScreenManager.weaponsCategory.SetVolume(GlobalStats.Config.EffectsVolume);
            ScreenManager.defaultCategory.SetVolume(GlobalStats.Config.EffectsVolume *.5f);

            if (GlobalStats.Config.EffectsVolume > 0 || GlobalStats.Config.MusicVolume > 0)
                ScreenManager.GlobalCategory.SetVolume(1);
            else ScreenManager.GlobalCategory.SetVolume(0);

            var para = ScreenManager.GraphicsDevice.PresentationParameters;
            var size = new Vector2(para.BackBufferWidth, para.BackBufferHeight);

            const string basepath = "Stardrive Main Logo 2_";
			for (int i = 0; i < 81; i++)
			{
				string remainder = i.ToString("00000.##");
				Texture2D logo = ScreenManager.Content.Load<Texture2D>("MainMenu/Stardrive logo/" + basepath + remainder);
				LogoAnimation.Add(logo);
			}
			if (size.Y <= 1080)
			{
				StarField = ScreenManager.Content.Load<Texture2D>("MainMenu/nebula_stars_bg");
				StarFieldRect = new Rectangle(0, 0, (int)size.X, (int)size.Y);
			}
			else
			{
				StarField = ScreenManager.Content.Load<Texture2D>("MainMenu/HR_nebula_stars_bg");
                StarFieldRect = new Rectangle(0, 0, (int)size.X, (int)size.Y);
            }

            Vector2 pos = new Vector2(size.X - 200, size.Y / 2 - 100);
            PlayGame  = Button(ref pos, "New Campaign", localization: 1);
            Adventure = Button(ref pos, "", "Battle Mode");
            Tutorials = Button(ref pos, "Tutorials", localization: 3);
            Load      = Button(ref pos, "Load Game", localization: 2);
            Options   = Button(ref pos, "Options", localization: 4);
            Mods      = Button(ref pos, "Mods", "Mods");
            Exit      = Button(ref pos, "Exit", localization: 5);

			ScreenManager.inter.ObjectManager.Clear();
			ScreenManager.inter.LightManager.Clear();
            ShieldManager.LoadContent(ScreenManager.Content);
			Beam.BeamEffect = ScreenManager.Content.Load<Effect>("Effects/BeamFX");
			Portrait = new Rectangle((int)size.X / 2 - 960, (int)size.Y / 2 - 540, 1920, 1080);

			while (Portrait.Width < size.X && Portrait.Height < size.Y)
			{
				Portrait.Width  += 12;
				Portrait.Height += 7;
                Portrait.X = (int)size.X / 2 - Portrait.Width  / 2;
                Portrait.Y = (int)size.Y / 2 - Portrait.Height / 2;
			}
			if (GlobalStats.ActiveMod != null && !string.IsNullOrEmpty(GlobalStats.ActiveMod.MainMenuMusic))
			{
				PlayMp3("Mods/" + GlobalStats.ActiveMod.ModPath + "/" + GlobalStats.ActiveMod.MainMenuMusic);
			}
			else if (ScreenManager.Music == null || ScreenManager.Music != null && ScreenManager.Music.IsStopped)
			{
				ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
				ScreenManager.Music = AudioManager.GetCue("SD_Theme_Reprise_06");
				ScreenManager.Music.Play();
			}

            
            string planet = "Model/SpaceObjects/planet_" + RandomMath.IntBetween(1,30);
            planetSO = new SceneObject(ScreenManager.Content.Load<Model>(planet).Meshes[0])
            {
                ObjectType = ObjectType.Dynamic
			};
            planetSO.AffineTransform(ShipPosition.X - 30000f, ShipPosition.Y - 500f, 80000f,
                                     20f.ToRadians(), 65f.ToRadians(), 1.57079637f, 25f);

            ScreenManager.inter.ObjectManager.Submit(planetSO);
            ShipPosition = new Vector2(size.X / 2 - 1200, LogoRect.Y + 400 - size.Y / 2);

            string modelPath = null;
            // FrostHand: do we actually need to show Model/Ships/speeder/ship07 in base version? Or could show random ship for base and modded version?
            if (GlobalStats.ActiveMod != null && ResourceManager.MainMenuShipList.ModelPaths.Count > 0)
            {
                int shipIndex = RandomMath.InRange(ResourceManager.MainMenuShipList.ModelPaths.Count);
                modelPath = ResourceManager.MainMenuShipList.ModelPaths[shipIndex];
            }
            else
            {
                var frigateHulls = ResourceManager.HullsDict.Values.Where(shipData => shipData.Role == ShipData.RoleName.frigate).ToArray();
                var hull = frigateHulls[RandomMath.InRange(frigateHulls.Length)];

                if (hull.Animated)
                {
                    SkinnedModel model = ResourceManager.GetSkinnedModel(hull.ModelPath);
                    shipSO = new SceneObject(model.Model)
                    {
                        ObjectType = ObjectType.Dynamic,
                        Visibility = ObjectVisibility.Rendered
                    };
                    ShipAnim = new AnimationController(model.SkeletonBones);
                    ShipAnim.StartClip(model.AnimationClips["Take 001"]);
                }
                else modelPath = hull.ModelPath;
            }

            if (shipSO == null) // not animated?
            {
                shipSO = new SceneObject(ResourceManager.GetModel(modelPath).Meshes[0])
                {
                    ObjectType = ObjectType.Dynamic,
                    Visibility = ObjectVisibility.Rendered
                };
            }
            ScreenManager.inter.ObjectManager.Submit(shipSO);
			ScreenManager.inter.LightManager.Submit(ScreenManager.Content.Load<LightRig>("example/ShipyardLightrig"));
			ScreenManager.environment = ScreenManager.Content.Load<SceneEnvironment>("example/scene_environment");

			Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);

			view = Matrix.CreateTranslation(0f, 0f, 0f) 
                * Matrix.CreateRotationY(180f.ToRadians())
                * Matrix.CreateRotationX(0f.ToRadians())
                * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));

            projection = Matrix.CreateOrthographic(size.X, size.Y, 1f, 80000f);

			LogoRect     = new Rectangle((int)size.X - 600, 128, 512, 128);
			MoonPosition = new Vector2(size.X / 2 - 300, LogoRect.Y + 70 - size.Y / 2);
            planetSO.AffineTransform(MoonPosition.ToVec3(zshift), MoonRotation.DegsToRad(), scale);
		}
        public void ReloadContent()
        {
            Buttons.Clear();
            LoadContent();
        }

		public void OnPlaybackStopped(object sender, EventArgs e)
		{
		    if (waveOut == null) return;
		    waveOut.Dispose();
		    mp3FileReader.Dispose();
		}

        private void OnPlayGame()
		{
			ScreenManager.AddScreen(new RaceDesignScreen(ScreenManager.GraphicsDevice, this));
		}

		private void PlayMp3(string fileName)
		{
			waveOut = new WaveOut();
			mp3FileReader = new Mp3FileReader(fileName);
			try
			{
				waveOut.Init(mp3FileReader);
#pragma warning disable CS0618 // Type or member is obsolete
                waveOut.Volume = GlobalStats.Config.MusicVolume;
#pragma warning restore CS0618 // Type or member is obsolete
                waveOut.Play();
				waveOut.PlaybackStopped += OnPlaybackStopped;
			}
			catch
			{
			}
		}

		public void ResetMusic()
		{
			if (GlobalStats.ActiveMod != null && !string.IsNullOrEmpty(GlobalStats.ActiveMod.MainMenuMusic))
			{
				PlayMp3("Mods/" + GlobalStats.ActiveMod.ModPath + "/" + GlobalStats.ActiveMod.MainMenuMusic);
				ScreenManager.musicCategory.Stop(AudioStopOptions.Immediate);
				return;
			}
			if (waveOut != null)
			{
				OnPlaybackStopped(null, null);
			}
			if (ScreenManager.Music == null || ScreenManager.Music != null && ScreenManager.Music.IsStopped)
			{
				ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
				ScreenManager.Music = AudioManager.GetCue("SD_Theme_Reprise_06");
				ScreenManager.Music.Play();
			}
		}

		private void SaveTechnologies()
		{
            var lf = new LocalizationFile();
			int i = 0;
			foreach (var shipModulesDict in ResourceManager.ShipModulesDict)
			{
				lf.TokenList.Add(new Token {  Index = 900 + i++  });
				lf.TokenList.Add(new Token {  Index = 900 + i++  });
			}
            using (TextWriter wf2 = new StreamWriter("Modules.xml"))
                new XmlSerializer(typeof(LocalizationFile)).Serialize(wf2, lf);

			AudioManager.PlayCue("echo_affirm");
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			ScreenManager.inter.Update(gameTime);
            TimeSpan timeSpan = gameTime.ElapsedGameTime;

			MoonPosition.X += timeSpan.Milliseconds / 400f;
            MoonRotation.X -= timeSpan.Milliseconds / 800f;
            planetSO.AffineTransform(MoonPosition.ToVec3(zshift), MoonRotation.DegsToRad(), scale);

            //planetSO.AffineTransform(MoonPosition.ToVec3(zshift), 20f.ToRadians(), 65f.ToRadians(), 1.57079637f - Zrotate, scale);
            //planetSO.World = (((((Matrix.Identity
            //    * Matrix.CreateScale(scale))
            //    * Matrix.CreateRotationZ(1.57079637f - Zrotate))
            //    * Matrix.CreateRotationX(20f.ToRadians()))
            //    * Matrix.CreateRotationY(65f.ToRadians()))
            //    * Matrix.CreateRotationZ(1.57079637f))
            //    * Matrix.CreateTranslation(new Vector3(MoonPosition, zshift));

            // Added by McShooterz: slow moves the ship across the screen
            if (shipSO != null)
            {
                ShipPosition.X += timeSpan.Milliseconds / 800f;
                ShipPosition.Y += timeSpan.Milliseconds / 1200f;
                shipSO.AffineTransform(ShipPosition.ToVec3(zshift), ShipRotation.DegsToRad(), scale * 1.75f);

                // Added by RedFox: support animated ships
                if (ShipAnim != null)
                {
                    shipSO.SkinBones = ShipAnim.SkinnedBoneTransforms;
                    ShipAnim.Update(Game1.Instance.TargetElapsedTime, Matrix.Identity);
                }
            }

		    ScreenManager.inter.Update(gameTime);
			if (IsExiting && TransitionPosition >= 0.99f && ScreenManager.Music != null)
			{
				ScreenManager.Music.Stop(AudioStopOptions.Immediate);
				ScreenManager.Music = null;
				ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
			}
			if (GlobalStats.ActiveMod == null || string.IsNullOrEmpty(GlobalStats.ActiveMod.MainMenuMusic))
			{
				if (ScreenManager.Music == null || ScreenManager.Music != null && ScreenManager.Music.IsStopped)
				{
					ScreenManager.Music = AudioManager.GetCue("SD_Theme_Reprise_06");
					ScreenManager.Music.Play();
				}
				else
				{
					ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
				}
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public class Comet
		{
			public Vector2 Position;
			public Vector2 Velocity;
			public float Rotation;
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~MainMenuScreen() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            if (disposing)
            {
                CometList?.Dispose();
                waveOut?.Dispose();
                mp3FileReader?.Dispose();
            }
            CometList = null;
            waveOut = null;
            mp3FileReader = null;
        }
	}
}