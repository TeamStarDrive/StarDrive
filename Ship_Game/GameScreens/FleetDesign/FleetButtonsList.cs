using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.GameScreens.FleetDesign;

/// <summary>
/// List of fleet buttons on the Left side of the screen
/// </summary>
public class FleetButtonsList : UIList
{
    public FleetButtonsList(RectF rect,
                            GameScreen parent,
                            UniverseScreen us,
                            Action<FleetButton> onClick,
                            Func<FleetButton, bool> isSelected)
        : base(rect, Color.TransparentBlack)
    {
        LayoutStyle = ListLayoutStyle.Clip;

        Vector2 buttonSize = new(52, 48);
        for (int key = Empire.FirstFleetKey; key <= Empire.LastFleetKey; ++key)
        {
            base.Add(new FleetButton(us, key, buttonSize)
            {
                FleetDesigner = parent is FleetDesignScreen,
                OnClick = onClick,
                IsActive = isSelected,
            });
        }

        base.PerformLayout();

        var animOffset = new Vector2(-128, 0);
        StartGroupTransition<FleetButton>(animOffset, -1, time:0.5f);
        parent.OnExit += () => StartGroupTransition<FleetButton>(animOffset, +1, time:0.5f);
    }
}
