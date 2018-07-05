using System;
using System.Linq;

namespace Ship_Game.Ships
{
    public class CarrierBays  // Created by Fat Bastard in to better deal with hangars
    {
        public ShipModule[] AllHangars { get; }
        public ShipModule[] AllTroopBays { get; }
        public ShipModule[] AllSupplyBays { get; }
        public ShipModule[] AllFighterHangars { get; }
        public ShipModule[] AllTransporters { get; }
        private readonly Ship Owner;
        public readonly bool HasHangars;
        public readonly bool HasSupplyBays;
        public readonly bool HasFighterBays;
        public readonly bool HasTroopBays;
        public readonly bool HasTransporters;
        public readonly bool HasOrdnanceTransporters;
        public readonly bool HasAssaultTransporters;
        private bool RecallingShipsBeforeWarp;

        private CarrierBays(ShipModule[] slots) // this is a constructor, initialize everything in here
        {
            AllHangars        = slots.FilterBy(module => module.Is(ShipModuleType.Hangar));
            AllTroopBays      = AllHangars.FilterBy(module => module.IsTroopBay);
            AllSupplyBays     = AllHangars.FilterBy(module => module.IsSupplyBay);
            AllTransporters   = AllHangars.FilterBy(module => module.TransporterOrdnance > 0 || module.TransporterTroopAssault > 0);
            AllFighterHangars = AllHangars.FilterBy(module => !module.IsTroopBay 
                                                              && !module.IsSupplyBay 
                                                              && module.ModuleType != ShipModuleType.Transporter);
            HasHangars              = AllHangars.Length > 0;
            HasSupplyBays           = AllSupplyBays.Length > 0;
            HasFighterBays          = AllFighterHangars.Length > 0;
            HasTroopBays            = AllTroopBays.Length > 0;
            HasTransporters         = AllTransporters.Length > 0;
            HasAssaultTransporters  = AllTransporters.Any(transporter => transporter.TransporterTroopAssault > 0);
            HasOrdnanceTransporters = AllTransporters.Any(transporter => transporter.TransporterOrdnance > 0);
            Owner                   = AllHangars.Length > 0 ? AllHangars[0].GetParent() : null;
        }

        public static CarrierBays None { get; } = new CarrierBays(Empty<ShipModule>.Array); // Returns NIL object

        public static CarrierBays Create(ShipModule[] slots)
        {
            return slots.Any(m => m.ModuleType == ShipModuleType.Hangar) ? new CarrierBays(slots) : None;
        }

        public ShipModule[] AllActiveHangars   => AllHangars.FilterBy(module => module.Active);

        public bool HasActiveHangars           => AllHangars.Any(module => module.Active); // FB: this changes dynamically

        public ShipModule[] AllActiveTroopBays => AllTroopBays.FilterBy(module => module.Active);

        public int NumActiveHangars => AllHangars.Count(hangar => hangar.Active);

        // this will return the number of assault shuttles ready to launch (regardless of troopcount)
        public int AvailableAssaultShuttles => AllTroopBays.Count(hangar => hangar.Active && hangar.hangarTimer <= 0 && hangar.GetHangarShip() == null);

        // this will return the number of assault shuttles in space
        public int LaunchedAssaultShuttles =>  AllTroopBays.Count(hangar => hangar.GetHangarShip()?.Active == true);

        public HangarInfo GrossHangarStatus // FB: needed to display hangar status to the player
        {
            get
            {
                HangarInfo info = new HangarInfo();
                foreach (ShipModule hangar in AllFighterHangars)
                {
                    if (hangar.FighterOut) ++info.Launched;
                    else if (hangar.hangarTimer > 0) ++info.Refitting;
                    else if (hangar.Active) ++info.ReadyToLaunch;
                }
                return info;
            }
        }

        public struct HangarInfo
        {
            public int Launched;
            public int Refitting;
            public int ReadyToLaunch;
        }

        public void ScrambleFighters()
        {
            if (Owner == null || Owner.engineState == Ship.MoveState.Warp || Owner.isSpooling || RecallingShipsBeforeWarp)
                return;

            for (int i = 0; i < AllActiveHangars.Length; ++i)
                AllActiveHangars[i].ScrambleFighters();
        }

        public void RecoverFighters()
        {
            foreach (ShipModule hangar in AllFighterHangars)
            {
                Ship hangarShip = hangar.GetHangarShip();
                if (hangarShip == null || !hangarShip.Active)
                    continue;

                hangarShip.AI.OrderReturnToHangar();
            }
        }

        public void ScuttleNonWarpHangarShips() // FB: get rid of no warp capable hangar ships to prevent them from crawling around
        {
            foreach (ShipModule hangar in AllFighterHangars)
            {
                Ship hangarShip = hangar.GetHangarShip();
                if (hangarShip != null && hangarShip.WarpThrust < 1f)
                    hangarShip.ScuttleTimer = 60f; // 60 seconds so surviving fighters will be able to continue combat for a while
            }
        }

        public void ScrambleAllAssaultShips() => ScrambleAssaultShips(0);

        public void ScrambleAssaultShips(float strengthNeeded)

        {
            if (Owner == null || Owner.TroopList.Count <= 0)
                return;

            if (Owner.engineState == Ship.MoveState.Warp || Owner.isSpooling || RecallingShipsBeforeWarp)
                return;

            bool limitAssaultSize = strengthNeeded > 0; // if Strendthneeded is 0,  this will be false and the ship will launch all troops

            foreach (ShipModule hangar in AllActiveTroopBays)
            {
                if (hangar.hangarTimer <= 0 && Owner.TroopList.Count > 0)
                {
                    if (limitAssaultSize && strengthNeeded < 0)
                        break;
                    strengthNeeded -= Owner.TroopList[0].Strength;
                    hangar.LaunchBoardingParty(Owner.TroopList[0]);
                    Owner.TroopList.RemoveAt(0);
                }
            }
        }

        public void RecoverAssaultShips()
        {
            foreach (ShipModule hangar in AllTroopBays)
            {
                Ship hangarship = hangar.GetHangarShip();
                if (hangarship == null || !hangarship.Active)
                    continue;

                if (hangarship.TroopList.Count != 0)
                    hangarship.AI.OrderReturnToHangar();
            }
        }

        public bool NeedResupplyTroops
        {
            get
            {
                if (Owner == null)
                    return false;

                int troopsNotInTroopListCount = LaunchedAssaultShuttles;
                troopsNotInTroopListCount += AllTransporters.Sum(sm => sm.TransporterTroopLanding);
                float troopsPresentRatio = (float)(Owner.TroopList.Count + troopsNotInTroopListCount) / Owner.TroopCapacity;
                return troopsPresentRatio < 0.5f;
            }
        }

        public int ReadyPlanetAssaulttTroops
        {
            get
            {
                if (Owner == null || Owner.TroopList.IsEmpty)
                    return 0;

                int assaultSpots = AllActiveHangars.Count(sm => sm.hangarTimer > 0 && sm.IsTroopBay);
                assaultSpots    += AllTransporters.Sum(sm => sm.TransporterTimer > 0 ? 0 : sm.TransporterTroopLanding);
                assaultSpots    += Owner.shipData.Role == ShipData.RoleName.troop ? 1 : 0;
                return Math.Min(Owner.TroopList.Count, assaultSpots);
            }
        }

        public float PlanetAssaultStrength 
        {
            get
            {
                if (Owner == null || Owner.TroopList.IsEmpty)
                    return 0.0f;

                int assaultSpots = Owner.DesignRole == ShipData.RoleName.troop
                                   || Owner.DesignRole == ShipData.RoleName.troopShip ? Owner.TroopList.Count : 0;

                assaultSpots += AllActiveHangars.FilterBy(sm => sm.IsTroopBay).Length;  // FB: inspect this
                assaultSpots += AllTransporters.Sum(sm => sm.TransporterTroopLanding);

                int troops = Math.Min(Owner.TroopList.Count, assaultSpots);
                return Owner.TroopList.SubRange(0, troops).Sum(troop => troop.Strength);
            }
        }

        public int PlanetAssaultCount
        {
            get
            {
                if (Owner == null)
                    return 0;
                int assaultSpots = 0;
                if (Owner.shipData.Role == ShipData.RoleName.troop)
                {
                    assaultSpots += Owner.TroopList.Count;
                }
                assaultSpots += AllActiveHangars.Count(sm => sm.IsTroopBay && sm.Active);
                assaultSpots += AllTransporters.Sum(at => at.TransporterTroopLanding); 

                if (assaultSpots > 0)
                {
                    int temp = assaultSpots - Owner.TroopList.Count;
                    assaultSpots -= temp < 0 ? 0 : temp;
                }
                return assaultSpots;
            }
        }

        public bool RecallingFighters() 
        {
            if (Owner == null || !Owner.RecallFightersBeforeFTL || !HasActiveHangars)
                return false;

            bool recallFighters               = false;
            float jumpDistance                = Owner.Center.Distance(Owner.AI.MovePosition);
            float slowestFighterSpeed         = Owner.Speed * 2;

            RecallingShipsBeforeWarp          = true; 
            if (jumpDistance > 7500f)
            {
                recallFighters = true;
                foreach (ShipModule hangar in AllActiveHangars.FilterBy(hangar => !hangar.IsSupplyBay))
                {
                    Ship hangarShip = hangar.GetHangarShip();
                    if (hangarShip == null)
                    {
                        recallFighters = false;
                        continue;
                    }
                    slowestFighterSpeed = Math.Min(slowestFighterSpeed, hangarShip.Speed);

                    float rangeTocarrier = hangarShip.Center.Distance(Owner.Center);
                    if (hangarShip.EMPdisabled
                        || !hangarShip.hasCommand
                        || hangarShip.dying
                        || hangarShip.EnginesKnockedOut
                        || rangeTocarrier > Owner.SensorRange
                        || rangeTocarrier > 25000f && hangarShip.WarpThrust < 1f) // scuttle non warp capable ships if they are too far
                    {
                        recallFighters = false;
                        if (hangarShip.ScuttleTimer <= 0f) hangarShip.ScuttleTimer = 10f; // FB: this will scuttle hanger ships if they cant reach the mothership
                        continue;
                    }
                    recallFighters = true;
                    break;
                }
            }
            if (!recallFighters)
            {
                RecallingShipsBeforeWarp = false;
                return false;
            }
            RecoverAssaultShips();
            RecoverFighters();
            if (DoneRecovering)
            {
                RecallingShipsBeforeWarp = false;
                return false;
            }
            if (Owner.Speed * 2 > slowestFighterSpeed)
                Owner.Speed = slowestFighterSpeed * .25f;
            return true;
        }

        public bool DoneRecovering
        {
            get
            {
                return AllHangars.FilterBy(hangar => hangar.Active)
                       .Select(hangar => hangar.GetHangarShip())
                       .All(hangarShip => hangarShip != null && !hangarShip.Active);
            }
        }
    }
}
