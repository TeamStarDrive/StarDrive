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
                if (Rect.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
                {
                    switch (ActiveValue)
                    {
                        case ShieldsWarpBehavior.OnFullChargeAtWarpExit: ToolTip.CreateTooltip("Shields stay online during warp, they will draw more power"); break;
                        case ShieldsWarpBehavior.LowDischargeDownTo50Percent: ToolTip.CreateTooltip("Shields will not consume excess power at warp but will discharge slowly to a minimum of 50% of total shield power"); break;
                        case ShieldsWarpBehavior.MediumDischargeDownTo25Percent: ToolTip.CreateTooltip("Shields will consume less power at warp but will discharge to a minimum of 25% of total shield power"); break;
                        case ShieldsWarpBehavior.HighDischargeDownTo0Percent: ToolTip.CreateTooltip("Shields will not consume power at warp but will discharge quickly to zero"); break;
                    }
                }
                return base.HandleInput(input);
            }
        }
    }
}