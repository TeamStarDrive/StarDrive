using System;
using System.Collections.Generic;
using System.Diagnostics;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using SDUtils;

namespace Ship_Game.GameScreens.NewGame
{
    public static class ShipDesignUtils
    {
        public static void MarkDesignsUnlockable(ProgressCounter progress = null)
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
                // set root techs to null because they are always unlocked
                string requiredTech = tech.IsRootNode ? null : tech.UID;
                for (int i = 0; i < tech.HullsUnlocked.Count; ++i)
                    hullUnlocks[tech.HullsUnlocked[i].Name] = requiredTech;
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
                int numParents = tech.Parents.Length;
                string[] techs = new string[numParents == 0 ? 1 : numParents];
                techs[0] = tech.UID;
                // NOTE: we include the last tech, since it's always the ROOT node
                for (int i = 0; i < numParents - 1; ++i)
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
            foreach (ShipHull hull in ResourceManager.Hulls)
            {
                hull.TechsNeeded.Clear(); // always clear techs list
                hull.Unlockable = false;

                if (hull.Role == RoleName.disabled)
                    continue;

                if (hullUnlocks.TryGetValue(hull.HullName, out string requiredTech))
                {
                    if (requiredTech != null) // ignore root techs
                    {
                        hull.Unlockable = true;
                        AddRange(hull.TechsNeeded, techTreePaths[requiredTech]);
                    }
                }

                if (hull.Role < RoleName.fighter || hull.TechsNeeded.Count == 0)
                    hull.Unlockable = true;
            }
        }

        static void MarkShipsUnlockable(Map<string, string> moduleUnlocks,
                                        Map<string, string[]> techTreePaths, ProgressCounter step)
        {
            IReadOnlyList<IShipDesign> templates = ResourceManager.ShipDesigns;
            step?.Start(templates.Count);

            foreach (IShipDesign ship in templates)
            {
                step?.Advance();

                ship.TechsNeeded.Clear(); // always clear techs list
                ship.Unlockable = false;

                if (!ship.BaseHull.Unlockable || ship.HullRole == RoleName.disabled)
                    continue;
                
                // These are the leaf technologies which actually unlock our modules
                var leafTechsNeeds = new HashSet<string>();
                bool allModulesUnlockable = true;

                foreach (string moduleUID in ship.UniqueModuleUIDs)
                {
                    if (moduleUnlocks.TryGetValue(moduleUID, out string requiredTech))
                    {
                        if (requiredTech != null) // ignore root techs
                            leafTechsNeeds.Add(requiredTech);
                    }
                    else
                    {
                        allModulesUnlockable = false;
                        if (!ResourceManager.GetModuleTemplate(moduleUID, out ShipModule _))
                        {
                            Log.Info(ConsoleColor.Yellow, $"Module does not exist: ModuleUID='{moduleUID}'  ship='{ship.Name}'");
                        }
                        else
                        {
                            //Log.Info(ConsoleColor.Yellow, $"Module cannot be unlocked by tech: ModuleUID='{moduleUID}'  ship='{ship.Name}'");
                        }
                        break;
                    }
                }

                if (allModulesUnlockable)
                {
                    ship.Unlockable = true;

                    // add the full tree of techs to TechsNeeded
                    foreach (string techName in leafTechsNeeds)
                        AddRange(ship.TechsNeeded, techTreePaths[techName]);

                    // also add techs from basehull (already full tree)
                    AddRange(ship.TechsNeeded, ship.BaseHull.TechsNeeded);
                }
            }
        }
    }
}
