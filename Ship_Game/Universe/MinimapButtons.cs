using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	public sealed class MinimapButtons : UIElementContainer
    {
		
        private readonly ToggleButton ZoomOut;

        private readonly ToggleButton ZoomToShip;

        private readonly ToggleButton PlanetScreen;

        private readonly ToggleButton ShipScreen;

        private readonly ToggleButton AIScreen;

        private readonly ToggleButton DeepSpaceBuild;

        private readonly ToggleButton Fleets;

        private int ButtonOffset;

        private const string CNormal = "Minimap/button_C_normal";
        private const string BNormal = "Minimap/button_B_normal";
        private const string Normal  = "Minimap/button_normal";
        private const string Hover   = "Minimap/button_hover";
        private const string CHover  = "Minimap/button_hover";
        private const string Active  = "Minimap/button_active";
        private const string BHover  = "Minimap/button_B_hover";

        public MinimapButtons(Vector2 top) : base(null, top)
		{		    
            BeginVLayout(top, 22);
            ToggleButton(25, 22, CNormal, CNormal, CHover, CNormal, "Minimap/icons_zoomctrl");
		    ToggleButton(25, 22, CNormal, CNormal, CHover, CNormal, "Minimap/icons_zoomout");
		    ToggleButton(25, 22, BNormal, BNormal, BHover, BNormal, "UI/icon_planetslist");
		    ToggleButton(25, 22, Active, Normal, Hover, Normal, "UI/icon_ftloverlay");
		    ToggleButton(25, 22, Active, Normal, Hover, Normal, "UI/icon_rangeoverlay");
		    ToggleButton(25, 22, Active, Normal, Hover, Normal, "UI/icon_dsbw");
		    ToggleButton(25, 26, Active, "Minimap/button_down_inactive", "Minimap/button_down_hover"
		        , "Minimap/button_down_inactive", "AI");

		    EndLayout();

            
		}

	}
}