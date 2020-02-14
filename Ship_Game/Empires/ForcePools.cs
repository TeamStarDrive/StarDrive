using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Empires
{
    public class ShipForcePools : IDisposable
    {
        readonly Array<Ship> ForcePool = new Array<Ship>();
        readonly Empire Owner;
        EmpireAI OwnerAI => Owner.GetEmpireAI();

        readonly Array<Ship> ShipsToAdd = new Array<Ship>();

        public ShipForcePools(Empire empire)
        {
            Owner = empire;
        }

        public void ClearForcePools() => ForcePool.Clear();

        public bool Remove(Ship ship) => ForcePool.RemoveRef(ship);
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
            if (ship.shipData.Role > ShipData.RoleName.freighter &&
                ship.shipData.ShipCategory != ShipData.Category.Civilian)
            {
                Owner.RemoveShipFromFleetAndPools(ship);

                if (!AssignShipToForce(ship))
                {
                    ForcePool.Add(ship);
                }
            }
        }
        public bool ForcePoolContains(Ship s) => ForcePool.ContainsRef(s);

        void AddShipsToForcePoolFromShipsToAdd()
        {
            for (int i = 0; i < Owner.ShipsToAdd.Count; i++)
            {
                Ship s = Owner.ShipsToAdd[i];
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
                    Owner.RemoveShipFromFleetAndPools(ship);
                }
            }
        }

        public bool AssignShipToForce(Ship toAdd)
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

        public void UpdatePools()
        {
            RemoveInvalidShipsFromForcePool();
            AddShipsToForcePoolFromShipsToAdd();
        }

    }
}