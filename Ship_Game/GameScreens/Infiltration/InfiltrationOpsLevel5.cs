using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;
using SDUtils;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel5 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly Font Font;
        readonly UILabel LevelDescription;
        readonly UILabel PassiveTitle, Passive, ActiveTitle;
        readonly UICheckBox RebellionBox, ProjectionBox;
        readonly UILabel RebellionTurnsRemaining, ProjectionTurnsRemaining, MoneyLeeched;
        readonly int LevelDescriptionY, PassiveY;
        const int Level = 5;
        Ship_Game.Espionage Espionage;
        bool IncitingRebellion, DistuptingProjection;

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
            ProjectionBox = Add(new UICheckBox(() => DistuptingProjection, Font, GameText.EspioangeOpsDisruptProjection, GameText.EspioangeOpsDisruptProjectionTip));
            RebellionBox.OnChange = Rebellion;
            ProjectionBox.OnChange = StealTech;
            RebellionBox.CheckedTextColor = ProjectionBox.CheckedTextColor = Color.LightGreen;

            Passive.Tooltip = GameText.EspioangeOpsLeechIncomeTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;

            RebellionTurnsRemaining  = Add(new UILabel("", Font, Color.White));
            ProjectionTurnsRemaining = Add(new UILabel("", Font, Color.White));
            MoneyLeeched = Add(new UILabel("", Font, Color.White));
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos  = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description    = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel5Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos  = new Vector2(Rect.X + 5, PassiveY);
            Passive.Pos       = new Vector2(Rect.X + 60, PassiveTitle.Pos.Y);
            ActiveTitle.Pos   = new Vector2(Rect.X + 5, PassiveY + Font.LineSpacing + 2);
            RebellionBox.Pos  = new Vector2(Rect.X + 55, ActiveTitle.Y);
            ProjectionBox.Pos = new Vector2(Rect.X + 55, ActiveTitle.Y + Font.LineSpacing + 2);
            RebellionTurnsRemaining.Pos  = new Vector2(Rect.Right - 80, ActiveTitle.Y);
            ProjectionTurnsRemaining.Pos = new Vector2(Rect.Right - 80, ProjectionBox.Y);


            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level && Espionage.LimitLevel >= Level ? Color.LightGreen : Color.Gray;
                RebellionBox.Enabled = ProjectionBox.Enabled = Espionage.Level >= Level;
                RebellionBox.TextColor = ProjectionBox.TextColor = RebellionBox.Enabled ? Color.White : Color.Gray;
                LevelDescription.Color = RebellionBox.Enabled ? Player.EmpireColor : Color.Gray;
                IncitingRebellion = Espionage.IsOperationActive(InfiltrationOpsType.Rebellion);
                DistuptingProjection = Espionage.IsOperationActive(InfiltrationOpsType.DisruptProjection);
                RebellionTurnsRemaining.Visible = ProjectionTurnsRemaining.Visible = Espionage.Level >= Level;
                MoneyLeeched.Text = $"({HelperFunctions.GetNumberString(Espionage.TotalMoneyLeeched)} bc)";
                MoneyLeeched.Pos = HelperFunctions.GetRightAlignedPosForTitle(MoneyLeeched.Text.Text,
                    MoneyLeeched.Font, Rect.Right, Passive.Y); 
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            RebellionTurnsRemaining.Color = RebellionBox.Checked ? Color.LightGreen : Color.White;
            RebellionTurnsRemaining.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.Rebellion);
            RebellionTurnsRemaining.Pos  = HelperFunctions.GetRightAlignedPosForTitle(RebellionTurnsRemaining.Text.Text,
                RebellionTurnsRemaining.Font, Rect.Right, RebellionTurnsRemaining.Y);

            ProjectionTurnsRemaining.Color = ProjectionBox.Checked ? Color.LightGreen : Color.White;
            ProjectionTurnsRemaining.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.DisruptProjection);
            ProjectionTurnsRemaining.Pos  = HelperFunctions.GetRightAlignedPosForTitle(ProjectionTurnsRemaining.Text.Text,
                ProjectionTurnsRemaining.Font, Rect.Right, ProjectionTurnsRemaining.Y);

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
            if (DistuptingProjection)
                Espionage.AddOperation(InfiltrationOpsType.DisruptProjection);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.DisruptProjection);

            Player.UpdateEspionageDefenseRatio();
        }
    }
}
