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
            PassiveTitle      = Add(new UILabel("Passive:", Font, Color.Wheat));
            ActiveTitle       = Add(new UILabel("Active:", Font, Color.Wheat));
            Passive           = Add(new UILabel(GameText.EspionageOpsProjectorsAlert, Font, Color.Gray));
            PlantMoleBox      = Add(new UICheckBox(() => PlantingMole, Font, "Plant Mole", "Plant Mole"));
            PlantMoleBox.OnChange = PlantMole;
            PlantMoleBox.CheckedTextColor = player.EmpireColor;
            Passive.Tooltip   = GameText.EspionageOpsProjectorsAlertTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos  = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description    = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel2Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos      = new Vector2(Rect.X + 5, PassiveY);
            ActiveTitle.Pos       = new Vector2(Rect.X + 5, PassiveY + Font.LineSpacing + 2);
            PlantMoleBox.Pos      = new Vector2(Rect.X + 75, ActiveTitle.Y);
            Passive.Pos           = new Vector2(Rect.X + 75, PassiveTitle.Pos.Y);

            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage     = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level ? Player.EmpireColor : Color.Gray;
                PlantMoleBox.Enabled   = Espionage.Level >= Level;
                PlantMoleBox.TextColor = PlantMoleBox.Enabled ? Color.White : Color.Gray;
                LevelDescription.Color = PlantMoleBox.Enabled ? Player.EmpireColor : Color.Gray;
                PlantingMole           = Espionage.IsMissionActive(InfiltrationMissionType.PlantMole);
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

        void PlantMole(UICheckBox b)
        {
            if (PlantingMole)
                Espionage.AddMission(InfiltrationMissionType.PlantMole);
            else
                Espionage.RemoveMission(InfiltrationMissionType.PlantMole);
        }
    }
}
