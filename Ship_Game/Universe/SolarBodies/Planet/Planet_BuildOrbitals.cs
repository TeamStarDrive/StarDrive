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
        private void BuildPlatformsAndStations(PlanetBudget budget) // Rewritten by Fat Bastard
        {
            if (colonyType == ColonyType.Colony || Owner.isPlayer && !GovOrbitals
                                                || SpaceCombatNearPlanet
                                                || !HasSpacePort)
            {
                return;
            }
            var currentPlatforms = FilterOrbitals(ShipData.RoleName.platform);
            var currentStations  = FilterOrbitals(ShipData.RoleName.station);

            float maxSystemValue = ParentSystem.PlanetList.Max(v => v.ColonyValue);
            float ratioValue     = ColonyValue / maxSystemValue.ClampMin(1);
            int rank             = (int)(budget.SystemRank * ratioValue) + 3;
            var wantedOrbitals   = new WantedOrbitals(rank);

            BuildOrScrapShipyard(wantedOrbitals.Shipyards);
            BuildOrScrapStations(currentStations, wantedOrbitals.Stations, rank, budget.Orbitals);
            BuildOrScrapPlatforms(currentPlatforms, wantedOrbitals.Platforms, rank, budget.Orbitals);
        }

        void BuildOrScrapStations(Array<Ship> orbitals, int wanted, int rank, float budget)
            => BuildOrScrapOrbitals(orbitals, wanted, ShipData.RoleName.station, rank, budget);

        void BuildOrScrapPlatforms(Array<Ship> orbitals, int wanted, int rank, float budget)
            => BuildOrScrapOrbitals(orbitals, wanted, ShipData.RoleName.platform, rank, budget);

        float AverageProductionPercent      => Prod.NetMaxPotential / 3;
        bool GovernorShouldNotScrapBuilding => Owner.isPlayer && DontScrapBuildings;

        private Array<Ship> FilterOrbitals(ShipData.RoleName role)
        {
            var orbitalList = new Array<Ship>();
            foreach (Ship orbital in OrbitalStations.Values)
            {
                if (orbital.shipData.Role == role && !orbital.shipData.IsShipyard  // shipyards are not defense stations
                                                  && !orbital.IsConstructor)
                {
                    orbitalList.Add(orbital);
                }
            }
            return orbitalList;
        }

        int OrbitalsBeingBuilt(ShipData.RoleName role) => OrbitalsBeingBuilt(role, Owner);

        int OrbitalsBeingBuilt(ShipData.RoleName role, Empire owner)
        {
            // this also counts construction ships on the way, by checking the empire goals
            int numOrbitals = 0;
            foreach (Goal goal in owner.GetEmpireAI().Goals.Filter(g => g.type == GoalType.BuildOrbital && g.PlanetBuildingAt == this
                                                                     || g.type == GoalType.DeepSpaceConstruction && g.TetherTarget == guid))
            {
                if (ResourceManager.GetShipTemplate(goal.ToBuildUID, out Ship orbital) && orbital.shipData.Role == role)
                    numOrbitals++;
            }

            return numOrbitals;
        }

        int ShipyardsBeingBuilt() => ShipyardsBeingBuilt(Owner);

        private int ShipyardsBeingBuilt(Empire owner)
        {
            int shipyardsInQ = 0;
            foreach (Goal goal in owner.GetEmpireAI().Goals.Filter(g => g.type == GoalType.BuildOrbital && g.PlanetBuildingAt == this
                                                                     || g.type == GoalType.DeepSpaceConstruction && g.TetherTarget == guid))
            {
                if (ResourceManager.GetShipTemplate(goal.ToBuildUID, out Ship shipyard) && shipyard.shipData.IsShipyard)
                    shipyardsInQ++;
            }

            return shipyardsInQ;
        }

        private void BuildOrScrapOrbitals(Array<Ship> orbitalList, int orbitalsWeWant, ShipData.RoleName role, int colonyRank, float budget)
        {
            int orbitalsWeHave = orbitalList.Filter(o => !o.shipData.IsShipyard).Length + OrbitalsBeingBuilt(role);
            if (IsPlanetExtraDebugTarget())
                Log.Info($"{role}s we have: {orbitalsWeHave}, {role}s we want: {orbitalsWeWant}");

            if (orbitalList.NotEmpty && orbitalsWeHave > orbitalsWeWant)
            {
                Ship weakest = orbitalList.FindMin(s => s.BaseStrength);
                if (weakest != null)
                    ScrapOrbital(weakest); // remove this old garbage
                else
                    Log.Warning($"BuildOrScrapOrbitals: Weakest orbital is null even though orbitalList is not empty. Ignoring Scrap");
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
            if (OrbitalsInTheWorks)
                return;

            Ship orbital = PickOrbitalToBuild(role, colonyRank, budget);
            if (orbital == null)
                return;

            AddOrbital(orbital);
        }

        private int TimeVsCostThreshold => 25 + (int)(Owner.Money / 1000);

        // Adds an Orbital to ConstructionQueue
        public void AddOrbital(Ship orbital)
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
            float maxCost = (AverageProductionPercent * TimeVsCostThreshold) / ShipBuildingModifier;
            orbital       = GetBestOrbital(role, budget, maxCost);

            return orbital;
        }

        // This returns the best orbital the empire can build
        private Ship GetBestOrbital(ShipData.RoleName role, float orbitalsBudget)
        {
            Ship orbital = null;
            switch (role)
            {
                case ShipData.RoleName.platform: orbital = Owner.BestPlatformWeCanBuild; break;
                case ShipData.RoleName.station: orbital  = Owner.BestStationWeCanBuild; break;
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

        private bool LogicalBuiltTimeVsCost(float cost, int threshold)
        {
            float netCost = (Math.Max(cost - Storage.Prod, 0)) * ShipBuildingModifier;
            float ratio   = netCost / AverageProductionPercent;
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
                    case 1: Platforms  = 0;  Stations = 0; Shipyards = 0; break;
                    case 2: Platforms  = 0;  Stations = 0; Shipyards = 0; break;
                    case 3: Platforms  = 3;  Stations = 0; Shipyards = 0; break;
                    case 4: Platforms  = 5;  Stations = 0; Shipyards = 0; break;
                    case 5: Platforms  = 7;  Stations = 0; Shipyards = 0; break;
                    case 6: Platforms  = 2;  Stations = 1; Shipyards = 1; break;
                    case 7: Platforms  = 3;  Stations = 2; Shipyards = 1; break;
                    case 8: Platforms  = 5;  Stations = 2; Shipyards = 1; break;
                    case 9: Platforms  = 2;  Stations = 3; Shipyards = 1; break;
                    case 10: Platforms = 5;  Stations = 3; Shipyards = 2; break;
                    case 11: Platforms = 5;  Stations = 4; Shipyards = 2; break;
                    case 12: Platforms = 2;  Stations = 5; Shipyards = 2; break;
                    case 13: Platforms = 5;  Stations = 6; Shipyards = 2; break;
                    case 14: Platforms = 9;  Stations = 7; Shipyards = 2; break;
                    case 15: Platforms = 12; Stations = 8; Shipyards = 2; break;
                    default: Platforms = 0;  Stations = 0; Shipyards = 0; break;
                }
            }
        }

        // FB - gives a value from 1 to 15 based on the max colony value in the empire
        private int FindColonyRank(bool log = false)
        {
            int rank = (int)Math.Round(ColonyValue / Owner.MaxColonyValue * 10, 0);
            rank = ApplyRankModifiers(rank);

            if (IsPlanetExtraDebugTarget() && log)
                Log.Info($"COLONY RANK: {rank}, Colony Value: {ColonyValue}, Empire Max Value: {Owner.MaxColonyValue}," +
                         $" Time vs Cost threshold: {TimeVsCostThreshold}");

            return rank;
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
            rank += Owner.ColonyRankModifier;
            return rank.Clamped(0, 15);
        }

        private void BuildOrScrapShipyard(int numWantedShipyards)
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
                    && LogicalBuiltTimeVsCost(shipyard.GetCost(Owner), 50))
                {
                    AddOrbital(shipyard);
                }
            }
        }

        public int NumPlatforms => FilterOrbitals(ShipData.RoleName.platform).Count;
        public int NumStations  => FilterOrbitals(ShipData.RoleName.station).Count;

        public WantedOrbitals GovernorWantedOrbitals()
        {
            int rank = FindColonyRank();
            return new WantedOrbitals(rank);
        }

        public bool IsOutOfOrbitalsLimit(Ship ship) => IsOutOfOrbitalsLimit(ship, Owner);

        public bool IsOutOfOrbitalsLimit(Ship ship, Empire owner)
        {
            int numOrbitals  = OrbitalStations.Count + OrbitalsBeingBuilt(ship.shipData.Role, owner);
            int numShipyards = OrbitalStations.Values.Count(s => s.shipData.IsShipyard) + ShipyardsBeingBuilt(owner);
            if (numOrbitals >= ShipBuilder.OrbitalsLimit && ship.IsPlatformOrStation)
                return true;

            if (numShipyards >= ShipBuilder.ShipYardsLimit && ship.shipData.IsShipyard)
                return true;

            return false;
        }

        public void BuildAndScrapMilitaryBuildings(float budget)
        {
            if (BuildingInTheWorks || Prod.NetMaxPotential < 2)
                return;

            // if budget < 0 scrap
            // build
            // replace
        }

        void BuildMilitaryBuilding(float budget)
        {
            BuildingsCanBuild.FindMaxFiltered(b => b.IsMilitary && b.ActualMaintenance(this) > budget, b => b.MilitaryStrength);
        }
    }
}