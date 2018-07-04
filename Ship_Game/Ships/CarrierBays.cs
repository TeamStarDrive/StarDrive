using System;
using System.Linq;

namespace Ship_Game.Ships
{
    public class CarrierBays  // Created by Fat Bastard in to better deal with hangars
    {
        public ShipModule[] AllHangars { get; private set; }
        public ShipModule[] AllTroopBays { get; }
        public ShipModule[] AllSupplyBays { get; }
        public ShipModule[] AllFighterHangars { get; }
        public ShipModule[] AllTransporters { get; }
        public bool HasHangars;
        public bool HasSupplyBays;
        public bool HasFighterBays;
        public bool HasTroopBays;
        public bool HasTransporters;
        public bool HasOrdnanceTransporters;
        public bool HasAssaultTransporters;

        private CarrierBays(ShipModule[] slots) // this is a constructor, initialize everything in here
        {
            int hangarsCount = slots.Count(module => module.Is(ShipModuleType.Hangar));
            AllHangars       = new ShipModule[hangarsCount];
            int i            = 0;
            foreach (ShipModule module in slots)
            {
                if (module.Is(ShipModuleType.Hangar))
                {
                    AllHangars[i] = module;
                    ++i;
                }
            }
            AllTroopBays      = AllHangars.FilterBy(module => module.IsTroopBay);
            AllSupplyBays     = AllHangars.FilterBy(module => module.IsSupplyBay);
            AllTransporters   = AllHangars.FilterBy(module => module.TransporterOrdnance > 0 || module.TransporterTroopAssault > 0);
            AllFighterHangars = AllHangars.FilterBy(module => !module.IsTroopBay 
                                                              && !module.IsSupplyBay 
                                                              && module.ModuleType != ShipModuleType.Transporter);
            HasHangars        = AllHangars.Any();
            HasSupplyBays     = AllSupplyBays.Any();
            HasFighterBays    = AllFighterHangars.Any();
            HasTroopBays      = AllTroopBays.Any();
            HasTransporters   = AllTransporters.Any();
            HasAssaultTransporters = AllTransporters.Count(transporter => transporter.TransporterTroopAssault > 0) > 0;
            HasOrdnanceTransporters = AllTransporters.Count(transporter => transporter.TransporterOrdnance > 0) > 0;

        }

        public static CarrierBays None { get; } = new CarrierBays(Empty<ShipModule>.Array) // Returns NIL object
        {
            AllHangars = Empty<ShipModule>.Array,
        };

        public static CarrierBays Create(ShipModule[] slots)
        {
            return slots.Any(m => m.ModuleType == ShipModuleType.Hangar) ? new CarrierBays(slots) : None;
        }


        public ShipModule[] AllActiveHangars   => AllHangars.FilterBy(module => module.Active);

        public bool HasActiveHangars           => AllActiveHangars.Any(); // FB: this changes dynamically

        public ShipModule[] AllActiveTroopBays => AllTroopBays.FilterBy(module => module.Active);

        public int AvailableAssaultShuttles
        {
            get
            {
                return AllTroopBays.Count(hangar => hangar.Active && hangar.hangarTimer <= 0 && hangar.GetHangarShip() == null);
            }
        }

        public int LaunchedAssaultShuttles
        {
            get
            {
                int i = 0;
                foreach (ShipModule hangar in AllTroopBays)
                {
                    Ship hangarship = hangar.GetHangarShip();
                    if (hangarship != null && hangarship.Active)
                        i += 1;
                }
                return i;
            }
        }

        public HangarInfo GrossHangarStatus // FB: needed to display hangar status to the player
        {
            get
            {
                var info = new HangarInfo();
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

        public void ScrambleFighters(Ship ship)
        {
            if (ship.engineState == Ship.MoveState.Warp || ship.isSpooling)
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
        public void ScrambleAssaultShips(Ship ship, float strengthNeeded)
        {
            if (ship.TroopList.Count <= 0)
                return;

            bool flag = strengthNeeded > 0;

            foreach (ShipModule hangar in AllActiveTroopBays)
            {
                if (hangar.hangarTimer <= 0 && ship.TroopList.Count > 0)
                {
                    if (flag && strengthNeeded < 0)
                        break;
                    strengthNeeded -= ship.TroopList[0].Strength;
                    hangar.LaunchBoardingParty(ship.TroopList[0]);
                    ship.TroopList.RemoveAt(0);

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

        public bool NeedResupplyTroops(Ship ship)
        {
            int i = LaunchedAssaultShuttles;
            i += AllTransporters.Sum(sm => sm.TransporterTroopLanding); 
            return (float)(ship.TroopList.Count + i) / ship.TroopCapacity < 0.5f;
        }

        public int ReadyPlanetAssaulttTroops(Ship ship)
        {
            if (ship.TroopList.IsEmpty)
                return 0;

            int assaultSpots = AllActiveHangars.Count(sm => sm.hangarTimer > 0 && sm.IsTroopBay);
            assaultSpots += AllTransporters.Sum(sm => sm.TransporterTimer > 0 ? 0 : sm.TransporterTroopLanding);
            assaultSpots += ship.shipData.Role == ShipData.RoleName.troop ? 1 : 0;
            return Math.Min(ship.TroopList.Count, assaultSpots);
        }

        public float PlanetAssaultStrength(Ship ship) 
        {
            if (ship.TroopList.IsEmpty)
                return 0.0f;

            int assaultSpots = ship.DesignRole == ShipData.RoleName.troop 
                               || ship.DesignRole == ShipData.RoleName.troopShip 
                                    ? ship.TroopList.Count : 0;

            assaultSpots += AllActiveHangars.FilterBy(sm => sm.IsTroopBay).Length;  // FB: inspect this
            assaultSpots += AllTransporters.Sum(sm => sm.TransporterTroopLanding);

            int troops = Math.Min(ship.TroopList.Count, assaultSpots);
            return ship.TroopList.SubRange(0, troops).Sum(troop => troop.Strength);

        }
        public int PlanetAssaultCount(Ship ship) // move to carrier bays)
        {
            try
            {
                int assaultSpots = 0;
                if (ship.shipData.Role == ShipData.RoleName.troop)
                {
                    assaultSpots += ship.TroopList.Count;

                }
                if (HasTroopBays)
                    for (int index = 0; index < AllActiveHangars.Length; index++)  // FB: move to for each
                    {
                        ShipModule sm = AllActiveHangars[index];
                        if (sm.IsTroopBay)
                            assaultSpots++;
                    }
                if (HasAssaultTransporters)
                    for (int index = 0; index < AllTransporters.Length; index++)
                    {
                        ShipModule at = AllTransporters[index];
                        assaultSpots += at.TransporterTroopLanding;
                    }

                if (assaultSpots > 0)
                {
                    int temp = assaultSpots - ship.TroopList.Count;
                    assaultSpots -= temp < 0 ? 0 : temp;
                }
                return assaultSpots;
            }
            catch
            { }
            return 0;
        }

        public bool RecallingFighters(Ship ship) 
        {
            if (!ship.RecallFightersBeforeFTL || AllActiveHangars.Length <= 0)
                return false;

            bool recallFighters               = false;
            float jumpDistance                = ship.Center.Distance(ship.AI.MovePosition);
            float slowestFighter              = ship.Speed * 2;
            bool fightersLaunchedBeforeRecall = ship.FightersLaunched; // FB: remember the original state

            if (jumpDistance > 7500f)
            {
                recallFighters = true;
                foreach (ShipModule hangar in AllActiveHangars)
                {
                    Ship hangarShip = hangar.GetHangarShip();
                    if (hangar.IsSupplyBay || hangarShip == null)
                    {
                        recallFighters = false;
                        continue;
                    }
                    if (hangarShip.Speed < slowestFighter) slowestFighter = hangarShip.Speed;

                    float rangeTocarrier = hangarShip.Center.Distance(ship.Center);
                    if (hangarShip.EMPdisabled
                        || !hangarShip.hasCommand
                        || hangarShip.dying
                        || hangarShip.EnginesKnockedOut
                        || rangeTocarrier > ship.SensorRange
                        || rangeTocarrier > 25000f && hangarShip.WarpThrust < 1f) // scuttle non warp capable ships if they are too far
                    {
                        recallFighters = false;
                        // FB: this will scuttle hanger ships if they cant reach the mothership
                        if (hangarShip.ScuttleTimer <= 0f) hangarShip.ScuttleTimer = 10f;
                        continue;
                    }
                    ship.FightersLaunched = false;  // FB: if fighters out button is on, turn it off to allow recover till jump starts
                    recallFighters = true;
                    break;
                }
            }
            if (!recallFighters)
            {
                ship.FightersLaunched = fightersLaunchedBeforeRecall;
                return false;
            }
            RecoverAssaultShips();
            RecoverFighters();
            if (ship.DoneRecovering())
                return false;
            if (ship.Speed * 2 > slowestFighter)
                ship.Speed = slowestFighter * .25f;
            return true;
        }
    }
}
