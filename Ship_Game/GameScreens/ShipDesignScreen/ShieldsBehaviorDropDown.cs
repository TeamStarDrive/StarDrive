using Microsoft.Xna.Framework;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        private class ShieldBehaviorDropDown : DropOptions<ShieldsWarpBehavior>
        {
            public ShieldBehaviorDropDown(UIElementV2 parent, Rectangle dropdownRect) : base(parent, dropdownRect)
            {
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition)) 
                {
                    var tooltips = new string[] {
                        "Shields stay online during warp and also recharge, they will draw more power, like other modules",
                        "Shields will not consume excess power at warp but they will not recharge during warp. Upon warp exit" +
                        ", they will need to be activcated. The chance to activate them is affected by ship level and shield complexity",
                        "Shields will not consume power at warp but will discharge slowly to zero. They will also need activation after" +
                        " warp exit and will have lower activation chance"
                    };
                    string tooltip = tooltips[(int)ActiveValue];
                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}