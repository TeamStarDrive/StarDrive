﻿using SDGraphics;
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
                    string tooltip = new[]{
                        "This ship is designed for general purpose tasks.",

                        "This ship is designated as Anti Ship. It is designed to engage capital ships. " +
                        "Carrier Dynamic Anti Ship hangars will pick the best from Anti Ship designated ships when launching ships.",

                        "This ship is designated as Interceptor. It is designed to engage small craft. " +
                        "Carrier Dynamic Interceptor hangars will pick the best from interceptor designated ships when launching ships.",
                    }[(int)ActiveValue];

                    ToolTip.CreateTooltip(tooltip);
                }
                return base.HandleInput(input);
            }
        }
    }
}
