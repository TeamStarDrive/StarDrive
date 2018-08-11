using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public sealed class MinimapButtons : UIElementContainer
    {
        public MinimapButtons(Vector2 top) : base(null, top)
        {		    
            BeginVLayout(top, 22);
            ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomctrl");
            ToggleButton(ToggleButtonStyle.ButtonC, "Minimap/icons_zoomout");
            ToggleButton(ToggleButtonStyle.ButtonB, "UI/icon_planetslist");
            ToggleButton(ToggleButtonStyle.Button, "UI/icon_ftloverlay");
            ToggleButton(ToggleButtonStyle.Button, "UI/icon_rangeoverlay");
            ToggleButton(ToggleButtonStyle.Button, "UI/icon_dsbw");
            ToggleButton(ToggleButtonStyle.ButtonDown, "AI");
            EndLayout();
        }
    }
}