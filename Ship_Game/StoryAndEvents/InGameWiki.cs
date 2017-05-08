using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Ship_Game
{
    public sealed class InGameWiki : PopupWindow
    {
        private HelpTopics HelpTopics;
        private ScrollList HelpCategories;
        private Rectangle CategoriesRect;
        private Rectangle TextRect;
        private Vector2 TitlePosition;
        private ScrollList HelpEntries;
        private Video ActiveVideo;
        private VideoPlayer VideoPlayer;
        private Texture2D VideoFrame;
        private Rectangle SmallViewer;
        private Rectangle BigViewer;
        private HelpTopic ActiveTopic;
        private bool HoverSmallVideo;
        public bool PlayingVideo;

        public InGameWiki(GameScreen parent) : base(parent)
        {
            IsPopup = true;
            TransitionOnTime = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);

            var help = Dir.GetFiles("Content/HelpTopics/" + GlobalStats.Language);
            if (help.Length != 0)
                HelpTopics = help[0].Deserialize<HelpTopics>();
        }
        public InGameWiki(GameScreen parent, Rectangle r) : this(parent)
        {
            R = r;
        }

        protected override void Dispose(bool disposing)
        {
            VideoPlayer?.Dispose(ref VideoPlayer);
            HelpCategories?.Dispose(ref HelpCategories);
            HelpEntries?.Dispose(ref HelpEntries);
            base.Dispose(disposing);
        } 
         

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            DrawBase(gameTime);
            ScreenManager.SpriteBatch.Begin();
            HelpCategories.Draw(ScreenManager.SpriteBatch);
            Vector2 bCursor = new Vector2(R.X + 20, R.Y + 20);
            for (int i = HelpCategories.indexAtTop; i < HelpCategories.Copied.Count && i < HelpCategories.indexAtTop + HelpCategories.entriesToDisplay; i++)
            {
                bCursor = new Vector2(R.X + 35, R.Y + 20);
                ScrollList.Entry e = HelpCategories.Copied[i];
                bCursor.Y = e.clickRect.Y;
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
                        base.ScreenManager.SpriteBatch.Draw(ResourceManager.TextureDict["icon_play"], playIcon, new Color(255, 255, 255, 200));
                    }
                }
                else
                {
                    this.VideoFrame = this.VideoPlayer.GetTexture();
                    base.ScreenManager.SpriteBatch.Draw(this.VideoFrame, this.BigViewer, Color.White);
                    Primitives2D.DrawRectangle(base.ScreenManager.SpriteBatch, this.BigViewer, new Color(32, 30, 18));
                }
            }
            this.HelpEntries.Draw(base.ScreenManager.SpriteBatch);
            for (int i = this.HelpEntries.indexAtTop; i < this.HelpEntries.Copied.Count && i < this.HelpEntries.indexAtTop + this.HelpEntries.entriesToDisplay; i++)
            {
                ScrollList.Entry e = this.HelpEntries.Copied[i];
                bCursor.Y = (float)e.clickRect.Y;
                bCursor.X = (float)((int)bCursor.X);
                bCursor.Y = (float)((int)bCursor.Y);
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, e.item as string, bCursor, Color.White);
            }
            if (this.VideoPlayer != null && this.VideoPlayer.State != MediaState.Playing)
            {
                base.ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, this.ActiveTopic.Title, this.TitlePosition, Color.Orange);
                GameAudio.ResumeGenericMusic();
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
                GameAudio.ResumeGenericMusic();
            }
            base.ExitScreen();
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
                GameAudio.PlaySfxAsync("echo_affirm");
                this.ExitScreen();
            }
            this.HelpCategories.HandleInput(input);
            this.HelpEntries.HandleInput(input);
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
                            GameAudio.PauseGenericMusic();
                        }
                    }
                }
                else if (this.VideoPlayer.State == MediaState.Playing)
                {
                    this.HoverSmallVideo = false;
                    if (HelperFunctions.CheckIntersection(this.BigViewer, input.CursorPosition) && input.InGameSelect)
                    {
                        this.VideoPlayer.Pause();
                        GameAudio.ResumeGenericMusic();
                    }
                }
            }
            for (int i = 0; i < this.HelpCategories.Copied.Count; i++)
            {
                ScrollList.Entry e = this.HelpCategories.Copied[i];
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
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    }
                    e.clickRectHover = 1;
                    if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released && e.item is HelpTopic)
                    {
                        this.HelpEntries.Entries.Clear();
                        this.HelpEntries.Copied.Clear();
                        this.HelpEntries.indexAtTop = 0;
                        this.ActiveTopic = e.item as HelpTopic;
                        if (this.ActiveTopic.Text != null)
                        {
                            HelperFunctions.parseTextToSL(this.ActiveTopic.Text, (float)(this.TextRect.Width - 40), Fonts.Arial12Bold, ref this.HelpEntries);
                            this.TitlePosition = new Vector2((float)(this.TextRect.X + this.TextRect.Width / 2) - Fonts.Arial20Bold.MeasureString(this.ActiveTopic.Title).X / 2f - 15f, this.TitlePosition.Y);
                        }
                        if (!String.IsNullOrEmpty(ActiveTopic.Link))
                        {
                            try

                            {
                                if (SteamManager.isInitialized)
                                    SteamManager.ActivateOverlayWebPage(ActiveTopic.Link);
                                else
                                    Process.Start(ActiveTopic.Link);
                            }
                            catch { }
                        }
                        if (this.ActiveTopic.VideoPath == null)
                        {
                            this.ActiveVideo = null;
                            this.VideoPlayer = null;
                        }
                        else
                        {
                            this.HelpEntries.Copied.Clear();
                            this.VideoPlayer = new VideoPlayer();
                            this.ActiveVideo = TransientContent.Load<Video>(string.Concat("Video/", this.ActiveTopic.VideoPath));
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
            TitleText += $" {GlobalStats.ExtendedVersion}";
            Vector2 vector2 = new Vector2((float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84), (float)(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100));
            this.CategoriesRect = new Rectangle(this.R.X + 25, this.R.Y + 130, 330, 430);
            Submenu blah = new Submenu(base.ScreenManager, this.CategoriesRect);
            this.HelpCategories = new ScrollList(blah, 40);
            this.TextRect = new Rectangle(this.CategoriesRect.X + this.CategoriesRect.Width + 5, this.CategoriesRect.Y + 10, 375, 420);
            Rectangle TextSLRect = new Rectangle(this.CategoriesRect.X + this.CategoriesRect.Width + 5, this.CategoriesRect.Y + 10, 375, 420);
            Submenu bler = new Submenu(base.ScreenManager, TextSLRect);
            this.HelpEntries = new ScrollList(bler, Fonts.Arial12Bold.LineSpacing + 2);
            this.SmallViewer = new Rectangle(this.TextRect.X + 20, this.TextRect.Y + 40, 336, 189);
            this.BigViewer = new Rectangle(base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, base.ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
            this.VideoPlayer = new Microsoft.Xna.Framework.Media.VideoPlayer();
            this.ActiveTopic = new HelpTopic()
            {
                Title = Localizer.Token(1401),
                Text = Localizer.Token(1400)
            };
            HelperFunctions.parseTextToSL(this.ActiveTopic.Text, (float)(this.TextRect.Width - 40), Fonts.Arial12Bold, ref this.HelpEntries);
            this.TitlePosition = new Vector2((float)(this.TextRect.X + this.TextRect.Width / 2) - Fonts.Arial20Bold.MeasureString(this.ActiveTopic.Title).X / 2f - 15f, (float)(this.TextRect.Y + 10));
            Array<string> Categories = new Array<string>();
            foreach (HelpTopic halp in this.HelpTopics.HelpTopicsList)
            {
                if (Categories.Contains(halp.Category))
                {
                    continue;
                }
                Categories.Add(halp.Category);
                ModuleHeader mh = new ModuleHeader(halp.Category, 295f);
                this.HelpCategories.AddItem(mh);
            }
            foreach (ScrollList.Entry e in this.HelpCategories.Entries)
            {
                foreach (HelpTopic halp in this.HelpTopics.HelpTopicsList)
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
    }
}