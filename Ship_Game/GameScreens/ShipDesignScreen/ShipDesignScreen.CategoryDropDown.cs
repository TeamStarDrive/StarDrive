using Microsoft.Xna.Framework;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen : GameScreen
    {
        private class CategoryDropDown : DropOptions
        {            
            private readonly ScreenManager ScreenManager;
            public CategoryDropDown(Rectangle dropdownRect, GameScreen screen) : base(dropdownRect)
            {                
                ScreenManager = screen.ScreenManager;
            }
            public override void HandleInput (InputState input)
            {
                if (r.HitTest(input.CursorPosition)) //fbedard: add tooltip for CategoryList
                {
                    switch (Options[ActiveIndex].value)
                    {
                        case 1:
                            {
                                ToolTip.CreateTooltip("Repair when damaged at 75%", ScreenManager);
                                break;
                            }
                        case 2:
                            {
                                ToolTip.CreateTooltip(
                                    "Can be used as Freighter.\nEvade when enemy.\nRepair when damaged at 15%",
                                    ScreenManager);
                                break;
                            }
                        case 3:
                            {
                                ToolTip.CreateTooltip("Repair when damaged at 35%", ScreenManager);
                                break;
                            }
                        case 4:
                        case 5:
                        case 6:
                            {
                                ToolTip.CreateTooltip("Repair when damaged at 55%", ScreenManager);
                                break;
                            }
                        case 7:
                            {
                                ToolTip.CreateTooltip("Never Repair!", ScreenManager);
                                break;
                            }
                        default:
                            {
                                ToolTip.CreateTooltip("Repair when damaged at 75%", ScreenManager);
                                break;
                            }
                    }
                }
                base.HandleInput(input);
            }


        }
    }
}