using System;
using System.Linq;
using Ship_Game.AI;

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
        public readonly bool HasOrdnanceTransporters;
        public readonly bool HasAssaultTransporters;
        private bool RecallingShipsBeforeWarp;

        private CarrierBays(Ship owner, ShipModule[] slots)
        {
            AllHangars        = slots.Filter(module => module.Is(ShipModuleType.Hangar));
            AllTroopBays      = AllHangars.Filter(module => module.IsTroopBay);
            AllSupplyBays     = AllHangars.Filter(module => module.IsSupplyBay);
            AllTransporters   = AllHangars.Filter(module => module.TransporterOrdnance > 0 || module.TransporterTroopAssault > 0);
            AllFighterHangars = AllHangars.Filter(module => !module.IsTroopBay 
                                                              && !module.IsSupplyBay 
                                                              && module.ModuleType != ShipModuleType.Transporter);
            HasHangars              = AllHangars.Length > 0;
            HasSupplyBays           = AllSupplyBays.Length > 0;
            HasFighterBays          = AllFighterHangars.Length > 0;
            HasTroopBays            = AllTroopBays.Length > 0;
            HasAssaultTransporters  = AllTransporters.Any(transporter => transporter.TransporterTroopAssault > 0);
            HasOrdnanceTransporters = AllTransporters.Any(transporter => transporter.TransporterOrdnance > 0);
            Owner                   = owner;
        }

        private static readonly CarrierBays None = new CarrierBays(null, Empty<ShipModule>.Array); // NIL object pattern

        public static CarrierBays Create(Ship owner, ShipModule[] slots)
        {
            ShipData.RoleName role = owner.shipData.Role;
            if (slots.Any(m => m.ModuleType == ShipModuleType.Hangar
                            || m.ModuleType == ShipModuleType.Transporter)
                || role == ShipData.RoleName.troop)
                return new CarrierBays(owner, slots);
            return None;
        }

        public ShipModule[] AllActiveHangars   => AllHangars.Filter(module => module.Active);
        public bool HasActiveHangars           => AllHangars.Any(module => module.Active); // FB: this changes dynamically
        public bool HasTransporters            => AllTransporters.Length > 0;
        public bool CanInvadeOrBoard           => HasTroopBays || HasAssaultTransporters || Owner.DesignRole == ShipData.RoleName.troop;
        public ShipModule[] AllActiveTroopBays => AllTroopBays.Filter(module => module.Active);
        public int NumActiveHangars            => AllHangars.Count(hangar => hangar.Active);

        // this will return the number of assault shuttles ready to launch (regardless of troopcount)
        public int AvailableAssaultShuttles => AllTroopBays.Count(hangar => hangar.Active && hangar.hangarTimer <= 0 && hangar.GetHangarShip() == null);

        // this will return the number of assault shuttles in space
        public int LaunchedAssaultShuttles =>  AllTroopBays.Count(hangar => hangar.GetHangarShip()?.Active == true);

        public float OrdnanceInSpace  // returns potential Ordnance in space for carriers (as fighters cost ordnance to launch)
        {
            get
            {
                if (Owner == null)
                    return 0;

                float ordnance = 0;
                for (int i = 0; i < AllFighterHangars.Count(); ++i)
                {
                    ShipModule hangar = AllHangars[i];
                    if (hangar.FighterOut)
                        ordnance += hangar.GetHangarShip().ShipOrdLaunchCost;

                }
                return ordnance;
            }
        }


        public int NumTroopsInShipAndInSpace
        {
            get
            {
                if (Owner == null || !CanInvadeOrBoard)
                    return 0;

                return Owner.TroopList.Count + LaunchedAssaultShuttles;
            }
        }

        public float MaxTroopStrengthInShipToCommit
        {
            get
            {
                if (Owner == null || Owner.TroopList.Count <= 0)
                    return 0f;

                float troopStrength = 0f;
                int maxTroopsForLaunch = Math.Min(Owner.TroopList.Count, AvailableAssaultShuttles);
                for (int i = 0; i < maxTroopsForLaunch; ++i)
                {
                    troopStrength += Owner.TroopList[i].Strength;    
                }
                return troopStrength;
            }
        }

        public float MaxTroopStrengthInSpaceToCommit
        {
            get
            {
                if (Owner == null)
                    return 0f;

                //try to fix sentry bug : https://sentry.io/blackboxmod/blackbox/issues/628038060/
                float troopStrength = AllTroopBays.Sum(hangar =>
                {
                    var ship = hangar.GetHangarShip();
                    if (ship?.Active != true || ship.TroopList.IsEmpty) return 0;
                    return ship.TroopList[0].Strength;
                });
                
                return troopStrength;
            }
        }

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

            PrepShipHangars(Owner.loyalty);
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

            bool limitAssaultSize = strengthNeeded > 0; // if Strength needed is 0,  this will be false and the ship will launch all troops

            foreach (ShipModule hangar in AllActiveTroopBays)
            {
                if (hangar.hangarTimer <= 0 && Owner.TroopList.Count > 0)
                {
                    if (limitAssaultSize && strengthNeeded < 0)
                        break;

                    if (hangar.LaunchBoardingParty(Owner.TroopList[0]))
                    {
                        strengthNeeded -= Owner.TroopList[0].Strength;
                        Owner.TroopList.RemoveAt(0);
                    }
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

        public void RecoverSupplyShips()
        {
            foreach (ShipModule hangar in AllSupplyBays)
            {
                Ship hangarship = hangar.GetHangarShip();
                if (hangarship == null || !hangarship.Active)
                    continue;

                hangarship.AI.OrderReturnToHangar();
            }
        }

        public float TroopsMissingVsTroopCapacity
        {
            get
            {
                if (Owner == null)
                    return 1f;

                int troopsNotInTroopListCount = LaunchedAssaultShuttles;
                troopsNotInTroopListCount += AllTransporters.Sum(sm => sm.TransporterTroopLanding);
                float troopsPresentRatio = (float)(Owner.TroopList.Count + troopsNotInTroopListCount) / Owner.TroopCapacity;
                return troopsPresentRatio;
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

        public bool AnyPlanetAssaultAvailable
        {
            get
            {
                if (Owner == null || Owner.TroopList.IsEmpty)
                    return false;

                if (Owner.DesignRole == ShipData.RoleName.troop)
                    return true;

                return AllActiveHangars.Any(sm => sm.IsTroopBay) 
                       || AllTransporters.Any(sm => sm.TransporterTroopAssault >0);
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

                assaultSpots += AllActiveHangars.Filter(sm => sm.IsTroopBay).Length;  // FB: inspect this
                assaultSpots += AllTransporters.Sum(sm => sm.TransporterTroopLanding);

                int troops = Math.Min(Owner.TroopList.Count, assaultSpots);
                return Owner.TroopList.SubRange(0, troops).Sum(troop => troop.Strength);
            }
        }

        public int PlanetAssaultCount
        {
        get
            {
                if (Owner == null || !CanInvadeOrBoard)
                    return 0;

                int assaultSpots = NumTroopsInShipAndInSpace;
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

        public void AssaultPlanet(Planet planet)
        {
            ScrambleAllAssaultShips();
            foreach (ShipModule bay in AllTroopBays)
            {
                Ship hangarShip = bay.GetHangarShip();
                if (hangarShip != null && hangarShip.Active)
                    hangarShip.AI.OrderAssaultPlanet(planet);
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
                foreach (ShipModule hangar in AllActiveHangars)
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
                        || rangeTocarrier > 25000f )
                    {
                        recallFighters = false;
                        if (hangarShip.ScuttleTimer <= 0f && hangarShip.WarpThrust < 1f)
                            hangarShip.ScuttleTimer = 10f; // FB: this will scuttle hanger ships if they cant reach the mothership
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
            RecoverSupplyShips();
            RecoverFighters();
            if (DoneRecovering)
            {
                RecallingShipsBeforeWarp = false;
                return false;
            }
            if (Owner.Speed * 2 > slowestFighterSpeed)
                Owner.Speed = slowestFighterSpeed * 0.25f;
            return true;
        }

        public bool DoneRecovering
        {
            get
            {
                return AllHangars.Filter(hangar => hangar.Active)
                       .Select(hangar => hangar.GetHangarShip())
                       .All(hangarShip => hangarShip != null && !hangarShip.Active);
            }
        }

        public bool RebaseAssaultShip(Ship assaultShip)
        {
            if (Owner == null)
                return false;

            ShipModule hangar = AllTroopBays.Find(hangarSpot => hangarSpot.GetHangarShip() == null);
            if (hangar == null)
                return false;

            hangar.ResetHangarShipWithReturnToHangar(assaultShip);
            return true;
        }
        
        private Array<Troop> LandTroops(Planet at, int maxTroopsToLand)
        {
            var landedTroops = new Array<Troop>();
            foreach (Troop troop in Owner.TroopList)
            {
                if (maxTroopsToLand <= 0)
                    break;
                if (troop == null || troop.Loyalty != Owner.loyalty)
                    continue;
                if (troop.AssignTroopToTile(at))
                {
                    landedTroops.Add(troop);
                    --maxTroopsToLand;
                }
                else break;
            }
            return landedTroops;
        }

        private int TroopLandLimit
        {
            get
            {
                int landLimit = AllActiveTroopBays.Count(hangar => hangar.hangarTimer <= 0);
                foreach (ShipModule module in AllTransporters.Where(module => module.TransporterTimer <= 1f))
                    landLimit += module.TransporterTroopLanding;
                return landLimit;
            }
        }

        public void AssaultPlanetWithTransporters(Planet planet)
        {
            if (Owner == null)
                return;

            Array<Troop> landed = LandTroops(planet, TroopLandLimit);
            if (landed.Count <= 0)
                return;

            foreach (Troop to in landed)  //FB: not sure what this flag is for. Should be tested with STSA
            {
                bool flag = false; // = false;                        
                foreach (ShipModule module in AllActiveTroopBays)
                    if (module.hangarTimer < module.hangarTimerConstant)
                    {
                        module.hangarTimer = module.hangarTimerConstant;
                        flag = true;
                        break;
                    }
                if (!flag)
                    foreach (ShipModule module in AllTransporters)
                        if (module.TransporterTimer < module.TransporterTimerConstant)
                        {
                            module.TransporterTimer = module.TransporterTimerConstant;
                            break;
                        }
                Owner.TroopList.Remove(to);
            }
        }

        public void PrepShipHangars(Empire empire) // This will set dynamic hangar UIDs to the best ships
        {
            if (empire.Id == -1) // ID -1 since there is a loyalty without a race when saving s ship
                return;

            string defaultShip = empire.data.StartingShip;
            foreach (ShipModule hangar in AllFighterHangars.Filter(hangar => hangar.Active
                                                                               && hangar.hangarTimer <= 0
                                                                               && hangar.GetHangarShip() == null))
            {
                if (hangar.DynamicHangar == DynamicHangarOptions.Static)
                {
                    if (empire.ShipsWeCanBuild.Contains(hangar.hangarShipUID))
                        continue; // FB: we can build the ship, this hangar is sorted out
                }
                // If the ship we want cant be built, will try to launch the best we have by proceeding this method as if the hangar is dynamic
                string selectedShip = GetDynamicShipName(hangar, empire);
                hangar.hangarShipUID = selectedShip ?? defaultShip;
                if (Empire.Universe?.Debug == true)
                    Log.Info($"Chosen ship for Hangar launch: {hangar.hangarShipUID}");
            }
        }

        private ShipData.HangarOptions GetCategoryFromHangarType(DynamicHangarOptions hangarType)
        {
            switch (hangarType)
            {
                case DynamicHangarOptions.DynamicInterceptor:
                    return ShipData.HangarOptions.Interceptor;
                case DynamicHangarOptions.DynamicAntiShip:
                    return ShipData.HangarOptions.AntiShip;
                default:
                    return ShipData.HangarOptions.General;
            }
        }

        private string GetDynamicShipName(ShipModule hangar, Empire empire)
        {
            ShipData.HangarOptions desiredShipCategory = GetCategoryFromHangarType(hangar.DynamicHangar);
            string selectedShip = string.Empty;
            foreach (var role in hangar.HangarRoles)
            {
                selectedShip = ShipBuilder.PickFromCandidates(role, empire, maxSize: hangar.MaximumHangarShipSize,
                    designation: desiredShipCategory);

                // If no desired category is available in the empire, try to get the best ship we have regardless of category for this role
                if (selectedShip.IsEmpty() && hangar.DynamicHangar != DynamicHangarOptions.DynamicLaunch)
                    selectedShip = ShipBuilder.PickFromCandidates(role, empire, maxSize: hangar.MaximumHangarShipSize);
                if (selectedShip.NotEmpty()) break;
            }
            return selectedShip;
        }

        public bool AssaultTargetShip(Ship targetShip)
        {
            if (CanInvadeOrBoard && targetShip != null)
            {
                float totalTroopStrengthToCommit = MaxTroopStrengthInShipToCommit + MaxTroopStrengthInSpaceToCommit;
                float enemyStrength = targetShip.BoardingDefenseTotal * 1.5f; // FB: assume the worst, ensure boarding success!

                if (totalTroopStrengthToCommit > enemyStrength && (Owner.loyalty.isFaction || targetShip.GetStrength() > 0f))
                {
                    if (MaxTroopStrengthInSpaceToCommit < enemyStrength && targetShip.Center.InRadius(Owner.Center, Owner.maxWeaponsRange))
                        ScrambleAssaultShips(enemyStrength); // This will launch salvos of assault shuttles if possible

                    for (int i = 0; i < AllTroopBays.Length; i++)
                    {
                        ShipModule hangar = AllTroopBays[i];
                        if (hangar.GetHangarShip() == null)
                            continue;
                        hangar.GetHangarShip().AI.OrderTroopToBoardShip(targetShip);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
