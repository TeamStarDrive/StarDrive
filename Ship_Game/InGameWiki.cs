using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
	public class InGameWiki : PopupWindow
	{
		private Vector2 Cursor = Vector2.Zero;

		private HelpTopics ht;

		//private UniverseScreen screen;

		private ScrollList CategoriesSL;

		private Rectangle CategoriesRect;

		private Rectangle TextRect;

		private Vector2 TitlePosition;

		private ScrollList TextSL;

		private Video ActiveVideo;

		private Microsoft.Xna.Framework.Media.VideoPlayer VideoPlayer;

		private Texture2D VideoFrame;

		private Rectangle SmallViewer;

		private Rectangle BigViewer;

		private HelpTopic ActiveTopic;

		private bool HoverSmallVideo;

		public bool PlayingVideo;

		//private float transitionElapsedTime;

		public InGameWiki(Rectangle r)
		{
			FileInfo[] Help;
			this.r = r;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			Help = (GlobalStats.Config.Language == "English" ? HelperFunctions.GetFilesFromDirectory("Content/HelpTopics/English") : HelperFunctions.GetFilesFromDirectory(string.Concat("Content/HelpTopics/", GlobalStats.Config.Language)));
			XmlSerializer serializer1 = new XmlSerializer(typeof(HelpTopics));
			this.ht = (HelpTopics)serializer1.Deserialize(Help[0].OpenRead());
		}

		public InGameWiki()
		{
			FileInfo[] Help;
			base.IsPopup = true;
			base.TransitionOnTime = TimeSpan.FromSeconds(0.25);
			base.TransitionOffTime = TimeSpan.FromSeconds(0.25);
			Help = (GlobalStats.Config.Language != "German" ? HelperFunctions.GetFilesFromDirectory("Content/HelpTopics/English") : HelperFunctions.GetFilesFromDirectory("Content/HelpTopics/German"));
			XmlSerializer serializer1 = new XmlSerializer(typeof(HelpTopics));
			this.ht = (HelpTopics)serializer1.Deserialize(Help[0].OpenRead());
		}

		public new void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual new void Dispose(bool disposing)
		{
			if (disposing)
			{
				lock (this)
				{
				}
			}
		}

		public override void Draw(GameTime gameTime)
		{
			base.ScreenManager.FadeBackBufferToBlack(base.TransitionAlpha * 2 / 3);
			base.DrawBase(gameTime);
			base.ScreenManager.SpriteBatch.Begin();
			this.CategoriesSL.Draw(base.ScreenManager.SpriteBatch);
			Vector2 bCursor = new Vector2((float)(this.r.X + 20), (float)(this.r.Y + 20));
			for (int i = this.CategoriesSL.indexAtTop; i < this.CategoriesSL.Copied.Count && i < this.CategoriesSL.indexAtTop + this.CategoriesSL.entriesToDisplay; i++)
			{
				bCursor = new Vector2((float)(this.r.X + 35), (float)(this.r.Y + 20));
				ScrollList.Entry e = this.CategoriesSL.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				if (!(e.item is ModuleHeader))
				{
					bCursor.X = bCursor.X + 15f;
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (e.item as HelpTopic).Title, bCursor, (e.clickRectHover == 1 ? Color.Orange : Color.White));
					bCursor.Y = bCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, (e.item as HelpTopic).ShortDescription, bCursor, (e.clickRectHover == 1 ? Color.White : Color.Orange));
				}
				else
				{
					(e.item as ModuleHeader).Draw(base.ScreenManager, bCursor);
				}
			}
			bCursor = new Vector2((float)this.TextRect.X, (float)(this.TextRect.Y + 20));
			if (this.ActiveVideo != null)
			{
				if (this.VideoPlayer.State != MediaState.Playing)
				{
					this.VideoFrame = this.VideoPlayer.GetTexture();
					base.ScreenManager.SpriteBatch.Draw(this.VideoFrame, this.SmallViewer, Color.White);
					Primitives2D.DrawRectangleGlow(base.ScreenManager.SpriteBatch, this.SmallViewer);
					Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, this.SmallViewer, new Color(32, 30, 18));
					if (this.HoverSmallVideo)
					{
						Rectangle playIcon = new Rectangle(this.SmallViewer.X + this.SmallViewer.Width / 2 - 64, this.SmallViewer.Y + this.SmallViewer.Height / 2 - 64, 128, 128);
						base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["Textures/icon_play"], playIcon, new Color(255, 255, 255, 200));
					}
				}
				else
				{
					this.VideoFrame = this.VideoPlayer.GetTexture();
					base.ScreenManager.SpriteBatch.Draw(this.VideoFrame, this.BigViewer, Color.White);
					Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, this.BigViewer, new Color(32, 30, 18));
				}
			}
			this.TextSL.Draw(base.ScreenManager.SpriteBatch);
			for (int i = this.TextSL.indexAtTop; i < this.TextSL.Copied.Count && i < this.TextSL.indexAtTop + this.TextSL.entriesToDisplay; i++)
			{
				ScrollList.Entry e = this.TextSL.Copied[i];
				bCursor.Y = (float)e.clickRect.Y;
				bCursor.X = (float)((int)bCursor.X);
				bCursor.Y = (float)((int)bCursor.Y);
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.item as string, bCursor, Color.White);
			}
			if (this.VideoPlayer != null && this.VideoPlayer.State != MediaState.Playing)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveTopic.Title, this.TitlePosition, Color.Orange);
				base.ScreenManager.musicCategory.Resume();
			}
			else if (this.VideoPlayer == null)
			{
				base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveTopic.Title, this.TitlePosition, Color.Orange);
			}
			base.ScreenManager.SpriteBatch.End();
		}

		public override void ExitScreen()
		{
			if (this.VideoPlayer != null)
			{
				this.VideoPlayer.Stop();
				this.VideoPlayer = null;
				this.ActiveVideo = null;
				base.ScreenManager.musicCategory.Resume();
			}
			base.ExitScreen();
		}

		~InGameWiki()
		{
			this.Dispose(false);
		}

		public override void HandleInput(InputState input)
		{
			if (input.CurrentMouseState.RightButton == ButtonState.Pressed)
			{
				this.ExitScreen();
			}
			if (input.Escaped)
			{
				this.ExitScreen();
			}
            if (input.CurrentKeyboardState.IsKeyDown(Keys.P) && !input.LastKeyboardState.IsKeyDown(Keys.P) && !GlobalStats.TakingInput)
            {
                AudioManager.PlayCue("echo_affirm");
                this.ExitScreen();
            }
			this.CategoriesSL.HandleInput(input);
			this.TextSL.HandleInput(input);
			if (this.ActiveVideo != null)
			{
				if (this.VideoPlayer.State == MediaState.Paused)
				{
					if (!HelperFunctions.CheckIntersection(this.SmallViewer, input.CursorPosition))
					{
						this.HoverSmallVideo = false;
					}
					else
					{
						this.HoverSmallVideo = true;
						if (input.InGameSelect)
						{
							this.VideoPlayer.Play(this.ActiveVideo);
							base.ScreenManager.musicCategory.Pause();
						}
					}
				}
				else if (this.VideoPlayer.State == MediaState.Playing)
				{
					this.HoverSmallVideo = false;
					if (HelperFunctions.CheckIntersection(this.BigViewer, input.CursorPosition) && input.InGameSelect)
					{
						this.VideoPlayer.Pause();
						base.ScreenManager.musicCategory.Resume();
					}
				}
			}
			for (int i = 0; i < this.CategoriesSL.Copied.Count; i++)
			{
				ScrollList.Entry e = this.CategoriesSL.Copied[i];
				if (e.item is ModuleHeader)
				{
					(e.item as ModuleHeader).HandleInput(input, e);
				}
				else if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
				{
					e.clickRectHover = 0;
				}
				else
				{
					if (e.clickRectHover == 0)
					{
						AudioManager.PlayCue("sd_ui_mouseover");
					}
					e.clickRectHover = 1;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released && e.item is HelpTopic)
					{
						this.TextSL.Entries.Clear();
						this.TextSL.Copied.Clear();
						this.TextSL.indexAtTop = 0;
						this.ActiveTopic = e.item as HelpTopic;
						if (this.ActiveTopic.Text != null)
						{
							HelperFunctions.parseTextToSL(this.ActiveTopic.Text, (float)(this.TextRect.Width - 40), Fonts.Arial12Bold, ref this.TextSL);
							this.TitlePosition = new Vector2((float)(this.TextRect.X + this.TextRect.Width / 2) - Fonts.Arial20Bold.MeasureString(this.ActiveTopic.Title).X / 2f - 15f, this.TitlePosition.Y);
						}
						if (this.ActiveTopic.VideoPath == null)
						{
							this.ActiveVideo = null;
							this.VideoPlayer = null;
						}
						else
						{
							this.TextSL.Copied.Clear();
							this.VideoPlayer = new Microsoft.Xna.Framework.Media.VideoPlayer();
							this.ActiveVideo = base.ScreenManager.Content.Load<Video>(string.Concat("Video/", this.ActiveTopic.VideoPath));
							this.VideoPlayer.Play(this.ActiveVideo);
							this.VideoPlayer.Pause();
						}
					}
				}
			}
			base.HandleInput(input);
		}

		public override void LoadContent()
		{
			base.Setup();
			Vector2 vector2 = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
			this.CategoriesRect = new Rectangle(this.r.X + 25, this.r.Y + 130, 330, 430);
			Submenu blah = new Submenu(base.ScreenManager, this.CategoriesRect);
			this.CategoriesSL = new ScrollList(blah, 40);
			this.TextRect = new Rectangle(this.CategoriesRect.X + this.CategoriesRect.Width + 5, this.CategoriesRect.Y + 10, 375, 420);
			Rectangle TextSLRect = new Rectangle(this.CategoriesRect.X + this.CategoriesRect.Width + 5, this.CategoriesRect.Y + 10, 375, 420);
			Submenu bler = new Submenu(base.ScreenManager, TextSLRect);
			this.TextSL = new ScrollList(bler, Fonts.Arial12Bold.LineSpacing + 2);
			this.SmallViewer = new Rectangle(this.TextRect.X + 20, this.TextRect.Y + 40, 336, 189);
			this.BigViewer = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
			this.VideoPlayer = new Microsoft.Xna.Framework.Media.VideoPlayer();
			this.ActiveTopic = new HelpTopic()
			{
				Title = Localizer.Token(1401),
				Text = Localizer.Token(1400)
			};
			HelperFunctions.parseTextToSL(this.ActiveTopic.Text, (float)(this.TextRect.Width - 40), Fonts.Arial12Bold, ref this.TextSL);
			this.TitlePosition = new Vector2((float)(this.TextRect.X + this.TextRect.Width / 2) - Fonts.Arial20Bold.MeasureString(this.ActiveTopic.Title).X / 2f - 15f, (float)(this.TextRect.Y + 10));
			List<string> Categories = new List<string>();
			foreach (HelpTopic halp in this.ht.HelpTopicsList)
			{
				if (Categories.Contains(halp.Category))
				{
					continue;
				}
				Categories.Add(halp.Category);
				ModuleHeader mh = new ModuleHeader(halp.Category, 295f);
				this.CategoriesSL.AddItem(mh);
			}
			foreach (ScrollList.Entry e in this.CategoriesSL.Entries)
			{
				foreach (HelpTopic halp in this.ht.HelpTopicsList)
				{
					if (halp.Category != (e.item as ModuleHeader).Text)
					{
						continue;
					}
					e.AddItem(halp);
				}
			}
			base.LoadContent();
		}

		public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
		{
			base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
		}
	}
}