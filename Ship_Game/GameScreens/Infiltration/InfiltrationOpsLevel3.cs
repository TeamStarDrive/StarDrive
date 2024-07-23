using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationOpsLevel3 : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;
        readonly Font Font;
        readonly UILabel LevelDescription;
        readonly UILabel PassiveTitle, Passive, ActiveTitle;
        readonly UICheckBox UpriseBox, CounterBox;
        readonly UILabel UpriseTurnsRemaining, CounterTurnsRemaining;
        readonly int LevelDescriptionY, PassiveY;
        const int Level = 3;
        Ship_Game.Espionage Espionage;
        bool Uprising, CounteringEspionage;

        public InfiltrationOpsLevel3(InfiltrationScreen screen, Empire player, in Rectangle rect, int levelDescY, int passiveY, Font font)
            : base(rect)
        {
            Screen = screen;
            Player = player;
            Font   = font;
            LevelDescription = Add(new UILabel("", Font, Color.Wheat));
            PassiveTitle     = Add(new UILabel(GameText.Passive, Font, Color.Wheat));
            ActiveTitle      = Add(new UILabel(GameText.Active, Font, Color.Wheat));
            Passive          = Add(new UILabel(GameText.EspioangeHomeworldMole, Font, Color.Gray));
            UpriseBox        = Add(new UICheckBox(() => Uprising, Font, GameText.ArrangeUprise, GameText.ArrangeUpriseTip));
            CounterBox       = Add(new UICheckBox(() => CounteringEspionage, Font, GameText.CounterEspioangeOps, GameText.CounterEspioangeOpsTip));
            UpriseBox.OnChange  = ArrangeUprise;
            CounterBox.OnChange = CounterEspionage;
            UpriseBox.CheckedTextColor = CounterBox.CheckedTextColor = Color.LightGreen;

            Passive.Tooltip   = GameText.EspioangeHomeworldMoleTip;
            LevelDescriptionY = levelDescY;
            PassiveY = passiveY;

            UpriseTurnsRemaining  = Add(new UILabel("", Font, Color.White));
            CounterTurnsRemaining = Add(new UILabel("", Font, Color.White));

        }

        public override void PerformLayout()
        {
            base.PerformLayout();
            LevelDescription.Pos  = new Vector2(Rect.X + 5, LevelDescriptionY);
            string description    = Font.ParseText(Localizer.Token(GameText.InfiltrationLevel3Desc), Rect.Width - 10);
            LevelDescription.Text = description;
            PassiveTitle.Pos      = new Vector2(Rect.X + 5, PassiveY);
            Passive.Pos           = new Vector2(Rect.X + 60, PassiveTitle.Pos.Y);
            ActiveTitle.Pos       = new Vector2(Rect.X + 5, PassiveY + Font.LineSpacing + 2);
            UpriseBox.Pos         = new Vector2(Rect.X + 55, ActiveTitle.Y);
            CounterBox.Pos        = new Vector2(Rect.X + 55, ActiveTitle.Y + Font.LineSpacing + 2);
            UpriseTurnsRemaining.Pos  = new Vector2(Rect.Right - 80, ActiveTitle.Y);
            CounterTurnsRemaining.Pos = new Vector2(Rect.Right - 80, CounterBox.Y);

            if (!Screen.SelectedEmpire.isPlayer)
            {
                Espionage     = Player.GetEspionage(Screen.SelectedEmpire);
                Passive.Color = Espionage.Level >= Level && Espionage.LimitLevel >= Level ? Color.LightGreen : Color.Gray;
                UpriseBox.Enabled      = CounterBox.Enabled = Espionage.Level >= Level;
                UpriseBox.TextColor    = CounterBox.TextColor = UpriseBox.Enabled ? Color.White : Color.Gray;
                LevelDescription.Color = UpriseBox.Enabled  ? Player.EmpireColor : Color.Gray;
                Uprising               = Espionage.IsOperationActive(InfiltrationOpsType.Uprise);
                CounteringEspionage    = Espionage.IsOperationActive(InfiltrationOpsType.CounterEspionage);
                UpriseTurnsRemaining.Visible = CounterTurnsRemaining.Visible = Espionage.Level >= Level;
            }
        }

        public override void Update(float fixedDeltaTime)
        {
            UpriseTurnsRemaining.Color = UpriseBox.Checked ? Color.LightGreen : Color.White;
            UpriseTurnsRemaining.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.Uprise);
            UpriseTurnsRemaining.Pos  = HelperFunctions.GetRightAlignedPosForTitle(UpriseTurnsRemaining.Text.Text,
                UpriseTurnsRemaining.Font, Rect.Right, UpriseTurnsRemaining.Y);

            CounterTurnsRemaining.Color = CounterBox.Checked ? Color.LightGreen : Color.White;
            CounterTurnsRemaining.Text = Espionage.RemainingTurnsForOps(InfiltrationOpsType.CounterEspionage);
            CounterTurnsRemaining.Pos  = HelperFunctions.GetRightAlignedPosForTitle(CounterTurnsRemaining.Text.Text,
                CounterTurnsRemaining.Font, Rect.Right, CounterTurnsRemaining.Y);

            base.Update(fixedDeltaTime);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
        }

        void ArrangeUprise(UICheckBox b)
        {
            if (Uprising)
                Espionage.AddOperation(InfiltrationOpsType.Uprise);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.Uprise);
        }

        void CounterEspionage(UICheckBox b)
        {
            if (CounteringEspionage)
                Espionage.AddOperation(InfiltrationOpsType.CounterEspionage);
            else
                Espionage.RemoveOperation(InfiltrationOpsType.CounterEspionage);
        }
    }
}
