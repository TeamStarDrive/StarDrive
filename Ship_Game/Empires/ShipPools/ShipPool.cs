using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Empires.ShipPools
{
    public class ShipPool : IDisposable
    {
        readonly Empire Owner;
        readonly Array<Ship> ForcePool        = new Array<Ship>();
        EmpireAI OwnerAI                      => Owner.GetEmpireAI();
        readonly Array<Ship> ShipsToAdd       = new Array<Ship>();
        public void AddShipNextFame(Ship s)   => ShipsToAdd.Add(s);
        public bool ForcePoolContains(Ship s) => ForcePool.ContainsRef(s);
        public void ClearForcePools()         => ForcePool.Clear();
        public bool Remove(Ship ship)         => ForcePool.RemoveRef(ship);

        public ShipPool(Empire empire)
        {
            Owner = empire;
        }

        public void UpdatePools()
        {
            RemoveInvalidShipsFromForcePool();
            AddShipsToForcePoolFromShipsToAdd();
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

            OwnerAI.DefensiveCoordinator.Remove(ship);
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
            Owner.Pool.RemoveShipFromFleetAndPools(ship);

            if (!AssignShipsToOtherPools(ship))
            {
                if (ship.DesignRole != ShipData.RoleName.scout && 
                    (ship.DesignRole == ShipData.RoleName.troopShip ||
                ShipData.ShipRoleToRoleType(ship.DesignRole) == ShipData.RoleType.WwarSupport)
                    )

                {
                    if (!ForcePool.AddUniqueRef(ship))
                        Log.Warning($"Attempted to add an existing ship to Empire forcePool. ShipRole: {ship}");
                }
            }
        }

        bool AssignShipsToOtherPools(Ship toAdd)
        {
            int numWars = Owner.AtWarCount;

            float baseDefensePct = 0.1f;
            baseDefensePct += 0.15f * numWars;
            if (toAdd.DesignRole < ShipData.RoleName.fighter ||
                toAdd.BaseStrength <= 0f || toAdd.WarpThrust <= 0f || !toAdd.BaseCanWarp)
            {
                return false; // we don't need this ship
            }

            if (baseDefensePct > 0.35f)
                baseDefensePct = 0.35f;

            bool needDef = (Owner.CurrentMilitaryStrength * baseDefensePct - OwnerAI.DefStr) > 0
                        && OwnerAI.DefensiveCoordinator.DefenseDeficit > 0;
            if (needDef)
            {
                OwnerAI.DefensiveCoordinator.AddShip(toAdd);
                return true;
            }

            // need to rework this better divide the ships.
            AO area = OwnerAI.AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Center));
            if (area?.AddShip(toAdd) == true)
                return true;

            return false; // nothing to do with you
        }

        void AddShipsToForcePoolFromShipsToAdd()
        {
            for (int i = 0; i < ShipsToAdd.Count; i++)
            {
                Ship s = ShipsToAdd[i];
                Owner.AddShip(s);
                if (!Owner.isPlayer) ForcePoolAdd(s);
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
                if (!ship.Active)
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