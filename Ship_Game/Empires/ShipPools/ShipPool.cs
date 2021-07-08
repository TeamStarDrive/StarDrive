using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Utils;
using System;
using Ship_Game.Ships.Components;

namespace Ship_Game.Empires.ShipPools
{
    public class ShipPool : IShipPool
    {
        readonly Empire Owner;

        ChangePendingList<Ship> ForcePool;

        public Guid Guid { get; } = new Guid();
        public string Name { get; }
        public Empire OwnerEmpire => Owner;
        public EmpireAI OwnerAI => Owner.GetEmpireAI();
        public Array<Ship> Ships => ForcePool.Items;

        /// <summary>
        /// This is for adding to the Empire AI pool management.
        /// Player and other ships that can't be added to empireAI pool management will be safely ignored.
        /// </summary>
        public bool Add(Ship s)
        {
            if (s.loyalty != Owner && s.LoyaltyTracker.ChangeType == LoyaltyChanges.Type.None)
            {
                Log.Error($"Incorrect loyalty. Ship {s.loyalty} != Empire {Owner}");
                return false;
            }
            return EmpireForcePoolAdd(s);
        }

        public bool Contains(Ship s) => s.Pool == this;

        public float InitialStrength;
        public int InitialReadyFleets;
        public int InitialReadyShips;
        public int AllPoolShips;
        public float CurrentUseableStrength;
        public int CurrentUseableFleets;
        float PoolCheckTimer = 60;

        public FleetShips EmpireReadyFleets { get; private set; }

        public override string ToString()
        {
            return $"ShipPool {Guid} {Name} {Owner.Name} Ships={Ships.Count}";
        }

        public ShipPool(Empire empire, string name)
        {
            Owner = empire;
            Name = name;
            ForcePool = new ChangePendingList<Ship>(ShouldAddToForcePool);
        }

        bool ShouldAddToForcePool(Ship s)
        {
            return s.Active
                && s.loyalty == Owner
                && !s.loyalty.isPlayer
                && !s.loyalty.isFaction
                && !s.ShouldNotBeAddedToForcePools();
        }

        /// <summary>
        /// Ships meeting the criteria here should not be added to the empire force pools.
        /// these are temporary ships or soon to be removed or otherwise cant or should not be available
        /// to add to fleets. 
        /// </summary>
        bool ShouldNotAddToAnyPool(Ship ship) => ship.ShouldNotBeAddedToForcePools();
        bool ShouldAddToAOPools(Ship ship)    => ship.IsAWarShip;
        bool ShouldAddToEmpirePool(Ship ship) => ship.BaseCanWarp && ship.IsFleetSupportShip();

        public void Update()
        {
            ForcePool.Update();

            if (!Owner.isPlayer)
            {
                if (PoolCheckTimer-- < 0)
                {
                    PoolCheckTimer = 60;
                    ErrorCheckPools();
                }
            }
            var fleets             = new FleetShips(Owner, Owner.AllFleetReadyShips());
            EmpireReadyFleets      = fleets;
            CurrentUseableFleets   = InitialReadyFleets = EmpireReadyFleets.CountFleets(out float initialStrength);
            CurrentUseableStrength = InitialStrength = initialStrength;
            InitialReadyShips      = EmpireReadyFleets.TotalShips;
            var allShips           = GetShipsFromOffensePools();
            AllPoolShips           = allShips.Count;
        }

        public bool Remove(Ship ship)
        {
            if (ship.Pool != this)
                return false;

            ship.ClearFleet(returnToManagedPools: false);
            ForcePool.RemoveItemImmediate(ship);
            ship.Pool = null;
            return true;
        }

        public Array<Ship> GetShipsFromOffensePools(bool onlyAO = false)
        {
            var ships = new Array<Ship>();
            for (int i = 0; i < OwnerAI.AreasOfOperations.Count; i++)
            {
                AO ao = OwnerAI.AreasOfOperations[i];
                var newShips = ao.GetOffensiveForcePool();
                ships.AddRange(newShips);
            }

            if (!onlyAO)
                ships.AddRange(Ships);
            return ships;
        }

        /// <summary>
        /// Once all the logic errors are fixed in the ship pool tracking process this should be removed and turned into unit tests.
        /// the purpose of this method is to fix the errors where ships are incorrectly put into or not put into force pools for fleets and such. 
        /// </summary>
        void ErrorCheckPools()
        {
            if (Owner.isPlayer || Owner.isFaction) return;
            var allShips = Owner.OwnedShips;

            for (int i = 0; i < allShips.Count; i++)
            {
                var ship = allShips[i];
                if (ShouldNotAddToAnyPool(ship))
                {
                    continue;
                }

                switch (ship.AI.State)
                {
                    case AIState.Scrap:
                    case AIState.Resupply:
                    case AIState.Scuttle:
                    case AIState.Refit:
                        continue;
                }

                if (ForcePool.Contains(ship))
                {
                    if (ship.DesignRoleType == ShipData.RoleType.Warship && ship.DesignRole != ShipData.RoleName.carrier)
                        Log.Error("WarShip in wrong pool");
                    continue;
                }

                if (Owner.GetEmpireAI().DefensiveCoordinator.Contains(ship))
                    continue;

                if (ship.AI.State == AIState.SystemDefender)
                {
                    if (!OwnerAI.DefensiveCoordinator.DefensiveForcePool.Contains(ship))
                    {
                        ship.AI.ClearOrders();
                        Log.Warning("ShipPool: Ship was in a system defense state but not in system defense pool");
                        if (!AssignShipsToOtherPools(ship))
                        {
                            if (ShouldAddToEmpirePool(ship))
                                EmpireForcePoolAdd(ship);
                            Log.Error($"ShipPool: Could not assign ship to pools {ship}");
                        }
                    }
                }
                else if (ShouldAddToAOPools(ship) || ShouldAddToEmpirePool(ship))
                {
                    bool notInEmpireForcePool = !ForcePool.Contains(ship);
                    bool notInAOs = !OwnerAI.AreasOfOperations.Any(ao => ao.OffensiveForcePoolContains(ship));
                    if (ship.loyalty != Owner)
                    {
                        Log.Error($"WTF: {Owner} != {ship.loyalty}");
                        ship.RemoveFromPool();
                        if (!ship.loyalty.OwnedShips.ContainsRef(ship))
                            ship.LoyaltyChangeAtSpawn(ship.loyalty);
                    }
                    else if (notInAOs && notInEmpireForcePool && ship.BaseCanWarp &&
                             !ForcePool.Contains(ship) &&
                             ship.LoyaltyTracker.ChangeType == LoyaltyChanges.Type.None)
                    {
                        Log.Warning($"ShipPool: WarShip was not in any pools {ship}");
                        if (!AssignShipsToOtherPools(ship))
                        {
                            if (ShouldAddToEmpirePool(ship))
                                EmpireForcePoolAdd(ship);
                            else if (Owner.GetEmpireAI().AreasOfOperations.Count > 0)
                                Log.Info($"ShipPool: Could not assign ship to pools {ship}");
                        }
                    }
                }
            }
        }

        bool EmpireForcePoolAdd(Ship ship)
        {
            if (ship.Pool == this || Owner.isPlayer || Owner.isFaction || ShouldNotAddToAnyPool(ship))
                return false;

            ship.Pool?.Remove(ship);
            
            // first try to add to other pools
            if (!AssignShipsToOtherPools(ship))
            {
                if (ShouldAddToEmpirePool(ship))
                {
                    ship.Pool = this;
                    ForcePool.Add(ship);
                    return true;
                }

                if (ship.DesignRoleType == ShipData.RoleType.Warship && ship.BaseCanWarp)
                {
                    Log.Warning($"Could Not add ship to force pools. {ship} ");
                }
            }
            return false;
        }

        bool AssignShipsToOtherPools(Ship toAdd)
        {
            if (!ShouldAddToAOPools(toAdd) || ShouldNotAddToAnyPool(toAdd))
                return false; // we don't need this ship

            int numWars = Owner.AtWarCount;
            float baseDefensePct = 0.1f;
            baseDefensePct += 0.15f * numWars;

            if (baseDefensePct > 0.35f)
                baseDefensePct = 0.35f;

            if (OwnerAI != null)
            {
                // need to rework this better divide the ships.
                AO area = OwnerAI.AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Center));
                if (area?.Add(toAdd) == true)
                {
                    return true;
                }
                //bool needDef = (Owner.CurrentMilitaryStrength * baseDefensePct - OwnerAI.DefStr) >= 0
                //               && OwnerAI.DefensiveCoordinator.DefenseDeficit >= 0;
                //if (needDef && !Owner.isFaction)
                //{
                //    OwnerAI.DefensiveCoordinator.AddShip(toAdd);
                //    return true;
                //}
            }

            return false; // nothing to do with you
        }

        public void Clear()
        {
            ForcePool.Clear();
            Ships.Clear();
            EmpireReadyFleets?.Clear();
            ForcePool.Clear();
        }
    }
}
