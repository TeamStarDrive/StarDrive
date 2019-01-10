using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Ships;

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
            if (colonyType == ColonyType.Research && notResearching)
                colonyType = ColonyType.Industrial;

            Food.Percent = 0;
            Prod.Percent = 0;
            Res.Percent = 0;

            switch (colonyType)
            {
                case ColonyType.TradeHub:
                case ColonyType.Core:
                    //New resource management by Gretman
                    AssignCoreWorldWorkers();

                    if (colonyType == ColonyType.TradeHub)
                    {
                        DetermineFoodState(0.15f, 0.95f);   //Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                        DetermineProdState(0.15f, 0.95f);
                        break;
                    }

                    BuildBuildings(budget);
                    DetermineFoodState(0.25f, 0.666f);   //these will evaluate to: Start Importing if stores drop below 25%, and stop importing once stores are above 50%.
                    DetermineProdState(0.25f, 0.666f);   //                        Start Exporting if stores are above 66%, but dont stop exporting unless stores drop below 33%.

                    break;
                case ColonyType.Industrial:
                    //Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                    Food.Percent = FarmToPercentage(0.333f);
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(1));
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildBuildings(budget);
                    DetermineFoodState(0.50f, 1.0f);     //Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                    DetermineProdState(0.15f, 0.666f);   //Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.

                    break;

                case ColonyType.Research:
                    //This governor will rely on imports, focusing on research as long as no one is starving
                    Food.Percent = FarmToPercentage(0.333f);    //Farm to a small savings, and prevent starvation
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.333f));        //Save a litle production too
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildBuildings(budget);
                    DetermineFoodState(0.50f, 1.0f);     //Import if either drops below 50%, and stop importing once stores reach 100%.
                    DetermineProdState(0.50f, 1.0f);     //This planet will only export Food or Prod if there is excess FlatFood or FlatProd

                    break;

                case ColonyType.Agricultural:
                    Food.Percent = FarmToPercentage(1);     //Farm all you can
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.333f));    //Then work to a small savings
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildBuildings(budget);
                    DetermineFoodState(0.15f, 0.666f);   //Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    DetermineProdState(0.50f, 1.000f);   //Start Importing if prod drops below 50%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.

                    break;

                case ColonyType.Military:    //This on is incomplete
                    Food.Percent = FarmToPercentage(0.5f);     //Keep everyone fed, but dont be desperate for imports
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.5f));    //Keep some prod handy
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();
                    
                    BuildBuildings(budget);
                    DetermineFoodState(0.4f, 1.0f);     //Import if either drops below 40%, and stop importing once stores reach 80%.
                    DetermineProdState(0.4f, 1.0f);     //This planet will only export Food or Prod due to excess FlatFood or FlatProd

                    break;

            } //End Gov type Switch

            GovernTroopsAndPlatforms();

        }

        void GovernTroopsAndPlatforms()
        {
            if (ConstructionQueue.Count >= 5 || ParentSystem.CombatInSystem || DevelopmentLevel <= 2 ||
                colonyType == ColonyType.Research) return;

            //Added by McShooterz: build defense platforms

            if (HasShipyard && !ParentSystem.CombatInSystem
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
