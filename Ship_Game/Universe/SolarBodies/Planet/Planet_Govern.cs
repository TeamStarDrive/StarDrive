using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class Planet
    {
        public void DoGoverning()
        {
            RefreshBuildingsWeCanBuildHere();
            BuildOutpostIfAble();   //If there is no Outpost or Capital, build it

            if (colonyType == ColonyType.Colony) return; // No Governor? Nevermind!

            float budget = BuildingBudget();
            bool notResearching = string.IsNullOrEmpty(Owner.ResearchTopic);

            //Switch to Industrial if there is nothing in the research queue (Does not actually change assigned Governor)
            // FB - ignoring this if the owner is the player, or all of his research colonies will build stuff like
            // deep core mines if he forgets to add research
            if (colonyType == ColonyType.Research && notResearching && !Owner.isPlayer)
                colonyType = ColonyType.Core;

            Food.Percent = 0;
            Prod.Percent = 0;
            Res.Percent = 0;

            switch (colonyType)
            {
                case ColonyType.TradeHub:
                    DetermineFoodState(0.15f, 0.95f);   //Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                    DetermineProdState(0.15f, 0.95f);
                    break;
                case ColonyType.Core:
                    //New resource management by Gretman
                    AssignCoreWorldWorkers();
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.25f, 0.666f);   //these will evaluate to: Start Importing if stores drop below 25%, and stop importing once stores are above 50%.
                    DetermineProdState(0.25f, 0.666f);   //                        Start Exporting if stores are above 66%, but dont stop exporting unless stores drop below 33%.
                    break;
                case ColonyType.Industrial:
                    //Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                    Food.Percent = FarmToPercentage(0.333f);
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(1));
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.50f, 1.0f);     //Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                    DetermineProdState(0.15f, 0.666f);   //Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    break;
                case ColonyType.Research:
                    //This governor will rely on imports, focusing on research as long as no one is starving
                    Food.Percent = FarmToPercentage(0.333f);    //Farm to a small savings, and prevent starvation
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.333f));        //Save a litle production too
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.50f, 1.0f);     //Import if either drops below 50%, and stop importing once stores reach 100%.
                    DetermineProdState(0.50f, 1.0f);     //This planet will only export Food or Prod if there is excess FlatFood or FlatProd
                    break;
                case ColonyType.Agricultural:
                    Food.Percent = FarmToPercentage(1);     //Farm all you can
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.333f));    //Then work to a small savings
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.15f, 0.666f);   //Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    DetermineProdState(0.50f, 1.000f);   //Start Importing if prod drops below 50%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.
                    break;
                case ColonyType.Military:    //This on is incomplete
                    Food.Percent = FarmToPercentage(0.5f);     //Keep everyone fed, but dont be desperate for imports
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.5f));    //Keep some prod handy
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();
                    
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.4f, 1.0f);     //Import if either drops below 40%, and stop importing once stores reach 80%.
                    DetermineProdState(0.4f, 1.0f);     //This planet will only export Food or Prod due to excess FlatFood or FlatProd
                    break;
            } //End Gov type Switch
            //GovernTroopsAndPlatforms();
            BuildPlatformsAndStations();
        }

        private void BuildPlatformsAndStations()
        {
            int rank           = FindColonyRank();
            var wantedOrbitals = new WantedOrbitals(rank);
            var platforms      = FilterOrbitals(ShipData.RoleName.platform);
            var stations       = FilterOrbitals(ShipData.RoleName.station);

            BuildShipyardIfAble(wantedOrbitals.Shipyard);
            BuildOrScrapOrbitals(platforms, wantedOrbitals.Platforms, ShipData.RoleName.platform);
            BuildOrScrapOrbitals(stations, wantedOrbitals.Stations, ShipData.RoleName.station);
        }

        private Array<Ship> FilterOrbitals(ShipData.RoleName role)
        {
            var orbitalList = new Array<Ship>();
            foreach (Ship orbital in Shipyards.Values)
            {
                if (orbital.shipData.Role == role)
                    orbitalList.Add(orbital);
            }
            return orbitalList;
        }

        private void BuildOrScrapOrbitals(Array<Ship> orbitalList, int wantedOrbitals, ShipData.RoleName role)
        {
            int orbitalsWeHave = orbitalList.Count;
            if (wantedOrbitals > orbitalsWeHave)
            {
                Ship weakest = orbitalList.FindMin(s => s.BaseStrength);
                ScrapOrbital(weakest); // remove this old garbage
                return;
            }

            if (wantedOrbitals < orbitalsWeHave) // lets build an orbital
            {
                BuildOrbital(role);
                return;
            }

            ReplaceOrbital(orbitalList, role);  // check if we can replace an orbirtal with a better one
        }

        private void ScrapOrbital(Ship orbital)
        {
            float expectedStorage = Storage.Prod + orbital.BaseCost / 2;
            if (expectedStorage > Storage.Max) // excess cost will go to empire treasury
            {
                Storage.Prod = Storage.Max;
                Owner.AddMoney(expectedStorage - Storage.Max);
            }
            else
                Storage.Prod = expectedStorage;

            if (IsPlanetExtraDebugTarget())
                Log.Info($"SCRAPPED Orbital ----- {orbital.Name}, STR: {orbital.BaseStrength}");
            orbital.QueueTotalRemoval();
        }

        private void BuildOrbital(ShipData.RoleName role)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship orbital = GetBestOrbital(role);
            if (orbital == null)
                return;

            AddOrbital(orbital);
        }

        private void AddOrbital(Ship orbital) // add Orbital to ConstructionQueue
        {
            float cost = orbital.GetCost(Owner); // FB - need to check what happens with cost after shipyard is built.
            if (IsPlanetExtraDebugTarget())
                Log.Info($"ADDED Orbital ----- {orbital.Name}, cost: {cost}, STR: {orbital.BaseStrength}");
            ConstructionQueue.Add(new QueueItem(this)
            {
                isOrbital = true,
                sData = orbital.shipData,
                Cost = cost
            });
        }

        private Ship GetBestOrbital(ShipData.RoleName role)
        {
            string orbitalName = ShipBuilder.PickFromCandidates(role, Owner); // FB - get the best Orbital we can
            if (!ResourceManager.ShipsDict.TryGetValue(orbitalName, out Ship orbital))
                Log.Warning($"BuildOrbiral - Could not find {orbitalName} in {Owner.Name} list");

            return orbital;
        }

        private void ReplaceOrbital(Array<Ship> orbitalList, ShipData.RoleName role)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship weakestWeHave  = orbitalList.FindMin(s => s.BaseStrength);
            Ship bestWeCanBuild = GetBestOrbital(role);
            if (bestWeCanBuild.BaseStrength.Less(weakestWeHave.BaseStrength))
                return;

            ScrapOrbital(weakestWeHave);
            AddOrbital(bestWeCanBuild);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"REPLACED Orbital ----- {weakestWeHave.Name} with  {bestWeCanBuild.Name}, " +
                         $"STR: {weakestWeHave.BaseStrength} to {bestWeCanBuild.BaseStrength}");
        }

        private struct WantedOrbitals
        {
            public readonly int Platforms;
            public readonly int Stations;
            public readonly bool Shipyard;

            public WantedOrbitals(int rank)
            {
                switch (rank)
                {
                    case 1:  Platforms = 1; Stations = 0; break;
                    case 2:  Platforms = 2; Stations = 0; break;
                    case 3:  Platforms = 5; Stations = 0; break;
                    case 4:  Platforms = 7; Stations = 1; break;
                    case 5:  Platforms = 9; Stations = 1; break;
                    case 6:  Platforms = 5; Stations = 2; break;
                    case 7:  Platforms = 3; Stations = 2; break;
                    case 8:  Platforms = 1; Stations = 3; break;
                    case 9:  Platforms = 0; Stations = 3; break;
                    case 10: Platforms = 0; Stations = 4; break;
                    case 11: Platforms = 0; Stations = 4; break;
                    case 12: Platforms = 0; Stations = 5; break;
                    case 13: Platforms = 0; Stations = 6; break;
                    case 14: Platforms = 0; Stations = 7; break;
                    case 15: Platforms = 0; Stations = 8; break;
                    default: Platforms = 0; Stations = 0; break;
                }
                Shipyard = rank > 2;
            }
        }

        // FB - gives a value from 1 to 10 based on the max colony value in the empire
        private int FindColonyRank()
        {
            int rank = (int)(ColonyValue / Owner.MaxColonyValue * 10);
            return ApplyRankModifiers(rank);
        }

        private int ApplyRankModifiers(int currentRank)
        {
            int rank = currentRank -1 + (int)(Owner.Money / 10000);
            if (Owner.Money < 500)
                return (currentRank - 1).Clamped(0,15);

            if (RecentCombat)
                rank++;
            switch (colonyType)
            {
                case ColonyType.Core    : rank += 1; break;
                case ColonyType.Military: rank += 2; break;
            }
            return rank;
        }

        public void BuildShipyardIfAble(bool wantShipyard)
        {
            if (!wantShipyard || RecentCombat || !HasSpacePort) return;
            if (Shipyards.Any(ship => ship.Value.shipData.IsShipyard)
                || !Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
                return;

            bool hasShipyard = ConstructionQueue.Any(q => q.isShip && q.sData.IsShipyard);

            if (!hasShipyard && IsVibrant)
            {
                ConstructionQueue.Add(new QueueItem(this)
                {
                    isShip = true,
                    sData  = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                    Cost   = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner)
                });
            }
        }

        void GovernTroopsAndPlatforms()
        {
            if (ConstructionQueue.Count >= 5 || ParentSystem.CombatInSystem || IsMeagerOrBarren ||
                colonyType == ColonyType.Research) return;

            //Added by McShooterz: build defense platforms

            if (HasSpacePort && !ParentSystem.CombatInSystem
                            && (!Owner.isPlayer || colonyType == ColonyType.Military))
            {
                SystemCommander systemCommander;
                if (Owner.GetEmpireAI().DefensiveCoordinator.DefenseDict.TryGetValue(ParentSystem, out systemCommander))
                {
                    float defBudget = Owner.data.DefenseBudget * systemCommander.PercentageOfValue;

                    float platformUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.platform].Upkeep;
                    float stationUpkeep = ResourceManager.ShipRoles[ShipData.RoleName.station].Upkeep;

                    string station = Owner.GetEmpireAI().GetStarBase();
                    int platformCount = 0;

                    int stationCount = 0;
                    foreach (QueueItem queueItem in ConstructionQueue)
                    {
                        if (!queueItem.isShip)
                            continue;
                        if (queueItem.sData.HullRole == ShipData.RoleName.platform)
                        {
                            if (defBudget - platformUpkeep < -platformUpkeep * .5)
                            {
                                ConstructionQueue.QueuePendingRemoval(queueItem);
                                continue;
                            }

                            defBudget -= platformUpkeep;
                            platformCount++;
                        }

                        if (queueItem.sData.HullRole == ShipData.RoleName.station)
                        {
                            if (defBudget - stationUpkeep < -stationUpkeep)
                            {
                                ConstructionQueue.QueuePendingRemoval(queueItem);
                                continue;
                            }

                            defBudget -= stationUpkeep;
                            stationCount++;
                        }
                    }

                    foreach (Ship platform in Shipyards.Values)
                    {
                        if (platform.AI.State == AIState.Scrap)
                            continue;
                        switch (platform.shipData.HullRole)
                        {
                            case ShipData.RoleName.station:
                                stationUpkeep = platform.GetMaintCost();
                                if (defBudget - stationUpkeep < -stationUpkeep)
                                {
                                    platform.AI.OrderScrapShip();
                                    continue;
                                }

                                defBudget -= stationUpkeep;
                                stationCount++;
                                break;
                            case ShipData.RoleName.platform:
                                platformUpkeep = platform.GetMaintCost();
                                if (defBudget - platformUpkeep < -platformUpkeep)
                                {
                                    platform.AI.OrderScrapShip();

                                    continue;
                                }

                                defBudget -= platformUpkeep;
                                platformCount++;
                                break;
                        }
                    }

                    if (defBudget > stationUpkeep
                        && stationCount < (int) (systemCommander.RankImportance * .5f)
                        && stationCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                    {
                        if (!string.IsNullOrEmpty(station))
                        {
                            Ship ship = ResourceManager.ShipsDict[station];
                            if (ship.GetCost(Owner) / Prod.GrossIncome < 10)
                                ConstructionQueue.Add(new QueueItem(this)
                                {
                                    isShip = true,
                                    sData = ship.shipData,
                                    Cost = ship.GetCost(Owner)
                                });
                        }

                        defBudget -= stationUpkeep;
                    }

                    if (defBudget > platformUpkeep
                        && platformCount < systemCommander.RankImportance
                        && platformCount < GlobalStats.ShipCountLimit * GlobalStats.DefensePlatformLimit)
                    {
                        string platform = Owner.GetEmpireAI().GetDefenceSatellite();
                        if (!string.IsNullOrEmpty(platform))
                        {
                            Ship ship = ResourceManager.ShipsDict[platform];
                            ConstructionQueue.Add(new QueueItem(this)
                            {
                                isShip = true,
                                sData = ship.shipData,
                                Cost = ship.GetCost(Owner)
                            });
                        }
                    }
                }
            }
        }

    }
}
