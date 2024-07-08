using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel5 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly Font Font;
        readonly UILabel LevelDescription;
        readonly UILabel PassiveTitle, Passive, ActiveTitle;
        readonly UICheckBox RebellionBox, StealTechBox;
        readonly int LevelDescriptionY, PassiveY;
        const int Level = 5;
        Ship_Game.Espionage Espionage;
        bool IncitingRebellion, StealingTech;

        public InfiltrationOpsLevel5(InfiltrationScreen screen, Empire player, in Rectangle rect, int levelDescY, int passiveY, Font font)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Font = font;
            LevelDescription = Add(new UILabel("", Font, Color.Wheat));
            PassiveTitle = Add(new UILabel(GameText.Passive, Font, Color.Wheat));
            ActiveTitle = Add(new UILabel(GameText.Active, Font, Color.Wheat));
            Passive = Add(new UILabel(GameText.EspioangeOpsLeechIncome, Font, Color.Gray));
            RebellionBox = Add(new UICheckBox(() => IncitingRebellion, Font, GameText.EspioangeOpsRebellion, GameText.EspioangeOpsRebellionTip));
            StealTechBox = Add(new UICheckBox(() => StealingTech, Font, GameText.EspioangeOpsSlowResearch, GameText.EspioangeOpsSlowResearchTip));
            RebellionBox.OnChange = Rebellion;
            StealTechBox.OnChange = StealTech;
            RebellionBox.CheckedTextColor = StealTechBox.CheckedTextColor = player.EmpireColor;

            Passive.Tooltip = GameText.EspioangeOpsLeechIncomeTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel4Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos = new Vector2(Rect.X + 5, PassiveY);
            Passive.Pos = new Vector2(Rect.X + 75, PassiveTitle.Pos.Y);
            ActiveTitle.Pos = new Vector2(Rect.X + 5, PassiveY + Font.LineSpacing + 2);
            RebellionBox.Pos = new Vector2(Rect.X + 75, ActiveTitle.Y);
            StealTechBox.Pos = new Vector2(Rect.X + 75, ActiveTitle.Y + Font.LineSpacing + 2);

            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level ? Player.EmpireColor : Color.Gray;
                RebellionBox.Enabled = StealTechBox.Enabled = Espionage.Level >= Level;
                RebellionBox.TextColor = StealTechBox.TextColor = RebellionBox.Enabled ? Color.White : Color.Gray;
                LevelDescription.Color = RebellionBox.Enabled ? Player.EmpireColor : Color.Gray;
                IncitingRebellion = Espionage.IsOperationActive(InfiltrationOpsType.Rebellion);
                StealingTech = Espionage.IsOperationActive(InfiltrationOpsType.StealTech);
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }

        void Rebellion(UICheckBox b)
        {
            if (IncitingRebellion)
                Espionage.AddOperation(InfiltrationOpsType.Rebellion);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.Rebellion);
        }

        void StealTech(UICheckBox b)
        {
            if (StealingTech)
                Espionage.AddOperation(InfiltrationOpsType.StealTech);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.StealTech);
        }
    }
}
