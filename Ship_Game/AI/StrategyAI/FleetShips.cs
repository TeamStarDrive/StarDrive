using Ship_Game.AI.Tasks;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SDGraphics;
using SDUtils;
using static Ship_Game.Ships.ShipDesign;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.AI
{
    /// <summary>
    /// used to classify a group of ships into fleets according to the fleet ratios.
    /// usage: create class and give it ships. it will talley their fleet characteristics
    /// and provide methods for extracting ship sets used for fleets.
    /// in general before extracting make sure that the tallies at least match what is wanted. 
    /// </summary>
    public class FleetShips
    {
        // need to add a way to prefer ships near to a point
        public float AccumulatedStrength { get; private set; }
        Empire OwnerEmpire;
        FleetRatios Ratios;
        Array<Ship> Ships = new Array<Ship>();

        public float WantedFleetCompletePercentage = 0.25f;
        public int InvasionTroops { get; private set; }
        public float InvasionTroopStrength { get; private set; }
        public int BombSecsAvailable { get; private set; }
        public int CurrentUseableFleets { get; private set; }
        public readonly int InitialUsableFleets; 

        readonly int[] RoleCount;
        readonly float[] RoleStrength;
        public int ShipSetsExtracted;
        public int TotalShips => Ships.Count;

        FleetShips(Empire ownerEmpire)
        {
            OwnerEmpire  = ownerEmpire;
            Ratios = new FleetRatios(OwnerEmpire);

            int items = Enum.GetNames(typeof(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole)).Length;
            RoleCount = new int[items];
            RoleStrength = new float[items];
        }

        public FleetShips(Empire ownerEmpire, Array<Ship> ships) : this(ownerEmpire)
        {
            for (int i = 0; i < ships.Count; i++)
            {
                Ship ship = ships[i];
                AddShip(ship);
            }

            CurrentUseableFleets = InitialUsableFleets = CountFleets(out float initialStrength);
        }

        public void RemoveUsableFleets(int howMany)
        {
            CurrentUseableFleets -= howMany;
        }

        public bool AddShip(Ship ship)
        {
            if (!ship.ShipIsGoodForGoals())
                return false;

            if (ship.Fleet != null)
            {
                Log.Error($"FleetRatios: attempting to add a ship already in a fleet '{ship.Fleet.Name}'. removing from fleet");
                ship.ClearFleet(returnToManagedPools: false, clearOrders: false);
            }

            Ships.Add(ship);
            int roleIndex = (int)EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
            RoleCount[roleIndex] += 1;
            RoleStrength[roleIndex] += ship.GetStrength();
            AccumulatedStrength += ship.GetStrength();

            if (ShipRoleToRoleType(ship.DesignRole) == RoleType.Troop)
            {
                InvasionTroops += ship.Carrier.PlanetAssaultCount;
                InvasionTroopStrength += ship.Carrier.PlanetAssaultStrength;
            }

            if (ship.DesignRole == RoleName.bomber)
                BombSecsAvailable += ship.BombsGoodFor60Secs;

            return true;
        }

        public void RemoveShip(Ship ship)
        {
            Ships.RemoveRef(ship);

            int roleIndex = (int)EmpireAI.RoleBuildInfo.RoleCounts.ShipRoleToCombatRole(ship.DesignRole);
            RoleCount[roleIndex] -= 1;
            RoleStrength[roleIndex] -= ship.GetStrength();
            AccumulatedStrength -= ship.GetStrength();

            if (ShipRoleToRoleType(ship.DesignRole) == RoleType.Troop)
            {
                InvasionTroops -= ship.Carrier.PlanetAssaultCount;
                InvasionTroopStrength -= ship.Carrier.PlanetAssaultStrength;
            }

            if (ship.DesignRole == RoleName.bomber)
                BombSecsAvailable -= ship.BombsGoodFor60Secs;
        }

        public Array<Ship> ExtractShips(HashSet<Ship> shipsToExtract)
        {
            var results = new Array<Ship>((ICollection<Ship>)shipsToExtract);
            for (int i = 0; i < results.Count; i++)
            {
                Ship ship = results[i];
                RemoveShip(ship);
            }

            return results;
        }

        public int CountFleets(out float strength)
        {
            if (OwnerEmpire.IsFaction || OwnerEmpire.isPlayer)
            {
                strength = 0;
                return 0;
            }

            float filledRoles = float.MaxValue;
            strength = 0;
            float completionWanted = 0.1f;
            foreach(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole item in Enum.GetValues(typeof(EmpireAI.RoleBuildInfo.RoleCounts.CombatRole)))
            {
                float wanted = Ratios.GetWanted(item);
                if (wanted > 0f)
                {
                    wanted = Math.Max(wanted * completionWanted, 1);
                    int index = (int)item;

                    float have = RoleCount[index];
                    if (have > 0f)
                    {
                        float filled = have / wanted;
                        float roleStrength = RoleStrength[index] * wanted / have;
                        strength += (filled > 1 ? roleStrength : 0);

                        filledRoles = Math.Min(filled, filledRoles);
                    }
                }
            }
            if (filledRoles >= float.MaxValue)
                return 0;
            return (int)filledRoles;
        }

        public int GetFleetShipsUpToStrength(HashSet<Ship> results, float strengthNeeded, int wantedFleetCount)
        {
            float totalStrength = 0;
            int completeFleets = 0;
            do
            {
                if(!GetCoreFleet(results))
                    break;

                totalStrength = results.Sum(s => s.GetStrength());
                ++completeFleets;
                ++ShipSetsExtracted;

            } while (completeFleets < wantedFleetCount || totalStrength < strengthNeeded);

            // in case we have more room for ships, or all ships in get core are carriers or something.
            int unfilledFleets = (wantedFleetCount - completeFleets).LowerBound(0);
            for (int i = 0; i < unfilledFleets; i++)
                GetSupplementalFleet(results);

            // if we fail to get enough strength, cancel everything
            return totalStrength < strengthNeeded ? 0 :completeFleets;
        }

        // core combat section of a fleet
        public bool GetCoreFleet(HashSet<Ship> results)
        {
            int sizeBefore = results.Count;
            GetCoreFleetRole(results, RoleName.fighter);
            GetCoreFleetRole(results, RoleName.corvette);
            GetCoreFleetRole(results, RoleName.frigate);
            GetCoreFleetRole(results, RoleName.cruiser);
            GetCoreFleetRole(results, RoleName.battleship);
            GetCoreFleetRole(results, RoleName.capital);

            if (results.Count > 0) // Add support and carriers if there are some ships
            {
                GetCoreFleetRole(results, RoleName.carrier);
                GetCoreFleetRole(results, RoleName.support);
            }

            if (OwnerEmpire.data.Traits.Prototype > 0)
                GetCoreFleetRole(results, RoleName.prototype);

            return results.Count - sizeBefore > 0;
        }

        public void GetCoreFleetRole(HashSet<Ship> results, RoleName role)
        {
            float wanted = role == RoleName.prototype ? 1 : Ratios.GetWanted(role);
            GetShips(results, wanted, role);
        }

        public void GetSupplementalFleet(HashSet<Ship> results)
        {
            GetShips(results, Ratios.MinCarriers, RoleName.carrier);
            GetShips(results, Ratios.MinSupport, RoleName.support);
        }

        public Array<Ship> ExtractTroops(int planetAssaultTroopsStrWanted)
        {
            var results = new HashSet<Ship>();
            GetTroops(results, planetAssaultTroopsStrWanted);
            return ExtractShips(results);
        }

        public void GetTroops(HashSet<Ship> results, int planetAssaultTroopsStrWanted)
        {
            int troopsWanted = 1 + planetAssaultTroopsStrWanted / OwnerEmpire.GetTypicalTroopStrength();
            foreach (Ship s in Ships)
            {
                int troops = s.DesignRoleType == RoleType.Troop ? s.Carrier.PlanetAssaultCount : 0;
                if (troops > 0)
                {
                    results.Add(s);
                    troopsWanted -= troops;
                    if (troopsWanted <= 0)
                        break;
                }
            }
        }

        public void GetBombers(HashSet<Ship> results, int bombSecsWanted, int fleetCount)
        {
            if (bombSecsWanted > 0 && Ratios.MinBombers > 0)
            {
                int bombsWanted = bombSecsWanted;
                int shipsWanted = fleetCount * (int)Ratios.MinBombers;
                int shipsFound = 0;
                foreach (Ship s in Ships)
                {
                    int bombs = s.DesignRole == RoleName.bomber ? s.BombsGoodFor60Secs : 0;
                    if (bombs > 0)
                    {
                        results.Add(s);
                        bombsWanted -= bombs;
                        if (bombsWanted <= 0)
                            break;

                        ++shipsFound;
                        if (shipsFound >= shipsWanted)
                            break;
                    }
                }
            }
        }

        void GetShips(HashSet<Ship> results, float wanted, RoleName role)
        {
            int setWanted = (int)(wanted * WantedFleetCompletePercentage);
            if (wanted > 0) // ensure minimum of 1 fleet needed even if percentage is less than 100%
                setWanted = setWanted.LowerBound(1);

            if (setWanted > 0)
            {
                int shipsFound = 0;
                foreach (Ship ship in Ships)
                {
                    if (ship.DesignRole == role && results.Add(ship))
                    {
                        ++shipsFound;
                        if (shipsFound >= setWanted)
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts a ship set with wanted characteristics. 
        /// </summary>
        /// <param name="minStrength">Combat strength of fleet ships</param>
        /// <param name="planetTroops">Troops still on planets</param>
        /// <param name="wantedFleetCount">Attempt to get this many fleets</param>
        /// <returns></returns>
        public Array<Ship> ExtractShipSet(float minStrength, Array<Troop> planetTroops, int wantedFleetCount,
            MilitaryTask task, out int fleetCount)
        {
            fleetCount= 0;
            if (BombSecsAvailable < task.TaskBombTimeNeeded)
                return new Array<Ship>();

            SortShipsByDistanceToPoint(task.AO);

            var ships = new HashSet<Ship>();
            fleetCount = GetFleetShipsUpToStrength(ships, minStrength, wantedFleetCount);
            if (fleetCount == 0 || ships.Count == 0)
                return new Array<Ship>();
            
            if (task.NeededTroopStrength > 0)
            {
                if (InvasionTroopStrength < task.NeededTroopStrength)
                    LaunchTroopsAndAddToShipList(task.NeededTroopStrength, planetTroops);
                GetTroops(ships, task.NeededTroopStrength);
            }

            GetBombers(ships, task.TaskBombTimeNeeded, fleetCount);
            return ExtractShips(ships);
        }

        void SortShipsByDistanceToPoint(Vector2 point)
        {
            Ships.Sort(s =>
            {
                if (s.System?.HostileForcesPresent(OwnerEmpire) ?? false)
                    return s.Position.SqDist(point) + OwnerEmpire.Universe.Size;

                return s.Position.SqDist(point);
            });
        }

        private void LaunchTroopsAndAddToShipList(int wantedTroopStrength, Array<Troop> planetTroops)
        {
            foreach (Troop troop in planetTroops.Filter(delegate(Troop t)
            {
                if (t.HostPlanet != null
                    && t.Loyalty != null
                    && t.CanLaunch // save some iterations to find tiles for irrelevant troops
                    && !t.HostPlanet.RecentCombat
                    && !t.HostPlanet.System.DangerousForcesPresent(t.Loyalty))
                {
                    return true;
                }

                return false;
            }))
            {
                if (InvasionTroopStrength > wantedTroopStrength)
                    break;

                Ship launched = troop.Launch();
                if (launched != null)
                    AddShip(launched);
            }
        }

        public void Clear()
        {
            Ships.Clear();
            OwnerEmpire = null;
        }
    }
}