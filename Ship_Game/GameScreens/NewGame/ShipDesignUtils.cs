using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.NewGame
{
    public static class ShipDesignUtils
    {
        public static void MarkDesignsUnlockable(ProgressCounter progress)
        {
            if (ResourceManager.Hulls.Count == 0)
                throw new ResourceManagerFailure("Hulls not loaded yet!");

            var hullUnlocks = GetHullTechUnlocks(); // 0.3ms
            var moduleUnlocks = GetModuleTechUnlocks(); // 0.07ms
            Map<string, string[]> techTreePaths = GetFullTechTreePaths();

            MarkHullsUnlockable(hullUnlocks, techTreePaths); // 0.3ms
            MarkShipsUnlockable(moduleUnlocks, techTreePaths, progress); // 52.5ms
        }

        // Gets a map of <HullName, RequiredTech>
        static Map<string, string> GetHullTechUnlocks()
        {
            var hullUnlocks = new Map<string, string>();
            foreach (Technology tech in ResourceManager.TechTree.Values)
            {
                for (int i = 0; i < tech.HullsUnlocked.Count; ++i)
                    hullUnlocks[tech.HullsUnlocked[i].Name] = tech.UID;
            }
            return hullUnlocks;
        }

        // Gets a map of <ModuleUID, RequiredTech>
        static Map<string, string> GetModuleTechUnlocks()
        {
            var moduleUnlocks = new Map<string, string>();
            foreach (Technology tech in ResourceManager.TechTree.Values)
            {
                for (int i = 0; i < tech.ModulesUnlocked.Count; ++i)
                    moduleUnlocks[tech.ModulesUnlocked[i].ModuleUID] = tech.UID;
            }
            return moduleUnlocks;
        }
        
        // Gets all tech UID's mapped to include their preceding tech UID's
        // For example: Tech="Ace Training" has a full tree path of:
        //              ["Ace Training","FighterTheory","HeavyFighterHull","StarshipConstruction"]
        static Map<string, string[]> GetFullTechTreePaths()
        {
            var techParentTechs = new Map<string, string[]>();
            foreach (Technology tech in ResourceManager.TechTree.Values)
            {
                string[] techs = new string[tech.Parents.Length + 1];
                techs[0] = tech.UID;
                for (int i = 0; i < tech.Parents.Length; ++i)
                    techs[i + 1] = tech.Parents[i].UID;

                techParentTechs[tech.UID] = techs;
            }
            return techParentTechs;
        }

        static void AddRange(HashSet<string> destination, HashSet<string> source)
        {
            foreach (string str in source)
                destination.Add(str);
        }

        static void AddRange(HashSet<string> destination, string[] source)
        {
            foreach (string str in source)
                destination.Add(str);
        }

        static void MarkHullsUnlockable(Map<string, string> hullUnlocks,
                                        Map<string, string[]> techTreePaths)
        {
            foreach (ShipData hull in ResourceManager.Hulls)
            {
                if (hull.Role == ShipData.RoleName.disabled)
                    continue;

                hull.UnLockable = false;

                if (hullUnlocks.TryGetValue(hull.Name, out string requiredTech))
                {
                    hull.UnLockable = true;
                    AddRange(hull.TechsNeeded, techTreePaths[requiredTech]);
                }

                if (hull.Role < ShipData.RoleName.fighter || hull.TechsNeeded.Count == 0)
                    hull.UnLockable = true;
            }
        }

        static void MarkShipsUnlockable(Map<string, string> moduleUnlocks,
                                        Map<string, string[]> techTreePaths, ProgressCounter step)
        {
            var templates = ResourceManager.GetShipTemplates();
            step.Start(templates.Count);

            foreach (Ship ship in templates)
            {
                step.Advance();

                ShipData shipData = ship.shipData;
                if (shipData == null)
                    continue;

                shipData.UnLockable = false;
                shipData.HullUnlockable = false;
                shipData.AllModulesUnlockable = false;

                if (shipData.HullRole == ShipData.RoleName.disabled)
                    continue;

                if (!shipData.BaseHull.UnLockable)
                    continue;
                
                // These are the leaf technologies which actually unlock our modules
                var leafTechsNeeds = new HashSet<string>();
                
                shipData.TechsNeeded.Clear();
                shipData.HullUnlockable = true;
                shipData.AllModulesUnlockable = true;

                foreach (ModuleSlotData module in ship.shipData.ModuleSlots)
                {
                    if (module.IsDummy)
                        continue;

                    if (moduleUnlocks.TryGetValue(module.ModuleUID, out string requiredTech))
                    {
                        leafTechsNeeds.Add(requiredTech);
                    }
                    else
                    {
                        shipData.AllModulesUnlockable = false;
                        if (!ResourceManager.GetModuleTemplate(module.ModuleUID, out ShipModule _))
                            Log.Info(ConsoleColor.Yellow, $"Module does not exist: ModuleUID='{module.ModuleUID}'  ship='{ship.Name}'");
                        else
                            Log.Info(ConsoleColor.Yellow, $"Module cannot be unlocked by tech: ModuleUID='{module.ModuleUID}'  ship='{ship.Name}'");
                        break;
                    }
                }

                if (shipData.AllModulesUnlockable)
                {
                    shipData.UnLockable = true;
                    if (shipData.BaseStrength <= 0f)
                        shipData.BaseStrength = ship.CalculateShipStrength();

                    // add the full tree of techs to TechsNeeded
                    foreach (string techName in leafTechsNeeds)
                        AddRange(shipData.TechsNeeded, techTreePaths[techName]);

                    // also add techs from basehull (already full tree)
                    AddRange(shipData.TechsNeeded, shipData.BaseHull.TechsNeeded);

                    // now the TechScore can be calculated with full TechsNeeded
                    shipData.TechScore = 0;
                    foreach (string techname in shipData.TechsNeeded)
                    {
                        var tech = ResourceManager.TechTree[techname];
                        shipData.TechScore += tech.RootNode == 0 ? (int) tech.ActualCost : 0;
                    }
                }
                else
                {
                    shipData.BaseStrength = 0;
                }
            }
        }
    }
}
