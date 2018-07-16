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
                        case ShieldsWarpBehavior.Fully_Powered: ToolTip.CreateTooltip("Shields stay online during warp and also recharge, " +
                                                                                      "they will draw more power, like other modules"); break;
                        case ShieldsWarpBehavior.Maintained_With_Acticvation: ToolTip.CreateTooltip("Shields will not consume excess power at warp but " +
                                                                                                    "they will not recharge during warp and upon exit" +
                                                                                                    ", they will need to be brought online. The chance " +
                                                                                                    "to bring them online is affected by ship level and shield" +
                                                                                                    " complexcity"); break;
                        case ShieldsWarpBehavior.Discharged_With_Acticvation: ToolTip.CreateTooltip("Shields will not consume power at warp but will discharge" +
                                                                                                    " slowly to zero. They will also need activation after" +
                                                                                                    " warp exit and with less chances"); break;
                    }
                }
                return base.HandleInput(input);
            }
        }
    }
}