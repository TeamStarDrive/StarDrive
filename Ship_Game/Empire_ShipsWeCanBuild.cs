using System.Collections.Generic;
using System.Linq;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game;

public sealed partial class Empire
{
    [StarData] public HashSet<IShipDesign> ShipsWeCanBuild;
    // shipyards, platforms, SSP-s
    [StarData] public HashSet<IShipDesign> SpaceStationsWeCanBuild;

    // For TESTING
    public string[] ShipsWeCanBuildIds => ShipsWeCanBuild.Select(s => s.Name);

    /// <summary>
    /// TRUE if this Empire can build this ship
    /// </summary>
    public bool CanBuildShip(string shipUID)
    {
        if (ResourceManager.Ships.GetDesign(shipUID, out IShipDesign design))
            return ShipsWeCanBuild.Contains(design);
        return false;
    }

    public bool CanBuildShip(IShipDesign ship)
    {
        return ship != null && ShipsWeCanBuild.Contains(ship);
    }

    public bool CanBuildStation(IShipDesign station)
    {
        return station != null && SpaceStationsWeCanBuild.Contains(station);
    }

    public bool AddBuildableShip(IShipDesign ship)
    {
        bool added = ShipsWeCanBuild.Add(ship);
        if (added && ship.Role <= RoleName.station)
            SpaceStationsWeCanBuild.Add(ship);
        return added;
    }

    public bool RemoveBuildableShip(IShipDesign ship)
    {
        bool removed = ShipsWeCanBuild.Remove(ship);
        SpaceStationsWeCanBuild.Remove(ship);
        return removed;
    }

    public void ClearShipsWeCanBuild()
    {
        ShipsWeCanBuild.Clear();
        SpaceStationsWeCanBuild.Clear();
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
        if (ShipsWeCanBuild.Any(sd => !sd.IsValidDesign))
        {
            foreach (IShipDesign sd in ShipsWeCanBuild.ToArr())
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
        RemoveDuplicateShipDesigns(ShipsWeCanBuild);
        RemoveDuplicateShipDesigns(SpaceStationsWeCanBuild);
    }

    void RemoveDuplicateShipDesigns(HashSet<IShipDesign> designs)
    {
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

                designs.Remove(toRemove);
                unique[design.Name] = toKeep;
            }
            else
            {
                unique.Add(design.Name, design);
            }
        }
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
        foreach (IShipDesign design in ShipsWeCanBuild)
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
