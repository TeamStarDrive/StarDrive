using SDGraphics;
using Ship_Game.Ships;
using Rectangle = SDGraphics.Rectangle;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        class HangarDesignationDropDown : DropOptions<HangarOptions>
        {
            public HangarDesignationDropDown(in Rectangle hangarRect) : base(hangarRect)
            {
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition))
                {
                    GameText tooltip = ActiveValue switch
                    {
                        HangarOptions.AntiShip    => GameText.HangarDesignationAntiShipTip,
                        HangarOptions.Interceptor => GameText.HangarDesignationInterceptorTip,
                        _                         => GameText.HangarDesignationGeneralTip,
                    };
                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}
