using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
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

            if (colonyType == ColonyType.Colony)
                return; // No Governor? Nevermind!

            bool noResearch = Owner.ResearchTopic.IsEmpty();

            // Switch to Core if there is nothing in the research queue (Does not actually change assigned Governor)
            if (colonyType == ColonyType.Research && noResearch)
                colonyType = ColonyType.Core;

            Food.Percent = 0;
            Prod.Percent = 0;
            Res.Percent  = 0;

            ColonyBudget budget = AllocateColonyBudget();
            switch (colonyType) // New resource management by Gretman 
            {
                case ColonyType.TradeHub:
                    AssignCoreWorldWorkers();
                    DetermineFoodState(0.15f, 0.95f); // Minimal Intervention for the Tradehub, so the player can control it except in extreme cases
                    DetermineProdState(0.15f, 0.95f);
                    break;
                case ColonyType.Core:
                    AssignCoreWorldWorkers();
                    BuildAndScrapBuildings(budget.Buildings);
                    DetermineFoodState(0.2f, 0.5f); // Start Importing if stores drop below 20%, and stop importing once stores are above 50%.
                    DetermineProdState(0.2f, 0.5f); // Start Exporting if stores are above 50%, but dont stop exporting unless stores drop below 25%.
                    break;
                case ColonyType.Industrial:
                    // Farm to 33% storage, then devote the rest to Work, then to research when that starts to fill up
                    AssignOtherWorldsWorkers(0.333f, 1);
                    BuildAndScrapBuildings(budget.Buildings);
                    DetermineFoodState(0.5f, 1);    // Start Importing if food drops below 50%, and stop importing once stores reach 100%. Will only export food due to excess FlatFood.
                    DetermineProdState(0.15f, 0.666f); // Start Importing if prod drops below 15%, stop importing at 30%. Start exporting at 66%, and dont stop unless below 33%.
                    break;
                case ColonyType.Research:
                    //This governor will rely on imports, focusing on research as long as no one is starving
                    AssignOtherWorldsWorkers(0.15f, 0.15f);
                    BuildAndScrapBuildings(budget.Buildings);
                    DetermineFoodState(0.5f, 1); // Import if either drops below 50%, and stop importing once stores reach 100%.
                    DetermineProdState(0.2f, 0.6f); // This planet will export when stores reach 60%
                    break;
                case ColonyType.Agricultural:
                    AssignOtherWorldsWorkers(1, 0.333f);
                    BuildAndScrapBuildings(budget.Buildings);
                    DetermineFoodState(0.15f, 0.5f); // Start Importing if food drops below 15%, stop importing at 30%. Start exporting at 50%, and dont stop unless below 33%.
                    DetermineProdState(0.25f, 1);    // Start Importing if prod drops below 25%, and stop importing once stores reach 100%. Will only export prod due to excess FlatProd.
                    break;
                case ColonyType.Military:
                    AssignOtherWorldsWorkers(0.3f, 0.7f);
                    BuildAndScrapBuildings(budget.Buildings);
                    DetermineFoodState(0.4f, 0.95f); // Import if either drops below 40%, and stop importing once stores reach 95%.
                    DetermineProdState(0.75f, 1); // This planet will only export Food or Prod due to excess FlatFood or FlatProd
                    break;
            } // End Gov type Switch

            BuildPlatformsAndStations(budget.Orbitals);
            BuildMilitia();
        }

        private void BuildPlatformsAndStations(float budget) // Rewritten by Fat Bastard
        {
            if (colonyType == ColonyType.Colony || Owner.isPlayer && !GovOrbitals || SpaceCombatNearPlanet)
                return;

            var currentPlatforms      = FilterOrbitals(ShipData.RoleName.platform);
            var currentStations       = FilterOrbitals(ShipData.RoleName.station);
            int rank                  = FindColonyRank(log: true);
            var wantedOrbitals        = new WantedOrbitals(rank);

            BuildShipyardIfAble(wantedOrbitals.Shipyards);
            BuildOrScrapOrbitals(currentStations, wantedOrbitals.Stations, ShipData.RoleName.station, rank, budget);
            BuildOrScrapOrbitals(currentPlatforms, wantedOrbitals.Platforms, ShipData.RoleName.platform, rank, budget);
        }

        private Array<Ship> FilterOrbitals(ShipData.RoleName role)
        {
            var orbitalList = new Array<Ship>();
            foreach (Ship orbital in OrbitalStations.Values)
            {
                if (orbital.shipData.Role == role && !orbital.shipData.IsShipyard  // shipyards are not defense stations
                                                  && !orbital.isConstructor)
                {
                    orbitalList.Add(orbital);
                }
            }
            return orbitalList;
        }

        private int OrbitalsBeingBuilt(ShipData.RoleName role)
        {
            // this also counts construction ships on the way, by checking the empire goals
            int numOrbitals = 0;
            foreach (Goal goal in Owner.GetEmpireAI().Goals.Filter(g => g.type == GoalType.BuildOrbital && g.PlanetBuildingAt == this))
            {
                if (ResourceManager.GetShipTemplate(goal.ToBuildUID, out Ship orbital) && orbital.shipData.Role == role)
                    numOrbitals++;
            }

            return numOrbitals;
        }

        private void BuildOrScrapOrbitals(Array<Ship> orbitalList, int orbitalsWeWant, ShipData.RoleName role, int colonyRank, float budget)
        {
            int orbitalsWeHave = orbitalList.Count + OrbitalsBeingBuilt(role);
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
                BuildOrbital(role, colonyRank, budget);
                return;
            }

            if (orbitalList.Count > 0)
                ReplaceOrbital(orbitalList, role, colonyRank, budget);  // check if we can replace an orbital with a better one
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

        private void BuildOrbital(ShipData.RoleName role, int colonyRank, float budget)
        {
            if (OrbitalsInTheWorks || !HasSpacePort)
                return;

            Ship orbital = PickOrbitalToBuild(role, colonyRank, budget);
            if (orbital == null)
                return;

            AddOrbital(orbital);
        }

        private int TimeVsCostThreshold => 50 + (int)(Owner.Money / 1000);

        // Adds an Orbital to ConstructionQueue
        private void AddOrbital(Ship orbital) 
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info($"ADDED Orbital ----- {orbital.Name}, cost: {orbital.GetCost(Owner)}, STR: {orbital.BaseStrength}");

            Goal buildOrbital = new BuildOrbital(this, orbital.Name, Owner);
            Owner.GetEmpireAI().Goals.Add(buildOrbital);
        }

        private void ReplaceOrbital(Array<Ship> orbitalList, ShipData.RoleName role, int rank, float budget)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship weakestWeHave  = orbitalList.FindMin(s => s.BaseStrength);
            Ship bestWeCanBuild = PickOrbitalToBuild(role, rank, budget);

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

        private Ship PickOrbitalToBuild(ShipData.RoleName role, int colonyRank, float budget)
        {
            Ship orbital         = GetBestOrbital(role, budget);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"Orbitals Budget: {budget}");

            if (orbital == null)
                return null;

            if (LogicalBuiltTimecVsCost(orbital.GetCost(Owner), TimeVsCostThreshold))
                return orbital;

            // we cannot build the best in the empire, lets try building something cheaper for now
            float maxCost = (Prod.NetMaxPotential / 2 * (50 + colonyRank) + Storage.Prod) / ShipBuildingModifier;
            orbital       = GetBestOrbital(role, budget, maxCost);
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
        private Ship GetBestOrbital(ShipData.RoleName role, float budget, float maxCost)
        {
            Ship orbital = null;
            switch (role)
            {
                case ShipData.RoleName.station:
                case ShipData.RoleName.platform: orbital = ShipBuilder.PickCostEffectiveShipToBuild(role, Owner, maxCost, budget); break;
            }
            return orbital;
        }

        private bool LogicalBuiltTimecVsCost(float cost, int threshold)
        {
            float netCost = (Math.Max(cost - Storage.Prod, 0)) * ShipBuildingModifier;
            float ratio   = netCost / Prod.NetMaxPotential;
            return ratio < threshold;
        }

        public struct WantedOrbitals
        {
            public readonly int Platforms;
            public readonly int Stations;
            public readonly int Shipyards;

            public WantedOrbitals(int rank)
            {
                switch (rank)
                {
                    case 1:  Platforms = 0;  Stations  = 0;  Shipyards = 0; break;
                    case 2:  Platforms = 0;  Stations  = 0;  Shipyards = 0; break;
                    case 3:  Platforms = 3;  Stations  = 0;  Shipyards = 0; break;
                    case 4:  Platforms = 5;  Stations  = 0;  Shipyards = 0; break;
                    case 5:  Platforms = 7;  Stations  = 0;  Shipyards = 0; break;
                    case 6:  Platforms = 2;  Stations  = 1;  Shipyards = 1; break;
                    case 7:  Platforms = 3;  Stations  = 2;  Shipyards = 1; break;
                    case 8:  Platforms = 5;  Stations  = 2;  Shipyards = 1; break;
                    case 9:  Platforms = 2;  Stations  = 3;  Shipyards = 1; break;
                    case 10: Platforms = 5;  Stations  = 3;  Shipyards = 2; break;
                    case 11: Platforms = 5;  Stations  = 4;  Shipyards = 2; break;
                    case 12: Platforms = 2;  Stations  = 5;  Shipyards = 2; break;
                    case 13: Platforms = 5;  Stations  = 6;  Shipyards = 2; break;
                    case 14: Platforms = 9;  Stations  = 7;  Shipyards = 2; break;
                    case 15: Platforms = 12; Stations  = 8;  Shipyards = 2; break;
                    default: Platforms = 0;  Stations  = 0;  Shipyards = 0; break;
                }
            }
        }

        // FB - gives a value from 1 to 15 based on the max colony value in the empire
        private int FindColonyRank(bool log = false)
        {
            int rank = (int)Math.Round(ColonyValue / Owner.MaxColonyValue * 10, 0);
            rank     = ApplyRankModifiers(rank);

            if (IsPlanetExtraDebugTarget() && log)
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

            if (MaxPopulationBillion.LessOrEqual(3))
                rank -= 2;

            switch (colonyType)
            {
                case ColonyType.Core    : rank += 1; break;
                case ColonyType.Military: rank += 3; break;
            }
            rank += Owner.ColonyRankModifier;
            return rank.Clamped(0, 15);
        }

        private void BuildShipyardIfAble(int numWantedShipyards)
        {
            if (numWantedShipyards == 0 || RecentCombat || !HasSpacePort || OrbitalsInTheWorks)
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

        public void BuildMilitia()
        {
            // Only for player empires, if they choose to build militia
            if (!Owner.isPlayer || !GovMilitia || colonyType == ColonyType.Colony)
                return;
            if (CanBuildInfantry)
            {
                int troopsWeWant = TroopsWeWant();
                int troopsWeHave = TroopsHere.Count + NumTroopsInTheWorks;

                if (troopsWeHave < troopsWeWant)
                    BuildTroop();
            }
        }

        private int TroopsWeWant()
        {
            switch (colonyType)
            {
                case ColonyType.Research: return 4;
                case ColonyType.Core:     return 6;
                case ColonyType.Military: return 7;
                default:                  return 5;
            }
        }

        private void BuildTroop()
        {
            if (TroopsInTheWorks)
                return;  // Build one militia at a time

            Troop template = ResourceManager.GetTroopTemplates().FindMinFiltered(t => Owner.WeCanBuildTroop(t.Name), t => t.ActualCost);
            Construction.AddTroop(template);
        }

        // returns the amount of production to spend in the build queue based on import/export state
        public float LimitedProductionExpenditure()
        {
            float prodToSpend;
            if (colonyType == ColonyType.Colony)
            {
                switch (PS)
                {
                    default: // Importing
                        prodToSpend = ProdHere; // we are manually importing, so let's spend it all
                        break;
                    case GoodState.STORE:
                        if (Storage.ProdRatio.AlmostEqual(1))
                            prodToSpend = Prod.NetIncome; // Spend all our Income since storage is full
                        else
                            prodToSpend = Prod.NetIncome * 0.5f; // Store 50% of our prod income
                        break;
                    case GoodState.EXPORT:
                        if (OutgoingProdFreighters > 0)
                            prodToSpend = Prod.NetIncome * Storage.ProdRatio; // We are actively exporting so save some for storage
                        else
                            prodToSpend = ProdHere * 0.5f; // Spend 50% from our current stores and net production
                        break;
                }
            }
            else // Governor is auto managing good state
            {
                switch (PS)
                {
                    default: // Importing
                        if (IncomingProdFreighters > 0)
                            prodToSpend = ProdHere * 0.5f; // We have incoming prod, so we can spend more now
                        else
                            prodToSpend = Prod.NetIncome * Storage.ProdRatio; // Spend less since nothing is coming
                        break;
                    case GoodState.STORE:
                            prodToSpend = ProdHere * 0.5f; // Spend some of our store since we are storing for building stuff
                        break;
                    case GoodState.EXPORT:
                        if (OutgoingProdFreighters > 0 && ConstructionQueue.Count < 6)
                        {
                            if (Storage.ProdRatio > 0.8f)
                                prodToSpend = Prod.NetIncome + Storage.Prod * 0.1f; // We are actively exporting but can afford some storage spending
                            else
                                prodToSpend = Prod.NetIncome * Storage.ProdRatio; // We are actively exporting so save some for storage
                        }
                        else
                            prodToSpend = ProdHere; // We are exporting but there is no demand or we are building many things, so let's spend it ourselves
                        break;
                }
            }
            return prodToSpend;
        }

        ColonyBudget AllocateColonyBudget()
        {
            float budget         = Owner.GetEmpireAI().PlanetBudget(this).Budget;
            var colonyBudget     = new ColonyBudget(budget, colonyType, Owner, GovOrbitals);
            return colonyBudget;
        }

        struct ColonyBudget
        {
            public readonly float Buildings;
            public readonly float Orbitals;

            public ColonyBudget(float totalBudget, ColonyType colonyType, Empire owner, bool govOrbitals)
            {
                float buildingsBudget;
                if (colonyType == ColonyType.Colony || owner.isPlayer && !govOrbitals)
                    buildingsBudget = totalBudget; // Governor does not manage orbitals
                else
                {
                    switch (colonyType)
                    {
                        case ColonyType.Industrial:
                        case ColonyType.Agricultural: buildingsBudget = totalBudget * 0.8f; break;
                        case ColonyType.Military:     buildingsBudget = totalBudget * 0.6f; break;
                        case ColonyType.Research:     buildingsBudget = totalBudget * 0.9f; break;
                        default:                      buildingsBudget = totalBudget * 0.75f; break;
                    }
                }

                Buildings = (float)Math.Round(buildingsBudget, 2);
                Orbitals  = (float)Math.Round(totalBudget - buildingsBudget, 2);
            }
        }

        public float ColonyMaintenance => Money.Maintenance + Construction.TotalQueuedBuildingMaintenance() + OrbitalsMaintenance;
        public float ColonyDebtTolerance
        {
            get
            {
                float debtTolerance = 3 * (1 - PopulationRatio); // the bigger the colony, the less debt tolerance it has, it should be earning money 
                if (MaxPopulationBillion < 2)
                    debtTolerance += 2f - MaxPopulationBillion;

                return debtTolerance;
            }
        }

        public int NumPlatforms => FilterOrbitals(ShipData.RoleName.platform).Count;
        public int NumStations  => FilterOrbitals(ShipData.RoleName.station).Count;

        public WantedOrbitals GovernorWantedOrbitals()
        {
            int rank = FindColonyRank();
            return new WantedOrbitals(rank);
        }
    }
}
