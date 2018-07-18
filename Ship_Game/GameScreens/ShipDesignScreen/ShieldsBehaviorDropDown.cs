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
                        "Shields are always ACTIVE, consume DOUBLE power and recharges during warp.",

                        "Shields are PARTIALLY ACTIVE, consume regular power and do not recharge during warp. "+
                        "Shield reactivation delay is affected by crew level and shield complexity.",

                        "Shields are SHUT DOWN, consume NO power and slowly DISCHARGE during warp. "+
                        "Shield reactivation delay is much longer.",
                    }[(int)ActiveValue];

                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}