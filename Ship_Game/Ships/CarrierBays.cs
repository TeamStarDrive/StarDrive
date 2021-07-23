﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.AI.CombatTactics;
using Ship_Game.Audio;

namespace Ship_Game.Ships
{
    public class CarrierBays  // Created by Fat Bastard in to better deal with hangars
    {
        public ShipModule[] AllHangars { get; private set; }
        public ShipModule[] AllTroopBays { get; private set; }
        public ShipModule[] AllSupplyBays { get; private set; }
        public ShipModule[] AllFighterHangars { get; private set; }
        public ShipModule[] AllTransporters { get; private set; }
        Ship Owner;
        public readonly bool HasHangars;
        public readonly bool HasSupplyBays;
        public readonly bool HasFighterBays;
        public readonly bool HasTroopBays;
        public readonly bool HasActiveTroopBays;
        public readonly bool HasOrdnanceTransporters;
        public readonly bool HasAssaultTransporters;
        public bool FightersLaunched { get; private set; }
        public bool TroopsLaunched { get; private set; }
        public bool SendTroopsToShip { get; private set; }
        public bool RecallFightersBeforeFTL { get; private set; }
        public bool RecallingShipsBeforeWarp { get; private set; }
        public static float DefaultHangarRange = 7500;
        public SupplyShuttles SupplyShuttle;
        public float HangarRange => HasActiveHangars ? DefaultHangarRange : 0;
        public bool IsPrimaryCarrierRoleForLaunchRange => 
                                            HasActiveHangars &&
                                            (Owner.WeaponsMaxRange.AlmostZero()
                                            || Owner.DesignRole == ShipData.RoleName.carrier
                                            || Owner.DesignRole == ShipData.RoleName.support
                                            || Owner.DesignRoleType == ShipData.RoleType.Orbital);

        AssaultShipCombat TroopTactics;

        public const int RecallMoveDistance = 25000;
        
        public Array<Ship> GetActiveFighters()
        {
            var fighters = new Array<Ship>();
            foreach (var hangar in AllFighterHangars)
            {
                if (hangar.TryGetHangarShipActive(out Ship fighter))
                    fighters.Add(fighter);
            }
            return fighters;
        }

        CarrierBays(Ship owner, ShipModule[] slots)
        {
            AllHangars        = slots.Filter(module => module.Is(ShipModuleType.Hangar));
            AllTroopBays      = AllHangars.Filter(module => module.IsTroopBay);
            AllSupplyBays     = AllHangars.Filter(module => module.IsSupplyBay);
            AllTransporters   = AllHangars.Filter(module => module.TransporterOrdnance > 0 || module.TransporterTroopAssault > 0);
            AllFighterHangars = AllHangars.Filter(module => module.IsFighterHangar);

            HasHangars              = AllHangars.Length > 0;
            HasSupplyBays           = AllSupplyBays.Length > 0;
            HasFighterBays          = AllFighterHangars.Length > 0;
            HasActiveTroopBays      = AllActiveTroopBays.Length > 0;
            HasTroopBays            = AllTroopBays.Length > 0;
            HasAssaultTransporters  = AllTransporters.Any(transporter => transporter.TransporterTroopAssault > 0);
            HasOrdnanceTransporters = AllTransporters.Any(transporter => transporter.TransporterOrdnance > 0);
            SendTroopsToShip        = true;
            RecallFightersBeforeFTL = true;
            Owner                   = owner;
            SupplyShuttle           = new SupplyShuttles(Owner);
            TroopTactics            = new AssaultShipCombat(owner);
        }

        static readonly CarrierBays None = new CarrierBays(null, Empty<ShipModule>.Array); // NIL object pattern

        public static CarrierBays Create(Ship owner, ShipModule[] slots)
        {
            ShipData.RoleName role = owner.shipData.Role;
            if (slots.Any(m => m.ModuleType == ShipModuleType.Hangar
                            || m.ModuleType == ShipModuleType.Transporter)
                            || role == ShipData.RoleName.troop)
            {
                return new CarrierBays(owner, slots);
            }

            return None;
        }

        public void Dispose()
        {
            if (Owner == null)
                return;
            Owner = null;

            foreach (ShipModule hangar in AllHangars) // FB: use here all hangars and not just active hangars
            {
                if (hangar.TryGetHangarShip(out Ship hangarShip))
                    hangarShip.Mothership = null; // Todo - Setting this to null might be risky
            }

            AllHangars        = Array.Empty<ShipModule>();
            AllTroopBays      = Array.Empty<ShipModule>();
            AllSupplyBays     = Array.Empty<ShipModule>();
            AllFighterHangars = Array.Empty<ShipModule>();
            AllTransporters   = Array.Empty<ShipModule>();
            SupplyShuttle?.Dispose();
            SupplyShuttle = null;
            TroopTactics  = null;
        }

        // aggressive dispose looks to cause a crash here. 
        public ShipModule[] AllActiveHangars   => AllHangars?.Filter(module => module.Active);
        public bool HasActiveHangars           => AllHangars.Any(module => module.Active); // FB: this changes dynamically
        public bool HasTransporters            => AllTransporters.Length > 0;
        public ShipModule[] AllActiveTroopBays => AllTroopBays.Filter(module => module.Active);
        public int NumActiveHangars            => AllHangars.Count(hangar => hangar.Active);

        public float EstimateFightersDps
        {
            get
            {
                if (Owner == null)
                    return 0;

                float dps = 0;
                for (int i = 0; i < AllFighterHangars.Length; i++)
                {
                    ShipModule hangar = AllFighterHangars[i];
                    if (hangar.TryGetHangarShip(out Ship fighter))
                        dps += fighter.TotalDps;
                    else
                        dps += hangar.MaximumHangarShipSize * 10;
                }

                return dps;
            }
        }

        // this will return the number of assault shuttles ready to launch (regardless of troopcount)
        public int AvailableAssaultShuttles => 
            AllTroopBays.Count(hangar => hangar.Active && hangar.hangarTimer <= 0 && !hangar.IsHangarShipActive);

        // this will return the number of assault shuttles in space
        public int LaunchedAssaultShuttles => AllTroopBays.Count(hangar => hangar.IsHangarShipActive);

        /// <summary>
        /// Are any of the supply shuttles launched
        /// </summary>
        public bool HasSupplyShuttlesInSpace => AllSupplyBays.Any(hangar => hangar.IsHangarShipActive);
        public ShipModule[] SupplyHangarsAlive => AllSupplyBays.Filter(hangar => hangar.Active);

        public void InitFromSave(SavedGame.ShipSaveData save)
        {
            if (Owner == null)
                return;

            FightersLaunched = save.FightersLaunched;
            TroopsLaunched   = save.TroopsLaunched;
            SendTroopsToShip = save.SendTroopsToShip;
            SetRecallFightersBeforeFTL(save.RecallFightersBeforeFTL);
        }
        
        public int NumTroopsInShipAndInSpace
        {
            get
            {
                if (Owner == null)
                    return 0;

                return Owner.TroopCount + LaunchedAssaultShuttles;
            }
        }

        public float MaxTroopStrengthInShipToCommit
        {
            get
            {
                if (Owner == null || !Owner.HasOurTroops)
                    return 0f;

                int maxTroopsForLaunch = Math.Min(Owner.TroopCount, AvailableAssaultShuttles);
                return Owner.GetOurTroopStrength(maxTroopsForLaunch);
            }
        }

        public float MaxTroopStrengthInSpaceToCommit
        {
            get
            {
                if (Owner == null || AllTroopBays.Length == 0)
                    return 0f;

                //try to fix sentry bug : https://sentry.io/blackboxmod/blackbox/issues/628038060/
                float troopStrength = AllTroopBays.Sum(hangar =>
                {
                    if (hangar.TryGetHangarShip(out Ship ship) && ship.Active && ship.GetOurFirstTroop(out Troop first))
                        return first.Strength;

                    return 0;
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
            if (Owner == null || Owner.IsSpoolingOrInWarp || RecallingShipsBeforeWarp)
                return;

            ShipModule[] readyHangars = PrepShipHangars(Owner.loyalty);
            for (int i = 0; i < readyHangars.Length; ++i)
                readyHangars[i].ScrambleFighters();
        }

        public void RecoverFighters()
        {
            foreach (ShipModule hangar in AllFighterHangars)
            {
                if (hangar.TryGetHangarShipActive(out Ship hangarShip) && hangarShip.AI.State != AIState.ReturnToHangar)
                {
                    hangarShip.AI.OrderReturnToHangarDeferred();
                }
            }
        }

        public void ScuttleHangarShips() // FB: todo: assign hangar ships a a new carrier, if able
        {
            foreach (ShipModule hangar in AllFighterHangars)
            {
                if (hangar.TryGetHangarShipActive(out Ship hangarShip))
                    hangarShip.ScuttleTimer = 60f; // 60 seconds so surviving fighters will be able to continue combat for a while
            }
        }

        public void TryAssaultShipCombat() => TroopTactics.TryBoardShip();

        public void ScrambleAllAssaultShips() => ScrambleAssaultShips(0);

        public bool ScrambleAssaultShips(float strengthNeeded)
        {
            if (Owner == null || !Owner.HasOurTroops)
                return false;

            if (Owner.IsSpoolingOrInWarp || RecallingShipsBeforeWarp)
                return false;

            bool limitAssaultSize = strengthNeeded > 0; // if Strength needed is 0,  this will be false and the ship will launch all troops
            bool sentAssault      = false;
            foreach (ShipModule hangar in AllActiveTroopBays)
            {
                if (hangar.hangarTimer <= 0 && Owner.HasOurTroops)
                {
                    if (limitAssaultSize && strengthNeeded < 0)
                        break;

                    if (Owner.GetOurFirstTroop(out Troop troop) &&
                        hangar.LaunchBoardingParty(troop, out _))
                    {
                        sentAssault = true;
                        strengthNeeded -= troop.Strength;
                    }
                }
            }

            return sentAssault;
        }

        public void ResetAllHangarTimers()
        {
            if (Owner == null)
                return;

            foreach (ShipModule m in AllHangars)
                m.ResetHangarTimer();
        }

        public bool TryScrambleSingleAssaultShuttle(Troop troop, out Ship assaultShuttle)
        {
            assaultShuttle = null;
            foreach (ShipModule hangar in AllActiveTroopBays)
            {
                if (hangar.hangarTimer <= 0 && Owner.HasOurTroops)
                {
                    hangar.LaunchBoardingParty(troop, out assaultShuttle);
                    break;
                }
            }

            return assaultShuttle != null;
        }

        public void RecoverAssaultShips()
        {
            foreach (ShipModule hangar in AllTroopBays)
            {
                if (hangar.TryGetHangarShipActive(out Ship hangarShip) && hangarShip.AI.State != AIState.ReturnToHangar)
                    hangarShip.AI.OrderReturnToHangarDeferred();
            }
        }

        public void RecoverSupplyShips()
        {
            foreach (ShipModule hangar in AllSupplyBays)
            {
                if (hangar.TryGetHangarShipActive(out Ship hangarShip) && hangarShip.AI.State != AIState.ReturnToHangar)
                    hangarShip.AI.OrderReturnToHangarDeferred();
            }
        }

        public float TroopsMissingVsTroopCapacity
        {
            get
            {
                if (Owner == null || Owner.TroopCapacity == 0)
                    return 1f;

                return (float)(Owner.TroopCapacity - MissingTroops) / Owner.TroopCapacity;
            }
        }

        public int MissingTroops
        {
            get
            {
                if (Owner == null)
                    return 0;

                int troopsNotInTroopListCount = LaunchedAssaultShuttles;
                troopsNotInTroopListCount    += AllTransporters.Sum(sm => sm.TransporterTroopLanding);

                return Owner.TroopCapacity - (Owner.TroopCount + troopsNotInTroopListCount);
            }
        }

        public bool AnyAssaultOpsAvailable
        {
            get
            {
                if (Owner == null || !Owner.HasTroopsPresentOrLaunched)
                    return false;

                if (Owner.IsDefaultTroopTransport)
                    return true;

                return AllActiveHangars.Any(sm => sm.IsTroopBay)
                    || AllTransporters.Any(sm => sm.TransporterTroopAssault > 0);
            }
        }

        public float PlanetAssaultStrength
        {
            get
            {
                if (Owner == null || !Owner.HasOurTroops)
                    return 0.0f;

                int assaultSpots = Owner.DesignRole == ShipData.RoleName.troop
                                || Owner.DesignRole == ShipData.RoleName.troopShip ? Owner.TroopCount : 0;

                assaultSpots += AllActiveHangars.Filter(sm => sm.IsTroopBay).Length;  // FB: inspect this
                assaultSpots += AllTransporters.Sum(sm => sm.TransporterTroopLanding);

                int troops = Math.Min(Owner.TroopCount, assaultSpots);
                return Owner.GetOurTroopStrength(troops);
            }
        }

        public int PlanetAssaultCount
        {
            get
            {
                if (Owner == null)
                    return 0;

                int assaultSpots = NumTroopsInShipAndInSpace;
                assaultSpots += AllActiveHangars.Count(sm => sm.IsTroopBay && sm.Active);
                assaultSpots += AllTransporters.Sum(at => at.TransporterTroopLanding);

                if (assaultSpots > 0)
                {
                    int temp = assaultSpots - Owner.TroopCount;
                    assaultSpots -= temp < 0 ? 0 : temp;
                }
                return assaultSpots;
            }
        }

        public void AssaultPlanet(Planet planet)
        {
            if (HasAssaultTransporters)
                AssaultPlanetWithTransporters(planet);

            ScrambleAllAssaultShips();
            foreach (ShipModule bay in AllTroopBays)
            {
                if (bay.TryGetHangarShipActive(out Ship hangarShip)
                    && !hangarShip.AI.HasPriorityOrder
                    && hangarShip.HasOurTroops)
                {
                    hangarShip.AI.OrderLandAllTroops(planet);
                    hangarShip.Rotation = Owner.Position.DirectionToTarget(planet.Center).ToRadians();
                }
            }
        }

        /// <summary>
        /// Returns fighter recall state.
        /// recalls fighters if needed.
        /// Currently there is a bug that will prevent a spooling check from working correctly
        /// <see href="https://sd-blackbox.atlassian.net/browse/SB-117?atlOrigin=eyJpIjoiNTNiMjMyMzQ3MzgyNGMzYWJjZGI4YWU5ZTk1YmEwMmMiLCJwIjoiaiJ9"/>
        /// </summary>
        public bool RecallingFighters()
        {
            if (Owner == null)
                return false; // not a carrier

            if (ShouldRecallFighters())
            {
                RecoverAssaultShips();
                RecoverSupplyShips();
                RecoverFighters();

                if (DoneRecovering)
                {
                    RecallingShipsBeforeWarp = false;
                    return false;
                }
                
                RecallingShipsBeforeWarp = true;
                return true;
            }
            
            RecallingShipsBeforeWarp = false;
            return false;
        }

        bool ShouldRecallFighters()
        {
            if (!RecallFightersBeforeFTL || !HasActiveHangars || Owner.IsInWarp)
                return false;

            Vector2 moveTo = Owner.AI.OrderQueue.PeekFirst?.MovePosition ?? Vector2.Zero;
            if (moveTo == Vector2.Zero)
                return false;

            float jumpDistance = Owner.Position.Distance(moveTo);

            if (jumpDistance > RecallMoveDistance) // allows the carrier to jump small distances and then recall fighters
            {
                bool recallFighters = true;
                float slowestFighterSpeed = Ship.LightSpeedConstant;

                foreach (ShipModule hangar in AllActiveHangars)
                {
                    if (!hangar.TryGetHangarShip(out Ship hangarShip) 
                        || hangar.IsSupplyBay && hangarShip.AI.State == AIState.Resupply)
                    {
                        recallFighters = false;
                        continue;
                    }

                    slowestFighterSpeed = hangarShip.SpeedLimit.UpperBound(slowestFighterSpeed);
                    float rangeToCarrier = hangarShip.Position.Distance(Owner.Position);
                    if (hangarShip.EMPdisabled
                        || !hangarShip.hasCommand
                        || hangarShip.dying
                        || hangarShip.EnginesKnockedOut
                        || rangeToCarrier > Owner.SensorRange)
                    {
                        recallFighters = false;
                        if (hangarShip.DesignRole == ShipData.RoleName.drone && hangarShip.ScuttleTimer <= 0f && hangarShip.Stats.WarpThrust < 1f)
                            hangarShip.ScuttleTimer = 10f; // FB: this will scuttle hanger ships if they cant reach the mothership
                        continue;
                    }

                    recallFighters = true;
                    break;
                }

                if (recallFighters)
                {
                    Owner.SetSpeedLimit((slowestFighterSpeed * 0.25f).UpperBound(Owner.SpeedLimit));
                    return true;
                }
            }
            
            return false;
        }

        public bool DoneRecovering
        {
            get
            {
                return !AllHangars.Any(h => h.TryGetHangarShipActive(out _));
            }
        }

        public bool RebaseAssaultShip(Ship assaultShip)
        {
            if (Owner == null)
                return false;

            ShipModule hangar = AllTroopBays.Find(bay => !bay.TryGetHangarShip(out Ship ship) 
                                                        || ship.TroopCount == 0);

            if (hangar == null)
                return false;

            hangar.ResetHangarShipWithReturnToHangar(assaultShip);
            return true;
        }

        int TroopLandLimit
        {
            get
            {
                int landLimit = AllActiveTroopBays.Count(hangar => hangar.hangarTimer <= 0);
                foreach (ShipModule module in AllTransporters.Where(module => module.TransporterTimer <= 1f))
                    landLimit += module.TransporterTroopLanding;
                return landLimit;
            }
        }

        private void AssaultPlanetWithTransporters(Planet planet)
        {
            if (Owner == null)
                return;

            int landed = Owner.LandTroopsOnPlanet(planet, TroopLandLimit);
            for (int i = 0; i < landed; ++i)  
            {
                // FB: not sure what this flag is for. Should be tested with STSA
                bool foundTransportToUse = false;
                foreach (ShipModule module in AllActiveTroopBays)
                {
                    if (module.hangarTimer < module.hangarTimerConstant)
                    {
                        module.hangarTimer = module.hangarTimerConstant;
                        foundTransportToUse = true;
                        break;
                    }
                }
                if (!foundTransportToUse)
                {
                    foreach (ShipModule module in AllTransporters)
                    {
                        if (module.TransporterTimer < module.TransporterTimerConstant)
                        {
                            module.TransporterTimer = module.TransporterTimerConstant;
                            break;
                        }
                    }
                }
            }
        }

        public ShipModule[] PrepShipHangars(Empire empire) // This will set dynamic hangar UIDs to the best ships
        {
            if (empire.Id == -1) // ID -1 since there is a loyalty without a race when saving s ship
                return Empty<ShipModule>.Array;

            string defaultShip = empire.data.StartingShip;
            ShipModule[] readyHangars = AllFighterHangars.Filter(hangar => hangar.Active
                                                                 && hangar.hangarTimer <= 0
                                                                 && !hangar.TryGetHangarShip(out _));
            foreach (ShipModule hangar in readyHangars)
            {
                if (hangar.DynamicHangar == DynamicHangarOptions.Static)
                {
                    if (empire.ShipsWeCanBuild.Contains(hangar.hangarShipUID))
                        continue; // FB: we can build the ship, this hangar is sorted out
                }
                // If the ship we want cant be built, will try to launch the best we have by proceeding this method as if the hangar is dynamic
                string selectedShip = GetDynamicShipName(hangar, empire);
                hangar.hangarShipUID = selectedShip;
                if (hangar.hangarShipUID == null || hangar.hangarShipUID.IsEmpty())
                    hangar.hangarShipUID = defaultShip;
                if (hangar.hangarShipUID == null || hangar.hangarShipUID.IsEmpty())
                {
                    hangar.hangarShipUID = EmpireManager.Player.data.StartingShip;
                    string roles = "";
                    foreach (var role in hangar.HangarRoles)
                    {
                        if (roles.NotEmpty())
                            roles += ", ";
                        roles += role;
                    }
                    Log.Warning($"No startingShip defined and no {roles} designs available for {Owner}");
                }
            }
            return readyHangars;
        }

        private static ShipData.HangarOptions GetCategoryFromHangarType(DynamicHangarOptions hangarType)
        {
            switch (hangarType)
            {
                case DynamicHangarOptions.DynamicInterceptor: return ShipData.HangarOptions.Interceptor;
                case DynamicHangarOptions.DynamicAntiShip:    return ShipData.HangarOptions.AntiShip;
                default:                                      return ShipData.HangarOptions.General;
            }
        }

        public static string GetDynamicShipNameShipDesign(ShipModule hangar) => GetDynamicShipName(hangar, EmpireManager.Player);

        static string GetDynamicShipName(ShipModule hangar, Empire empire)
        {
            ShipData.HangarOptions desiredShipCategory = GetCategoryFromHangarType(hangar.DynamicHangar);
            float strongest = 0;
            string bestShip = string.Empty;
            foreach (var role in hangar.HangarRoles)
            {
                Ship selectedShip = ShipBuilder.PickFromCandidates(role, empire, maxSize: hangar.MaximumHangarShipSize,
                    designation: desiredShipCategory);

                // If no desired category is available in the empire, try to get the best ship we have regardless of category for this role
                if (selectedShip == null && hangar.DynamicHangar != DynamicHangarOptions.DynamicLaunch)
                    selectedShip = ShipBuilder.PickFromCandidates(role, empire, maxSize: hangar.MaximumHangarShipSize);

                if (selectedShip != null && selectedShip.BaseStrength >= strongest)
                {
                    strongest = selectedShip.BaseStrength;
                    bestShip  = selectedShip.Name;
                }
            }

            return bestShip;
        }
        
        public bool IsInHangarLaunchRange(GameplayObject target) 
                                        => IsInHangarLaunchRange(target.Position.Distance(Owner.Position));

        /// <summary>
        /// Determines whether distance to target is near enough to launch hangar ships>.
        /// </summary>
        /// <param name="distanceToTarget">The distance to target.</param>
        /// <returns>
        ///   <c>true</c> if [is in hangar launch range] [the specified distance to target]; otherwise, <c>false</c>.
        /// </returns>
        public bool IsInHangarLaunchRange(float distanceToTarget)
        {
            if (HasActiveHangars && !Owner.IsSpoolingOrInWarp)
            {
                float range;
                if (IsPrimaryCarrierRoleForLaunchRange && Owner.AI.CombatState != CombatState.ShortRange && Owner.AI.CombatState != CombatState.AttackRuns)
                    range = Owner.SensorRange;
                else
                    range = Owner.WeaponsMaxRange;

                if (!Owner.ManualHangarOverride && distanceToTarget < range)
                    return true;
            }
            return false;
        }

        public bool FightersOut
        {
            get => FightersLaunched;
            set
            {
                if (Owner == null)
                    return;

                if (Owner.IsSpoolingOrInWarp)
                {
                    GameAudio.NegativeClick(); // dont allow changing button state if the ship is spooling or at warp
                    return;
                }

                FightersLaunched = value;
                if (FightersLaunched)
                    ScrambleFighters();
                else
                    RecoverFighters();
            }
        }

        public bool TroopsOut
        {
            get => TroopsLaunched;
            set
            {
                if (Owner == null)
                    return;

                if (Owner.IsSpoolingOrInWarp)
                {
                    GameAudio.NegativeClick(); // dont allow changing button state if the ship is spooling or at warp
                    return;
                }

                TroopsLaunched = value;
                if (TroopsLaunched)
                    ScrambleAllAssaultShips();
                else
                    RecoverAssaultShips();
            }
        }

        public void HandleHangarShipsScramble()
        {
            if (Owner?.loyalty.isPlayer == true)
            {

                if (FightersLaunched) // for ships with hangars and with fighters out button on.
                    ScrambleFighters(); // FB: If new fighters are ready in hangars, scramble them

                if (TroopsLaunched)
                    ScrambleAllAssaultShips(); // FB: if the troops out button is on, launch every available assault shuttle
            }
        }

        public void SetSendTroopsToShip(bool value)
        {
            SendTroopsToShip = value;
        }

        public void SetRecallFightersBeforeFTL(bool value)
        {
            RecallFightersBeforeFTL = value;
        }

        public void RecallAfterCombat()
        {
            if (Owner == null)
                return; // not a carrier

            bool isAI = !Owner.loyalty.isPlayer;
            for (int i = 0; i < AllFighterHangars.Length; i++)
            {
                ShipModule hangar = AllFighterHangars[i];
                if (hangar.TryGetHangarShip(out Ship hangarShip) && hangarShip.Active && hangarShip.AI.State != AIState.ReturnToHangar)
                {
                    bool hasPriorityOrderOrTarget = hangarShip.AI.HasPriorityTarget || hangarShip.AI.HasPriorityOrder;
                    if (isAI || !hasPriorityOrderOrTarget)
                    {
                        if (FightersLaunched)
                            hangarShip.DoEscort(Owner);
                        else
                            hangarShip.AI.OrderReturnToHangarDeferred();
                    }
                }
            }
        }
    }
}
