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

namespace Ship_Game
{
	public class MainMenuScreen : GameScreen
	{
		public static string Version;

		private IWavePlayer waveOut;

		private Mp3FileReader mp3FileReader;

		public BatchRemovalCollection<MainMenuScreen.Comet> CometList = new BatchRemovalCollection<MainMenuScreen.Comet>();

		//private Model xnaPlanet;

		private Model shieldModel;

		//private Effect AtmoEffect;

		//private Model atmoModel;

		private Effect ShieldEffect;

		private Texture2D shieldTex;

		private Texture2D shieldAlpha;

		//private Texture2D cloudTex;

		private Rectangle StarFieldRect = new Rectangle(0, 0, 1920, 1080);

		private Vector2 StarFieldOrigin = new Vector2(2048f, 2048f);

		private Texture2D StarField;

		private string fmt = "00000.##";

		private List<Texture2D> LogoAnimation = new List<Texture2D>();

		private UIButton PlayGame;

		private UIButton Adventure;

		private UIButton Load;

		private UIButton Tutorials;

		private UIButton Mods;

		private UIButton Options;

		private UIButton Exit;

		public List<UIButton> Buttons = new List<UIButton>();

		//private Texture2D MoonTexture;

		//private int whichPlanet;

		private SceneObject planetSO;

		private Vector2 MoonPosition = new Vector2();

		private Rectangle Portrait;

		private float zshift = 0.0f;    //was uninitialized, set to float default

		private float scale = 0.7f;

		private Matrix worldMatrix = Matrix.Identity;

		private Matrix view;

		private Matrix projection;

		private Model model;

		private SceneObject shipSO;

		private MouseState currentMouse;

		private MouseState previousMouse;

		private float Zrotate;

		private Vector2 ShipPosition = new Vector2(5400f, -2000f);

		private Vector2 FlareAdd = new Vector2(0f, 0f);

		private Rectangle LogoRect = new Rectangle(256, 256, 512, 128);

		private float rotate = 3.85f;

		private Vector3 cameraPosition = new Vector3(0f, 0f, 1600f);

		private int AnimationFrame;

		private bool flip;

		private bool StayOn;

		private int flareFrames;

		//private bool onplaygame;

		static MainMenuScreen()
		{
			MainMenuScreen.Version = "BlackBox Gravity : " + GlobalStats.ExtendedVersion;
		}

		public MainMenuScreen()
		{
			base.TransitionOnTime = TimeSpan.FromSeconds(1);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.5);
		}

		public override void Draw(GameTime gameTime)
		{
			float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			MainMenuScreen mainMenuScreen = this;
			mainMenuScreen.rotate = mainMenuScreen.rotate + elapsedTime / 350f;
			if ((double)RandomMath.RandomBetween(0f, 100f) > 99.75)
			{
				MainMenuScreen.Comet c = new MainMenuScreen.Comet()
				{
					Position = new Vector2(RandomMath.RandomBetween(-100f, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100)), 0f),
					Velocity = new Vector2(RandomMath.RandomBetween(-1f, 1f), 1f)
				};
				c.Velocity = Vector2.Normalize(c.Velocity);
				c.Rotation = (float)MathHelper.ToRadians(HelperFunctions.findAngleToTarget(c.Position, c.Position + c.Velocity));
				this.CometList.Add(c);
			}
			Vector2 CometOrigin = new Vector2((float)(Ship_Game.ResourceManager.TextureDict["GameScreens/comet"].Width / 2), (float)(Ship_Game.ResourceManager.TextureDict["GameScreens/comet"].Height / 2));
			if (SplashScreen.DisplayComplete)
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
					base.ScreenManager.SpriteBatch.Draw(Ship_Game.ResourceManager.TextureDict["GameScreens/comet2"], c.Position, nullable1, new Color(255, 255, 255, (byte)Alpha), c.Rotation, CometOrigin, 0.45f, SpriteEffects.None, 1f);
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
					b.State = UIButton.PressState.Normal;
				}
				else
				{
					okcomet = false;
					if (b.State != UIButton.PressState.Hover && b.State != UIButton.PressState.Pressed)
					{
						AudioManager.PlayCue("mouse_over4");
					}
					b.State = UIButton.PressState.Hover;
					if (this.currentMouse.LeftButton == ButtonState.Pressed && this.previousMouse.LeftButton == ButtonState.Pressed)
					{
						b.State = UIButton.PressState.Pressed;
					}
					if (this.currentMouse.LeftButton != ButtonState.Pressed || this.previousMouse.LeftButton != ButtonState.Released)
					{
						continue;
					}
					string launches = b.Launches;
					string str = launches;
					if (launches == null)
					{
						continue;
					}
					if (str == "New Campaign")
					{
						AudioManager.PlayCue("sd_ui_tactical_pause");
						this.OnPlayGame();
					}
					else if (str == "Tutorials")
					{
						AudioManager.PlayCue("sd_ui_tactical_pause");
						base.ScreenManager.AddScreen(new TutorialScreen());
					}
					else if (str == "Load Game")
					{
						AudioManager.PlayCue("sd_ui_tactical_pause");
						base.ScreenManager.AddScreen(new LoadSaveScreen(this));
					}
					else if (str == "Options")
					{
						OptionsScreen opt = new OptionsScreen(this, new Rectangle(0, 0, 600, 600))
						{
							TitleText = Localizer.Token(4),
							MiddleText = Localizer.Token(4004)
						};
						base.ScreenManager.AddScreen(opt);
					}
					else if (str == "Mods")
					{
						ModManager mm = new ModManager(this);
						base.ScreenManager.AddScreen(mm);
					}
					else if (str == "Exit")
					{
						Game1.Instance.Exit();
					}
				}
			}
			if (input.C && input.CurrentKeyboardState.IsKeyDown(Keys.LeftShift))
			{
				base.ScreenManager.AddScreen(new ShipToolScreen());
				this.ExitScreen();
			}
			if (okcomet && input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
			{
				MainMenuScreen.Comet c = new MainMenuScreen.Comet();
				//{  
                c.Position = new Vector2(RandomMath.RandomBetween(-100f, (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth + 100)), 0f);
					c.Velocity = HelperFunctions.FindVectorToTarget(c.Position, input.CursorPosition);
				//};
				c.Velocity = Vector2.Normalize(c.Velocity);
				c.Rotation = (float)MathHelper.ToRadians(HelperFunctions.findAngleToTarget(c.Position, c.Position + c.Velocity));
				this.CometList.Add(c);
			}
			this.previousMouse = input.LastMouseState;
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			if (ConfigurationSettings.AppSettings["ActiveMod"] != "")
			{
				if (!File.Exists(string.Concat("Mods/", ConfigurationSettings.AppSettings["ActiveMod"], ".xml")))
				{
					Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
					config.AppSettings.Settings["ActiveMod"].Value = "";
					config.Save();
					Ship_Game.ResourceManager.WhichModPath = "Content";
					Ship_Game.ResourceManager.Reset();
					Ship_Game.ResourceManager.Initialize(base.ScreenManager.Content);
				}
				else
				{
					FileInfo FI = new FileInfo(string.Concat("Mods/", ConfigurationSettings.AppSettings["ActiveMod"], ".xml"));
					Stream file = FI.OpenRead();
					ModInformation data = (ModInformation)Ship_Game.ResourceManager.ModSerializer.Deserialize(file);
					file.Close();
					file.Dispose();
					ModEntry me = new ModEntry(base.ScreenManager, data, Path.GetFileNameWithoutExtension(FI.Name));
					GlobalStats.ActiveMod = me;
					Ship_Game.ResourceManager.LoadMods(string.Concat("Mods/", ConfigurationSettings.AppSettings["ActiveMod"]));
				}
			}
			base.ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
			base.ScreenManager.weaponsCategory.SetVolume(GlobalStats.Config.EffectsVolume);
			string basepath = "Stardrive Main Logo 2_";
			for (int i = 0; i < 81; i++)
			{
				string remainder = i.ToString(this.fmt);
				Texture2D logo = base.ScreenManager.Content.Load<Texture2D>(string.Concat("MainMenu/Stardrive logo/", basepath, remainder));
				this.LogoAnimation.Add(logo);
			}
			if (base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight <= 1080)
			{
				this.StarField = base.ScreenManager.Content.Load<Texture2D>("MainMenu/nebula_stars_bg");
				this.StarFieldRect = new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
			}
			else
			{
				this.StarFieldRect = new Rectangle(0, 0, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight);
				this.StarField = base.ScreenManager.Content.Load<Texture2D>("MainMenu/HR_nebula_stars_bg");
			}
			Vector2 Cursor = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 200), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.PlayGame = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(1)
			};
			this.Buttons.Add(this.PlayGame);
			this.PlayGame.Launches = "New Campaign";
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Adventure = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = "Battle Mode"
			};
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Tutorials = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(3),
				Launches = "Tutorials"
			};
			this.Buttons.Add(this.Tutorials);
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Load = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(2),
				Launches = "Load Game"
			};
			this.Buttons.Add(this.Load);
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Options = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(4),
				Launches = "Options"
			};
			this.Buttons.Add(this.Options);
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Mods = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = "Mods",
				Launches = "Mods"
			};
			this.Buttons.Add(this.Mods);
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			this.Exit = new UIButton()
			{
				Rect = new Rectangle((int)Cursor.X, (int)Cursor.Y, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Width, Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height),
				NormalTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"],
				HoverTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_hover"],
				PressedTexture = Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px_pressed"],
				Text = Localizer.Token(5),
				Launches = "Exit"
			};
			this.Buttons.Add(this.Exit);
			Cursor.Y = Cursor.Y + (float)(Ship_Game.ResourceManager.TextureDict["EmpireTopBar/empiretopbar_btn_168px"].Height + 15);
			base.ScreenManager.inter.ObjectManager.Clear();
			base.ScreenManager.inter.LightManager.Clear();
			this.shieldModel = base.ScreenManager.Content.Load<Model>("Model/Projectiles/shield");
			this.shieldTex = base.ScreenManager.Content.Load<Texture2D>("Model/Projectiles/shield_d");
			this.shieldAlpha = base.ScreenManager.Content.Load<Texture2D>("Model/Projectiles/shieldgradient");
			this.ShieldEffect = base.ScreenManager.Content.Load<Effect>("Effects/scale");
			Beam.BeamEffect = base.ScreenManager.Content.Load<Effect>("Effects/BeamFX");
			ShieldManager.shieldModel = this.shieldModel;
			ShieldManager.shieldTexture = this.shieldTex;
			ShieldManager.gradientTexture = this.shieldAlpha;
			ShieldManager.ShieldEffect = this.ShieldEffect;
			this.Portrait = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 960, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 540, 1920, 1080);
			while (this.Portrait.Width < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth && this.Portrait.Height < base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight)
			{
				this.Portrait.Width = this.Portrait.Width + 12;
				this.Portrait.X = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - this.Portrait.Width / 2;
				this.Portrait.Height = this.Portrait.Height + 7;
				this.Portrait.Y = base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - this.Portrait.Height / 2;
			}
			if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.MainMenuMusic != "")
			{
				this.PlayMp3(string.Concat("Mods/", GlobalStats.ActiveMod.ModPath, "/", GlobalStats.ActiveMod.MainMenuMusic));
			}
			else if (base.ScreenManager.Music == null || base.ScreenManager.Music != null && base.ScreenManager.Music.IsStopped)
			{
				base.ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
				base.ScreenManager.Music = AudioManager.GetCue("SD_Theme_Reprise_06");
				base.ScreenManager.Music.Play();
			}
			//this.whichPlanet = 1;
            //Added by McShooterz: Random Main menu planet
            Random rd = new Random();
            int planetIndex = rd.Next(1,30);
            Model planetModel = base.ScreenManager.Content.Load<Model>(string.Concat("Model/SpaceObjects/planet_", planetIndex));
            ModelMesh mesh = planetModel.Meshes[0];
			this.planetSO = new SceneObject(mesh)
			{
				ObjectType = ObjectType.Dynamic,
				World = (((((Matrix.Identity * Matrix.CreateScale(25f)) * Matrix.CreateRotationZ(1.57079637f - this.Zrotate)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(this.ShipPosition.X - 30000f, this.ShipPosition.Y - 500f, 80000f)
			};
			base.ScreenManager.inter.ObjectManager.Submit(this.planetSO);
            //Added by McShooterz: random ship in main menu
            this.ShipPosition = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 1200), (float)(this.LogoRect.Y + 400 - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
            if (GlobalStats.ActiveMod != null && ResourceManager.MainMenuShipList.ModelPaths.Count > 0)
            {
                int shipIndex = rd.Next(0, ResourceManager.MainMenuShipList.ModelPaths.Count);
                this.shipSO = new SceneObject(((ReadOnlyCollection<ModelMesh>)Ship_Game.ResourceManager.GetModel(ResourceManager.MainMenuShipList.ModelPaths[shipIndex]).Meshes)[0]);
                this.shipSO.ObjectType = ObjectType.Dynamic;
                this.shipSO.World = this.worldMatrix;
                this.shipSO.Visibility = ObjectVisibility.Rendered;
                base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
            }
            else
            {
                this.shipSO = new SceneObject(((ReadOnlyCollection<ModelMesh>)Ship_Game.ResourceManager.GetModel("Model/Ships/speeder/ship07").Meshes)[0]);
                this.shipSO.ObjectType = ObjectType.Dynamic;
                this.shipSO.World = this.worldMatrix;
                this.shipSO.Visibility = ObjectVisibility.Rendered;
                base.ScreenManager.inter.ObjectManager.Submit(this.shipSO);
            }
			LightRig rig = base.ScreenManager.Content.Load<LightRig>("example/MM_light_rig");
			base.ScreenManager.inter.LightManager.Submit(rig);
			base.ScreenManager.environment = base.ScreenManager.Content.Load<SceneEnvironment>("example/scene_environment");
			float width = (float)base.ScreenManager.GraphicsDevice.Viewport.Width;
			Viewport viewport = base.ScreenManager.GraphicsDevice.Viewport;
			float height = width / (float)viewport.Height;
			Vector3 camPos = new Vector3(0f, 0f, 1500f) * new Vector3(-1f, 1f, 1f);
			this.view = ((Matrix.CreateTranslation(0f, 0f, 0f) * Matrix.CreateRotationY(MathHelper.ToRadians(180f))) * Matrix.CreateRotationX(MathHelper.ToRadians(0f))) * Matrix.CreateLookAt(camPos, new Vector3(camPos.X, camPos.Y, 0f), new Vector3(0f, -1f, 0f));
			this.projection = Matrix.CreateOrthographic((float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth, (float)base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight, 1f, 80000f);
			base.LoadContent();
			this.LogoRect = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth - 600, 128, 512, 128);
			this.MoonPosition = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 300), (float)(this.LogoRect.Y + 70 - base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2));
			this.planetSO.World = (((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationZ(1.57079637f - this.Zrotate)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(new Vector3(this.MoonPosition, this.zshift));
		}

		protected void OnAdventure()
		{
		}

		public void OnPlaybackStopped(object sender, EventArgs e)
		{
			if (this.waveOut != null)
			{
				this.waveOut.Dispose();
				this.mp3FileReader.Dispose();
			}
		}

		protected void OnPlayGame()
		{
			RaceDesignScreen rds = new RaceDesignScreen(base.ScreenManager.GraphicsDevice, this);
			base.ScreenManager.AddScreen(rds);
		}

		private void PlayMp3(string fileName)
		{
			this.waveOut = new WaveOut();
			this.mp3FileReader = new Mp3FileReader(fileName);
			try
			{
				this.waveOut.Init(this.mp3FileReader);
				this.waveOut.Volume = GlobalStats.Config.MusicVolume;
				this.waveOut.Play();
				this.waveOut.PlaybackStopped += new EventHandler<StoppedEventArgs>(this.OnPlaybackStopped);
			}
			catch
			{
			}
		}

		private void RedoShipDesigns()
		{
			XmlSerializer Serializer2 = new XmlSerializer(typeof(test));
			TextWriter wf2 = new StreamWriter("test.xml");
			Serializer2.Serialize(wf2, new test());
			wf2.Close();
		}

		public void ResetMusic()
		{
			if (GlobalStats.ActiveMod != null && GlobalStats.ActiveMod.MainMenuMusic != "")
			{
				this.PlayMp3(string.Concat("Mods/", GlobalStats.ActiveMod.ModPath, "/", GlobalStats.ActiveMod.MainMenuMusic));
				base.ScreenManager.musicCategory.Stop(AudioStopOptions.Immediate);
				return;
			}
			if (this.waveOut != null)
			{
				this.OnPlaybackStopped(null, null);
			}
			if (base.ScreenManager.Music == null || base.ScreenManager.Music != null && base.ScreenManager.Music.IsStopped)
			{
				base.ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
				base.ScreenManager.Music = AudioManager.GetCue("SD_Theme_Reprise_06");
				base.ScreenManager.Music.Play();
			}
		}

		private void SaveTechnologies()
		{
			LocalizationFile lf = new LocalizationFile()
			{
				TokenList = new List<Token>()
			};
			int i = 0;
			foreach (KeyValuePair<string, Ship_Game.Gameplay.ShipModule> shipModulesDict in Ship_Game.ResourceManager.ShipModulesDict)
			{
				Token t = new Token()
				{
					Index = 900 + i
				};
				lf.TokenList.Add(t);
				i++;
				t = new Token()
				{
					Index = 900 + i
				};
				lf.TokenList.Add(t);
				i++;
			}
			XmlSerializer Serializer2 = new XmlSerializer(typeof(LocalizationFile));
			TextWriter wf2 = new StreamWriter("Modules.xml");
			Serializer2.Serialize(wf2, lf);
			wf2.Close();
			AudioManager.PlayCue("echo_affirm");
		}

		public override void UnloadContent()
		{
			base.UnloadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.ScreenManager.inter.Update(gameTime);
			MainMenuScreen milliseconds = this;
			float zrotate = milliseconds.Zrotate;
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			milliseconds.Zrotate = zrotate + (float)elapsedGameTime.Milliseconds / 20000f;
			float x = this.MoonPosition.X;
			TimeSpan timeSpan = gameTime.ElapsedGameTime;
			this.MoonPosition.X = x + (float)timeSpan.Milliseconds / 400f;
			this.planetSO.World = (((((Matrix.Identity * Matrix.CreateScale(this.scale)) * Matrix.CreateRotationZ(1.57079637f - this.Zrotate)) * Matrix.CreateRotationX(MathHelper.ToRadians(20f))) * Matrix.CreateRotationY(MathHelper.ToRadians(65f))) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateTranslation(new Vector3(this.MoonPosition, this.zshift));
            //Added by McShooterz: slow moves the ship across the screen
            if (this.shipSO != null)
            {
                this.ShipPosition.X += (float)timeSpan.Milliseconds / 800f;
                this.ShipPosition.Y += (float)timeSpan.Milliseconds / 1200f;
                this.shipSO.World = (((((Matrix.Identity * Matrix.CreateScale(this.scale * 1.75f)) * Matrix.CreateRotationZ(1.57079637f)) * Matrix.CreateRotationX(MathHelper.ToRadians(-15f))) * Matrix.CreateRotationY(MathHelper.ToRadians(60f))) * Matrix.CreateRotationZ(1f)) * Matrix.CreateTranslation(new Vector3(this.ShipPosition, this.zshift));
            }
			base.ScreenManager.inter.Update(gameTime);
			if (base.IsExiting && base.TransitionPosition >= 0.99f && base.ScreenManager.Music != null)
			{
				base.ScreenManager.Music.Stop(AudioStopOptions.Immediate);
				base.ScreenManager.Music = null;
				base.ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
			}
			if (GlobalStats.ActiveMod == null || !(GlobalStats.ActiveMod.MainMenuMusic != ""))
			{
				if (base.ScreenManager.Music == null || base.ScreenManager.Music != null && base.ScreenManager.Music.IsStopped)
				{
					base.ScreenManager.Music = AudioManager.GetCue("SD_Theme_Reprise_06");
					base.ScreenManager.Music.Play();
				}
				else
				{
					base.ScreenManager.musicCategory.SetVolume(GlobalStats.Config.MusicVolume);
				}
			}
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}

		public class Comet
		{
			public Vector2 Position;

			public Vector2 Velocity;

			public float Rotation;

			public Comet()
			{
			}
		}
	}
}