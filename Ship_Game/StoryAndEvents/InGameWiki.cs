using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class InGameWiki : PopupWindow
    {
        private readonly HelpTopics HelpTopics;
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

        public InGameWiki(GameScreen parent) : base(parent, 750, 600)
        {
            IsPopup           = true;
            TransitionOnTime  = TimeSpan.FromSeconds(0.25);
            TransitionOffTime = TimeSpan.FromSeconds(0.25);
            var help          = ResourceManager.GatherFilesModOrVanilla("HelpTopics/" + GlobalStats.Language,"xml");
            if (help.Length  != 0)
                HelpTopics    = help[0].Deserialize<HelpTopics>();

            TitleText  = Localizer.Token(2304);
            MiddleText = Localizer.Token(2303);
        }

        protected override void Destroy()
        {
            VideoPlayer?.Dispose(ref VideoPlayer);
            base.Destroy();
        }


        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch);

            ScreenManager.SpriteBatch.Begin();
            HelpCategories.Draw(ScreenManager.SpriteBatch);
            Vector2 bCursor;
            foreach (ScrollList.Entry e in HelpCategories.VisibleExpandedEntries)
            {
                bCursor = new Vector2(Rect.X + 35, e.Y);
                if (e.item is ModuleHeader header)
                {
                    header.Draw(ScreenManager, bCursor);
                }
                else if (e.item is HelpTopic help)
                {
                    bCursor.X += 15f;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold,
                        help.Title, bCursor, (e.Hovered ? Color.Orange : Color.White));

                    bCursor.Y += Fonts.Arial12Bold.LineSpacing;
                    ScreenManager.SpriteBatch.DrawString(Fonts.Arial12,
                        help.ShortDescription, bCursor, (e.Hovered ? Color.White : Color.Orange));
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
            foreach (ScrollList.Entry e in HelpEntries.VisibleExpandedEntries)
            {
                bCursor.Y = e.Y;
                bCursor.X = (int)bCursor.X;
                bCursor.Y = (int)bCursor.Y;
                ScreenManager.SpriteBatch.DrawString(Fonts.Arial12Bold, (string)e.item, bCursor, Color.White);
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


        public override bool HandleInput(InputState input)
        {
            if (input.RightMouseClick || input.Escaped)
            {
                ExitScreen();
                return true;
            }

            if (input.ExitWiki)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }
            HelpCategories.HandleInput(input);
            HelpEntries.HandleInput(input);
            if (ActiveVideo != null)
            {
                if (VideoPlayer.State == MediaState.Paused)
                {
                    if (!SmallViewer.HitTest(input.CursorPosition))
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
                    if (BigViewer.HitTest(input.CursorPosition) && input.InGameSelect)
                    {
                        VideoPlayer.Pause();
                        GameAudio.ResumeGenericMusic();
                    }
                }
            }

            foreach (ScrollList.Entry e in HelpCategories.AllExpandedEntries)
            {
                if (e.item is ModuleHeader header)
                    if (header.HandleInput(input, e))
                        break;
                    else if (e.CheckHover(input))
                    {
                        if (input.LeftMouseClick && e.item is HelpTopic)
                        {
                            HelpEntries.Reset();
                            ActiveTopic = (HelpTopic)e.item;
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
                                HelpEntries.Reset();
                                VideoPlayer = new VideoPlayer();
                                ActiveVideo = TransientContent.Load<Video>(string.Concat("Video/", ActiveTopic.VideoPath));
                                VideoPlayer.Play(ActiveVideo);
                                VideoPlayer.Pause();
                            }
                        }
                    }
            }
            return base.HandleInput(input);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            TitleText += $" {GlobalStats.ExtendedVersion}";
            if (GlobalStats.HasMod)
            {
                MiddleText = $"Mod Loaded: {GlobalStats.ModName} Ver: {GlobalStats.ActiveModInfo.Version}";
            }
            var presentation = ScreenManager.GraphicsDevice.PresentationParameters;

            CategoriesRect       = new Rectangle(Rect.X + 25, Rect.Y + 130, 330, 430);
            Submenu blah         = new Submenu(CategoriesRect);
            HelpCategories       = new ScrollList(blah, 40);
            TextRect             = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            Rectangle textSlRect = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            Submenu bler         = new Submenu(textSlRect);
            HelpEntries          = new ScrollList(bler, Fonts.Arial12Bold.LineSpacing + 2);
            SmallViewer          = new Rectangle(TextRect.X + 20, TextRect.Y + 40, 336, 189);
            BigViewer            = new Rectangle(presentation.BackBufferWidth / 2 - 640, presentation.BackBufferHeight / 2 - 360, 1280, 720);
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

            var categories = new HashSet<string>();
            foreach (HelpTopic halp in HelpTopics.HelpTopicsList)
            {
                if (categories.Add(halp.Category))
                {
                    ScrollList.Entry e = HelpCategories.AddItem(new ModuleHeader(halp.Category, 295));
                    e.AddSubItem(halp);
                }
            }
        }
    }
}