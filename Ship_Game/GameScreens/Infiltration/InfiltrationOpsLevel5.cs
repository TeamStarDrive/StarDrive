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
        readonly UICheckBox RebellionBox, ProjectionBox;
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
            RebellionBox.CheckedTextColor = ProjectionBox.CheckedTextColor = player.EmpireColor;

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
            ProjectionBox.Pos = new Vector2(Rect.X + 75, ActiveTitle.Y + Font.LineSpacing + 2);

            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level ? Player.EmpireColor : Color.Gray;
                RebellionBox.Enabled = ProjectionBox.Enabled = Espionage.Level >= Level;
                RebellionBox.TextColor = ProjectionBox.TextColor = RebellionBox.Enabled ? Color.White : Color.Gray;
                LevelDescription.Color = RebellionBox.Enabled ? Player.EmpireColor : Color.Gray;
                IncitingRebellion = Espionage.IsOperationActive(InfiltrationOpsType.Rebellion);
                DistuptingProjection = Espionage.IsOperationActive(InfiltrationOpsType.DisruptProjection);
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
            if (DistuptingProjection)
                Espionage.AddOperation(InfiltrationOpsType.DisruptProjection);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.DisruptProjection);
        }
    }
}
