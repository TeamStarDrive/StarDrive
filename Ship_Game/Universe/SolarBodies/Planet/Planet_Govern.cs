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

            switch (colonyType) //New resource management by Gretman
            {
                case ColonyType.TradeHub:
                    AssignCoreWorldWorkers();
                    DetermineFoodState(0.15f, 0.95f);   //Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                    DetermineProdState(0.15f, 0.95f);
                    break;
                case ColonyType.Core:
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

            BuildPlatformsAndStations();
        }

        private void BuildPlatformsAndStations()
        {
            var currentPlatforms      = FilterOrbitals(ShipData.RoleName.platform);
            var currentStations       = FilterOrbitals(ShipData.RoleName.station);
            int rank                  = FindColonyRank();
            var wantedOrbitals        = new WantedOrbitals(rank, currentStations);

            BuildShipyardIfAble(wantedOrbitals.Shipyard);
            BuildOrScrapOrbitals(currentPlatforms, wantedOrbitals.Platforms, ShipData.RoleName.platform);
            BuildOrScrapOrbitals(currentStations, wantedOrbitals.Stations, ShipData.RoleName.station);
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

        private void BuildOrScrapOrbitals(Array<Ship> orbitalList, int orbitalsWeWant, ShipData.RoleName role)
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
                BuildOrbital(role);
                return;
            }
            if (orbitalsWeHave > 0)
                ReplaceOrbital(orbitalList, role);  // check if we can replace an orbital with a better one
        }

        private void ScrapOrbital(Ship orbital)
        {
            float expectedStorage = Storage.Prod + orbital.GetCost(Owner) / 2;
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

            if (!LogicalBuiltTimecVsCost(orbital.GetCost(Owner), TimeVsCostThreshold))
                return;

            AddOrbital(orbital);
        }

        private int TimeVsCostThreshold => 50 + (int)(Owner.Money / 1000);

        private void AddOrbital(Ship orbital) // add Orbital to ConstructionQueue
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

        private void ReplaceOrbital(Array<Ship> orbitalList, ShipData.RoleName role)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship weakestWeHave  = orbitalList.FindMin(s => s.BaseStrength);
            Ship bestWeCanBuild = GetBestOrbital(role);

            if (bestWeCanBuild == null)
                return;

            if (bestWeCanBuild.BaseStrength.LessOrEqual(weakestWeHave.BaseStrength))
                return;

            if (!LogicalBuiltTimecVsCost(bestWeCanBuild.GetCost(Owner), 50))
                return;

            ScrapOrbital(weakestWeHave);
            AddOrbital(bestWeCanBuild); 
            if (IsPlanetExtraDebugTarget())
                Log.Info($"REPLACING Orbital ----- {weakestWeHave.Name} with  {bestWeCanBuild.Name}, " +
                         $"STR: {weakestWeHave.BaseStrength} to {bestWeCanBuild.BaseStrength}");
        }

        private Ship GetBestOrbital(ShipData.RoleName role)
        {
            Ship orbital =  null;
            switch (role)
            {
                case ShipData.RoleName.platform: orbital = Owner.BestPlatformWeCanBuild; break;
                case ShipData.RoleName.station:  orbital = Owner.BestStationWeCanBuild; break;
            }
            return orbital;
        }

        private bool LogicalBuiltTimecVsCost(float cost, int threshold)
        {
            float netCost = Math.Max(cost - Storage.Prod, 0);
            float ratio   = netCost / Prod.NetMaxPotential;
            return ratio < threshold;
        }

        private struct WantedOrbitals
        {
            public readonly int Platforms;
            public readonly int Stations;
            public readonly bool Shipyard;

            public WantedOrbitals(int rank, Array<Ship> stationList)
            {
                switch (rank)
                {
                    case 1:  Platforms = 0; Stations = 0; break;
                    case 2:  Platforms = 0; Stations = 0; break;
                    case 3:  Platforms = 3; Stations = 0; break;
                    case 4:  Platforms = 6; Stations = 0; break;
                    case 5:  Platforms = 8; Stations = 0; break;
                    case 6:  Platforms = 7; Stations = 1; break;
                    case 7:  Platforms = 7; Stations = 1; break;
                    case 8:  Platforms = 6; Stations = 2; break;
                    case 9:  Platforms = 5; Stations = 3; break;
                    case 10: Platforms = 4; Stations = 4; break;
                    case 11: Platforms = 3; Stations = 5; break;
                    case 12: Platforms = 2; Stations = 6; break;
                    case 13: Platforms = 1; Stations = 7; break;
                    case 14: Platforms = 0; Stations = 8; break;
                    case 15: Platforms = 8; Stations = 9; break;
                    default: Platforms = 0; Stations = 0; break;
                }
                Shipyard = rank > 4;
                // Fb - this will replace stations with temp platforms until proper stations are built
                int existingStations = stationList.Count;
                if (existingStations < Stations)
                    Platforms += Stations - existingStations; 
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
            switch (colonyType)
            {
                case ColonyType.Core    : rank += 1; break;
                case ColonyType.Military: rank += 2; break;
            }
            return rank.Clamped(0, 15);
        }

        public void BuildShipyardIfAble(bool wantShipyard)
        {
            if (!wantShipyard || RecentCombat || !HasSpacePort)
                return;

            if (OrbitalStations.Any(ship => ship.Value.shipData.IsShipyard)
                || !Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
                return;

            bool hasShipyard = ConstructionQueue.Any(q => q.isShip && q.sData.IsShipyard);
            float cost       = ResourceManager.ShipsDict[Owner.data.DefaultShipyard].GetCost(Owner);
            if (!LogicalBuiltTimecVsCost(cost, 30))
                return;

            if (!hasShipyard && IsVibrant && LogicalBuiltTimecVsCost(cost, 30))
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
