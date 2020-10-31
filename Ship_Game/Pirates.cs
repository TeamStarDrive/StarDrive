using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public class Pirates // Created by Fat Bastard April 2020
    {
        // Pirates Class is created for factions which are defined as pirates in their XML
        // An init goal will be created for the pirates (PirateAI.cs) That goal will launch
        // A Payment director for each major empire, which will be responsible for collecting
        // Money by set period (defined in the race XML). 
        // If the player or the AI refuse to pay, the payment Director will create a Raid
        // Director goal for the relevant empire, and that raid director will decide which
        // raids to launch vs the target empire. 
        // Pirates go up in levels when successfully completing raids, but its harder for
        // them to level when they are get more powerful. 
        // When Pirates level up, they create more bases in the galaxy - in asteroid belts,
        // Lone systems and even in deep space not located in sensor ranges.
        // Pirates have threat level per empire and this sets how aggressive and how many
        // Raids they can have vs. that empire in any given time. 
        // When their bases are destroyed, they level down and their threat level goes down
        // across the board as well.

        // Note that multiple pirate factions is supported. Modders can add their own.
        // Pirates which got paid might even protect targets from other pirates factions.

        public const int MaxLevel = 20;
        public readonly Empire Owner;
        public readonly BatchRemovalCollection<Goal> Goals;
        public Map<int, int> ThreatLevels { get; private set; }    = new Map<int, int>();  // Empire IDs are used here
        public Map<int, int> PaymentTimers { get; private set; }   = new Map<int, int>(); // Empire IDs are used here
        public Array<Guid> SpawnedShips { get; private set; }      = new Array<Guid>();
        public Array<string> ShipsWeCanSpawn { get; private set; } = new Array<string>();
        public int Level { get; private set; }

        public Pirates(Empire owner, bool fromSave, BatchRemovalCollection<Goal> goals)
        {
            Owner = owner;
            Goals = goals;

            if (!fromSave)
                goals.Add(new PirateAI(Owner));
        }

        public HashSet<string> ShipsWeCanBuild => Owner.ShipsWeCanBuild;
        public int MinimumColoniesForPayment   => Owner.data.MinimumColoniesForStartPayment;
        int PaymentPeriodTurns                 => Owner.data.PiratePaymentPeriodTurns;
        public bool PaidBy(Empire victim)      => !Owner.IsAtWarWith(victim);

        public void AddGoalDirectorPayment(Empire victim) => 
            AddGoal(victim, GoalType.PirateDirectorPayment, null);

        public void AddGoalDirectorRaid(Empire victim) => 
            AddGoal(victim, GoalType.PirateDirectorRaid, null);

        public void AddGoalBase(Ship ship, string sysName) => 
            AddGoal(null, GoalType.PirateBase, ship, sysName);

        public void AddGoalRaidTransport(Empire victim) => 
            AddGoal(victim, GoalType.PirateRaidTransport, null);

        public void AddGoalRaidOrbital(Empire victim) =>
            AddGoal(victim, GoalType.PirateRaidOrbital, null);

        public void AddGoalRaidProjector(Empire victim) =>
            AddGoal(victim, GoalType.PirateRaidProjector, null);

        public void AddGoalRaidCombatShip(Empire victim) =>
            AddGoal(victim, GoalType.PirateRaidCombatShip, null);

        public void AddGoalDefendBase(Empire victim, Ship baseToDefend) =>
            AddGoal(victim, GoalType.PirateDefendBase, baseToDefend);

        public void AddGoalProtection(Empire victim, Ship shipToDefend) =>
            AddGoal(victim, GoalType.PirateDefendBase, shipToDefend);

        void AddGoal(Empire victim, GoalType type, Ship ship, string systemName = "")
        {
            switch (type)
            {
                case GoalType.PirateDirectorPayment: Goals.Add(new PirateDirectorPayment(Owner, victim));  break;
                case GoalType.PirateDirectorRaid:    Goals.Add(new PirateDirectorRaid(Owner, victim));     break;
                case GoalType.PirateBase:            Goals.Add(new PirateBase(Owner, ship, systemName));   break;
                case GoalType.PirateRaidTransport:   Goals.Add(new PirateRaidTransport(Owner, victim));    break;
                case GoalType.PirateRaidOrbital:     Goals.Add(new PirateRaidOrbital(Owner, victim));      break;
                case GoalType.PirateRaidProjector:   Goals.Add(new PirateRaidProjector(Owner, victim));    break;
                case GoalType.PirateRaidCombatShip:  Goals.Add(new PirateRaidCombatShip(Owner, victim));   break;
                case GoalType.PirateDefendBase:      Goals.Add(new PirateDefendBase(Owner, ship));         break;
                case GoalType.PirateProtection:      Goals.Add(new PirateProtection(Owner, victim, ship)); break;
                default:                             Log.Warning($"Goal type {type} invalid for Pirates"); break;
            }
        }

        public void Init() // New Game
        {
            foreach (Empire empire in EmpireManager.MajorEmpires)
            {
                ThreatLevels.Add(empire.Id, -1);
                PaymentTimers.Add(empire.Id, PaymentPeriodTurns);
            }

            PopulateDefaultBasicShips(fromSave: false);
        }

        public void RestoreFromSave(SavedGame.EmpireSaveData sData)

        {
            ThreatLevels    = sData.PirateThreatLevels;
            PaymentTimers   = sData.PiratePaymentTimers;
            SpawnedShips    = sData.SpawnedShips;
            ShipsWeCanSpawn = sData.ShipsWeCanSpawn;

            SetLevel(sData.PirateLevel);
            PopulateDefaultBasicShips(fromSave: true);
        }

        public int PaymentTimerFor(Empire victim)          => PaymentTimers[victim.Id];
        public void DecreasePaymentTimerFor(Empire victim) => PaymentTimers[victim.Id] -= 1;
        public void ResetPaymentTimerFor(Empire victim)    => PaymentTimers[victim.Id] = PaymentPeriodTurns;

        public void IncreaseThreatLevelFor(Empire victim) => SetThreatLevelFor(victim, ThreatLevels[victim.Id] + 1);
        public void DecreaseThreatLevelFor(Empire victim) => SetThreatLevelFor(victim,  ThreatLevels[victim.Id] - 1);
        public void ResetThreatLevelFor(Empire victim) => SetThreatLevelFor(victim, 1);
        void SetThreatLevelFor(Empire victim, int value)  => ThreatLevels[victim.Id] = value.Clamped(1, MaxLevel);

        // For the Pirates themselves
        public void SetLevel(int value)   => Level = value;
        public void IncreaseLevel()       => SetLevel(Level + 1);
        void DecreaseLevel()              => SetLevel(Level - 1);

        public int ThreatLevelFor(Empire victim) => ThreatLevels[victim.Id];

        bool GetOrbitals(out Array<Ship> orbitals, Array<string> orbitalNames)
        {
            orbitals = new Array<Ship>();
            var shipList = Owner.GetShips();
            for (int i = 0; i < shipList.Count; i++)
            {
                Ship ship = shipList[i];
                if (orbitalNames.Contains(ship.Name))
                    orbitals.Add(ship);
            }

            return orbitals.Count > 0;
        }

        public bool GetBases(out Array<Ship> bases)    => GetOrbitals(out bases, Bases());
        public bool GetStations(out Array<Ship> bases) => GetOrbitals(out bases, Stations());

        Array<string> Bases()
        {
            Array<string> bases = new Array<string>
            {
                Owner.data.PirateBaseBasic, 
                Owner.data.PirateBaseImproved, 
                Owner.data.PirateBaseAdvanced
            };


            return bases;
        }

        Array<string> Stations()
        {
            Array<string> stations = new Array<string>
            {
                Owner.data.PirateStationBasic, 
                Owner.data.PirateStationImproved, 
                Owner.data.PirateStationAdvanced
            };

            return stations;
        }

        bool GetOrbitalsOrbitingPlanets(out Array<Ship> planetBases)
        {
            planetBases = new Array<Ship>();
            GetBases(out Array<Ship> bases);
            GetStations(out Array<Ship> stations);
            bases.AddRange(stations);

            for (int i = 0; i < bases.Count; i++)
            {
                Ship pirateBase = bases[i];
                if (pirateBase.GetTether() != null)
                    planetBases.AddUnique(pirateBase);
            }

            return planetBases.Count > 0;
        }

        public int GetMoneyModifier(Empire victim, float basePercentage)
        {
            float multiplier         = victim.DifficultyModifiers.PiratePayModifier;
            float minimumPayment     = Level * 100 * multiplier;
            float victimNetPotential = (victim.PotentialIncome - victim.AllSpending).LowerBound(0) * multiplier;
            float payment            = (victimNetPotential*PaymentPeriodTurns) * basePercentage/100;
            payment                 *= (Level / 2).LowerBound(1);

            return (payment * multiplier).LowerBound(minimumPayment).RoundTo10();
        }

        public bool VictimIsDefeated(Empire victim)
        {
            return victim.data.Defeated;
        }

        public void LevelDown()
        {
            var empires = EmpireManager.MajorEmpires;
            for (int i = 0; i < empires.Length; i++)
            {
                Empire empire = empires[i];
                DecreaseThreatLevelFor(empire);
            }

            DecreaseLevel();
            RemovePiratePresenceFromSystem();
            if (Level < 1)
            {
                Owner.GetEmpireAI().Goals.Clear();
                Owner.SetAsDefeated();
                Empire.Universe.NotificationManager.AddEmpireDiedNotification(Owner);
            }
            else
            {
                AlertPlayerAboutPirateOps(PirateOpsWarning.LevelDown);
            }
        }

        public void TryLevelUp(bool alwaysLevelUp = false)
        {
            if (Level == MaxLevel)
                return;

            int dieRoll = Level * DifficultyMultiplier() * (int)CurrentGame.Pace;
            if (alwaysLevelUp || RandomMath.RollDie(dieRoll) == 1)
            {
                int newLevel = Level + 1;
                if (NewLevelOperations(newLevel))
                {
                    IncreaseLevel();
                    AlertPlayerAboutPirateOps(PirateOpsWarning.LevelUp);
                    Log.Info(ConsoleColor.Green, $"---- Pirates: {Owner.Name} are now level {Level} ----");
                }
            }
        }

        int DifficultyMultiplier()
        {
            int max = (int)Enum.GetValues(typeof(UniverseData.GameDifficulty)).Cast<UniverseData.GameDifficulty>().Max() + 1;
            if (Level < 10)
                --max;

            return (max - (int)CurrentGame.Difficulty).LowerBound(1);
        }

        void AlertPlayerAboutPirateOps(PirateOpsWarning warningType)
        {
            if (!Owner.IsKnown(EmpireManager.Player))
                return;

            float espionageStr = EmpireManager.Player.GetSpyDefense();
            if (espionageStr <= Level)
                return; // not enough espionage strength to learn about pirate activities

            switch (warningType)
            {
                case PirateOpsWarning.LevelUp:   Empire.Universe.NotificationManager.AddPiratesAreGettingStronger(Owner, Level); break;
                case PirateOpsWarning.LevelDown: Empire.Universe.NotificationManager.AddPiratesAreGettingWeaker(Owner, Level);   break;
                case PirateOpsWarning.Flagship:  Empire.Universe.NotificationManager.AddPiratesFlagshipSighted(Owner);           break;
            }
        }

        bool NewLevelOperations(int level)
        {
            bool success;
            NewBaseSpot spotType = (NewBaseSpot)RandomMath.IntBetween(0, 4);
            switch (spotType)
            {
                case NewBaseSpot.GasGiant:
                case NewBaseSpot.Habitable:    success = BuildBaseOrbitingPlanet(spotType, level); break;
                case NewBaseSpot.AsteroidBelt: success = BuildBaseInAsteroids(level);              break;
                case NewBaseSpot.DeepSpace:    success = BuildBaseInDeepSpace(level);              break;
                case NewBaseSpot.LoneSystem:   success = BuildBaseInLoneSystem(level);             break;
                default:                       success = false;                                    break;
            }

            if (success)
            {
                AdvanceInTech(level);
                AdvanceInShips(level);
                BuildStation(level);
                SpawnFlagShip(level);
            }

            return success;
        }

        void SpawnFlagShip(int level)
        {
            if (level != MaxLevel / 2)
                return;

            var shipList = Owner.GetShips();
            using (shipList.AcquireReadLock())
            {
                if (shipList.Any(s => s.Name == Owner.data.PirateFlagShip))
                    return;
            }

            if (GetOrbitalsOrbitingPlanets(out Array<Ship> planetBases))
            {
                Ship pirateBase = planetBases.RandItem();
                Planet planet   = pirateBase.GetTether();
                if (SpawnShip(PirateShipType.FlagShip, planet.Center, out Ship flagShip))
                {
                    flagShip.AI.OrderToOrbit(planet);
                    AlertPlayerAboutPirateOps(PirateOpsWarning.Flagship);
                }
            }
            else if (GetBases(out Array<Ship> bases))
            {
                Ship pirateBase = bases.RandItem();
                Vector2 pos     = pirateBase.Center.GenerateRandomPointOnCircle(2000);
                if (SpawnShip(PirateShipType.FlagShip, pos, out Ship flagShip))
                {
                    flagShip.AI.AddEscortGoal(pirateBase);
                    AlertPlayerAboutPirateOps(PirateOpsWarning.Flagship);
                }
            }
        }

        void BuildStation(int level)
        {
            if (level % 3 != 0)
                return; // Build a station every 3 levels

            GetStations(out Array<Ship> stations);
            if (stations.Count >= level / 2)
                return; // too many stations

            if (GetBases(out Array<Ship> bases))
            {
                Ship selectedBase = bases.RandItem();
                Planet planet     = selectedBase.GetTether();
                Vector2 pos       = planet?.Center ?? selectedBase.Center;

                Vector2 stationPos = pos.GenerateRandomPointOnCircle(2000);
                if (SpawnShip(PirateShipType.Station, stationPos, out Ship station, level) && planet != null)
                    station.TetherToPlanet(planet);
            }
        }

        void AdvanceInTech(int level)
        {
            switch (level)
            {
                case 2: Owner.data.FuelCellModifier      = 1.2f.LowerBound(Owner.data.FuelCellModifier); break;
                case 3: Owner.data.FuelCellModifier      = 1.4f.LowerBound(Owner.data.FuelCellModifier); break;
                case 4: Owner.data.FTLPowerDrainModifier = 0.8f;                                         break;
            }

            Owner.data.BaseShipLevel = level / 4;
            EmpireShipBonuses.RefreshBonuses(Owner);
        }

        bool BuildBaseInDeepSpace(int level)
        {
            if (!GetBaseSpotDeepSpace(out Vector2 pos))
                return false;

            if (!SpawnShip(PirateShipType.Base, pos, out Ship pirateBase, level)) 
                return false;

            AddGoalBase(pirateBase, pirateBase.SystemName); // SystemName is "Deep Space"
            return true;
        }

        bool BuildBaseInAsteroids(int level)
        {
            if (GetBaseAsteroidsSpot(out Vector2 pos, out SolarSystem system)
                && SpawnShip(PirateShipType.Base, pos, out Ship pirateBase, level))
            {
                AddGoalBase(pirateBase, system.Name);
                system.SetPiratePresence(true);
                return true;
            }

            return BuildBaseInDeepSpace(level);
        }

        bool BuildBaseInLoneSystem(int level)
        {
            if (GetLoneSystem(out SolarSystem system))
            {
                Vector2 pos = system.Position.GenerateRandomPointOnCircle((system.Radius * 0.75f).LowerBound(10000));
                if (SpawnShip(PirateShipType.Base, pos, out Ship pirateBase, level))
                {
                    AddGoalBase(pirateBase, system.Name);
                    system.SetPiratePresence(true);
                    return true;
                }
            }

            return BuildBaseInDeepSpace(level);
        }

        bool BuildBaseOrbitingPlanet(NewBaseSpot spot, int level)
        {
            if (GetBasePlanet(spot, out Planet planet))
            {
                Vector2 pos = planet.Center.GenerateRandomPointOnCircle(planet.ObjectRadius + 2000);
                if (SpawnShip(PirateShipType.Base, pos, out Ship pirateBase, level))
                {
                    pirateBase.TetherToPlanet(planet);
                    AddGoalBase(pirateBase, planet.ParentSystem.Name);
                    planet.ParentSystem.SetPiratePresence(true);
                    return true;
                }
            }

            return BuildBaseInDeepSpace(level);
        }

        bool GetBaseSpotDeepSpace(out Vector2 position)
        {
            position               = Vector2.Zero;
            var sortedThreatLevels = ThreatLevels.SortedDescending(l => l.Value);
            var empires            = new Array<Empire>();

            foreach (KeyValuePair<int, int> threatLevel in sortedThreatLevels)
                empires.Add(EmpireManager.GetEmpireById(threatLevel.Key));

            // search for a hidden place near an empire from 400K to 300K
            for (int i = 0; i <= 50; i++)
            {
                int spaceReduction = i * 2000;
                foreach (Empire victim in empires.Filter(e => !e.data.Defeated))
                {
                    SolarSystem system = victim.GetOwnedSystems().RandItem();
                    var pos = PickAPositionNearSystem(system, 400000 - spaceReduction);
                    foreach (Empire empire in empires)
                    {
                        if (empire.SensorNodes.Any(n => n.Position.InRadius(pos, n.Radius)))
                            break;
                    }

                    position = pos; // We found a position not in sensor range of any empire
                    return true;
                }
            }

            return false; // We did not find a hidden position
        }

        Vector2 PickAPositionNearSystem(SolarSystem system, float radius)
        {
            Vector2 pos;
            do
            {
                pos = system.Position.GenerateRandomPointOnCircle(radius);
            } while (!HelperFunctions.IsInUniverseBounds(Empire.Universe.UniverseSize, pos));

            return pos;
        }

        bool GetBaseAsteroidsSpot(out Vector2 position, out SolarSystem system)
        {
            position   = Vector2.Zero;
            system     = null;

            if (!GetUnownedSystems(out SolarSystem[] systems))
                return false;

            var systemsWithAsteroids = systems.Filter(s => s.RingList
                                       .Any(r => r.Asteroids && s.InSafeDistanceFromRadiation(r.OrbitalDistance)));

            if (systemsWithAsteroids.Length == 0)
                return false;

            SolarSystem selectedSystem    = systemsWithAsteroids.RandItem();
            var asteroidRings             = selectedSystem.RingList.Filter(r => r.Asteroids);
            SolarSystem.Ring selectedRing = asteroidRings.RandItem();

            float ringRadius = selectedRing.OrbitalDistance + RandomMath.IntBetween(-250, 250);
            position         = selectedSystem.Position.GenerateRandomPointOnCircle(ringRadius);
            system           = selectedSystem;

            return position != Vector2.Zero;
        }
        
        bool GetBasePlanet(NewBaseSpot spot, out Planet selectedPlanet)
        {
            selectedPlanet = null;
            if (!GetUnownedSystems(out SolarSystem[] systems))
                return false;

            Array<Planet> planets = new Array<Planet>();
            for (int i = 0; i < systems.Length; i++)
            {
                SolarSystem system = systems[i];
                switch (spot)
                {
                    case NewBaseSpot.Habitable: 
                        planets.AddRange(system.PlanetList.Filter(p => p.Habitable 
                                         && p.InSafeDistanceFromRadiation())); 

                        break;
                    case NewBaseSpot.GasGiant: 
                        planets.AddRange(system.PlanetList.Filter(p => p.Category == PlanetCategory.GasGiant
                                         && p.InSafeDistanceFromRadiation())); 

                        break;
                }
            }

            if (planets.Count == 0)
                return false;

            selectedPlanet = planets.RandItem();
            return selectedPlanet != null;
        }

        public bool RaidingThisShip(Ship ship)
        {
            var goals = Owner.GetEmpireAI().Goals;

            using (goals.AcquireReadLock())
            {
                return goals.Any(g => g.TargetShip == ship);
            }
        }

        bool GetUnownedSystems(out SolarSystem[] systems)
        {
            systems = UniverseScreen.SolarSystemList.Filter(s => s.OwnerList.Count == 0 
                                                                 && s.RingList.Count > 0 
                                                                 && !s.PiratePresence
                                                                 && !s.PlanetList.Any(p => p.Guardians.Count > 0));

            return systems.Length > 0;
        }

        bool GetLoneSystem(out SolarSystem system)
        {
            system = null;
            var systems = UniverseScreen.SolarSystemList.Filter(s => s.RingList.Count == 0 
                                                                     && !s.PiratePresence);

            if (systems.Length > 0)
                system = systems.RandItem();

            return system != null;
        }

        void RemovePiratePresenceFromSystem()
        {
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                if (!system.ShipList.Any(s => s.IsPlatformOrStation && s.loyalty.WeArePirates))
                    system.SetPiratePresence(false);
            }
        }

        public void ProcessShip(Ship ship, Ship pirateBase)
        {
            if (SpawnedShips.Contains(ship.guid))
            {
                // We cannot salvage ships that we spawned
                // remove it with no benefits
                SpawnedShips.RemoveSwapLast(ship.guid);
                ship.QueueTotalRemoval();
                CleanUpSpawnedShips();
            }
            else
            {
                SalvageShip(ship, pirateBase);
            }
        }

        void CleanUpSpawnedShips()
        {
            for (int i = SpawnedShips.Count - 1; i >= 0;  i--)
            {
                Guid shipGuid = SpawnedShips[i];
                {
                    if (!Owner.GetShips().Any(s => s.guid == shipGuid))
                        SpawnedShips.RemoveAtSwapLast(i);
                }
            }
        }

        public void AdvanceInShips(int level)
        {
            string shipToAdd;
            switch (level)
            {
                case 5:  shipToAdd = Owner.data.PirateFighterImproved; break;
                case 6:  shipToAdd = Owner.data.PirateSlaverImproved;  break;
                case 7:  shipToAdd = Owner.data.PirateFrigateImproved; break;
                case 8:  shipToAdd = Owner.data.PirateFighterAdvanced; break;
                case 9:  shipToAdd = Owner.data.PirateSlaverAdvanced;  break;
                case 10: shipToAdd = Owner.data.PirateFrigateAdvanced; break;
                default:                                               return;
            }

            if (shipToAdd.NotEmpty())
            {
                ShipsWeCanSpawn.AddUnique(shipToAdd);
                Ship fighter = ResourceManager.GetShipTemplate(shipToAdd);
                if (fighter != null && fighter.shipData.HullRole == ShipData.RoleName.fighter
                                    && !ShipsWeCanBuild.Contains(shipToAdd))
                {
                    ShipsWeCanBuild.Add(shipToAdd); // For carriers to spawn the default fighters
                }
            }
            else
            {
                Log.Warning($"Could not find a default ship to add for {Owner.Name}, " +
                            "check their default ships in the race XML");
            }
        }

        public struct PirateForces
        {
            public readonly string Fighter;
            public readonly string Frigate;
            public readonly string BoardingShip;
            public readonly string Base;
            public readonly string Station;
            public readonly string FlagShip;
            public readonly string Random;

            public PirateForces(Empire pirates, int effectiveLevel) : this()
            {
                FlagShip         = pirates.data.PirateFlagShip;
                int levelDivider = 1; 

                switch (CurrentGame.Difficulty) // Don't let pirates spawn advanced tech too early at lower difficulty
                {
                    case UniverseData.GameDifficulty.Easy:   levelDivider = 3; break;
                    case UniverseData.GameDifficulty.Normal: levelDivider = 2; break;
                }

                switch (effectiveLevel / levelDivider)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        Fighter      = pirates.data.PirateFighterBasic;
                        Frigate      = pirates.data.PirateFrigateBasic;
                        BoardingShip = pirates.data.PirateSlaverBasic;
                        Base         = pirates.data.PirateBaseBasic;
                        Station      = pirates.data.PirateStationBasic;
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        Fighter      = pirates.data.PirateFighterImproved;
                        Frigate      = pirates.data.PirateFrigateImproved;
                        BoardingShip = pirates.data.PirateSlaverImproved;
                        Base         = pirates.data.PirateBaseImproved;
                        Station      = pirates.data.PirateStationImproved;
                        break;
                    default:
                        Fighter      = pirates.data.PirateFighterAdvanced;
                        Frigate      = pirates.data.PirateFrigateAdvanced;
                        BoardingShip = pirates.data.PirateSlaverAdvanced;
                        Base         = pirates.data.PirateBaseAdvanced;
                        Station      = pirates.data.PirateStationAdvanced;
                        break;
                }

                Random = GetRandomShipFromSpawnList(pirates, out string shipName) ? shipName : GetRandomDefaultShip();
            }

            bool GetRandomShipFromSpawnList(Empire empire, out string shipName)
            {
                shipName = "";
                if (empire.Pirates.ShipsWeCanSpawn?.Count > 0)
                    shipName = empire.Pirates.ShipsWeCanSpawn.RandItem();

                return shipName.NotEmpty();
            }

            string GetRandomDefaultShip() => RandomMath.RollDice(80) ? Fighter : Frigate;
        }

        public bool GetTarget(Empire victim, TargetType type, out Ship target)
        {
            target          = null;
            var targets     = new Array<Ship>(); 
            var victimShips = type == TargetType.Projector ? victim.GetProjectors() : victim.GetShips();
            
            for (int i = 0; i < victimShips.Count; i++)
            {
                Ship ship = victimShips[i];
                if (RaidingThisShip(ship))
                    continue;

                switch (type)
                {
                    case TargetType.Shipyard         when ship.shipData.IsShipyard:
                    case TargetType.FreighterAtWarp  when IsFreighterAtWarpNoOwnedSystem(ship):
                    case TargetType.CombatShipAtWarp when IsCombatShipAtWarp(ship):
                    case TargetType.Station          when ship.IsStation:
                    case TargetType.Platform         when ship.IsPlatform:
                    case TargetType.Projector: targets.Add(ship); break; // Add all of above cases in to targets
                }
            }

            if (targets.Count == 0)
                return false;

            target = targets.RandItem();
            return target != null;

            bool IsFreighterAtWarpNoOwnedSystem(Ship ship)
            {
                return (ship.isColonyShip || ship.AI.FindGoal(ShipAI.Plan.DropOffGoods, out _)) 
                       && ship.IsInWarp 
                       && (ship.System == null || !ship.System.IsOwnedBy(ship.loyalty));
            }

            bool IsCombatShipAtWarp(Ship ship)
            {
                return !ship.IsPlatformOrStation && ship.BaseStrength > 0 && ship.IsInWarp;
            }
        }

        public void OrderEscortShip(Ship shipToEscort, Array<Ship> force)
        {
            if (force.Count == 0 || shipToEscort == null)
                return;

            for (int i = 0; i < force.Count; i++)
            {
                Ship ship = force[i];
                ship.AI.AddEscortGoal(shipToEscort);
            }
        }

        public void OrderAttackShip(Ship target, Array<Ship> force)
        {
            if (force.Count == 0 || target == null)
                return;

            for (int i = 0; i < force.Count; i++)
            {
                Ship ship = force[i];
                ship.AI.OrderAttackPriorityTarget(target);
            }
        }

        public bool SpawnBoardingShip(Ship targetShip, Vector2 where, out Ship boardingShip)
        {
            if (SpawnShip(PirateShipType.Boarding, where, out boardingShip))
                boardingShip.AI.OrderAttackSpecificTarget(targetShip);

            return boardingShip != null;
        }

        float GaugeNeededStrForForce(Ship targetShip)
        {
            if (targetShip == null)
                return 0;

            float enemyStr;
            var shipList = targetShip.AI.FriendliesNearby;
            using (shipList.AcquireReadLock())
                enemyStr = shipList.Sum(s => s.BaseStrength);

            float maxStrModifier       = ((int)CurrentGame.Difficulty + 1) * 0.15f; // easy will be 15%
            float availableStrModifier = (float)Level / MaxLevel;

            return (enemyStr * maxStrModifier * availableStrModifier).LowerBound(Level * 500);
        }

        public bool SpawnForce(Ship targetShip, Vector2 pos, float radius, out Array<Ship> force)
        {
            float currentStr = GaugeNeededStrForForce(targetShip);
            force = new Array<Ship>();
            while (currentStr.Greater(0) && force.Count <= Level * 10) 
            {
                Vector2 finalPos = pos.GenerateRandomPointOnCircle(radius);
                if (SpawnShip(PirateShipType.Random, finalPos, out Ship ship))
                {
                    force.Add(ship);
                    currentStr -= ship.BaseStrength;
                    continue;
                }

                currentStr -= 1000; // Safety exit
            }

            return force.Count > 0;
        }

        public bool SpawnShip(PirateShipType shipType, Vector2 where, out Ship pirateShip, int level = 0)
        {
            PirateForces forces = new PirateForces(Owner, level);
            string shipName     = "";
            pirateShip          = null;

            switch (shipType)
            {
                case PirateShipType.Fighter:  shipName = forces.Fighter;      break;
                case PirateShipType.Frigate:  shipName = forces.Frigate;      break;
                case PirateShipType.Boarding: shipName = forces.BoardingShip; break;
                case PirateShipType.Base:     shipName = forces.Base;         break;
                case PirateShipType.Station:  shipName = forces.Station;      break;
                case PirateShipType.FlagShip: shipName = forces.FlagShip;     break;
                case PirateShipType.Random:   shipName = forces.Random;       break;
            }

            if (shipName.NotEmpty())
            {
                pirateShip = Ship.CreateShipAtPoint(shipName, Owner, where);
                if (pirateShip != null)
                    SpawnedShips.Add(pirateShip.guid);
                else
                    Log.Warning($"Could not spawn required pirate ship named {shipName} for {Owner.Name}, check race xml");
            }
            else
            {
                Log.Warning($"Pirate ship name was empty for {Owner.Name}, check race xml for typos");
            }

            return shipName.NotEmpty() && pirateShip != null;
        }

        void SalvageShip(Ship ship, Ship pirateBase)
        {
            if (ship.IsFreighter || ship.isColonyShip)
                SalvageFreighter(ship);
            else 
                SalvageCombatShip(ship, pirateBase);
        }

        void SalvageFreighter(Ship freighter)
        {
            TryLevelUp();
            freighter.QueueTotalRemoval();
        }

        void SalvageCombatShip(Ship ship, Ship pirateBase)
        {
            if (ShouldSalvageCombatShip(ship)) 
            {
                // We can use this ship in future endeavors, ha ha ha!
                if (ship.BaseStrength.Greater(0))
                    ShipsWeCanSpawn.AddUnique(ship.Name);

                ship.QueueTotalRemoval();
                TryLevelUp();
            }
            else
            {
                // We might use this ship for defense or future attacks
                if (ship.AI.State != AIState.Orbit 
                    || ship.AI.State != AIState.Escort 
                    || ship.AI.State != AIState.Resupply)
                {
                    ship.AI.AddEscortGoal(pirateBase);
                }
            }
        }

        bool ShouldSalvageCombatShip(Ship ship)
        {
            if (ship.BaseStrength.AlmostZero())
                return true;

            // we can salvage ships which are not in our spawn list.
            // But we always keep the big ships. Pirates cannot spawn bigger ships
            if (ShipsWeCanSpawn.Contains(ship.Name))
                return false;

            return ship.shipData.HullRole != ShipData.RoleName.capital 
                   || ship.shipData.HullRole != ShipData.RoleName.cruiser;
        }

        void PopulateDefaultBasicShips(bool fromSave)
        {
            ShipsWeCanBuild.Clear();
            if (Owner.data.PirateFighterBasic.NotEmpty()
                && !ShipsWeCanBuild.Contains(Owner.data.PirateFighterBasic))
            {
                ShipsWeCanBuild.Add(Owner.data.PirateFighterBasic); // For carriers
            }

            if (!fromSave)
            {
                AddToShipWeCanSpawn(Owner.data.PirateFighterBasic, "Basic Fighter");
                AddToShipWeCanSpawn(Owner.data.PirateFrigateBasic, "Basic Frigate");
                AddToShipWeCanSpawn(Owner.data.PirateSlaverBasic, "Basic Slaver");
            }
            else
            {
                // TODO - restore fighters to ship we can build
            }

            void AddToShipWeCanSpawn(string shipName, string whichType)
            {
                if (shipName.NotEmpty())
                    ShipsWeCanSpawn.AddUnique(shipName);
                else
                    Log.Warning($"Could not find a {whichType} in {Owner.Name} race xml");
            }
        }

        public void ExecuteProtectionContracts(Empire victim, Ship shipToDefend)
        {
            foreach (Empire faction in EmpireManager.PirateFactions.Filter(f => f != Owner))
            {
                if (victim.IsNAPactWith(faction))
                {
                    int executeChance = faction.Pirates.Level * 3;
                    if (RandomMath.RollDice(executeChance))
                    {
                        AddGoalProtection(victim, shipToDefend);
                        return;
                    }
                }
            }
        }

        public void ExecuteVictimRetaliation(Empire victim)
        {
            if (victim.isPlayer)
                return; // Players should attack pirate bases themselves

            EmpireAI ai             = victim.GetEmpireAI();
            int currentAssaultGoals = ai.SearchForGoals(GoalType.AssaultPirateBase).Count;
            int maxAssaultGoals     = ((int)(CurrentGame.Difficulty + 1)).UpperBound(3);
            if (currentAssaultGoals >= maxAssaultGoals || victim.data.TaxRate > 0.8f) 
                return;

            int chanceMultiplier = FoundPirateBaseInSystemOf(victim, out _) ? 8 : 4;
            if (RandomMath.RollDice(Level * chanceMultiplier))
            {
                Goal goal = new AssaultPirateBase(victim, Owner);
                victim.GetEmpireAI().AddGoal(goal);
            }
        }

        public void KillBaseReward(Empire killer, Ship killedShip)
        {
            if (!GetBases(out Array<Ship> bases)    
                || !bases.Any(b => b == killedShip)
                || killer.isFaction)
            {
                return; // The killed ship is not a pirate base or not relevant
            }

            float reward = (500 + RandomMath.RollDie(1000) + Level * 100).RoundUpToMultipleOf(10);
            killer.AddMoney(reward);
            if (killer.isPlayer)
                Empire.Universe.NotificationManager.AddDestroyedPirateBase(killedShip, reward);
        }

        bool FoundPirateBaseInSystemOf(Empire victim, out Ship pirateBase)
        {
            pirateBase = null;
            var victimSystems = victim.GetOwnedSystems();
            if (!GetBases(out Array<Ship> bases))
                return false;

            for (int i = 0; i < bases.Count; i++)
            {
                pirateBase = bases[i];
                if (victimSystems.Contains(pirateBase.System))
                    break;
            }

            return pirateBase != null;
        }

        public bool CanDoAnotherRaid(out int numRaids)
        {
            numRaids = Goals.Count(g => g.IsRaid);
            return numRaids < Level;
        }

        enum NewBaseSpot
        {
            AsteroidBelt,
            GasGiant,
            Habitable,
            DeepSpace,
            LoneSystem
        }

        enum PirateOpsWarning
        {
            LevelUp,
            LevelDown,
            Flagship
        }

        public enum TargetType
        {
            FreighterAtWarp,
            CombatShipAtWarp,
            Projector,
            Shipyard,
            Station,
            Platform
        }
    }

    public enum PirateShipType
    {
        Fighter,
        Frigate,
        Boarding,
        Base,
        Station,
        FlagShip,
        Random
    }
}
