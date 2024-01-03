using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
using Ship_Game.Ships;
using Ship_Game.ExtensionMethods;
using SDUtils;

namespace Ship_Game;

public partial class Empire
{
    /// <summary>
    /// Utilities for finding stuff within our empire
    /// </summary>

    // Finds a planet by Id
    public Planet FindPlanet(int planetId)
    {
        foreach (Planet p in OwnedPlanets)
            if (p.Id == planetId)
                return p;
        return null;
    }

    public Planet FindPlanet(string planetName)
    {
        foreach (Planet p in OwnedPlanets)
            if (p.Name == planetName)
                return p;
        return null;
    }

    public bool TryFindClosestScoutTo(Vector2 pos, out Ship scout)
    {
        scout = null;
        var ships = EmpireShips.OwnedShips;
        var potentialScouts = ships.Filter(s => s.IsGoodScout());
        if (potentialScouts.Length > 0)
            scout = potentialScouts.FindMin(s => s.Position.SqDist(pos));

        return scout != null;
    }

    /// <summary>
    /// Finds the closest projector at location which falls inside the radius
    /// </summary>
    public bool FindProjectorAt(Vector2 pos, float radius, out Ship projector)
    {
        Spatial.SearchOptions search = new(pos, radius, GameObjectType.Ship)
        {
            SortByDistance = true,
            OnlyLoyalty = this,
            FilterFunction = go => go is Ship s && s.IsSubspaceProjector
        };

        projector = Universe.Spatial.FindOne(ref search) as Ship;
        return projector != null;
    }

    /// <summary>
    /// Finds all projectors at location and radius
    /// </summary>
    public Ship[] FindProjectorsAt(Vector2 pos, float radius)
    {
        Spatial.SearchOptions search = new(pos, radius, GameObjectType.Ship)
        {
            OnlyLoyalty = this,
            FilterFunction = go => go is Ship s && s.IsSubspaceProjector
        };
        return Universe.Spatial.FindNearby(ref search)
            .FastCast<Spatial.SpatialObjectBase, Ship>();
    }

    /// <summary>
    /// Finds all ships at location and radius, with an optional filter
    /// </summary>
    public Ship[] FindShipsAt(Vector2 pos, float radius, Predicate<Ship> filter = null)
    {
        Spatial.SearchOptions search = new(pos, radius, GameObjectType.Ship)
        {
            OnlyLoyalty = this,
            FilterFunction = filter != null ? (go => go is Ship s && filter(s)) : null,
        };
        return Universe.Spatial.FindNearby(ref search).
            FastCast<Spatial.SpatialObjectBase, Ship>();
    }

    /// <summary>
    /// Finds the closest ship at location + radius, with an optional filter
    /// </summary>
    public bool FindShipAt(Vector2 pos, float radius, out Ship ship, Predicate<Ship> filter = null)
    {
        Spatial.SearchOptions search = new(pos, radius, GameObjectType.Ship)
        {
            SortByDistance = true,
            OnlyLoyalty = this,
            FilterFunction = filter != null ? (go => go is Ship s && filter(s)) : null,
        };
        ship = Universe.Spatial.FindOne(ref search) as Ship;
        return ship != null;
    }
}
