using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;
using System;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;
using Ship_Game;
using SDGraphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class EspionageLevelPanel : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly UIPanel LevelPanel;
        readonly UILabel Title;
        readonly ProgressBar LevelProgress;
        readonly Font TitleFont, Font;
        readonly UIElementContainer LevelOps;
        int InfiltrationProgress;
        int LevelCost;
        readonly int Level;

        public EspionageLevelPanel(InfiltrationScreen screen, Empire player, in Rectangle rect, int level)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Level = level;
            LevelPanel = Add(new UIPanel(rect, InfiltrationScreen.PanelBackground));
            string title = $"Infiltration Level {level}";
            Title = Add(new UILabel(title, Fonts.Arial20Bold, Color.White));
            LevelProgress = new ProgressBar(rect, 50, 0);
            LevelProgress.color = "green";
            TitleFont = screen.LowRes ? Fonts.Arial12Bold : Fonts.Arial20Bold;
            Font = screen.LowRes ? Fonts.Arial8Bold : Fonts.Arial12Bold;
            switch (level) 
            {
                default:
                case 1: LevelOps = Add(new InfiltrationOpsLevel1(screen, player, rect)); break;
            }
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            Title.Pos = new Vector2(HelperFunctions.GetMiddlePosForTitle(Title.Text.Text, TitleFont, Rect.Width, Rect.X), Rect.Y + 35);
            var levelRect = new Rectangle(Rect.X + 5, Rect.Y + 5, Rect.Width - 10, 30);
            LevelProgress.SetRect(levelRect);
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);

        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            LevelProgress.Draw(batch);
        }

        public void RefreshEmpire()
        {
            if (Screen.SelectedEmpire == Player)
                return;

            Ship_Game.Espionage espionage = Player.GetRelations(Screen.SelectedEmpire).Espionage;
            LevelProgress.Max = espionage.LevelCost(Level);
            LevelProgress.Progress = espionage.Level >= Level ? LevelProgress.Max: espionage.LevelProgress;
        }
    }
}
