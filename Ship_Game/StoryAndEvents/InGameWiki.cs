using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ship_Game.Audio;
using Ship_Game.GameScreens;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class InGameWiki : PopupWindow
    {
        readonly HelpTopics HelpTopics;
        ScrollList2<WikiHelpCategoryListItem> HelpCategories;
        Rectangle CategoriesRect;
        Rectangle TextRect;
        Vector2 TitlePosition;
        ScrollList2<TextListItem> HelpEntries;

        ScreenMediaPlayer Player;
        Rectangle SmallViewer;
        Rectangle BigViewer;
        HelpTopic ActiveTopic;

        public InGameWiki(GameScreen parent) : base(parent, 750, 600)
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
            var help = ResourceManager.GatherFilesModOrVanilla("HelpTopics/" + GlobalStats.Language,"xml");
            if (help.Length  != 0)
                HelpTopics    = help[0].Deserialize<HelpTopics>();

            TitleText  = Localizer.Token(2304);
            MiddleText = Localizer.Token(2303);
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

            CategoriesRect = new Rectangle(Rect.X + 25, Rect.Y + 130, 330, 430);
            HelpCategories = Add(new ScrollList2<WikiHelpCategoryListItem>(CategoriesRect));
            TextRect       = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            var textSlRect = new Rectangle(CategoriesRect.X + CategoriesRect.Width + 5, CategoriesRect.Y + 10, 375, 420);
            HelpEntries = Add(new ScrollList2<TextListItem>(textSlRect, Fonts.Arial12Bold.LineSpacing + 2));
            SmallViewer = new Rectangle(TextRect.X + 20, TextRect.Y + 40, 336, 189);
            BigViewer   = new Rectangle(ScreenWidth / 2 - 640, ScreenHeight / 2 - 360, 1280, 720);
            Player = new ScreenMediaPlayer(ContentManager)
            {
                EnableInteraction = true,
                OnPlayStatusChange = OnPlayerStatusChanged
            };
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
                        new WikiHelpCategoryListItem(topic.Category)
                    );
                    cat.AddSubItem(new WikiHelpCategoryListItem(topic));
                }
            }
            HelpCategories.OnClick = OnHelpCategoryClicked;
        }

        void OnHelpCategoryClicked(WikiHelpCategoryListItem item)
        {
            if (item.Topic == null)
            {
                Player.Stop();
                Player.Visible = false;
                return;
            }

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
                Player.Stop();
                Player.Visible = false;
            }
            else
            {
                HelpEntries.Reset();
                Player.PlayVideo(ActiveTopic.VideoPath, looping: false, startPaused: true);
                Player.Visible = true;
            }
        }
        
        void OnPlayerStatusChanged()
        {
            Player.Rect = Player.IsPlaying ? BigViewer : SmallViewer;
        }

        public override bool HandleInput(InputState input)
        {
            if (Player.HandleInput(input))
                return true;

            if (input.ExitWiki)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            base.Draw(batch, elapsed);

            batch.Begin();

            Player.Draw(batch);
            if (Player.IsPaused)
            {
                batch.DrawRectangleGlow(Player.Rect);
                batch.DrawString(Fonts.Arial20Bold, ActiveTopic.Title, TitlePosition, Color.Orange);
            }

            batch.End();
        }

        public override void ExitScreen()
        {
            Player.Stop();
            base.ExitScreen();
        }
    }
}