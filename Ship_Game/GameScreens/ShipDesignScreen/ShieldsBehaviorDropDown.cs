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
                    string tooltip = new []{
                        "Shields are always ACTIVE, consume MORE power and recharge during warp.",

                        "Shields in warp are PARTIALLY ACTIVE and will not recharge, their power." +
                        "consumption is equal to sublight consumption. There is a shield reactivation delay" +
                        "upon warp exit which is affected by crew level and shield complexity. ",

                        "Shields in warp are SHUTDOWN, slowly DISCHARGE and consume NO power." +
                        " Shield reactivation delay is much longer. "

                    }[(int)ActiveValue];

                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}