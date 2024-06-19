using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel1 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly Font Font;
        readonly UILabel LevelDescription;
        readonly UILabel PassiveTitle, Passive;
        readonly int LevelDescriptionY, PassiveY;
        const int Level = 1;
        public InfiltrationOpsLevel1(InfiltrationScreen screen, Empire player, in Rectangle rect, int levelDescY, int passiveY, Font font)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Font   = font;
            LevelDescription  = Add(new UILabel("", Font, Color.Wheat));
            PassiveTitle      = Add(new UILabel("Passive:", Font, Color.Wheat));
            Passive           = Add(new UILabel(GameText.EspionageOpsAllowScanShips, Font, Color.Gray));
            Passive.Tooltip   = GameText.EspionageOpsAllowScanShipsTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;
        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos  = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description    = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel1Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos      = new Vector2(Rect.X + 5, PassiveY);
            Passive.Pos           = new Vector2(Rect.X + 75, PassiveTitle.Pos.Y);
        }

        public override void Update(float fixedDeltaTime)
        {
            base.Update(fixedDeltaTime);
            if (Screen.SelectedEmpire.isPlayer)
                return;

            Ship_Game.Espionage espionage = Player.GetRelations(Screen.SelectedEmpire).Espionage;

            //LevelDescription.Visible = espionage.Level < 1;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }
    }
}
