using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;
using SynapseGaming.LightingSystem.Rendering;

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