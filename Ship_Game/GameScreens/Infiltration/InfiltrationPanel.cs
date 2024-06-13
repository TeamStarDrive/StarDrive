using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;
using Ship_Game.GameScreens.Espionage;

namespace Ship_Game.GameScreens.EspionageNew
{
    public class InfiltrationPanel : UIElementContainer
    {
        readonly InfiltrationScreen Screen;
        readonly Empire Player;

        public InfiltrationPanel(InfiltrationScreen screen, Empire player, in Rectangle rect)
            : base(rect)
        {
            Screen = screen;
            Player = player;

            var empires = player.Universe.ActiveMajorEmpires;
            float x = Screen.ScreenWidth / 2f - (148f * empires.Length) / 2f;
            Pos = new Vector2(x, rect.Y + 10);

            UIList list = AddList(new Vector2(Pos.X + 10, rect.Y + 50));
            list.Padding = new Vector2(10f, 10f);
            list.LayoutStyle = ListLayoutStyle.ResizeList;
            list.Direction = new Vector2(1f, 0f);

            foreach (Empire e in empires)
                list.Add(new EmpireButton(screen, e, new Rectangle(0, 0, 134, 148), OnEmpireSelected));

            Size = new Vector2(list.Width, 188);
            Screen.SelectedEmpire = Screen.Universe.Player;
        }

        void OnEmpireSelected(EmpireButton button)
        {
            if (Screen.Universe.Player == button.Empire || Screen.Universe.Player.IsKnown(button.Empire))
            {
                Screen.SelectedEmpire = button.Empire;
            }
        }
    }
}