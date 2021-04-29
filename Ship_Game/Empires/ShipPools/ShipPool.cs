using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Empires.ShipPools
{
    public class ShipPool : IDisposable
    {
        readonly Empire Owner;
        public readonly Array<Ship> ForcePool        = new Array<Ship>();
        EmpireAI OwnerAI                      => Owner.GetEmpireAI();
        readonly Array<Ship> ShipsToAdd       = new Array<Ship>();
        public void AddShipNextFame(Ship s)   => ShipsToAdd.Add(s);
        public bool ForcePoolContains(Ship s) => ForcePool.ContainsRef(s);
        public void ClearForcePools()         => ForcePool.Clear();
        public bool Remove(Ship ship)         => ForcePool.RemoveRef(ship);
        public float InitialStrength = 0;
        public int InitialReadyFleets =0;
        public float CurrentUseableStrength = 0;
        public int CurrentUseableFleets = 0;
        float PoolCheckTimer = 0;

        public FleetShips EmpireReadyFleets { get; private set; }
        public ShipPool(Empire empire)
        {
            Owner = empire;
        }

        public void UpdatePools()
        {
            if (!Owner.isPlayer)
            {
                AddShipsToForcePoolFromShipsToAdd();
            
                if (PoolCheckTimer-- < 0)
                {
                    PoolCheckTimer = 60;
                    RemoveInvalidShipsFromForcePool();
                    ErrorCheckPools();
                }
            }
            var fleets = new FleetShips(Owner, Owner.AllFleetReadyShips());
            EmpireReadyFleets = fleets;
            CurrentUseableFleets = InitialReadyFleets = EmpireReadyFleets.CountFleets(out float initialStrength);
            CurrentUseableStrength = InitialStrength = initialStrength;
        }

        public void RemoveShipFromFleetAndPools(Ship ship)
        {
            ship.ClearFleet();
            Remove(ship);

            RemoveFromOtherPools(ship);
        }

        void RemoveFromOtherPools(Ship ship, AO ao = null)
        {
            if (ao == null)
                foreach (AO aos in OwnerAI.AreasOfOperations)
                    aos.RemoveShip(ship);
            else
                ao.RemoveShip(ship);

            OwnerAI.DefensiveCoordinator.Remove(ship, false);
        }

        public Array<Ship> GetShipsFromOffensePools(bool onlyAO = false)
        {
            var ships = new Array<Ship>();
            for (int i = 0; i < OwnerAI.AreasOfOperations.Count; i++)
            {
                AO ao = OwnerAI.AreasOfOperations[i];
                ships.AddRange(ao.GetOffensiveForcePool());
            }

            if (!onlyAO)
                ships.AddRange(ForcePool);
            return ships;
        }

        private void ErrorCheckPools() // TODO - this is so expensive, it goes all over the ships and throws tons of logs, i disabled the logs for now
        {
            if (Owner.isPlayer || Owner.isFaction) return;
            var allShips = Owner.GetShips();
            // error check. there is a hole in the ship pools causing 
            for (int i = 0; i < allShips.Count; i++)
            {
                var ship = allShips[i];
                if (!ship.Active 
                    || ship.fleet != null 
                    || ship.IsHangarShip 
                    || ship.IsHomeDefense
                    || ship.AI.HasPriorityOrder)
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

                if (ShipsToAdd.ContainsRef(ship))
                    continue;

                if (ForcePoolContains(ship))
                {
                    if (ship.DesignRoleType == ShipData.RoleType.Warship && ship.DesignRole != ShipData.RoleName.carrier)
                        Log.Warning("WarShip in wrong pool");
                    continue;
                }

                if (Owner.GetEmpireAI().DefensiveCoordinator.Contains(ship))
                    continue;

                if (ship.AI.State == AIState.SystemDefender)
                {
                    if (!OwnerAI.DefensiveCoordinator.DefensiveForcePool.Contains(ship))
                    {
                        ship.AI.SystemToDefend = null;
                        ship.AI.SystemToDefendGuid = Guid.Empty;
                        ship.AI.ClearOrders();
                        Log.Warning("ShipPool: Ship was in a system defense state but not in system defense pool");
                        if (!AssignShipsToOtherPools(ship))
                        {
                            if (ship.DesignRole < ShipData.RoleName.fighter)
                                ForcePoolAdd(ship);
                            Log.Info($"ShipPool: Could not assign ship to pools {ship}");
                        }
                    }
                }
                else if (ship.DesignRoleType == ShipData.RoleType.Warship)
                {
                    bool notInForcePool = !ForcePool.ContainsRef(ship);
                    bool notInAOs = !OwnerAI.AreasOfOperations.Any(ao => ao.OffensiveForcePoolContains(ship));
                    if (ship.loyalty != Owner)
                    {
                        Log.Warning($"WTF: {Owner} != {ship.loyalty}");
                        RemoveFromOtherPools(ship);
                        Owner.RemoveShip(ship);
                        ship.loyalty.Pool.AddShipNextFame(ship);
                    }
                    else if (notInAOs && notInForcePool && ship.BaseCanWarp)
                    {
                        Log.Info("ShipPool: WarShip was not in any pools");
                        if (!AssignShipsToOtherPools(ship))
                        {
                            if (ship.DesignRole < ShipData.RoleName.fighter)
                                ForcePoolAdd(ship);
                            Log.Info($"ShipPool: Could not assign ship to pools {ship}");
                        }
                    }
                }
            }
        }

        public void ForcePoolAdd(Array<Ship> ships)
        {
            for (int i = 0; i < ships.Count; i++)
                ForcePoolAdd(ships[i]);
        }

        public void ForcePoolAdd(Ship[] ships)
        {
            for (int i = 0; i < ships.Length - 1; i++)
                ForcePoolAdd(ships[i]);
        }

        public void ForcePoolAdd(Ship ship)
        {
            if (Owner.isFaction || ship.IsHangarShip || ship.IsHomeDefense) 
                return;

            Owner.Pool.RemoveShipFromFleetAndPools(ship);
            if (ship.loyalty != Owner)
            {
                Log.Warning("wrong loyalty added to force pool");
                ship.loyalty.Pool.AddShipNextFame(ship);
                return;
            }
            if (!AssignShipsToOtherPools(ship))
            {
                if (ship.DesignRoleType    == ShipData.RoleType.Troop 
                    || ship.DesignRoleType == ShipData.RoleType.WarSupport
                    || ship.DesignRole     == ShipData.RoleName.carrier)
                {
                    if (!ForcePool.AddUniqueRef(ship))
                        Log.Warning($"Attempted to add an existing ship to Empire forcePool. ShipRole: {ship}");
                }
                else if(ship.DesignRoleType == ShipData.RoleType.Warship && ship.BaseCanWarp) 
                {
                    Log.Warning($"Could Not add ship to force pools. {ship} ");
                }
            }
        }

        bool AssignShipsToOtherPools(Ship toAdd)
        {
            int numWars = Owner.AtWarCount;
            if (toAdd.loyalty != Owner)
            {
                Log.Warning("wrong loyalty added to force pool");
                RemoveFromOtherPools(toAdd);
                Remove(toAdd);
                toAdd.loyalty.Pool.AddShipNextFame(toAdd);
                return true;
            }
            float baseDefensePct = 0.1f;
            baseDefensePct      += 0.15f * numWars;

            if (toAdd.DesignRole < ShipData.RoleName.fighter 
                || !toAdd.Active
                || toAdd.BaseStrength <= 0f 
                || !toAdd.BaseCanWarp 
                || toAdd.IsHangarShip 
                || toAdd.IsHomeDefense)
            {
                return false; // we don't need this ship
            }

            if (baseDefensePct > 0.35f)
                baseDefensePct = 0.35f;

            bool needDef = (Owner.CurrentMilitaryStrength * baseDefensePct - OwnerAI.DefStr) > 0
                        && OwnerAI.DefensiveCoordinator.DefenseDeficit > 0;
            if (needDef && !Owner.isFaction)
            {
                OwnerAI.DefensiveCoordinator.AddShip(toAdd);
                return true;
            }

            // need to rework this better divide the ships.
            AO area = OwnerAI.AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Center));
            if (area?.AddShip(toAdd) != false)
                return true;

            return false; // nothing to do with you
        }

        void AddShipsToForcePoolFromShipsToAdd()
        {
            for (int i = 0; i < ShipsToAdd.Count; i++)
            {
                Ship s = ShipsToAdd[i];
                Owner.AddShip(s);
                if (!Owner.isPlayer && !Owner.isFaction && s.Active && !s.IsHomeDefense && !s.IsHomeDefense) 
                    ForcePoolAdd(s);
            }

            ShipsToAdd.Clear();
        }

        void RemoveInvalidShipsFromForcePool()
        {
            if (Owner.isPlayer && ForcePool.Count > 0)
                Log.Warning($"Player ForcePool should be empty!: {ForcePool.Count}");

            for (int i = ForcePool.Count - 1; i >= 0; --i)
            {
                Ship ship = ForcePool[i];
                if (!ship.Active || ship.loyalty != Owner)
                {
                    Owner.Pool.RemoveShipFromFleetAndPools(ship);
                }
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ForcePool.ClearAndDispose();
            ShipsToAdd.ClearAndDispose();
        }

    }
}