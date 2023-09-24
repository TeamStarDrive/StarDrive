using SDUtils;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Ships
{
    [StarDataType]
    public class MiningBays
    {
        Ship Owner;
        public ShipModule[] AllMiningBays { get; private set; }

        public MiningBays(Ship ship, ShipModule[] slots)
        {
            Owner = ship;
            AllMiningBays = slots.Filter(module => module.IsMiningBay);
        }

        public MiningBays() 
        {
        }

        public void Dispose()
        {
            Owner = null;
        }

        public void ProcessMiningBays(float rawResourcesStored)
        {
            if (Owner == null
                || rawResourcesStored / (Owner.CargoSpaceMax*0.5) > 0.5f
                || !HasOrdnanceToLaunch())
            {
                return;
            }

            foreach (ShipModule miningBay in AllMiningBays)
            {
                if (miningBay.Active 
                    && miningBay.HangarTimer <= 0
                    && !miningBay.TryGetHangarShipActive(out _))
                {
                    CreateMiningShip(miningBay, out Ship miningShip);
                    miningBay.HangarTimer = miningBay.HangarTimerConstant;
                    miningShip.AI.OrderMinePlanet(Owner.GetTether());
                    return;
                }
            }
        }

        bool HasOrdnanceToLaunch()
        {
            if (AllMiningBays.Length == 0)
                return false;

            ShipModule miningBay = AllMiningBays[0];
            miningBay.HangarShipUID = Owner.Loyalty.GetMiningShipName();
            Ship miningShipTemplate = ResourceManager.GetShipTemplate(miningBay.HangarShipUID);
            return miningShipTemplate.ShipOrdLaunchCost < Owner.Ordinance;
        }

        void CreateMiningShip(ShipModule hangar, out Ship miningShip)
        {
            miningShip = Ship.CreateShipFromHangar(Owner.Universe, hangar, Owner.Loyalty, Owner.Position, Owner);
            Owner.ChangeOrdnance(-miningShip.ShipOrdLaunchCost);
            hangar.SetHangarShip(miningShip);
        }
    }
}
