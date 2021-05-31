using Ship_Game.AI;
using Ship_Game.Ships;
using Ship_Game.Utils;
using System;

namespace Ship_Game.Empires.ShipPools
{
    public class ShipPool : IDisposable
    {
        readonly Empire Owner;
        ChangePendingList<Ship> ForcePool;

        object ChangeLocker = new object();

        EmpireAI OwnerAI => Owner.GetEmpireAI();
        
        /// <summary>
        /// This is for adding to the Empire AI pool management.
        /// Player and other ships that can't be added to empireAI pool management will be safely ignored. 
        /// </summary>
        public void AddToEmpireForcePoolNextFame(Ship s)
        {
            if (s.loyalty != Owner)
                Log.Error($"Incorrect loyalty. Ship {s.loyalty} != Empire {Owner}");

            if (!Owner.isPlayer && !Owner.isFaction && s.Active && !s.IsHomeDefense)
                EmpireForcePoolAdd(s);
        }

        public bool EmpireForcePoolContains(Ship s) => EmpireForcePool.ContainsRef(s);
        public bool Remove(Ship ship)         => EmpireForcePool.RemoveRef(ship);
        public float InitialStrength          = 0;
        public int InitialReadyFleets         = 0;
        public float CurrentUseableStrength   = 0;
        public int CurrentUseableFleets       = 0;
        float PoolCheckTimer                  = 0;

        public Array<Ship> EmpireForcePool { get; private set; } = new Array<Ship>();
        public FleetShips EmpireReadyFleets { get; private set; }

        public ShipPool(Empire empire)
        {
            Owner = empire;
            ForcePool = new ChangePendingList<Ship>(s => s.loyalty == Owner &&
                                                                    !s.loyalty.isPlayer &&
                                                                    !s.loyalty.isFaction &&
                                                                     s.Active &&
                                                                    !s.IsHomeDefense);
        }

        public void Update()
        {
            lock (ChangeLocker)
                ForcePool.Update();

            if (!Owner.isPlayer)
            {
                if (PoolCheckTimer-- < 0)
                {
                    PoolCheckTimer = 60;
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

            ForcePool.RemoveItemImmediate(ship);

            RemoveFromOtherPools(ship);
        }

        void RemoveFromOtherPools(Ship ship, AO ao = null)
        {
            if (OwnerAI == null)
                return;

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
                var newShips = ao.GetOffensiveForcePool();
                ships.AddRange(newShips);
            }

            if (!onlyAO)
                ships.AddRange(EmpireForcePool);
            return ships;
        }

        void ErrorCheckPools() 
        {
            if (Owner.isPlayer || Owner.isFaction) return;
            var allShips = Owner.OwnedShips;
            
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

                if (ForcePool.Contains(ship))
                    continue;

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
                        ship.AI.SystemToDefend = null;
                        ship.AI.SystemToDefendGuid = Guid.Empty;
                        ship.AI.ClearOrders();
                        Log.Warning("ShipPool: Ship was in a system defense state but not in system defense pool");
                        if (!AssignShipsToOtherPools(ship))
                        {
                            if (ship.DesignRole < ShipData.RoleName.fighter)
                                EmpireForcePoolAdd(ship);
                            Log.Error($"ShipPool: Could not assign ship to pools {ship}");
                        }
                    }
                }
                else if (ship.DesignRoleType == ShipData.RoleType.Warship)
                {
                    bool notInEmpireForcePool = !ForcePool.Contains(ship);
                    bool notInAOs = !OwnerAI.AreasOfOperations.Any(ao => ao.OffensiveForcePoolContains(ship));
                    if (ship.loyalty != Owner)
                    {
                        Log.Error($"WTF: {Owner} != {ship.loyalty}");
                        RemoveFromOtherPools(ship);
                        Owner.RemoveShipFromAIPools(ship);
                        if (!ship.loyalty.OwnedShips.ContainsRef(ship))
                            ship.LoyaltyTracker.SetLoyaltyForNewShip(ship.loyalty);
                    }
                    else if (notInAOs && notInEmpireForcePool && ship.BaseCanWarp)
                    {
                        Log.Warning($"ShipPool: WarShip was not in any pools {ship}");
                        if (!AssignShipsToOtherPools(ship))
                        {
                            if (ship.DesignRole < ShipData.RoleName.fighter)
                                EmpireForcePoolAdd(ship);
                            Log.Info($"ShipPool: Could not assign ship to pools {ship}");
                        }
                    }
                }
            }
        }

        void EmpireForcePoolAdd(Ship ship)
        {
            if (Owner.isPlayer || Owner.isFaction || ship.IsHangarShip || ship.IsHomeDefense || !ship.Active || ship.fleet != null) 
                return;

            RemoveShipFromFleetAndPools(ship);
            if (ship.loyalty != Owner)
            {
                Log.Error("wrong loyalty added to force pool");
                ship.loyalty.AddShipToManagedPools(ship);
                return;
            }
            if (!AssignShipsToOtherPools(ship))
            {
                if (ship.DesignRoleType    == ShipData.RoleType.Troop 
                    || ship.DesignRoleType == ShipData.RoleType.WarSupport
                    || ship.DesignRole     == ShipData.RoleName.carrier)
                {
                    if (!ForcePool.AddItemPending(ship))
                        Log.Warning($"Attempted to add an existing ship to Empire EmpireForcePool. ShipRole: {ship}");
                }
                else if(ship.DesignRoleType == ShipData.RoleType.Warship && ship.BaseCanWarp) 
                {
                    Log.Error($"Could Not add ship to force pools. {ship} ");
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
                ImmediateRemoveShipFromEmpire(toAdd);
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
            if (OwnerAI != null)
            {
                bool needDef = (Owner.CurrentMilitaryStrength * baseDefensePct - OwnerAI.DefStr) >= 0
                               && OwnerAI.DefensiveCoordinator.DefenseDeficit >= 0;
                if (needDef && !Owner.isFaction)
                {
                    OwnerAI.DefensiveCoordinator.AddShip(toAdd);
                    return true;
                }

                // need to rework this better divide the ships.
                AO area = OwnerAI.AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Center));
                if (area?.AddShip(toAdd) == true)
                {
                    return true;
                }
            }

            return false; // nothing to do with you
        }
        
        /// <summary>
        /// This is not thread safe. run this on empire thread for safe adds. 
        /// </summary>
        public void Add(Ship s)
        {
            EmpireForcePoolAdd(s);
        }

        public bool ImmediateRemoveShipFromEmpire(Ship ship)
        {
            if(RemoveShipFromEmpire(ship))
            {
                Update();
                return true;
            }
            return false;
        }

        /// <summary>
        /// This is not thread safe. run this on empire thread for safe removals. 
        /// </summary>
        public bool RemoveShipFromEmpire(Ship ship)
        {
            lock (ChangeLocker)
                RemoveShipFromFleetAndPools(ship);
            bool removed = false;
            if (ship == null)
            {
                Log.Error($"Empire '{Owner.Name}' RemoveShip failed: ship was null");
                return false;
            }

            ship.AI?.ClearOrders();

            return removed;
        }
        
        public void Clear()
        {
            ForcePool.ClearOut();
            EmpireForcePool      = new Array<Ship>();
            EmpireReadyFleets    = new FleetShips(Owner);
        }
        
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            EmpireForcePool.ClearAndDispose();
            ForcePool.ClearAndDispose();
        }
    }
}