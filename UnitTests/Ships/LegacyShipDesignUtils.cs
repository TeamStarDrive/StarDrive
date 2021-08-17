using System.Collections.Generic;
using Ship_Game;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.NewGame;
using Ship_Game.Ships;

namespace UnitTests.Ships
{
    
    /// <summary>
    /// LEGACY ShipDesignUtils, used to test the same behaviour on the new one. Delete once we make sure everything works as it should
    /// </summary>
    public static class ShipDesignUtilsOld
    {

        public static void MarkDesignsUnlockable(ProgressCounter progress = null)
        {
            if (ResourceManager.Hulls.Count == 0)
                throw new ResourceManagerFailure("Hulls not loaded yet!");

            Map<Technology, Array<string>> shipTechs = GetShipTechs(); // 2ms
            MarkDefaultUnlockable(shipTechs); // 0ms
            MarkShipsUnlockable(shipTechs, progress); // 220ms
        }

        static Map<Technology, Array<string>> GetShipTechs()
        {
            var shipTechs = new Map<Technology, Array<string>>();
            foreach (Technology tech in ResourceManager.TechTree.Values)
            {
                if ((tech.ModulesUnlocked.Count > 0 || tech.HullsUnlocked.Count > 0) && tech.Unlockable)
                {
                    shipTechs.Add(tech, FindPreviousTechs(tech, new Array<string>()));
                }
            }
            return shipTechs;
        }

        static Array<string> FindPreviousTechs(Technology target, Array<string> alreadyFound)
        {
            //this is supposed to reverse walk through the tech tree.
            foreach (var techTreeItem in ResourceManager.TechTree)
            {
                Technology tech = techTreeItem.Value;
                foreach (Technology.LeadsToTech leadsto in tech.LeadsTo)
                {
                    // if it finds a tech that leads to the target tech then find the tech that leads to it.
                    if (leadsto.UID == target.UID )
                    {
                        alreadyFound.Add(target.UID);
                        return FindPreviousTechs(tech, alreadyFound);
                    }
                }
            }
            return alreadyFound;
        }

        static void MarkDefaultUnlockable(Map<Technology, Array<string>> shipTechs)
        {
            foreach (ShipHull hull in ResourceManager.Hulls)
            {
                if (hull.Role == RoleName.disabled)
                    continue;

                hull.Unlockable = false;
                foreach (Technology tech in shipTechs.Keys)
                {
                    if (tech.HullsUnlocked.Count == 0) continue;
                    foreach (Technology.UnlockedHull hulls in tech.HullsUnlocked)
                    {
                        if (hulls.Name == hull.HullName)
                        {
                            foreach (string tree in shipTechs[tech])
                            {
                                hull.TechsNeeded.Add(tree);
                                hull.Unlockable = true;
                            }
                            break;
                        }
                    }
                    if (hull.Unlockable)
                        break;
                }

                if (hull.Role < RoleName.fighter || hull.TechsNeeded.Count == 0)
                    hull.Unlockable = true;
            }
        }

        static void MarkShipsUnlockable(Map<Technology, Array<string>> shipTechs, ProgressCounter step)
        {
            IReadOnlyList<ShipDesign> templates = ResourceManager.GetShipDesigns();
            step?.Start(templates.Count);

            foreach (ShipDesign ship in templates)
            {
                step?.Advance();

                ship.Unlockable = false;
                if (ship.HullRole == RoleName.disabled)
                    continue;

                bool hullUnlockable = false;
                bool allModulesUnlockable = false;
                if (ship.BaseHull.Unlockable)
                {
                    foreach (string str in ship.BaseHull.TechsNeeded)
                        ship.TechsNeeded.Add(str);
                    hullUnlockable = true;
                }

                if (hullUnlockable)
                {
                    allModulesUnlockable = true;
                    foreach (DesignSlot slot in ship.GetOrLoadDesignSlots())
                    {
                        bool modUnlockable = false;
                        foreach (Technology technology in shipTechs.Keys)
                        {
                            foreach (Technology.UnlockedMod mods in technology.ModulesUnlocked)
                            {
                                if (mods.ModuleUID != slot.ModuleUID) continue;
                                modUnlockable = true;
                                ship.TechsNeeded.Add(technology.UID);
                                foreach (string tree in shipTechs[technology])
                                    ship.TechsNeeded.Add(tree);
                                break;
                            }

                            if (modUnlockable)
                                break;
                        }

                        if (modUnlockable) continue;

                        allModulesUnlockable = false;
                        //Log.WarningVerbose($"Unlockable module : '{module.InstalledModuleUID}' in ship : '{kv.Key}'");
                        break;
                    }
                }

                if (allModulesUnlockable)
                {
                    ship.Unlockable = true;

                    // REMOVED from new version, so not needed here either
                    //shipData.TechScore = 0;
                    //foreach (string techname in shipData.TechsNeeded)
                    //{
                    //    var tech = ResourceManager.TechTree[techname];
                    //    shipData.TechScore += tech.RootNode == 0 ? (int) tech.ActualCost : 0;
                    //}
                }
                else
                {
                    ship.Unlockable = false;
                    ship.TechsNeeded.Clear();
                }
            }
        }
    }
}