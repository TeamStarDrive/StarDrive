using Microsoft.Xna.Framework;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        private class CategoryDropDown : DropOptions<ShipData.Category>
        {            
            public CategoryDropDown(UIElementV2 parent, Rectangle dropdownRect) : base(parent, dropdownRect)
            {                
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
                {
                    switch (ActiveValue)
                    {
                        default:
                        case ShipData.Category.Unclassified: ToolTip.CreateTooltip("Repair when structural integrity is 40% or less"); break;
                        case ShipData.Category.Civilian: ToolTip.CreateTooltip("Can be used as Freighter. Evade when enemies are near. " +
                                                                               "Repair when structural integrity is 85% or less"); break;
                        case ShipData.Category.Recon:    ToolTip.CreateTooltip("Repair when structural integrity is 65% or less"); break;
                        case ShipData.Category.Combat: ToolTip.CreateTooltip("Repair when structural integrity is 35% or less"); break;
                        case ShipData.Category.Bomber: ToolTip.CreateTooltip("Repair when structural integrity is 35% or less. " +
                                                                              "Designate as Bomber. Dynamic AntiShip Hangars " +
                                                                              "will be able to pick the best from this category"); break;
                        case ShipData.Category.Fighter:  ToolTip.CreateTooltip("Repair when structural integrity is 30% or less. " +
                                                                               "Designate as Fighter. Dynamic Interceptor Hangars " +
                                                                               "will be able to pick the best from this category"); break;
                        case ShipData.Category.Kamikaze: ToolTip.CreateTooltip("Never Repair! Never Rearm!\n(unless ordered)"); break;
                    }
                }
                return base.HandleInput(input);
            }
        }
    }
}