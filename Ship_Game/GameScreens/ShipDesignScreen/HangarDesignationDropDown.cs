using Microsoft.Xna.Framework;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game
{
    public sealed partial class ShipDesignScreen
    {
        private class HangarDesignationDropDown : DropOptions<ShipData.HangarOptions>
        {
            public HangarDesignationDropDown(UIElementV2 parent, Rectangle hangarRect) : base(parent, hangarRect)
            {
            }
            public override bool HandleInput(InputState input)
            {
                if (Rect.HitTest(input.CursorPosition))
                {
                    string tooltip = new[]{
                        "This ship is desgined for general purpose tasks.",

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
