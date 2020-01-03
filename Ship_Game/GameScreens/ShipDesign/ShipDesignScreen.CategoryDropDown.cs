using Microsoft.Xna.Framework;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        private class CategoryDropDown : DropOptions<ShipData.Category>
        {            
            public CategoryDropDown(UIElementV2 parent, Rectangle dropdownRect) : base(dropdownRect)
            {                
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
                {
                    string tooltip = new[]{
                        "Repair when structural integrity is 40% or less",

                        "Can be used as Freighter. Evade when enemies are near. " +
                        "Repair when structural integrity is 85% or less" ,

                        "Can be used as Scout +" +
                        "Repair when structural integrity is 65% or less" ,

                        "Repair when structural integrity is 50% or less" ,

                        "Repair when structural integrity is 35% or less. " ,

                        "Repair when structural integrity is 20% or less." ,

                        "Never Repair! Never Rearm!\n(unless ordered)"

                    }[(int)ActiveValue];

                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}