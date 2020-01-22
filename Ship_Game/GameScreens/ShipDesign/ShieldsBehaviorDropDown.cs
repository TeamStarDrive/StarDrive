using Microsoft.Xna.Framework;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        private class ShieldBehaviorDropDown : DropOptions<ShieldsWarpBehavior>
        {
            public ShieldBehaviorDropDown(UIElementV2 parent, Rectangle dropdownRect) : base(dropdownRect)
            {
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition)) 
                {
                    string tooltip = new []{
                        "Shields are always ACTIVE, consume MORE power and recharge during warp.",

                        "Shields are ACTIVE, consume normal power (without FTL multiplier) and do NOT recharge during warp. "+
                        "Shield reactivation delay is affected by crew level and shield complexity.",

                        "Shields are completely SHUT DOWN, consume NO power and slowly DISCHARGE during warp. "+
                        "Shield reactivation delay is TWICE as long."
                    }[(int)ActiveValue];

                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}