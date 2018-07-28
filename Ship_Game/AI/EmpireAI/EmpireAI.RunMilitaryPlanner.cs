using System;
using System.Collections.Generic;
using System.Linq;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        private void RunMilitaryPlanner()
        {
            if (OwnerEmpire.isPlayer)
                return;            
            RunGroundPlanner();
            NumberOfShipGoals = 0;
            foreach (Planet p in OwnerEmpire.GetPlanets())
            {
                if (p.DevelopmentLevel < 3 || p.TotalTurnsInConstruction > 10)
                    continue;

                NumberOfShipGoals++;
            }
            int shipCountLimit        = GlobalStats.ShipCountLimit;
            RoleBuildInfo buildRatios = new RoleBuildInfo(BuildCapacity, this, false);
            float goalsInConstruction = buildRatios.CountShipsUnderConstruction();

            while (goalsInConstruction < NumberOfShipGoals
                   && (Empire.Universe.globalshipCount < shipCountLimit + Recyclepool
                       || OwnerEmpire.empireShipTotal < OwnerEmpire.EmpireShipCountReserve))
            {
                string s = GetAShip(buildRatios);
                if (string.IsNullOrEmpty(s))
                    break;

                if (Recyclepool > 0)
                    Recyclepool--;

                Goals.Add(new BuildOffensiveShips(s, OwnerEmpire));              
                goalsInConstruction = goalsInConstruction + 1f;
            }
            
            Goals.ApplyPendingRemovals();            

            //this where the global AI attack stuff happenes.
            using (TaskList.AcquireReadLock())
            {
                int toughNutCount = 0;

                foreach (var task in TaskList)
                {             
                    if (task.IsToughNut) toughNutCount++;
                    task.Evaluate(OwnerEmpire);
                }
                Toughnuts = toughNutCount;
            }
            TaskList.AddRange(TasksToAdd);
            TasksToAdd.Clear();
            TaskList.ApplyPendingRemovals();
        }

        public class RoleBuildInfo
        {
            public float RatioFighters;
            public float RatioCorvettes;
            public float RatioFrigates;
            public float RatioCruisers;
            public float RatioCapitals;         
            public float RatioBombers   = 0;
            public float RatioCarriers  = 0;
            public float RatioSupport   = 0;
            public float RatioTroopShip = 0;
            public float CapFighters = 1;
            public float CapCorvettes;
            public float CapFrigates;
            public float CapCruisers;
            public float CapCapitals;
            public float CapBombers;
            public float CapCarriers;
            public float CapSupport;
            public float CapTroops;
            public float NumFighters;
            public float NumCorvettes;
            public float NumFrigates;
            public float NumCruisers;
            public float NumCarriers;
            public float NumBombers;
            public float NumCapitals;
            public float NumTroops;
            public float NumSupport;
            public float TotalUpkeep =1;
            public float TotalMilShipCount;
            private readonly EmpireAI EmpireAI;
            private Empire OwnerEmpire  => EmpireAI.OwnerEmpire;
            public float DesiredFighters;
            public float DesiredCorvettes;
            public float DesiredFrigates;
            public float DesiredCruisers;
            public float DesiredCapitals;
            public float DesiredCarriers;
            public float DesiredBombers;
            public float DesiredSupport;
            public float DesiredTroops;

            public RoleBuildInfo(float capacity, EmpireAI eAI, bool ignoreDebt)
            {                
                EmpireAI = eAI;
                RatioFighters = 0.5f;

                var availableShips = OwnerEmpire.GetShips().FilterBy(item => 
                    !( item == null || !item.Active || item.Mothership != null || item.AI.State == AIState.Scrap
                    || item.shipData.Role == ShipData.RoleName.prototype
                    || item.shipData.Role < ShipData.RoleName.troopShip
                    ));

                for (int i = 0; i < availableShips.Length; i++)
                {
                    Ship item = availableShips[i];
             
                    ShipData.RoleName roleName = item.DesignRole;
                    float upkeep = item.GetMaintCost();
                    CountShips(upkeep, roleName);
                }
                
                //Set ratio of capacity by class
                float totalRatio;

                if (OwnerEmpire.canBuildCapitals)
                {
                    totalRatio = SetRatios(fighters: 1, corvettes: 4 , frigates: 8, cruisers: 6 , capitals: 1, bombers: 1f, carriers: 1f, support: 1f, troopShip: 1f);                    
                }
                else if (OwnerEmpire.canBuildCruisers)
                {
                    totalRatio = SetRatios(fighters: 3, corvettes: 12, frigates: 4, cruisers: 1, capitals: 0, bombers: 1f, carriers: 1f, support: 1f, troopShip: 1f);                    
                }
                else if (OwnerEmpire.canBuildFrigates)
                {
                    totalRatio = SetRatios(fighters: 2, corvettes: 3, frigates: 1, cruisers: 0, capitals: 0, bombers: 1f, carriers: 1f, support: 1f, troopShip: 1f);                    
                }
                else if (OwnerEmpire.canBuildCorvettes)
                {
                    totalRatio = SetRatios(fighters: 2, corvettes: 1, frigates: 0, cruisers: 0, capitals: 0, bombers: 1f, carriers: 1f, support: 25f, troopShip: 1f);                    
                }
                else
                {
                    totalRatio = SetRatios(1, 0, 0, 0, 0, 0, 0, 0, 0);
                }

                float tempCap = TotalUpkeep - capacity;
                if (tempCap > 0)
                {
                    if (ignoreDebt)
                        capacity += tempCap;
                    else
                    {
                        tempCap = capacity + TotalUpkeep * .1f;
                        capacity = Math.Min(TotalUpkeep, tempCap);
                    }

                }
                DesiredFighters          = SetCounts(NumFighters, CapFighters, capacity, RatioFighters ,totalRatio);
                DesiredCorvettes         = SetCounts(NumCorvettes, CapCorvettes, capacity, RatioCorvettes, totalRatio);
                DesiredFrigates          = SetCounts(NumFrigates, CapFrigates, capacity, RatioFrigates, totalRatio);
                DesiredCruisers          = SetCounts(NumCruisers, CapCruisers, capacity, RatioCruisers, totalRatio);
                DesiredCapitals          = SetCounts(NumCapitals, CapCapitals, capacity, RatioCapitals, totalRatio);
                DesiredCarriers          = SetCounts(NumCarriers, CapCarriers, capacity, RatioCarriers, totalRatio);
                DesiredBombers           = SetCounts(NumBombers, CapBombers, capacity, RatioBombers, totalRatio);
                DesiredSupport           = SetCounts(NumSupport, CapSupport, capacity, RatioSupport, totalRatio);
                DesiredTroops            = SetCounts(NumTroops, CapTroops, capacity, RatioTroopShip, totalRatio);

                KeepRoleRatios(DesiredFighters, DesiredCorvettes, DesiredFrigates, DesiredCruisers
                    , DesiredCarriers, DesiredBombers, DesiredCapitals, DesiredTroops, DesiredSupport);
            }

            public int CountShipsUnderConstruction()
            {
                int underConstruction = 0;
                foreach (Goal g in EmpireAI.Goals)
                {
                    if (g.UID != "BuildOffensiveShips")
                        continue;
                    if (g.beingBuilt == null)
                        continue;
                    var upKeep = g.beingBuilt.GetMaintCost();
                    var role = g.beingBuilt.DesignRole;
                    CountShips(upKeep, role);
                    underConstruction++;                    
                }
                return underConstruction;
            }

            private void CountShips(float upkeep, ShipData.RoleName roleName)
            {
                if (upkeep < .01)
                    return;
                //carrier
                switch (roleName)
                {
                    case ShipData.RoleName.carrier:
                        SetCountsTrackRole(ref NumCarriers, ref CapCarriers, upkeep);
                        break;
                    case ShipData.RoleName.troopShip:
                        SetCountsTrackRole(ref NumTroops, ref CapTroops, upkeep);
                        break;
                    case ShipData.RoleName.support:
                        SetCountsTrackRole(ref NumSupport, ref CapSupport, upkeep);
                        break;
                    case ShipData.RoleName.bomber:
                        SetCountsTrackRole(ref NumBombers, ref CapBombers, upkeep);
                        break;
                    case ShipData.RoleName.capital:
                        SetCountsTrackRole(ref NumCapitals, ref CapCapitals, upkeep);
                        break;
                    case ShipData.RoleName.fighter:
                        SetCountsTrackRole(ref NumFighters, ref CapFighters, upkeep);
                        break;
                    case ShipData.RoleName.corvette:
                    case ShipData.RoleName.gunboat:
                        SetCountsTrackRole(ref NumCorvettes, ref CapCorvettes, upkeep);
                        break;
                    case ShipData.RoleName.frigate:
                    case ShipData.RoleName.destroyer:
                        SetCountsTrackRole(ref NumFrigates, ref CapFrigates, upkeep);
                        break;
                    case ShipData.RoleName.cruiser:
                        SetCountsTrackRole(ref NumCruisers, ref CapCruisers, upkeep);
                        break;
                }
            }

            private int SetCounts(float roleCount, float roleUpkeep, float capacity, float ratio, float totalRatio)
            {

                if (ratio < .01f) return 0;
                
                float shipUpkeep = Math.Max(roleUpkeep, 1) / Math.Max(roleCount, 1);
                float mainRatio = shipUpkeep * ratio;// / Math.Max(TotalUpkeep , 1);
                float possible = capacity * mainRatio / shipUpkeep;

                return (int)Math.Round(possible);

                
            }
            private void SetCountsTrackRole(ref float roleCount, ref float roleMaint, float upkeep)
            {
                roleCount++;
                roleMaint += upkeep;
                TotalMilShipCount++;
                TotalUpkeep += upkeep;
            }

            private float SetRatios(float fighters, float corvettes, float frigates, 
                float cruisers, float capitals, float bombers, float carriers, float support, float troopShip)
            {                
                RatioFighters      = fighters;
                RatioCorvettes     = corvettes;
                RatioFrigates      = frigates;
                RatioCruisers      = cruisers;
                RatioCapitals      = capitals;
                float totalRatio = RatioFighters + RatioCorvettes + RatioFrigates + RatioCruisers
                                   + RatioCapitals;


                if (OwnerEmpire.canBuildTroopShips)
                    RatioTroopShip = troopShip;
                if (OwnerEmpire.canBuildBombers)
                    RatioBombers   = bombers;

                if (OwnerEmpire.canBuildCarriers)
                {
                    RatioCarriers = carriers;  
                    RatioFighters = 0;
                }
                if (OwnerEmpire.canBuildSupportShips)
                    RatioSupport = support;
                return totalRatio + RatioSupport + RatioCarriers + RatioBombers + RatioTroopShip;
            }

            public void IncrementShipCount(ShipData.RoleName role)
            {
                switch (role)
                {                   
                    case ShipData.RoleName.troopShip:
                        NumTroops++;
                        break;
                    case ShipData.RoleName.support:
                        NumSupport++;
                        break;
                    case ShipData.RoleName.bomber:
                        NumBombers++;
                        break;
                    case ShipData.RoleName.fighter:
                        NumFighters++;
                        break;             
                    case ShipData.RoleName.corvette:
                        NumCorvettes++;
                        break;
                    case ShipData.RoleName.frigate:
                        NumFrigates++;
                        break;                    
                    case ShipData.RoleName.cruiser:
                        NumCruisers++;
                        break;
                    case ShipData.RoleName.carrier:
                        NumCarriers++;
                        break;
                    case ShipData.RoleName.capital:
                        NumCapitals++;
                        break;
                }
            }

            private bool KeepRoleRatios(float desiredFighters, float desiredCorvettes,
            float desiredFrigates, float desiredCruisers, float desiredCarriers, float desiredBombers, float desiredCapitals,
            float desiredTroops, float desiredSupport)
            {
                //Scrap ships when over building by class

                if (NumFighters <= desiredFighters && NumCorvettes <= desiredCorvettes &&
                    NumFrigates <= desiredFrigates && NumCruisers <= desiredCruisers &&
                    NumCarriers <= desiredCarriers && NumBombers <= desiredBombers &&
                    NumCapitals <= desiredCapitals && NumTroops <= desiredTroops && NumSupport <= desiredSupport)
                {
                    return false;
                }
                foreach (var ship in OwnerEmpire.GetShips()
                        .FilterBy(ship => !ship.InCombat &&
                                          (!ship.fleet?.IsCoreFleet ?? true)
                                          && ship.AI.State != AIState.Scrap && ship.AI.State != AIState.Scuttle && ship.AI.State != AIState.Resupply
                                          && ship.Mothership == null && ship.Active
                                          && ship.shipData.HullRole >= ShipData.RoleName.fighter &&
                                          ship.GetMaintCost(OwnerEmpire) > 0)
                        .OrderBy(ship => ship.shipData.TechsNeeded.Count)

                )
                {
                    if (!CheckRoleAndScrap(ref NumFighters , desiredFighters, ship, ShipData.RoleName.fighter) &&
                        !CheckRoleAndScrap(ref NumCarriers , desiredCarriers, ship, ShipData.RoleName.carrier) &&
                        !CheckRoleAndScrap(ref NumTroops   , desiredTroops, ship, ShipData.RoleName.troopShip) &&
                        !CheckRoleAndScrap(ref NumBombers  , desiredBombers, ship, ShipData.RoleName.bomber) &&
                        !CheckRoleAndScrap(ref NumCorvettes, desiredCorvettes, ship, ShipData.RoleName.corvette) &&
                        !CheckRoleAndScrap(ref NumFrigates , desiredFrigates, ship, ShipData.RoleName.frigate) &&
                        !CheckRoleAndScrap(ref NumCruisers , desiredCruisers, ship, ShipData.RoleName.cruiser) &&
                        !CheckRoleAndScrap(ref NumCapitals , desiredCapitals, ship, ShipData.RoleName.capital) &&
                        !CheckRoleAndScrap(ref NumSupport  , desiredSupport, ship, ShipData.RoleName.support))
                        continue;
                    if (NumFighters <= desiredFighters
                        && NumCorvettes <= desiredCorvettes
                        && NumFrigates <= desiredFrigates
                        && NumCruisers <= desiredCruisers
                        && NumCarriers <= desiredCarriers
                        && NumBombers <= desiredBombers
                        && NumCapitals <= desiredCapitals
                        && NumTroops <= desiredTroops
                        && NumSupport <= desiredSupport)
                        return false;
                }

                return true;

            }

            private static bool CheckRoleAndScrap(ref float numShips, float desiredShips, Ship ship, ShipData.RoleName role)
            {
                if (numShips <= desiredShips + 1 || ship.DesignRole != role || ship.fleet?.IsCoreFleet == false)
                    return false;
                numShips--;
                ship.AI.OrderScrapShip();
                return true;
            }
        }

        //fbedard: Build a ship with a random role
        
        private string GetAShip(RoleBuildInfo buildRatios)
        {
                    
            //Find ship to build
           
            var pickRoles = new Map<ShipData.RoleName, float>();
            PickRoles(ref buildRatios.NumCarriers, buildRatios.DesiredCarriers, ShipData.RoleName.carrier, pickRoles);
            PickRoles(ref buildRatios.NumTroops, buildRatios.DesiredTroops, ShipData.RoleName.troopShip, pickRoles);
            PickRoles(ref buildRatios.NumSupport, buildRatios.DesiredSupport, ShipData.RoleName.support, pickRoles);                        
            PickRoles(ref buildRatios.NumFrigates , buildRatios.DesiredFrigates, ShipData.RoleName.frigate, pickRoles);
            PickRoles(ref buildRatios.NumBombers  , buildRatios.DesiredBombers, ShipData.RoleName.bomber, pickRoles);
            PickRoles(ref buildRatios.NumCruisers , buildRatios.DesiredCruisers, ShipData.RoleName.cruiser, pickRoles);
            PickRoles(ref buildRatios.NumCapitals , buildRatios.DesiredCapitals, ShipData.RoleName.capital, pickRoles);
            PickRoles(ref buildRatios.NumFighters, buildRatios.DesiredFighters, ShipData.RoleName.fighter, pickRoles);
            PickRoles(ref buildRatios.NumCorvettes, buildRatios.DesiredCorvettes, ShipData.RoleName.corvette, pickRoles);




            foreach (var kv in pickRoles.OrderBy(val => val.Value))
            {
                string buildThis = PickFromCandidates(kv.Key);
                if (string.IsNullOrEmpty(buildThis)) continue;
                buildRatios.IncrementShipCount(kv.Key);
                return buildThis;
            }
           
            return null;  //Find nothing to build !
        }
    
        private static void PickRoles(ref float numShips, float desiredShips, ShipData.RoleName role, Map<ShipData.RoleName, float>
             rolesPicked)
        {            
            if (numShips >= desiredShips)
                return;            
            rolesPicked.Add(role,  numShips / desiredShips);
        }
        public string PickFromCandidates(ShipData.RoleName role) => PickFromCandidates(role, false, ShipModuleType.Dummy);
        public string PickFromCandidates(ShipData.RoleName role, bool efficiency, ShipModuleType targetModule)
        {
            var potentialShips = new Array<Ship>();
            string name = "";
            Ship ship;
            int maxTech = 0;
            float bestEfficiency = 0;
            foreach (string shipsWeCanBuild in OwnerEmpire.ShipsWeCanBuild)
            {
                if ((ship = ResourceManager.GetShipTemplate(shipsWeCanBuild, false)) == null) continue;


                if (role != ship.DesignRole)
                    continue;
                maxTech = Math.Max(maxTech, ship.shipData.TechsNeeded.Count);

                potentialShips.Add(ship);
                if (efficiency)
                {
                    bestEfficiency = Math.Max(bestEfficiency, ship.PercentageOfShipByModules(targetModule));
                }
            }
            float nearmax = maxTech * .80f;
            bestEfficiency *= .80f;
            if (potentialShips.Count > 0)
            {
                Ship[] sortedList = potentialShips.FilterBy(ships =>
                {
                    if (efficiency)
                        return ships.PercentageOfShipByModules(targetModule) >= bestEfficiency;
                    return ships.shipData.TechsNeeded.Count >= nearmax;
                });
                int newRand = (int)RandomMath.RandomBetween(0, sortedList.Length - 1);


                newRand = Math.Max(0, newRand);
                newRand = Math.Min(sortedList.Length - 1, newRand);

                ship = sortedList[newRand];
                name = ship.Name;
                if (Empire.Universe?.showdebugwindow ?? false)
                    Log.Info($"Chosen Role: {ship.DesignRole}  Chosen Hull: {ship.shipData.Hull}  " +
                             $"Strength: {ship.BaseStrength} Name: {ship.Name} ");
            }

            return name;
        }
    }
}