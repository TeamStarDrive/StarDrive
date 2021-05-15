using System;
using System.Collections.Generic;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Empires.ShipPools
{
    public class ShipPool : IDisposable
    {
        readonly Empire Owner;
        Array<Ship> OwnedShips           = new Array<Ship>();
        Array<Ship> OwnedProjectors      = new Array<Ship>();
        Array<Ship> ShipsBackBuffer      = new Array<Ship>();
        Array<Ship> ProjectorsBackBuffer = new Array<Ship>();
        Array<Ship> ShipsToAddBackBuffer = new Array<Ship>();
        readonly object PoolLocker       = new object();

        EmpireAI OwnerAI                  => Owner.GetEmpireAI();
        
        
        /// <summary>
        /// This is for adding to the Empire AI pool management.
        /// Player and other ships that can't be added to empireAI pool management will be safely ignored. 
        /// </summary>
        public void AddToForcePoolNextFame(Ship s)
        {
            if (s.loyalty != Owner)
                Log.Error($"Incorrect loyalty. Ship {s.loyalty} != Empire {Owner}")

            if (!Owner.isPlayer && !Owner.isFaction && s.Active && !s.IsHomeDefense)
                ShipsToAddBackBuffer.Add(s);
        }

        public bool ForcePoolContains(Ship s) => ForcePool.ContainsRef(s);
        public bool Remove(Ship ship)         => ForcePool.RemoveRef(ship);
        public float InitialStrength          = 0;
        public int InitialReadyFleets         = 0;
        public float CurrentUseableStrength   = 0;
        public int CurrentUseableFleets       = 0;
        float PoolCheckTimer                  = 0;

        public IReadOnlyList<Ship> EmpireShips => OwnedShips;
        public IReadOnlyList<Ship> EmpireProjectors => OwnedProjectors;
        public Array<Ship> ForcePool { get; private set; } = new Array<Ship>();
        public FleetShips EmpireReadyFleets { get; private set; }

        public ShipPool(Empire empire)
        {
            Owner = empire;
        }

        public void Update()
        {
            lock (PoolLocker)
            {
                OwnedShips           = ShipsBackBuffer;
                OwnedProjectors      = ProjectorsBackBuffer;
                ShipsBackBuffer      = new Array<Ship>(OwnedShips);
                ProjectorsBackBuffer = new Array<Ship>(ProjectorsBackBuffer);
            }

            AddShipsToForcePoolFromShipsToAdd();

            if (!Owner.isPlayer)
            {
                if (PoolCheckTimer-- < 0)
                {
                    PoolCheckTimer = 60;
                    RemoveInvalidShipsFromForcePool();
                    ErrorCheckPools();
                }
            }
            var fleets             = new FleetShips(Owner, Owner.AllFleetReadyShips());
            EmpireReadyFleets      = fleets;
            CurrentUseableFleets   = InitialReadyFleets = EmpireReadyFleets.CountFleets(out float initialStrength);
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

        void ErrorCheckPools() 
        {
            if (Owner.isPlayer || Owner.isFaction) return;
            var allShips = OwnedShips;
            
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

                if (ShipsToAddBackBuffer.ContainsRef(ship))
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
                        ship.loyalty.AddShip(ship);
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

        void ForcePoolAdd(Ship ship)
        {
            if (Owner.isFaction || ship.IsHangarShip || ship.IsHomeDefense) 
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
                toAdd.loyalty.AddShip(toAdd);
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
            for (int i = 0; i < ShipsToAddBackBuffer.Count; i++)
            {
                Ship s = ShipsToAddBackBuffer[i];
                if (!Owner.isPlayer && !Owner.isFaction && s.Active && !s.IsHomeDefense && !s.IsHomeDefense) 
                    ForcePoolAdd(s);
            }

            ShipsToAddBackBuffer.Clear();
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
                    RemoveShipFromFleetAndPools(ship);
                }
            }
        }

        public void AddShipToEmpire(Ship s)
        {
            AddToForcePoolNextFame(s);

            bool alreadyAdded;
            if (s.IsSubspaceProjector)
            {
                lock (PoolLocker)
                    alreadyAdded = !ProjectorsBackBuffer.AddUniqueRef(s);
            }
            else
            {
                lock (PoolLocker)
                    alreadyAdded = !ShipsBackBuffer.AddUniqueRef(s);
            }

            if (alreadyAdded)
                Log.WarningWithCallStack(
                    "Empire.AddShip BUG: https://bitbucket.org/codegremlins/stardrive-blackbox/issues/147/doubled-projectors");
        }

        public void RemoveShipFromEmpire(Ship ship)
        {
            if (ship == null)
            {
                Log.Error($"Empire '{Owner.Name}' RemoveShip failed: ship was null");
                return;
            }

            if (ship.IsSubspaceProjector)
            {
                lock (PoolLocker)
                    ProjectorsBackBuffer.RemoveRef(ship);
            }
            else
            {
                lock (PoolLocker)
                    ShipsBackBuffer.RemoveRef(ship);
            }

            ship.AI.ClearOrders();
            RemoveShipFromFleetAndPools(ship);
        }
        
        public void CleanOut()
        {
            OwnedShips        = new Array<Ship>();
            OwnedProjectors   = new Array<Ship>();
            ShipsToAddBackBuffer        = new Array<Ship>();
            ForcePool         = new Array<Ship>();
            EmpireReadyFleets = new FleetShips(Owner);
        }
        
        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize((object)this);
        }

        protected virtual void Dispose(bool disposing)
        {
            ForcePool.ClearAndDispose();
            ShipsToAddBackBuffer.ClearAndDispose();
            OwnedProjectors.ClearAndDispose();
            OwnedShips.ClearAndDispose();
        }

    }
}