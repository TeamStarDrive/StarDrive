using System;
using System.Linq;
using Ship_Game.AI;
using Ship_Game.AI.Budget;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;

namespace Ship_Game
{
    public partial class Planet 
    {
        public byte WantedPlatforms { get; private set; }
        public byte WantedStations  { get; private set; }
        public byte WantedShipyards { get; private set; }
        public bool GovOrbitals      = false;
        public bool GovGroundDefense = false;
        public bool AutoBuildTroops  = false;
        public bool ManualOrbitals   = false;
        public int GarrisonSize;
        public float ManualCivilianBudget { get; private set; } = 0; // 0 is Auto Budget
        public float ManualGrdDefBudget   { get; private set; } = 0; // 0 is Auto Budget
        public float ManualSpcDefBudget   { get; private set; } = 0; // 0 is Auto Budget

        private void BuildPlatformsAndStations(PlanetBudget budget) // Rewritten by Fat Bastard
        {
            if (colonyType == ColonyType.Colony || Owner.isPlayer && !GovOrbitals
                                                || SpaceCombatNearPlanet
                                                || !HasSpacePort)
            {
                return;
            }

            int rank             = GetColonyRank();
            var currentPlatforms = FilterOrbitals(ShipData.RoleName.platform);
            var currentStations  = FilterOrbitals(ShipData.RoleName.station);
            UpdateWantedOrbitals(rank);

            BuildOrScrapShipyard(WantedShipyards, budget.RemainingSpaceDef);
            BuildOrScrapStations(currentStations, WantedStations, budget.RemainingSpaceDef);
            BuildOrScrapPlatforms(currentPlatforms, WantedPlatforms, budget.RemainingSpaceDef);
        }

        public int GetColonyRank()
        {
            int rank = (int)Math.Round(ColonyValue/Owner.MaxColonyValue * 10, 0);
            return ApplyRankModifiers(rank);
        }

        void BuildOrScrapStations(Array<Ship> orbitals, byte wanted, float budget)
            => BuildOrScrapOrbitals(orbitals, wanted, ShipData.RoleName.station, budget);

        void BuildOrScrapPlatforms(Array<Ship> orbitals, byte wanted, float budget)
            => BuildOrScrapOrbitals(orbitals, wanted, ShipData.RoleName.platform, budget);

        bool GovernorShouldNotScrapBuilding => Owner.isPlayer && DontScrapBuildings;

        private Array<Ship> FilterOrbitals(ShipData.RoleName role)
        {
            var orbitalList = new Array<Ship>();
            foreach (Ship orbital in OrbitalStations)
            {
                if (orbital.shipData.Role == role && !orbital.shipData.IsShipyard  // shipyards are not defense stations
                                                  && !orbital.IsConstructor)
                {
                    orbitalList.Add(orbital);
                }
            }
            return orbitalList;
        }

        public int OrbitalsBeingBuilt(ShipData.RoleName role) => OrbitalsBeingBuilt(role, Owner);

        int OrbitalsBeingBuilt(ShipData.RoleName role, Empire owner)
        {
            if (owner == null)
                return 0;

            // this also counts construction ships on the way, by checking the empire goals
            int numOrbitals = 0;
            var goals = owner.GetEmpireAI().Goals;
            for (int i = 0; i < goals.Count; i++)
            {
                Goal g = goals[i];
                if (g != null && g.type == GoalType.BuildOrbital && g.PlanetBuildingAt == this 
                              || g.type == GoalType.DeepSpaceConstruction && g.TetherTarget == guid)
                {
                    if (ResourceManager.GetShipTemplate(g.ToBuildUID, out Ship orbital) && orbital.shipData.Role == role
                                                                                        && !orbital.shipData.IsShipyard)
                    {
                        numOrbitals++;
                    }
                }
            }

            return numOrbitals;
        }

        public int ShipyardsBeingBuilt() => ShipyardsBeingBuilt(Owner);

        private int ShipyardsBeingBuilt(Empire owner)
        {
            if (owner == null)
                return 0;

            int shipyardsInQ = 0;
            foreach (Goal goal in owner.GetEmpireAI().Goals.Filter(g => g.type == GoalType.BuildOrbital && g.PlanetBuildingAt == this
                                                                     || g.type == GoalType.DeepSpaceConstruction && g.TetherTarget == guid))
            {
                if (ResourceManager.GetShipTemplate(goal.ToBuildUID, out Ship shipyard) && shipyard.shipData.IsShipyard)
                    shipyardsInQ++;
            }

            return shipyardsInQ;
        }

        private void BuildOrScrapOrbitals(Array<Ship> orbitalList, byte orbitalsWeWant, ShipData.RoleName role, float budget)
        {
            int orbitalsWeHave = orbitalList.Filter(o => !o.shipData.IsShipyard).Length + OrbitalsBeingBuilt(role);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"{role}s we have: {orbitalsWeHave}, {role}s we want: {orbitalsWeWant}");

            if (orbitalList.NotEmpty && (orbitalsWeHave > orbitalsWeWant || budget < 0))
            {
                Ship weakest = orbitalList.FindMin(s => s.NormalizedStrength);
                if (weakest != null)
                    ScrapOrbital(weakest);
               
                return;
            }

            if (orbitalsWeHave < orbitalsWeWant) // lets build an orbital
            {
                BuildOrbital(role, budget);
                return;
            }

            if (orbitalList.Count > 0)
                ReplaceOrbital(orbitalList, role, budget);  // check if we can replace an orbital with a better one
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
                Log.Info(ConsoleColor.Magenta,$"{Name}, {Owner.Name} - SCRAPPED Orbital ----- {orbital.Name}" +
                         $", STR: {orbital.NormalizedStrength}");

            orbital.QueueTotalRemoval();
        }

        private void BuildOrbital(ShipData.RoleName role, float budget)
        {
            if (OrbitalsInTheWorks)
                return;

            Ship orbital = PickOrbitalToBuild(role, budget);
            if (orbital == null)
                return;

            AddOrbital(orbital);
        }

        private int TimeVsCostThreshold => 40 + (int)(Owner.Money / 1000);

        // Adds an Orbital to ConstructionQueue
        public void AddOrbital(Ship orbital)
        {
            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Green,$"{Name}, {Owner.Name} - ADDED Orbital ----- {orbital.Name}, " +
                         $"cost: {orbital.GetCost(Owner)}, STR: {orbital.NormalizedStrength}");

            Goal buildOrbital = new BuildOrbital(this, orbital.Name, Owner);
            Owner.GetEmpireAI().Goals.Add(buildOrbital);
        }

        private void ReplaceOrbital(Array<Ship> orbitalList, ShipData.RoleName role, float budget)
        {
            if (orbitalList.IsEmpty || OrbitalsInTheWorks)
                return;

            Ship weakestWeHave = orbitalList.FindMin(s => s.NormalizedStrength);
            if (weakestWeHave.AI.State == AIState.Refit)
                return; // refit one orbital at a time

            float weakestMaint  = weakestWeHave.GetMaintCost(Owner);
            Ship bestWeCanBuild = PickOrbitalToBuild(role, budget + weakestMaint);

            if (bestWeCanBuild == null)
                return;

            if (bestWeCanBuild.NormalizedStrength.Less(weakestWeHave.NormalizedStrength * 1.2f))
                return; // replace only if str is 20% more than the current weakest orbital

            string debugReplaceOrRefit;
            if (weakestWeHave.DesignRole == bestWeCanBuild.DesignRole)
            {
                Goal refitOrbital = new RefitOrbital(weakestWeHave, bestWeCanBuild.Name, Owner);
                Owner.GetEmpireAI().Goals.Add(refitOrbital);
                debugReplaceOrRefit = "REFITTING";
            }
            else
            {
                ScrapOrbital(weakestWeHave);
                AddOrbital(bestWeCanBuild);
                debugReplaceOrRefit = "REPLACING";
            }

            if (IsPlanetExtraDebugTarget())
                Log.Info(ConsoleColor.Cyan, $"{Name}, {Owner.Name} - {debugReplaceOrRefit} Orbital ----- {weakestWeHave.Name}" +
                         $" with {bestWeCanBuild.Name}, STR: {weakestWeHave.NormalizedStrength} to {bestWeCanBuild.NormalizedStrength}");
        }

        private Ship PickOrbitalToBuild(ShipData.RoleName role, float budget)
        {
            Ship orbital = GetBestOrbital(role, budget);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"Orbitals Budget: {budget}");

            if (orbital != null)
            {
                // If we can build the selected orbital in a timely manner at full production potential, select it.
                if (LogicalBuiltTimeVsCost(orbital.GetCost(Owner), TimeVsCostThreshold))
                    return orbital;
            }

            // We cannot build the best in the empire, lets try building something cheaper for now
            // and check if this can be built in a timely manner.
            float maxCost = (EstimatedAverageProduction * TimeVsCostThreshold) - Storage.Prod;
            maxCost      /= ShipBuildingModifier;
            orbital       = GetBestOrbital(role, budget, maxCost);

            return orbital;
        }

        // This returns the best orbital the empire can build
        private Ship GetBestOrbital(ShipData.RoleName role, float budget)
        {
            Ship orbital = null;
            switch (role)
            {
                case ShipData.RoleName.platform: orbital = Owner.BestPlatformWeCanBuild; break;
                case ShipData.RoleName.station: orbital  = Owner.BestStationWeCanBuild;  break;
            }
            if (orbital != null && orbital.GetMaintCost(Owner) > budget)
                return null; // Too much maintenance

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

        private bool LogicalBuiltTimeVsCost(float cost, int threshold)
        {
            float netCost = (Math.Max(cost - Storage.Prod, 0)) * ShipBuildingModifier;
            float ratio   = netCost / EstimatedAverageProduction;
            return ratio < threshold;
        }

        private int ApplyRankModifiers(int currentRank)
        {
            int rank = currentRank + ((int)(Owner.Money / 10000)).Clamped(-3, 3);
            if (Owner.Money < 500)
                rank -= 2;
            else if (Owner.Money < 1000)
                rank -= 1;

            if (MaxPopulationBillion.LessOrEqual(3))
                rank -= 2;

            switch (colonyType)
            {
                case ColonyType.Core: rank += 1; break;
                case ColonyType.Military: rank += 3; break;
            }
            rank += Owner.DifficultyModifiers.ColonyRankModifier;
            return rank.Clamped(0, 15);
        }

        private void BuildOrScrapShipyard(int numWantedShipyards, float budget)
        {
            if (numWantedShipyards == 0 || OrbitalsInTheWorks
                                        || !Owner.ShipsWeCanBuild.Contains(Owner.data.DefaultShipyard))
            {
                return;
            }

            int totalShipyards = NumShipyards + ShipyardsBeingBuilt();
            if (totalShipyards < numWantedShipyards)
            {
                string shipyardName = Owner.data.DefaultShipyard;
                if (ResourceManager.GetShipTemplate(shipyardName, out Ship shipyard)
                    && shipyard.GetMaintCost(Owner) < budget
                    && LogicalBuiltTimeVsCost(shipyard.GetCost(Owner), TimeVsCostThreshold))
                {
                    AddOrbital(shipyard);
                }
            }
        }

        public int NumPlatforms => FilterOrbitals(ShipData.RoleName.platform).Count;
        public int NumStations  => FilterOrbitals(ShipData.RoleName.station).Count;

        public bool IsOutOfOrbitalsLimit(Ship ship) => IsOutOfOrbitalsLimit(ship, Owner);

        public bool IsOutOfOrbitalsLimit(Ship ship, Empire owner)
        {
            int numOrbitals  = OrbitalStations.Count + OrbitalsBeingBuilt(ship.shipData.Role, owner);
            int numShipyards = OrbitalStations.Count(s => s.shipData.IsShipyard) + ShipyardsBeingBuilt(owner);
            if (numOrbitals >= ShipBuilder.OrbitalsLimit && ship.IsPlatformOrStation)
                return true;

            if (numShipyards >= ShipBuilder.ShipYardsLimit && ship.shipData.IsShipyard)
                return true;

            return false;
        }

        public void BuildTroopsForEvents()
        {
            if (TroopsHere.Count > 0 || Owner.isPlayer || TroopsInTheWorks || !EventsOnTiles())
                return;

            if (CanBuildInfantry)
                BuildSingleMilitiaTroop();
        }

        public void BuildTroops() // Relevant only for players with the Garrison Checkbox checked.
        {
            if (!Owner.isPlayer || !AutoBuildTroops || RecentCombat)
                return;

            int numTroopsInTheWorks = NumTroopsInTheWorks;
            if (numTroopsInTheWorks > 0)
                return; // We are already building troops

            int troopsWeHave = TroopsHere.Count; // No need to filter our troops here since the planet must not be in RecentCombat
            if (troopsWeHave < GarrisonSize && GetFreeTiles(Owner) > 0)
            {
                if (CanBuildInfantry)
                    BuildSingleMilitiaTroop();
                else
                    TryBuildMilitaryBase();
            }
        }

        void TryBuildMilitaryBase()
        {
            if (MilitaryBuildingInTheWorks)
                return;

            var cheapestInfantryBuilding = BuildingsCanBuild.FindMinFiltered(b => b.AllowInfantry, b => b.ActualCost);
            if (cheapestInfantryBuilding != null)
                Construction.Enqueue(cheapestInfantryBuilding);
        }

        void BuildSingleMilitiaTroop()
        {
            if (TroopsInTheWorks)
                return;  // Build one militia at a time

            Troop cheapestTroop = ResourceManager.GetTroopTemplatesFor(Owner).First();
            Construction.Enqueue(cheapestTroop);
        }

        void BuildAndScrapMilitaryBuildings(float budget)
        {
            if (Owner.isPlayer && !GovGroundDefense)
                return;

            if (MilitaryBuildingInTheWorks)
                return;

            if (budget < 0)
                TryScrapMilitaryBuilding();
            else
                TryBuildMilitaryBuilding(budget);
        }

        void TryBuildMilitaryBuilding(float budget)
        {
            if (FreeHabitableTiles == 0)
                return;

            Building building =  BuildingsCanBuild.FindMaxFiltered(b => b.IsMilitary && b.ActualMaintenance(this) < budget
                                 , b => b.CostEffectiveness);

            if (building != null)
                Construction.Enqueue(building);
        }
        
        void TryScrapMilitaryBuilding()
        {
            Building weakest = BuildingList.FindMinFiltered(b => b.IsMilitary 
                                                                 && b.Scrappable 
                                                                 && !b.IsPlayerAdded, b => b.CostEffectiveness);

            if (weakest != null)
                ScrapBuilding(weakest);
        }

        public void AddTroop(Troop troop, PlanetGridSquare tile)
        {
            TroopsHere.Add(troop);
            tile.AddTroop(troop);
            troop.SetPlanet(this);
        }

        public void UpdateWantedOrbitals(int rank)
        {
            if (ManualOrbitals || !Owner.isPlayer)
                return;

            switch (rank)
            {
                case 1:  WantedPlatforms = 0; WantedStations = 0; WantedShipyards = 0; break;
                case 2:  WantedPlatforms = 0; WantedStations = 0; WantedShipyards = 0; break;
                case 3:  WantedPlatforms = 1; WantedStations = 0; WantedShipyards = 0; break;
                case 4:  WantedPlatforms = 2; WantedStations = 0; WantedShipyards = 0; break;
                case 5:  WantedPlatforms = 3; WantedStations = 1; WantedShipyards = 0; break;
                case 6:  WantedPlatforms = 4; WantedStations = 1; WantedShipyards = 1; break;
                case 7:  WantedPlatforms = 5; WantedStations = 1; WantedShipyards = 1; break;
                case 8:  WantedPlatforms = 6; WantedStations = 2; WantedShipyards = 1; break;
                case 9:  WantedPlatforms = 7; WantedStations = 2; WantedShipyards = 1; break;
                case 10: WantedPlatforms = 8; WantedStations = 3; WantedShipyards = 2; break;
                case 11: WantedPlatforms = 9; WantedStations = 3; WantedShipyards = 2; break;
                case 12: WantedPlatforms = 9; WantedStations = 4; WantedShipyards = 2; break;
                case 13: WantedPlatforms = 9; WantedStations = 4; WantedShipyards = 2; break;
                case 14: WantedPlatforms = 9; WantedStations = 5; WantedShipyards = 2; break;
                case 15: WantedPlatforms = 9; WantedStations = 6; WantedShipyards = 2; break;
                default: WantedPlatforms = 0; WantedStations = 0; WantedShipyards = 0; break;
            }
        }

        public void RestoreWantedOrbitals(byte platforms, byte stations, byte shipyards)
        {
            SetWantedPlatforms(platforms);
            SetWantedStations(stations);
            SetWantedShipyards(shipyards);
        }

        public void SetWantedPlatforms(byte num)
        {
            WantedPlatforms = num;
        }

        public void SetWantedShipyards(byte num)
        {
            WantedShipyards = num;
        }

        public void SetWantedStations(byte num)
        {
            WantedStations = num;
        }

        public void RestoreManualBudgets(float civ, float grd, float spc)
        {
            SetManualCivBudget(civ);
            SetManualGroundDefBudget(grd);
            SetManualSpaceDefBudget(spc);
        }

        public void SetManualCivBudget(float num)
        {
            ManualCivilianBudget = num;
        }

        public void SetManualGroundDefBudget(float num)
        {
            ManualGrdDefBudget = num;
        }

        public void SetManualSpaceDefBudget(float num)
        {
            ManualSpcDefBudget = num;
        }
    }
}