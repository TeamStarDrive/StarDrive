using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Universe;

namespace Ship_Game.Debug
{
    public class DebugEmpireSelectionSubmenu : Submenu
    {
        new readonly DebugInfoScreen Parent;
        UniverseScreen Screen => Parent.Screen;
        UniverseState Universe => Parent.Screen.UState;

        public Empire Selected;
        public Empire[] Empires; // cached list of empires

        public DebugEmpireSelectionSubmenu(DebugInfoScreen parent, RectF rect) : base(rect)
        {
            Parent = parent;

            ResetUI();
        }

        void ResetUI()
        {
            RemoveAll();
            ClearTabs();

            Empires = Universe.Empires.ToArr();
            OnTabChange = (index) => Selected = Empires[index];

            foreach (Empire e in Empires)
                AddTab(e.Name);

            SelectedIndex = Selected != null ? Empires.IndexOf(Selected) : 0;
        }

        public override void Update(float fixedDeltaTime)
        {
            if (!Universe.Empires.EqualElements(Empires)) // empires list changed, reset everything
            {
                ResetUI();
            }

            base.Update(fixedDeltaTime);
        }
    }
}
