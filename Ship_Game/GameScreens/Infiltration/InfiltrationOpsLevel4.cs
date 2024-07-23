using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel4 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly Font Font;
        readonly UILabel LevelDescription;
        readonly UILabel PassiveTitle, Passive, ActiveTitle;
        readonly UICheckBox SabotageBox, SlowBox;
        readonly UILabel SabotageTurnsRemaining, SlowResearchTurnsRemaining;
        readonly int LevelDescriptionY, PassiveY;
        const int Level = 4;
        Ship_Game.Espionage Espionage;
        bool Sabotaging, SlowingResearch;

        public InfiltrationOpsLevel4(InfiltrationScreen screen, Empire player, in Rectangle rect, int levelDescY, int passiveY, Font font)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Font   = font;
            LevelDescription = Add(new UILabel("", Font, Color.Wheat));
            PassiveTitle     = Add(new UILabel(GameText.Passive, Font, Color.Wheat));
            ActiveTitle      = Add(new UILabel(GameText.Active, Font, Color.Wheat));
            Passive          = Add(new UILabel(GameText.EspioangeOpsLeechTech, Font, Color.Gray));
            SabotageBox      = Add(new UICheckBox(() => Sabotaging, Font, GameText.Sabotage, GameText.EspioangeOpsSabotageTip));
            SlowBox          = Add(new UICheckBox(() => SlowingResearch, Font, GameText.EspioangeOpsSlowResearch, GameText.EspioangeOpsSlowResearchTip));
            SabotageBox.OnChange = Sabotage;
            SlowBox.OnChange     = SlowResearch;
            SabotageBox.CheckedTextColor = SlowBox.CheckedTextColor = Color.LightGreen;

            Passive.Tooltip = GameText.EspioangeOpsLeechTechTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;

            SabotageTurnsRemaining     = Add(new UILabel("", Font, Color.White));
            SlowResearchTurnsRemaining = Add(new UILabel("", Font, Color.White));
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos  = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description    = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel4Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos = new Vector2(Rect.X + 5, PassiveY);
            Passive.Pos      = new Vector2(Rect.X + 60, PassiveTitle.Pos.Y);
            ActiveTitle.Pos  = new Vector2(Rect.X + 5, PassiveY + Font.LineSpacing + 2);
            SabotageBox.Pos  = new Vector2(Rect.X + 55, ActiveTitle.Y);
            SlowBox.Pos      = new Vector2(Rect.X + 55, ActiveTitle.Y + Font.LineSpacing + 2);
            SabotageTurnsRemaining.Pos = new Vector2(Rect.Right - 80, ActiveTitle.Y);
            SlowResearchTurnsRemaining.Pos = new Vector2(Rect.Right - 80, SlowBox.Y);

            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage     = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level && Espionage.LimitLevel >= Level ? Color.LightGreen : Color.Gray;
                SabotageBox.Enabled    = SlowBox.Enabled = Espionage.Level >= Level;
                SabotageBox.TextColor  = SlowBox.TextColor = SabotageBox.Enabled ? Color.White : Color.Gray;
                LevelDescription.Color = SabotageBox.Enabled ? Player.EmpireColor : Color.Gray;
                Sabotaging      = Espionage.IsOperationActive(InfiltrationOpsType.Sabotage);
                SlowingResearch = Espionage.IsOperationActive(InfiltrationOpsType.SlowResearch);
                SabotageTurnsRemaining.Visible = SlowResearchTurnsRemaining.Visible = Espionage.Level >= Level;
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            SabotageTurnsRemaining.Color = SabotageBox.Checked ? Color.LightGreen : Color.White;
            SabotageTurnsRemaining.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.Sabotage);
            SabotageTurnsRemaining.Pos  = HelperFunctions.GetRightAlignedPosForTitle(SabotageTurnsRemaining.Text.Text,
                SabotageTurnsRemaining.Font, Rect.Right, SabotageTurnsRemaining.Y);

            SlowResearchTurnsRemaining.Color = SlowBox.Checked ? Color.LightGreen : Color.White;
            SlowResearchTurnsRemaining.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.SlowResearch);
            SlowResearchTurnsRemaining.Pos  = HelperFunctions.GetRightAlignedPosForTitle(SlowResearchTurnsRemaining.Text.Text,
                SlowResearchTurnsRemaining.Font, Rect.Right, SlowResearchTurnsRemaining.Y);

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }

        void Sabotage(UICheckBox b)
        {
            if (Sabotaging)
                Espionage.AddOperation(InfiltrationOpsType.Sabotage);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.Sabotage);
        }

        void SlowResearch(UICheckBox b)
        {
            if (SlowingResearch)
                Espionage.AddOperation(InfiltrationOpsType.SlowResearch);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.SlowResearch);
        }
    }
}
