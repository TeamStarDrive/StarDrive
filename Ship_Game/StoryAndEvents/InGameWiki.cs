using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
            IsPopup           = true;
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            var help          = ResourceManager.GatherFilesModOrVanilla("HelpTopics/" + GlobalStats.Language,"xml");
            if (help.Length  != 0)
                HelpTopics    = help[0].Deserialize<HelpTopics>();
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
            for (int i = HelpCategories.indexAtTop; i < HelpCategories.Copied.Count 
                && i < HelpCategories.indexAtTop + HelpCategories.entriesToDisplay; i++)
            {
                bCursor = new Vector2(R.X + 35, R.Y + 20);
                ScrollList.Entry e = HelpCategories.Copied[i];
                bCursor.Y = e.clickRect.Y;
                if (!(e.item is ModuleHeader))
                {
                    bCursor.X = bCursor.X + 15f;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, 
                        ((HelpTopic) e.item).Title, bCursor, (e.clickRectHover == 1 ? Color.Orange : Color.White));
                    bCursor.Y = bCursor.Y + Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12, 
                        ((HelpTopic) e.item).ShortDescription, bCursor, (e.clickRectHover == 1 ? Color.White : Color.Orange));
                }
                else
                {
                    ((ModuleHeader) e.item).Draw(ScreenManager, bCursor);
                }
            }
            bCursor = new Vector2(TextRect.X, TextRect.Y + 20);
            if (ActiveVideo != null)
            {
                if (VideoPlayer.State != MediaState.Playing)
                {
                    VideoFrame = VideoPlayer.GetTexture();
                    ScreenManager.SpriteBatch.Draw(VideoFrame, SmallViewer, Color.White);
                    ScreenManager.SpriteBatch.DrawRectangleGlow(SmallViewer);
                    ScreenManager.SpriteBatch.DrawRectangle(SmallViewer, new Color(32, 30, 18));
                    if (HoverSmallVideo)
                    {
                        Rectangle playIcon = new Rectangle(SmallViewer.X + SmallViewer.Width / 2 - 64, 
                            SmallViewer.Y + SmallViewer.Height / 2 - 64, 128, 128);
                        ScreenManager.SpriteBatch.Draw(ResourceManager.Texture("icon_play"), 
                            playIcon, new Color(255, 255, 255, 200));
                    }
                }
                else
                {
                    VideoFrame = VideoPlayer.GetTexture();
                    ScreenManager.SpriteBatch.Draw(VideoFrame, BigViewer, Color.White);
                    ScreenManager.SpriteBatch.DrawRectangle(BigViewer, new Color(32, 30, 18));
                }
            }
            HelpEntries.Draw(ScreenManager.SpriteBatch);
            for (int i = HelpEntries.indexAtTop; i < HelpEntries.Copied.Count && i < HelpEntries.indexAtTop + HelpEntries.entriesToDisplay; i++)
            {
                ScrollList.Entry e = HelpEntries.Copied[i];
                bCursor.Y = e.clickRect.Y;
                bCursor.X = (int)bCursor.X;
                bCursor.Y = (int)bCursor.Y;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (string) e.item, bCursor, Color.White);
            }
            if (VideoPlayer != null && VideoPlayer.State != MediaState.Playing)
            {
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, ActiveTopic.Title, TitlePosition, Color.Orange);
                GameAudio.ResumeGenericMusic();
            }
            else if (VideoPlayer == null)            
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial20Bold, ActiveTopic.Title, TitlePosition, Color.Orange);
            
            ScreenManager.SpriteBatch.End();
        }

        public override void ExitScreen()
        {
            if (VideoPlayer != null)
            {
                VideoPlayer.Stop();
                VideoPlayer = null;
                ActiveVideo = null;
                GameAudio.ResumeGenericMusic();
            }
            base.ExitScreen();
        }


        public override void HandleInput(InputState input)
        {
            if (input.RightMouseClick)            
                ExitScreen();
            
            if (input.Escaped)            
                ExitScreen();
            
            if (input.ExitWiki)
            {
                GameAudio.PlaySfxAsync("echo_affirm");
                ExitScreen();
            }
            HelpCategories.HandleInput(input);
            HelpEntries.HandleInput(input);
            if (ActiveVideo != null)
            {
                if (VideoPlayer.State == MediaState.Paused)
                {
                    if (!HelperFunctions.CheckIntersection(SmallViewer, input.CursorPosition))
                    {
                        HoverSmallVideo = false;
                    }
                    else
                    {
                        HoverSmallVideo = true;
                        if (input.InGameSelect)
                        {
                            VideoPlayer.Play(ActiveVideo);
                            GameAudio.PauseGenericMusic();
                        }
                    }
                }
                else if (VideoPlayer.State == MediaState.Playing)
                {
                    HoverSmallVideo = false;
                    if (HelperFunctions.CheckIntersection(BigViewer, input.CursorPosition) && input.InGameSelect)
                    {
                        VideoPlayer.Pause();
                        GameAudio.ResumeGenericMusic();
                    }
                }
            }
            for (int i = 0; i < HelpCategories.Copied.Count; i++)
            {
                ScrollList.Entry e = HelpCategories.Copied[i];
                if (e.item is ModuleHeader)                
                    ((ModuleHeader) e.item).HandleInput(input, e);
                else if (!HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))                
                    e.clickRectHover = 0;                
                else
                {
                    if (e.clickRectHover == 0)                    
                        GameAudio.PlaySfxAsync("sd_ui_mouseover");
                    
                    e.clickRectHover = 1;
                    if (input.LeftMouseClick && e.item is HelpTopic)
                    {
                        HelpEntries.Entries.Clear();
                        HelpEntries.Copied.Clear();
                        HelpEntries.indexAtTop = 0;
                        ActiveTopic = (HelpTopic) e.item;                        
                        if (ActiveTopic.Text != null)
                        {
                            HelperFunctions.parseTextToSL(ActiveTopic.Text, (TextRect.Width - 40), Fonts.Arial12Bold, ref HelpEntries);
                            TitlePosition = new Vector2((TextRect.X + TextRect.Width / 2) 
                                - Fonts.Arial20Bold.MeasureString(ActiveTopic.Title).X / 2f - 15f, TitlePosition.Y);
                        }
                        if (!string.IsNullOrEmpty(ActiveTopic.Link))
                        {
                            try
                            {
                                SteamManager.ActivateOverlayWebPage(ActiveTopic.Link);                                
                            }
                            catch
                            {
                                Process.Start(ActiveTopic.Link);
                            }
                        }
                        if (ActiveTopic.VideoPath == null)
                        {
                            ActiveVideo = null;
                            VideoPlayer = null;
                        }
                        else
                        {
                            HelpEntries.Copied.Clear();
                            VideoPlayer = new VideoPlayer();
                            ActiveVideo = TransientContent.Load<Video>(string.Concat("Video/", ActiveTopic.VideoPath));
                            VideoPlayer.Play(ActiveVideo);
                            VideoPlayer.Pause();
                        }
                    }
                }
            }
            base.HandleInput(input);
        }

        public override void LoadContent()
        {
            Setup();
            TitleText += $" {GlobalStats.ExtendedVersion}";
            if (GlobalStats.HasMod)
            {
                MiddleText =$"Mod Loaded: {GlobalStats.ModName} Ver: {GlobalStats.ActiveModInfo.Version}";
            }
            Vector2 vector2      = new Vector2(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 84, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 100);
            CategoriesRect       = new Rectangle(R.X + 25, R.Y + 130, 330, 430);
            Submenu blah         = new Submenu(ScreenManager, CategoriesRect);
            HelpCategories       = new ScrollList(blah, 40);
            TextRect             = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            Rectangle TextSLRect = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            Submenu bler         = new Submenu(ScreenManager, TextSLRect);
            HelpEntries          = new ScrollList(bler, Fonts.Arial12Bold.LineSpacing + 2);
            SmallViewer          = new Rectangle(TextRect.X + 20, TextRect.Y + 40, 336, 189);
            BigViewer            = new Rectangle(ScreenManager.GraphicsDevice.PresentationParameters.BackBufferWidth / 2 - 640, ScreenManager.GraphicsDevice.PresentationParameters.BackBufferHeight / 2 - 360, 1280, 720);
            VideoPlayer          = new VideoPlayer();
            ActiveTopic          = new HelpTopic
            {
                Title = Localizer.Token(1401),
                Text  = Localizer.Token(1400)
            };
            HelperFunctions.parseTextToSL(ActiveTopic.Text, TextRect.Width 
                - 40, Fonts.Arial12Bold, ref HelpEntries);
            TitlePosition = new Vector2(TextRect.X + TextRect.Width / 2 
                - Fonts.Arial20Bold.MeasureString(ActiveTopic.Title).X / 2f - 15f, TextRect.Y + 10);
            Array<string> categories = new Array<string>();
            foreach (HelpTopic halp in HelpTopics.HelpTopicsList)
            {
                if (categories.Contains(halp.Category))                
                    continue;
                
                categories.Add(halp.Category);
                ModuleHeader mh = new ModuleHeader(halp.Category, 295f);
                HelpCategories.AddItem(mh);
            }
            foreach (ScrollList.Entry e in HelpCategories.Entries)
            {
                foreach (HelpTopic halp in HelpTopics.HelpTopicsList)
                {
                    if (halp.Category != (e.item as ModuleHeader)?.Text)                    
                        continue;
                    
                    e.AddItem(halp);
                }
            }
            base.LoadContent();
        }
    }
}