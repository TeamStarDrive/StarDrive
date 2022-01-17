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
        readonly ChangePendingList<Ship> ForcePool;

        public Guid Guid { get; } = Guid.NewGuid();
        public string Name { get; }
        public Empire OwnerEmpire => Owner;
        public EmpireAI OwnerAI => Owner.GetEmpireAI();
        public Array<Ship> Ships => ForcePool.Items;

        public float InitialStrength  { get; private set; }
        public int InitialReadyFleets { get; private set; }
        public int InitialReadyShips { get; private set; }
        public float CurrentUseableStrength { get; private set; }
        public int CurrentUseableFleets { get; set; }

        public FleetShips EmpireReadyFleets { get; private set; }

        public override string ToString()
        {
            return $"ShipPool {Guid} {Name} {Owner.Name} Ships={Ships.Count}";
        }

        public ShipPool(Empire empire, string name)
        {
            Owner = empire;
            Name = name;
            ForcePool = new ChangePendingList<Ship>();
        }

        /// <summary>
        /// This is for adding to the Empire AI pool management.
        /// Player and other ships that can't be added to empireAI pool management will be safely ignored.
        /// </summary>
        public bool Add(Ship s)
        {
            if (s.Pool == this || Owner.isPlayer || Owner.isFaction || s.Loyalty != Owner ||
                s.ShouldNotBeAddedToForcePools())
                return false;

            if (s.Loyalty != Owner && s.LoyaltyTracker.ChangeType == LoyaltyChanges.Type.None)
            {
                Log.Error($"Incorrect loyalty. Ship {s.Loyalty} != Empire {Owner}");
                return false;
            }

            s.Pool?.Remove(s);

            // first try to add to AO pools
            if (s.IsAWarShip)
            {
                // need to rework this better divide the ships.
                AO area = OwnerAI.AreasOfOperations.FindMin(ao => s.Position.SqDist(ao.Center));
                if (area?.Add(s) == true)
                    return true;
            }

            if (s.BaseCanWarp && s.IsFleetSupportShip())
            {
                s.Pool = this;
                ForcePool.Add(s);
                return true;
            }

            if (s.DesignRoleType == RoleType.Warship && s.BaseCanWarp)
            {
                Log.Warning($"Could Not add ship to force pools. AO Pools {OwnerAI.AreasOfOperations.Count} {s.DesignRole} {s}");
            }
            return false;
        }

        public bool Remove(Ship ship)
        {
            if (ship.Pool != this)
                return false;

            ship.ClearFleet(returnToManagedPools: false, clearOrders: false/*we don't have the authority to clear orders here*/);
            ForcePool.RemoveItemImmediate(ship);
            ship.Pool = null;
            return true;
        }
        
        public bool Contains(Ship s) => s.Pool == this;

        public void Update()
        {
            ForcePool.Update();

            EmpireReadyFleets = new FleetShips(Owner, Owner.AllFleetReadyShips());
            InitialReadyShips = EmpireReadyFleets.TotalShips;
            int fleets = EmpireReadyFleets.CountFleets(out float initialStrength);
            CurrentUseableFleets = InitialReadyFleets = fleets;
            CurrentUseableStrength = InitialStrength = initialStrength;
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

        public void Clear()
        {
            ForcePool.Clear();
            Ships.Clear();
            EmpireReadyFleets?.Clear();
            ForcePool.Clear();
        }
    }
}
