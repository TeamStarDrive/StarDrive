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
                if (Rect.HitTest(input.CursorPosition))
                {
                    GameText tooltip = ActiveValue switch
                    {
                        ShipCategory.Civilian     => GameText.ShipCategoryCivilianTip,
                        ShipCategory.Recon        => GameText.ShipCategoryReconTip,
                        ShipCategory.Conservative => GameText.ShipCategoryConservativeTip,
                        ShipCategory.Neutral      => GameText.ShipCategoryNeutralTip,
                        ShipCategory.Reckless     => GameText.ShipCategoryRecklessTip,
                        ShipCategory.Kamikaze     => GameText.ShipCategoryKamikazeTip,
                        _                         => GameText.ShipCategoryUnclassifiedTip,
                    };
                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}