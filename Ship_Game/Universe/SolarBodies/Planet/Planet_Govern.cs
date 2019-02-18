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

            switch (colonyType) //New resource management by Gretman  // @todo FB - dont assign researchers if they can do nothing 
            {
                case ColonyType.TradeHub:
                    AssignCoreWorldWorkers();
                    DetermineFoodState(0.15f, 0.95f);   //Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                    DetermineProdState(0.15f, 0.95f);
                    break;
                case ColonyType.Core:
                    AssignCoreWorldWorkers();
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.2f, 0.5f);   //these will evaluate to: Start Importing if stores drop below 20%, and stop importing once stores are above 50%.
                    DetermineProdState(0.2f, 0.5f);   //                        Start Exporting if stores are above 50%, but dont stop exporting unless stores drop below 33%.
                    break;
                case ColonyType.Industrial:
                    //Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                    Food.Percent = FarmToPercentage(0.333f);
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(1));
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.5f, 1.0f);     //Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                    DetermineProdState(0.15f, 0.666f);   //Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    break;
                case ColonyType.Research: 
                    //This governor will rely on imports, focusing on research as long as no one is starving
                    Food.Percent = FarmToPercentage(0.333f);    //Farm to a small savings, and prevent starvation
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.333f));        //Save a little production too
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.5f, 1.0f);     //Import if either drops below 50%, and stop importing once stores reach 100%.
                    DetermineProdState(0.5f, 1.0f);     //This planet will only export Food or Prod if there is excess FlatFood or FlatProd
                    break;
                case ColonyType.Agricultural:
                    Food.Percent = FarmToPercentage(1);     //Farm all you can
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.333f));    //Then work to a small savings
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();

                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.15f, 0.5f);   //Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    DetermineProdState(0.5f, 1.000f);   //Start Importing if prod drops below 50%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.
                    break;
                case ColonyType.Military:    //This on is incomplete
                    Food.Percent = FarmToPercentage(0.5f);     //Keep everyone fed, but dont be desperate for imports
                    Prod.Percent = Math.Min(1 - Food.Percent, WorkToPercentage(0.5f));    //Keep some prod handy
                    if (ConstructionQueue.Count > 0) Prod.Percent = Math.Max(Prod.Percent, (1 - Food.Percent) * 0.5f);
                    Res.AutoBalanceWorkers();
                    
                    BuildAndScrapBuildings(budget);
                    DetermineFoodState(0.5f, 1.0f);     //Import if either drops below 40%, and stop importing once stores reach 80%.
                    DetermineProdState(0.5f, 1.0f);     //This planet will only export Food or Prod due to excess FlatFood or FlatProd
                    break;
            } //End Gov type Switch

            BuildPlatformsAndStations();
        }

        private void BuildPlatformsAndStations()
        {
            var currentPlatforms      = FilterOrbitals(ShipData.RoleName.platform);
            var currentStations       = FilterOrbitals(ShipData.RoleName.station);
            int rank                  = FindColonyRank();
            var wantedOrbitals        = new WantedOrbitals(rank, currentStations);

            BuildShipyardIfAble(wantedOrbitals.Shipyards);
            BuildOrScrapOrbitals(currentStations, wantedOrbitals.Stations, ShipData.RoleName.station, rank);
            BuildOrScrapOrbitals(currentPlatforms, wantedOrbitals.Platforms, ShipData.RoleName.platform, rank);
        }

        private Array<Ship> FilterOrbitals(ShipData.RoleName role)
        {
            var orbitalList = new Array<Ship>();
            foreach (Ship orbital in OrbitalStations.Values)
            {
                if (orbital.shipData.Role == role && !orbital.shipData.IsShipyard  // shipyards are not defense stations
                                                  && !orbital.isConstructor) 
                    orbitalList.Add(orbital);
            }
            return orbitalList;
        }

        private void BuildOrScrapOrbitals(Array<Ship> orbitalList, int orbitalsWeWant, ShipData.RoleName role, int colonyRank)
        {
            int orbitalsWeHave = orbitalList.Count;
            if (IsPlanetExtraDebugTarget())
                Log.Info($"{role}s we have: {orbitalsWeHave}, {role}s we want: {orbitalsWeWant}");

            if (orbitalsWeHave > orbitalsWeWant)
            {
                Ship weakest = orbitalList.FindMin(s => s.BaseStrength);
                ScrapOrbital(weakest); // remove this old garbage
                return;
            }

            if (orbitalsWeHave < orbitalsWeWant) // lets build an orbital
            {
                BuildOrbital(role, colonyRank);
                return;
            }
            if (orbitalsWeHave > 0)
                ReplaceOrbital(orbitalList, role, colonyRank);  // check if we can replace an orbital with a better one
        }

        private void ScrapOrbital(Ship orbital)
        {
            float expectedStorage = Storage.Prod + orbital.GetCost(Owner) / 2;
            if (expectedStorage > Storage.Max) // taxed excess cost will go to empire treasury
            {
                Storage.Prod = Storage.Max;
                Owner.AddMoney((expectedStorage - Storage.Max) * Owner.data.TaxRate);
            }
            else
                Storage.Prod = expectedStorage;

            if (IsPlanetExtraDebugTarget())
                Log.Info($"SCRAPPED Orbital ----- {orbital.Name}, STR: {orbital.BaseStrength}");
            orbital.QueueTotalRemoval();
        }

        private void BuildOrbital(ShipData.RoleName role, int colonyRank)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship orbital = PickOrbitalToBuild(role, colonyRank);
            if (orbital == null)
                return;

            AddOrbital(orbital);
        }

        private int TimeVsCostThreshold => 50 + (int)(Owner.Money / 1000);

        // Adds an Orbital to ConstructionQueue
        private void AddOrbital(Ship orbital) 
        {
            float cost = orbital.GetCost(Owner);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"ADDED Orbital ----- {orbital.Name}, cost: {cost}, STR: {orbital.BaseStrength}");
            ConstructionQueue.Add(new QueueItem(this)
            {
                isOrbital = true,
                isShip    = true,
                sData = orbital.shipData,
                Cost = cost
            });
        }

        private void ReplaceOrbital(Array<Ship> orbitalList, ShipData.RoleName role, int rank)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship weakestWeHave  = orbitalList.FindMin(s => s.BaseStrength);
            Ship bestWeCanBuild = PickOrbitalToBuild(role, rank);

            if (bestWeCanBuild == null)
                return;

            if (bestWeCanBuild.BaseStrength.LessOrEqual(weakestWeHave.BaseStrength * 1.25f))
                return;

            ScrapOrbital(weakestWeHave);
            AddOrbital(bestWeCanBuild); 
            if (IsPlanetExtraDebugTarget())
                Log.Info($"REPLACING Orbital ----- {weakestWeHave.Name} with  {bestWeCanBuild.Name}, " +
                         $"STR: {weakestWeHave.BaseStrength} to {bestWeCanBuild.BaseStrength}");
        }

        private Ship PickOrbitalToBuild(ShipData.RoleName role, int colonyRank)
        {
            float orbitalsBudget = Money.NetRevenue + colonyRank - OrbitalsMaintenance;
            Ship orbital         = GetBestOrbital(role, orbitalsBudget);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"Orbitals Budget: {orbitalsBudget}");

            if (orbital == null)
                return null;

            if (LogicalBuiltTimecVsCost(orbital.GetCost(Owner), TimeVsCostThreshold))
                return orbital;

            // we cannot build the best in the empire, lets try building something cheaper for now
            float maxCost = (Prod.NetMaxPotential / 2 * (50 + colonyRank) + Storage.Prod) / ShipBuildingModifier;
            orbital       = GetBestOrbital(role, orbitalsBudget, maxCost);
            return orbital == null || orbital.Name == "Subspace Projector" ? null : orbital;
        }

        // This returns the best orbital the empire can build
        private Ship GetBestOrbital(ShipData.RoleName role, float orbitalsBudget)
        {
            Ship orbital =  null;
            switch (role)
            {
                case ShipData.RoleName.platform: orbital = Owner.BestPlatformWeCanBuild; break;
                case ShipData.RoleName.station:  orbital = Owner.BestStationWeCanBuild;  break;
            }
            if (orbital != null && orbitalsBudget - orbital.GetMaintCost(Owner) < 0)
                return null;

            return orbital;
        }

        //This returns the best orbital the Planet can build based on cost
        private Ship GetBestOrbital(ShipData.RoleName role, float orbitalsBudget, float maxCost)
        {
            Ship orbital = null;
            switch (role)
            {
                case ShipData.RoleName.station:
                case ShipData.RoleName.platform: orbital = ShipBuilder.PickCostEffectiveShipToBuild(role, Owner, maxCost, orbitalsBudget); break;
            }
            return orbital;
        }

        private bool LogicalBuiltTimecVsCost(float cost, int threshold)
        {
            float netCost = (Math.Max(cost - Storage.Prod, 0)) * ShipBuildingModifier;
            float ratio   = netCost / Prod.NetMaxPotential;
            return ratio < threshold;
        }

        private struct WantedOrbitals
        {
            public readonly int Platforms;
            public readonly int Stations;
            public readonly int Shipyards;

            public WantedOrbitals(int rank, Array<Ship> stationList)
            {
                switch (rank)
                {
                    case 1:  Platforms = 0;  Stations = 0;  Shipyards = 0; break;
                    case 2:  Platforms = 0;  Stations = 0;  Shipyards = 0; break;
                    case 3:  Platforms = 3;  Stations = 0;  Shipyards = 0; break;
                    case 4:  Platforms = 6;  Stations = 1;  Shipyards = 0; break;
                    case 5:  Platforms = 6;  Stations = 1;  Shipyards = 1; break;
                    case 6:  Platforms = 5;  Stations = 2;  Shipyards = 1; break;
                    case 7:  Platforms = 5;  Stations = 2;  Shipyards = 1; break;
                    case 8:  Platforms = 4;  Stations = 3;  Shipyards = 1; break;
                    case 9:  Platforms = 6;  Stations = 3;  Shipyards = 1; break;
                    case 10: Platforms = 8;  Stations = 4;  Shipyards = 2; break;
                    case 11: Platforms = 8;  Stations = 5;  Shipyards = 2; break;
                    case 12: Platforms = 9;  Stations = 6;  Shipyards = 2; break;
                    case 13: Platforms = 9;  Stations = 7;  Shipyards = 2; break;
                    case 14: Platforms = 12; Stations = 8;  Shipyards = 2; break;
                    case 15: Platforms = 15; Stations = 10; Shipyards = 2; break;
                    default: Platforms = 0;  Stations = 0;  Shipyards = 0; break;
                }
            }
        }

        // FB - gives a value from 1 to 10 based on the max colony value in the empire
        private int FindColonyRank()
        {
            int rank = (int)(ColonyValue / Owner.MaxColonyValue * 10);
            rank     = ApplyRankModifiers(rank);

            if (IsPlanetExtraDebugTarget())
                Log.Info($"COLONY RANK: {rank}, Colony Value: {ColonyValue}, Empire Max Value: {Owner.MaxColonyValue}," +
                         $" Time vs Cost threshold: {TimeVsCostThreshold}");

            return rank;
        }

        private int ApplyRankModifiers(int currentRank)
        {
            int rank = currentRank + ((int)(Owner.Money / 10000)).Clamped(-3,3);
            if (Owner.Money < 500)
                rank -= 2;
            else if (Owner.Money < 1000)
                rank -= 1;

            if (RecentCombat)
                rank += 1;

            if (MaxPopulationBillion.LessOrEqual(1))
                rank -= 2;

            switch (colonyType)
            {
                case ColonyType.Core    : rank += 1; break;
                case ColonyType.Military: rank += 3; break;
            }
            rank += Owner.ColonyRankModifier;
            return rank.Clamped(0, 15);
        }

        public void BuildShipyardIfAble(int numWantedShipyards)
        {
            if (numWantedShipyards == 0 || RecentCombat || !HasSpacePort)
                return;

            if (NumShipyards >= numWantedShipyards
                || !Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
                return;

            int shipyardsInQ = ConstructionQueue.Count(q => q.isShip && q.sData.IsShipyard);
            float cost       = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner);
            if (!LogicalBuiltTimecVsCost(cost, 30))
                return;

            if (NumShipyards + shipyardsInQ < numWantedShipyards)
            {
                ConstructionQueue.Add(new QueueItem(this)
                {
                    isShip    = true,
                    sData     = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].shipData,
                    Cost      = cost
                });
            }
        }
    }
}
