using System;
using Ship_Game.Universe;

namespace Ship_Game;

public abstract class PlanetScreen : GameScreen
{
    public readonly Planet P;
    public readonly UniverseState Universe;
    public readonly Empire Player;

    protected PlanetScreen(GameScreen parent, Planet p) : base(parent, toPause: null)
    {
        P = p ?? throw new ArgumentNullException(nameof(p));
        Universe = p.Universe;
        Player = Universe.Player;
        IsPopup = true; // auto-dismiss with right-click
    }
}
