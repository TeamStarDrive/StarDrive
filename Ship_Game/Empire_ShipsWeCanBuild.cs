using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game;

public sealed partial class Empire
{
    // Source of truth; private so all external access goes through the safe API
    // (Snapshot/Count for reads, AddBuildableShip/RemoveBuildableShip/ClearShipsWeCanBuild
    // for writes). Both HashSets are mutated under the same lock and share the cache
    // invalidation flow.
    [StarData] HashSet<IShipDesign> ShipsWeCanBuild;
    // shipyards, platforms, SSP-s
    [StarData] HashSet<IShipDesign> SpaceStationsWeCanBuild;

    // Copy-on-write cache for safe lock-free iteration. Sim-thread readers used
    // to iterate the HashSets directly and could crash with `Collection was modified`
    // when any other code path mutated them concurrently. Now readers call the
    // *Snapshot properties which return these cached arrays; the cache is
    // invalidated to null by every writer under ShipsWeCanBuildLock and lazily
    // rebuilt on the next read. `volatile` is required because the reader's
    // fast path is lock-free — without it the JIT could cache the field in a
    // register and never observe the writer's invalidation.
    readonly object ShipsWeCanBuildLock = new();
    volatile IShipDesign[] CachedShipsWeCanBuildSnapshot;
    volatile IShipDesign[] CachedSpaceStationsWeCanBuildSnapshot;

    // For TESTING
    public string[] ShipsWeCanBuildIds => ShipsWeCanBuildSnapshot.Select(s => s.Name);

    /// <summary>
    /// Stable snapshot of ShipsWeCanBuild for safe iteration. Lock-free in the
    /// common case (cache hit). Use this instead of iterating ShipsWeCanBuild
    /// directly from any code path that might race with ship-design mutations.
    /// </summary>
    public IShipDesign[] ShipsWeCanBuildSnapshot
    {
        get
        {
            var snap = CachedShipsWeCanBuildSnapshot;
            if (snap != null)
                return snap;
            lock (ShipsWeCanBuildLock)
                return CachedShipsWeCanBuildSnapshot ??= ShipsWeCanBuild.ToArray();
        }
    }

    /// <summary>Stable snapshot of SpaceStationsWeCanBuild. See ShipsWeCanBuildSnapshot.</summary>
    public IShipDesign[] SpaceStationsWeCanBuildSnapshot
    {
        get
        {
            var snap = CachedSpaceStationsWeCanBuildSnapshot;
            if (snap != null)
                return snap;
            lock (ShipsWeCanBuildLock)
                return CachedSpaceStationsWeCanBuildSnapshot ??= SpaceStationsWeCanBuild.ToArray();
        }
    }

    public int ShipsWeCanBuildCount         => ShipsWeCanBuildSnapshot.Length;
    public int SpaceStationsWeCanBuildCount => SpaceStationsWeCanBuildSnapshot.Length;

    // changes whenever ShipsWeCanBuild does
    volatile int ShipsRevision;
    public int ShipsWeCanBuildRevision => ShipsRevision;

    void ShipsWeCanBuildChanged()
    {
        CachedShipsWeCanBuildSnapshot = null; // first, so a reader of the new revision never gets the old ships
        ++ShipsRevision;
    }

    // the strength of a new ship of each design built by this empire
    readonly ConcurrentDictionary<IShipDesign, (float Strength, int BonusRevision, int ShipsRevision)> DesignStrengths = new();

    // the ship each kind of dynamic hangar is valued by; real launches pick again
    readonly ConcurrentDictionary<(string Module, DynamicHangarOptions Launch), (string ShipName, int ShipsRevision)> DynamicHangarShips = new();

    // kept until this empire's bonuses or, for carriers, the ships it can build change
    public float GetDesignStrength(IShipDesign design)
    {
        DesignSlot[] slots = design.GetOrLoadDesignSlots();
        if (slots == null || slots.Length == 0) // a deleted design has no slots, its ships keep its last strength
            return DesignStrengths.TryGetValue(design, out var kept) ? kept.Strength : design.BaseStrength;

        int bonusRevision = EmpireHullBonuses.GetBonusRevisionId(this);
        if (bonusRevision == 0) // the revision exists once the bonuses do
        {
            EmpireHullBonuses.Get(this);
            bonusRevision = EmpireHullBonuses.GetBonusRevisionId(this);
        }
        int shipsRevision = design.Hangars.Length > 0 ? ShipsWeCanBuildRevision : 0;

        if (DesignStrengths.TryGetValue(design, out var cached)
            && cached.BonusRevision == bonusRevision && cached.ShipsRevision == shipsRevision)
        {
            return cached.Strength;
        }

        float strength = ShipStrength.OfDesign(design, slots, this);
        DesignStrengths[design] = (strength, bonusRevision, shipsRevision);
        return strength;
    }

    // picked once for each change to the ships this empire can build
    public string GetDynamicHangarShip(ShipModule hangar)
    {
        int shipsRevision = ShipsWeCanBuildRevision;
        var key = (hangar.UID, hangar.DynamicHangar);
        if (DynamicHangarShips.TryGetValue(key, out var cached) && cached.ShipsRevision == shipsRevision)
            return cached.ShipName;

        string shipName = CarrierBays.HangarShipName(this, hangar);
        DynamicHangarShips[key] = (shipName, shipsRevision);
        return shipName;
    }

    /// <summary>
    /// TRUE if this Empire can build this ship
    /// </summary>
    public bool CanBuildShip(string shipUID)
    {
        if (!ResourceManager.Ships.GetDesign(shipUID, out IShipDesign design))
            return false;
        lock (ShipsWeCanBuildLock)
            return ShipsWeCanBuild.Contains(design);
    }

    public bool CanBuildShip(IShipDesign ship)
    {
        if (ship == null) return false;
        lock (ShipsWeCanBuildLock)
            return ShipsWeCanBuild.Contains(ship);
    }

    public bool CanBuildStation(IShipDesign station)
    {
        if (station == null) return false;
        lock (ShipsWeCanBuildLock)
            return SpaceStationsWeCanBuild.Contains(station);
    }

    public bool AddBuildableShip(IShipDesign ship)
    {
        lock (ShipsWeCanBuildLock)
        {
            bool added = ShipsWeCanBuild.Add(ship);
            if (added)
            {
                ShipsWeCanBuildChanged();
                if (ship.Role <= RoleName.station && SpaceStationsWeCanBuild.Add(ship))
                    CachedSpaceStationsWeCanBuildSnapshot = null;
            }
            return added;
        }
    }

    public bool RemoveBuildableShip(IShipDesign ship)
    {
        lock (ShipsWeCanBuildLock)
        {
            bool removed = ShipsWeCanBuild.Remove(ship);
            if (removed)
                ShipsWeCanBuildChanged();
            if (SpaceStationsWeCanBuild.Remove(ship))
                CachedSpaceStationsWeCanBuildSnapshot = null;
            return removed;
        }
    }

    public void ClearShipsWeCanBuild()
    {
        lock (ShipsWeCanBuildLock)
        {
            ShipsWeCanBuild.Clear();
            SpaceStationsWeCanBuild.Clear();
            ShipsWeCanBuildChanged();
            CachedSpaceStationsWeCanBuildSnapshot = null;
        }
    }

    public void FactionShipsWeCanBuild()
    {
        if (!IsFaction) return;
        foreach (Ship ship in ResourceManager.Ships.Ships)
        {
            if ((data.Traits.ShipType == ship.ShipData.ShipStyle
                 || ship.ShipData.ShipStyle == "Misc"
                 || ship.ShipData.ShipStyle.IsEmpty())
                && ship.ShipData.CanBeAddedToBuildableShips(this))
            {
                AddBuildableShip(ship.ShipData);
                foreach (ShipModule hangar in ship.Carrier.AllHangars)
                {
                    if (hangar.HangarShipUID.NotEmpty())
                    {
                        var hangarShip = ResourceManager.Ships.GetDesign(hangar.HangarShipUID, throwIfError: false);
                        if (hangarShip?.CanBeAddedToBuildableShips(this) == true)
                            AddBuildableShip(hangarShip);
                    }
                }
            }
        }

        foreach (var hull in UnlockedHullsDict.Keys.ToArr())
            UnlockedHullsDict[hull] = true;
    }

    public void RemoveInvalidShipDesigns()
    {
        IShipDesign[] snapshot = ShipsWeCanBuildSnapshot;
        for (int i = 0; i < snapshot.Length; i++)
        {
            IShipDesign sd = snapshot[i];
            if (!sd.IsValidDesign)
            {
                Log.Warning($"Removing invalid Buildable Ship: {sd.Name}");
                RemoveBuildableShip(sd);
            }
        }
    }

    public void UpdateShipsWeCanBuild(Array<string> hulls = null, bool debug = false)
    {
        // validate all existing ship designs, in case some of them have become invalid
        RemoveInvalidShipDesigns();

        if (IsFaction)
        {
            FactionShipsWeCanBuild();
            return;
        }

        foreach (IShipDesign sd in ResourceManager.Ships.Designs)
        {
            if (sd.Name == "Target Dummy")
                continue;
            if (hulls != null && !hulls.Contains(sd.Hull))
                continue;

            // we can already build this
            if (CanBuildShip(sd))
                continue;
            if (!sd.CanBeAddedToBuildableShips(this))
                continue;

            if (WeCanBuildThis(sd, debug))
            {
                bool shipAdded = AddBuildableShip(sd);

                if (isPlayer)
                    Universe.Screen?.OnPlayerBuildableShipsUpdated();

                if (shipAdded)
                {
                    UpdateBestOrbitals();
                    UpdateDefenseShipBuildingOffense();
                    MarkShipRolesUsableForEmpire(sd);
                }
            }
        }
    }

    public void RemoveDuplicateShipDesigns()
    {
        lock (ShipsWeCanBuildLock)
        {
            if (RemoveDuplicateShipDesigns(ShipsWeCanBuild))
                ShipsWeCanBuildChanged();
            if (RemoveDuplicateShipDesigns(SpaceStationsWeCanBuild))
                CachedSpaceStationsWeCanBuildSnapshot = null;
        }
    }

    bool RemoveDuplicateShipDesigns(HashSet<IShipDesign> designs)
    {
        bool anyRemoved = false;
        Map<string, IShipDesign> unique = new();
        foreach (IShipDesign design in designs.ToArr())
        {
            // these two designs clash, need to remove one
            if (unique.TryGetValue(design.Name, out IShipDesign existing))
            {
                bool areEqual = design.BaseCost == existing.BaseCost
                            && design.BaseWarpThrust == existing.BaseWarpThrust
                            && design.BaseStrength == existing.BaseStrength;

                bool isNewer = design.BaseCost > existing.BaseCost
                            || design.BaseWarpThrust > existing.BaseWarpThrust
                            || design.BaseStrength > existing.BaseStrength;

                IShipDesign toKeep = !areEqual && isNewer ? design : existing;
                IShipDesign toRemove = (toKeep != existing ? existing : design);
                if (areEqual)
                {
                    Log.Warning($"{Name} duplicate ShipDesign={toKeep.Name}. Both designs appear equal.");
                }
                else
                {
                    Log.Warning($"{Name} duplicate ShipDesign={toKeep.Name}. "+
                                $"Keep Cost={toKeep.BaseCost} Warp={toKeep.BaseWarpThrust} Str={toKeep.BaseStrength}. "+
                                $"Remove Cost={toRemove.BaseCost} Warp={toRemove.BaseWarpThrust} Str={toRemove.BaseStrength}.");
                }

                if (designs.Remove(toRemove))
                    anyRemoved = true;
                unique[design.Name] = toKeep;
            }
            else
            {
                unique.Add(design.Name, design);
            }
        }
        return anyRemoved;
    }

    public bool WeCanShowThisWIP(IShipDesign shipData)
    {
        return WeCanBuildThis(shipData, debug: true);
    }

    public bool WeCanBuildThis(string shipName, bool debug = false)
    {
        if (!ResourceManager.Ships.GetDesign(shipName, out IShipDesign shipData))
        {
            Log.Warning($"Ship does not exist: {shipName}");
            return false;
        }

        return WeCanBuildThis(shipData, debug);
    }

    public bool WeCanBuildThis(IShipDesign design, bool debug = false)
    {
        // If this hull is not unlocked, then we can't build it
        if (!IsHullUnlocked(design.Hull))
        {
            if (debug) Log.Write($"WeCanBuildThis:false Reason:LockedHull Design:{design.Name}");
            return false;
        }

        if (design.TechsNeeded.Count > 0)
        {
            if (!design.Unlockable)
            {
                if (debug) Log.Write($"WeCanBuildThis:false Reason:NotUnlockable Design:{design.Name}");
                return false;
            }

            foreach (string shipTech in design.TechsNeeded)
            {
                if (!ShipTechs.Contains(shipTech))
                {
                    // some ShipDesigns are loaded from savegame only, and the tech might no longer exist
                    // in this case the ship is no longer buildable
                    if (!TryGetTechEntry(shipTech, out TechEntry onlyShipTech))
                    {
                        if (debug)
                            Log.Write($"WeCanBuildThis:false Reason:MissingTech={shipTech} Design:{design.Name}");
                        return false;
                    }
                    else if (onlyShipTech.Locked)
                    {
                        if (debug) Log.Write($"WeCanBuildThis:false Reason:LockedTech={shipTech} Design:{design.Name}");
                        return false;
                    }
                }
            }
        }
        else
        {
            // check if all modules in the ship are unlocked
            foreach (string moduleUID in design.UniqueModuleUIDs)
            {
                if (!IsModuleUnlocked(moduleUID))
                {
                    if (debug) Log.Write($"WeCanBuildThis:false Reason:LockedModule={moduleUID} Design:{design.Name}");
                    return false; // can't build this ship because it contains a locked Module
                }
            }
        }

        if (debug) Log.Write($"WeCanBuildThis:true Design:{design.Name}");
        return true;
    }

    public bool WeCanUseThisTech(TechEntry checkedTech, IShipDesign[] ourFactionShips)
    {
        if (checkedTech.IsHidden(this))
            return false;

        if (!checkedTech.IsOnlyShipTech() || isPlayer)
            return true;

        return WeCanUseThisInDesigns(checkedTech, ourFactionShips);
    }

    static bool WeCanUseThisInDesigns(TechEntry checkedTech, IShipDesign[] ourFactionShips)
    {
        // Dont offer tech to AI if it does not have designs for it.
        Technology tech = checkedTech.Tech;
        foreach (IShipDesign design in ourFactionShips)
        {
            foreach (Technology.UnlockedMod entry in tech.ModulesUnlocked)
            {
                if (design.UniqueModuleUIDs.Contains(entry.ModuleUID))
                    return true;
            }
        }
        return false;
    }

    public IShipDesign ChooseScoutShipToBuild()
    {
        if (!ChooseScoutShipToBuild(out IShipDesign scout))
            throw new($"{Name} is not able to find any Scout ships! ShipsWeCanBuild={string.Join(",", ShipsWeCanBuildIds)}");
        return scout;
    }

    public bool ChooseScoutShipToBuild(out IShipDesign scout)
    {
        if (isPlayer && ResourceManager.Ships.GetDesign(Universe.Player.data.CurrentAutoScout, out scout))
            return true;

        var scoutShipsWeCanBuild = new Array<IShipDesign>();
        foreach (IShipDesign design in ShipsWeCanBuildSnapshot)
            if (design.Role == RoleName.scout)
                scoutShipsWeCanBuild.Add(design);

        if (scoutShipsWeCanBuild.IsEmpty)
        {
            scout = null;
            return false;
        }

        // pick the scout with fastest FTL speed
        scout = scoutShipsWeCanBuild.FindMax(s => s.BaseWarpThrust);
        return scout != null;
    }
}
