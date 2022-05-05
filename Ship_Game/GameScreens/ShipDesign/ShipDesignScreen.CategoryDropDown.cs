using SDGraphics;
using Ship_Game.Ships;
using Rectangle = SDGraphics.Rectangle;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        class CategoryDropDown : DropOptions<ShipCategory>
        {
            public CategoryDropDown(in Rectangle dropdownRect) : base(dropdownRect)
            {
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
                {
                    string tooltip = new[]{
                        "Repair when structural integrity is 70% or less",

                        "Can be used as Freighter. Evade when enemies are near. " +
                        "Repair when structural integrity is 95% or less" ,

                        "Can be used as Scout +" +
                        "Repair when structural integrity is 85% or less" ,

                        "Repair when structural integrity is 80% or less" ,

                        "Repair when structural integrity is 75% or less. " ,

                        "Repair when structural integrity is 50% or less." ,

                        "Never Repair! Never Rearm!\n(unless ordered)"

                    }[(int)ActiveValue];

                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}