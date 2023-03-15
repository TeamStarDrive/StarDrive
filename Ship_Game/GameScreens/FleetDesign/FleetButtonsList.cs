using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Fleets;

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

    public FleetButtonsList(RectF rect, GameScreen parent, UniverseScreen us,
                            Action<FleetButton> onClick,
                            Action<FleetButton> onHotKey,
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
                OnHotKey = onHotKey,
                IsSelected = isSelected
            };
            Buttons.Add(b);
            base.Add(b);
        }

        base.PerformLayout();

        Vector2 animOffset = new(-256, 0);
        StartGroupTransition<FleetButton>(animOffset, -1, time:0.4f);
        parent.OnExit += () => StartGroupTransition<FleetButton>(animOffset, +1, time:0.5f);
    }

    // In some conditions, the fleet buttons should automatically be disabled
    bool ShouldHideInUniverse => Us.LookingAtPlanet;
    bool ShouldHide => (IsUniverse && ShouldHideInUniverse)
                    || Us.DefiningAO // FB dont show fleet list when selected AOs and Trade Routes
                    || Us.DefiningTradeRoutes;

    bool IsInputDisabled => (IsUniverse && Us.pieMenu.Visible);

    static int InputFleetSelection(InputState input)
    {
        if (input.Fleet1) return 1;
        if (input.Fleet2) return 2;
        if (input.Fleet3) return 3;
        if (input.Fleet4) return 4;
        if (input.Fleet5) return 5;
        if (input.Fleet6) return 6;
        if (input.Fleet7) return 7;
        if (input.Fleet8) return 8;
        if (input.Fleet9) return 9;
        return -1;
    }

    public override bool HandleInput(InputState input)
    {
        if (ShouldHide || IsInputDisabled)
            return false;

        foreach (FleetButton b in Buttons)
        {
            // always handle hotkeys, since they can be used to create new fleets
            if (InputFleetSelection(input) == b.FleetKey)
            {
                b.OnHotKey?.Invoke(b);
                return true;
            }
        }

        return base.HandleInput(input);
    }

    public override void Update(float fixedDeltaTime)
    {
        if (ShouldHide)
            return;

        foreach (FleetButton b in Buttons)
        {
            Fleet f = Player.GetFleetOrNull(b.FleetKey);
            bool visible = f != null;
            if (IsUniverse)
                visible = visible && f.CountShips > 0;

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
