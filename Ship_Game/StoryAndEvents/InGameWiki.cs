using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ship_Game.Audio;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class InGameWiki : PopupWindow
    {
        private readonly HelpTopics HelpTopics;
        private ScrollList<WikiHelpCategoryListItem> HelpCategories;
        private Rectangle CategoriesRect;
        private Rectangle TextRect;
        private Vector2 TitlePosition;
        private ScrollList<TextListItem> HelpEntries;
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
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
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

        

        void InitHelpEntries()
        {
            HelpEntries.ResetWithParseText(Fonts.Arial12Bold, ActiveTopic.Text, TextRect.Width - 40);
            TitlePosition = new Vector2((TextRect.X + TextRect.Width / 2)
                - Fonts.Arial20Bold.MeasureString(ActiveTopic.Title).X / 2f - 15f, TextRect.Y + 10);
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
            HelpCategories       = new ScrollList<WikiHelpCategoryListItem>(blah, 40);
            TextRect             = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            Rectangle textSlRect = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            Submenu bler = new Submenu(textSlRect);
            HelpEntries = new ScrollList<TextListItem>(bler, Fonts.Arial12Bold.LineSpacing + 2);
            SmallViewer = new Rectangle(TextRect.X + 20, TextRect.Y + 40, 336, 189);
            BigViewer   = new Rectangle(presentation.BackBufferWidth / 2 - 640, presentation.BackBufferHeight / 2 - 360, 1280, 720);
            VideoPlayer = new VideoPlayer();
            ActiveTopic = new HelpTopic
            {
                Title = Localizer.Token(1401),
                Text  = Localizer.Token(1400)
            };

            InitHelpEntries();

            var categories = new HashSet<string>();
            foreach (HelpTopic topic in HelpTopics.HelpTopicsList)
            {
                if (categories.Add(topic.Category))
                {
                    WikiHelpCategoryListItem cat = HelpCategories.AddItem(
                        new WikiHelpCategoryListItem{Header = new ModuleHeader(topic.Category, 295)}
                    );
                    cat.AddSubItem(new WikiHelpCategoryListItem{ Topic = topic });
                }
            }
            HelpCategories.OnClick = OnHelpCategoryClicked;
        }

        void OnHelpCategoryClicked(WikiHelpCategoryListItem item)
        {
            if (item.Topic == null)
                return;

            HelpEntries.Reset();
            ActiveTopic = item.Topic;
            if (ActiveTopic.Text != null)
            {
                HelpEntries.ResetWithParseText(Fonts.Arial12Bold, ActiveTopic.Text, TextRect.Width - 40);
                TitlePosition = new Vector2((TextRect.X + TextRect.Width / 2)
                    - Fonts.Arial20Bold.MeasureString(ActiveTopic.Title).X / 2f - 15f, TitlePosition.Y);
            }
            if (ActiveTopic.Link.NotEmpty())
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
                ActiveVideo = TransientContent.Load<Video>("Video/"+ActiveTopic.VideoPath);
                VideoPlayer.Play(ActiveVideo);
                VideoPlayer.Pause();
            }
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

            if (HelpCategories.HandleInput(input))
                return true;

            if (HelpEntries.HandleInput(input))
                return true;

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
            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch);

            batch.Begin();
            HelpCategories.Draw(batch);

            if (ActiveVideo != null)
            {
                if (VideoPlayer.State != MediaState.Playing)
                {
                    VideoFrame = VideoPlayer.GetTexture();
                    batch.Draw(VideoFrame, SmallViewer, Color.White);
                    batch.DrawRectangleGlow(SmallViewer);
                    batch.DrawRectangle(SmallViewer, new Color(32, 30, 18));
                    if (HoverSmallVideo)
                    {
                        Rectangle playIcon = new Rectangle(SmallViewer.X + SmallViewer.Width / 2 - 64,
                            SmallViewer.Y + SmallViewer.Height / 2 - 64, 128, 128);
                        batch.Draw(ResourceManager.Texture("icon_play"),
                            playIcon, new Color(255, 255, 255, 200));
                    }
                }
                else
                {
                    VideoFrame = VideoPlayer.GetTexture();
                    batch.Draw(VideoFrame, BigViewer, Color.White);
                    batch.DrawRectangle(BigViewer, new Color(32, 30, 18));
                }
            }

            HelpEntries.Draw(batch);

            if (VideoPlayer != null && VideoPlayer.State != MediaState.Playing)
            {
                batch.DrawString(Fonts.Arial20Bold, ActiveTopic.Title, TitlePosition, Color.Orange);
                GameAudio.ResumeGenericMusic();
            }
            else if (VideoPlayer == null)
            {
                batch.DrawString(Fonts.Arial20Bold, ActiveTopic.Title, TitlePosition, Color.Orange);
            }

            batch.End();
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
    }
}