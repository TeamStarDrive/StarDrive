using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;



namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel2 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly Font Font;
        readonly UILabel LevelDescription;
        readonly UILabel PassiveTitle, Passive, ActiveTitle;
        readonly UICheckBox PlantMoleBox;
        readonly UILabel PlantMoleTurnsRemaning;
        readonly int LevelDescriptionY, PassiveY;
        const int Level = 2;
        bool PlantingMole;
        Ship_Game.Espionage Espionage;

        public InfiltrationOpsLevel2(InfiltrationScreen screen, Empire player, in Rectangle rect, int levelDescY, int passiveY, Font font)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Font   = font;
            LevelDescription  = Add(new UILabel("", Font, Color.Wheat));
            PassiveTitle      = Add(new UILabel(GameText.Passive, Font, Color.Wheat));
            ActiveTitle       = Add(new UILabel(GameText.Active, Font, Color.Wheat));
            Passive           = Add(new UILabel(GameText.EspionageOpsProjectorsAlert, Font, Color.Gray));
            PlantMoleBox      = Add(new UICheckBox(() => PlantingMole, Font, GameText.PlantAgent, GameText.PlantAgentTip));
            PlantMoleBox.OnChange = PlantMole;
            PlantMoleBox.CheckedTextColor = Color.LightGreen;
            Passive.Tooltip   = GameText.EspionageOpsProjectorsAlertTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;
            PlantMoleTurnsRemaning = Add(new UILabel("", Font, Color.Wheat));
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos  = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description    = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel2Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos      = new Vector2(Rect.X + 5, PassiveY);
            ActiveTitle.Pos       = new Vector2(Rect.X + 5, PassiveY + Font.LineSpacing + 2);
            PlantMoleBox.Pos      = new Vector2(Rect.X + 55, ActiveTitle.Y);
            Passive.Pos           = new Vector2(Rect.X + 60, PassiveTitle.Pos.Y);
            PlantMoleTurnsRemaning.Pos = new Vector2(Rect.Right -80, ActiveTitle.Y);

            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage     = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level ? Color.LightGreen : Color.Gray;
                PlantMoleBox.Enabled   = Espionage.Level >= Level;
                PlantMoleBox.TextColor = PlantMoleBox.Enabled ? Color.Red : Color.Gray;
                LevelDescription.Color = PlantMoleBox.Enabled ? Player.EmpireColor : Color.Gray;
                PlantingMole           = Espionage.IsOperationActive(InfiltrationOpsType.PlantMole);
                PlantMoleTurnsRemaning.Visible = Espionage.Level >= Level;
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            PlantMoleTurnsRemaning.Color = PlantMoleBox.Checked ? Color.LightGreen : Color.Red;
            PlantMoleTurnsRemaning.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.PlantMole);
            PlantMoleTurnsRemaning.Pos = HelperFunctions.GetRightAlignedPosForTitle(PlantMoleTurnsRemaning.Text.Text, 
                PlantMoleTurnsRemaning.Font, Rect.Right, ActiveTitle.Y);

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }

        void PlantMole(UICheckBox b)
        {
            if (PlantingMole)
                Espionage.AddOperation(InfiltrationOpsType.PlantMole);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.PlantMole);
        }
    }
}
