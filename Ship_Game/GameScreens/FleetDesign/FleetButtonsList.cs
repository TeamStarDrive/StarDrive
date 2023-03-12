using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;

namespace Ship_Game.GameScreens.FleetDesign;

/// <summary>
/// List of fleet buttons on the Left side of the screen
/// </summary>
public class FleetButtonsList : UIList
{
    readonly bool IsFleetDesigner;
    bool IsUniverse => !IsFleetDesigner;
    readonly UniverseScreen Us;
    readonly Empire Player;
    readonly Array<FleetButton> Buttons = new();

    public FleetButtonsList(RectF rect,
                            GameScreen parent,
                            UniverseScreen us,
                            Action<FleetButton> onClick,
                            Func<FleetButton, bool> isSelected)
        : base(rect, Color.TransparentBlack)
    {
        Us = us;
        Player = us.Player;
        LayoutStyle = ListLayoutStyle.Clip;
        IsFleetDesigner = parent is FleetDesignScreen;

        Vector2 buttonSize = new(52, 48);
        for (int key = Empire.FirstFleetKey; key <= Empire.LastFleetKey; ++key)
        {
            FleetButton b = new(us, key, buttonSize)
            {
                FleetDesigner = IsFleetDesigner,
                OnClick = onClick,
                IsActive = isSelected
            };
            Buttons.Add(b);
            base.Add(b);
        }

        base.PerformLayout();

        Vector2 animOffset = new(-128, 0);
        if (IsUniverse) animOffset.X = -256;
        StartGroupTransition<FleetButton>(animOffset, -1, time:0.5f);
        parent.OnExit += () => StartGroupTransition<FleetButton>(animOffset, +1, time:0.5f);
    }

    // In some conditions, the fleet buttons should automatically be disabled
    bool ShouldHideInUniverse => Us.LookingAtPlanet;
    bool ShouldHide => (IsUniverse && ShouldHideInUniverse)
                    || Us.DefiningAO // FB dont show fleet list when selected AOs and Trade Routes
                    || Us.DefiningTradeRoutes;

    bool IsInputDisabled => (IsUniverse && Us.pieMenu.Visible);

    public override bool HandleInput(InputState input)
    {
        if (ShouldHide || IsInputDisabled)
            return false;
        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        if (ShouldHide)
            return;

        foreach (FleetButton b in Buttons)
        {
            Fleets.Fleet f = Player.GetFleetOrNull(b.FleetKey);
            bool visible = f is { CountShips: > 0 };

            // make sure to do layout if any visibility changes
            RequiresLayout |= (visible != b.Visible);
            b.Visible = visible;
        }

        base.Update(fixedDeltaTime);
    }

    public override void Draw(SpriteBatch batch, DrawTimes elapsed)
    {
        if (ShouldHide)
            return;

        base.Draw(batch, elapsed);
    }
}
