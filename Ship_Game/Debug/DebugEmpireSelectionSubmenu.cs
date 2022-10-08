using SDGraphics;
using Ship_Game.Universe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Debug
{
    public class DebugEmpireSelectionSubmenu : Submenu
    {
        new readonly DebugInfoScreen Parent;
        UniverseScreen Screen => Parent.Screen;
        UniverseState Universe => Parent.Screen.UState;

        public DebugEmpireSelectionSubmenu(DebugInfoScreen parent, RectF rect) : base(rect)
        {
            Parent = parent;
        }
    }
}
