﻿using Ship_Game;
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
            foreach (ShipData hull in ResourceManager.Hulls)
            {
                if (hull.Role == ShipData.RoleName.disabled)
                    continue;

                hull.UnLockable = false;
                foreach (Technology tech in shipTechs.Keys)
                {
                    if (tech.HullsUnlocked.Count == 0) continue;
                    foreach (Technology.UnlockedHull hulls in tech.HullsUnlocked)
                    {
                        if (hulls.Name == hull.Hull)
                        {
                            foreach (string tree in shipTechs[tech])
                            {
                                hull.TechsNeeded.Add(tree);
                                hull.UnLockable = true;
                            }
                            break;
                        }
                    }
                    if (hull.UnLockable)
                        break;
                }

                if (hull.Role < ShipData.RoleName.fighter || hull.TechsNeeded.Count == 0)
                    hull.UnLockable = true;
            }
        }

        static void MarkShipsUnlockable(Map<Technology, Array<string>> shipTechs, ProgressCounter step)
        {
            var templates = ResourceManager.GetShipTemplates();
            step?.Start(templates.Count);

            foreach (Ship ship in templates)
            {
                step?.Advance();

                ShipData shipData = ship.shipData;
                if (shipData == null)
                    continue;
                shipData.UnLockable = false;
                if (shipData.HullRole == ShipData.RoleName.disabled)
                    continue;

                if (shipData.BaseHull.UnLockable)
                {
                    foreach (string str in shipData.BaseHull.TechsNeeded)
                        shipData.TechsNeeded.Add(str);
                    shipData.HullUnlockable = true;
                }
                else
                {
                    shipData.AllModulesUnlockable = false;
                    shipData.HullUnlockable = false;
                    //Log.WarningVerbose($"Unlockable hull : '{shipData.Hull}' in ship : '{kv.Key}'");
                }

                if (shipData.HullUnlockable)
                {
                    shipData.AllModulesUnlockable = true;
                    foreach (ModuleSlotData module in ship.shipData.ModuleSlots)
                    {
                        if (module.IsDummy)
                            continue;
                        bool modUnlockable = false;
                        foreach (Technology technology in shipTechs.Keys)
                        {
                            foreach (Technology.UnlockedMod mods in technology.ModulesUnlocked)
                            {
                                if (mods.ModuleUID != module.ModuleUID) continue;
                                modUnlockable = true;
                                shipData.TechsNeeded.Add(technology.UID);
                                foreach (string tree in shipTechs[technology])
                                    shipData.TechsNeeded.Add(tree);
                                break;
                            }

                            if (modUnlockable)
                                break;
                        }

                        if (modUnlockable) continue;

                        shipData.AllModulesUnlockable = false;
                        //Log.WarningVerbose($"Unlockable module : '{module.InstalledModuleUID}' in ship : '{kv.Key}'");
                        break;
                    }
                }

                if (shipData.AllModulesUnlockable)
                {
                    shipData.UnLockable = true;
                    if (shipData.BaseStrength <= 0f)
                        shipData.BaseStrength = ship.CalculateShipStrength();

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
                    shipData.UnLockable = false;
                    shipData.TechsNeeded.Clear();
                    shipData.BaseStrength = 0;
                }
            }
        }
    }
}