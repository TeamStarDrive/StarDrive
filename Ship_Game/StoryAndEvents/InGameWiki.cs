using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Diagnostics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.GameScreens;
using Ship_Game.Utils;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed class InGameWiki : PopupWindow
    {
        readonly HelpTopics HelpTopics;
        ScrollList<WikiHelpCategoryListItem> HelpCategories;
        RectF TextRect;
        Vector2 TitlePosition;
        UITextBox HelpEntries;

        ScreenMediaPlayer Player;
        RectF SmallViewer;
        RectF BigViewer;
        HelpTopic ActiveTopic;

        public InGameWiki(GameScreen parent) : base(parent, 750, 600)
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
            var help = ResourceManager.GatherFilesModOrVanilla("HelpTopics/" + GlobalStats.Language,"xml");
            if (help.Length  != 0)
                HelpTopics    = help[0].Deserialize<HelpTopics>();

            TitleText  = Localizer.Token(GameText.StardriveHelp2);
            MiddleText = Localizer.Token(GameText.ThisHelpMenuContainsInformation);
        }

        public override void LoadContent()
        {
            base.LoadContent();

            TitleText += $" {GlobalStats.ExtendedVersion}";
            if (GlobalStats.HasMod)
            {
                MiddleText = $"Mod Loaded: {GlobalStats.ModName} Ver: {GlobalStats.ActiveMod.Mod.Version}";
            }

            ActiveTopic = new HelpTopic
            {
                Title = Localizer.Token(GameText.StardriveHelp),
                Text  = Localizer.Token(GameText.SelectATopicOnThe)
            };

            RectF CategoriesRect = new(Rect.X + 25, MidSepBot.Y + 10, 330, 430);
            HelpCategories = Add(new ScrollList<WikiHelpCategoryListItem>(CategoriesRect));

            RectF textSlRect = new(CategoriesRect.X + CategoriesRect.W + 5, CategoriesRect.Y + 10, 375, 420);
            HelpEntries = Add(new UITextBox(textSlRect, useBorder:false));
            TextRect = new(HelpCategories.X + HelpCategories.Width + 5, HelpCategories.Y + 10, 375, 420);

            ResetActiveTopic();

            SmallViewer = new(TextRect.X + 20, TextRect.Y + 40, 336, 189);
            BigViewer = new(ScreenWidth / 2 - 640, ScreenHeight / 2 - 360, 1280, 720);
            Player = new(ContentManager)
            {
                EnableInteraction = true,
                OnPlayStatusChange = OnPlayerStatusChanged
            };

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
        
        void ResetActiveTopic()
        {
            HelpEntries.SetLines(ActiveTopic.Text, Fonts.Arial12Bold, Color.White);
            float titleW = Fonts.Arial20Bold.TextWidth(ActiveTopic.Title);
            TitlePosition = new(TextRect.CenterX - titleW / 2f - 15f, TextRect.Y + 10);
        }

        void OnHelpCategoryClicked(WikiHelpCategoryListItem item)
        {
            if (item.Topic == null)
            {
                Player.Stop();
                Player.Visible = false;
                return;
            }

            HelpEntries.Clear();
            ActiveTopic = item.Topic;

            if (ActiveTopic.Text != null)
            {
                ResetActiveTopic();
            }

            if (ActiveTopic.Link.NotEmpty())
            {
                Log.OpenURL(ActiveTopic.Link);
            }

            if (ActiveTopic.VideoPath == null)
            {
                Player.Stop();
                Player.Visible = false;
            }
            else
            {
                HelpEntries.Clear();
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

            if (!GlobalStats.TakingInput && input.ExitWiki)
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

            batch.SafeBegin();

            Player.Draw(batch);
            if (Player.IsPaused)
            {
                batch.DrawRectangleGlow(Player.Rect);
                batch.DrawString(Fonts.Arial20Bold, ActiveTopic.Title, TitlePosition, Color.Orange);
            }

            batch.SafeEnd();
        }

        public override void ExitScreen()
        {
            Player.Stop();
            base.ExitScreen();
        }

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;
            Player.Dispose();
            base.Dispose(disposing); // sets IsDisposed = true
        }
    }
}
