using Microsoft.Xna.Framework;

namespace Ship_Game.GameScreens.Espionage
{
    public class EmpiresPanel : UIElementContainer
    {
        readonly EspionageScreen Screen;
        //readonly ScrollList<OperationsListItem> OperationsSL;

        //class OperationsListItem : ScrollList<OperationsListItem>.Entry
        //{
        //    public Operation Operation;
        //}

        public EmpiresPanel(EspionageScreen screen, in Rectangle rect, Rectangle operationsRect)
            : base(rect)
        {
            Screen = screen;

            //var opsRect = new Rectangle(operationsRect.X + 20, operationsRect.Y + 20, 
            //                            operationsRect.Width - 40, operationsRect.Height - 45);
            //OperationsSL = new ScrollList<OperationsListItem>(new Submenu(opsRect), Fonts.Arial12Bold.LineSpacing + 5);

            var empires = new Array<Empire>();
            foreach (Empire e in EmpireManager.Empires)
                if (!e.isFaction) empires.Add(e);

            float x = Screen.ScreenWidth / 2f - (148f * empires.Count) / 2f;
            Pos = new Vector2(x, rect.Y + 10);

            UIList list = AddList(new Vector2(Pos.X + 10, rect.Y + 40));
            list.Padding = new Vector2(10f, 10f);
            list.LayoutStyle = ListLayoutStyle.ResizeList;
            list.Direction = new Vector2(1f, 0f);

            foreach (Empire e in empires)
                list.Add(new EmpireButton(screen, e, new Rectangle(0, 0, 134, 148), OnEmpireSelected));

            Size = new Vector2(list.Width, 188);
            Screen.SelectedEmpire = EmpireManager.Player;
        }

        void OnEmpireSelected(EmpireButton button)
        {
            if (EmpireManager.Player == button.Empire || EmpireManager.Player.IsKnown(button.Empire))
            {
                Screen.SelectedEmpire = button.Empire;
                Screen.Agents.Reinitialize();
            }
        }
    }
}