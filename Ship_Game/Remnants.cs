﻿using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System.Linq;
using SDGraphics;
using SDUtils;
using Ship_Game.Fleets;
using Ship_Game.AI.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.ExtensionMethods;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Utils;
#pragma warning disable CA1065

namespace Ship_Game
{
    using static HelperFunctions;

    [StarDataType]
    public class Remnants
    {
        public const int MaxLevel = 20;
        [StarData] public readonly Empire Owner;
        public RandomBase Random => Owner.Random;
        public UniverseState Universe => Owner.Universe ?? throw new NullReferenceException("Remnants.Owner.Universe must not be null");

        [StarData] public float StoryTriggerKillsXp { get; private set; }
        [StarData] public float PlayerStepTriggerXp { get; private set; }
        [StarData] public float NextLevelUpDate { get; private set; }
        [StarData] public bool Activated { get; private set; }
        [StarData] public RemnantStory Story { get; private set; }
        [StarData] public float Production { get; private set; }
        [StarData] public float DefenseProduction { get; private set; }
        [StarData] public int Level { get; private set; } = 1;
        [StarData] public Map<RemnantShipType, float> ShipCosts { get; private set; } = new();
        [StarData] public int StoryStep { get; private set; } = 1;
        [StarData] public bool OnlyRemnantLeft { get; private set; }
        [StarData] public int HibernationTurns { get; private set; } // Remnants will not attack and gain 20% of normal production if above 0
        [StarData] public float ActivationXpNeeded { get; private set; } // xp of killed Remnant ships needed to for story activation
        [StarData] public Empire FocusOnEmpire { get; private set; }

        // whether to display verbose state logs
        public bool Verbose;

        [StarDataConstructor]
        Remnants() {}

        public Remnants(Empire owner, EmpireAI ai)
        {
            Owner = owner;
            Owner.data.FuelCellModifier      = 1.4f;
            Owner.data.FTLPowerDrainModifier = 0.8f;
            Owner.data.FTLModifier           = 50;
            Owner.data.MassModifier          = 0.9f;

            Story = InitAndPickStory(ai);
            CalculateShipCosts();
        }

        public void IncrementKillsForStory(Empire empire, int xp)
        {
            if (!Activated)
                StoryTriggerKillsXp += xp;

            if (!Activated && StoryTriggerKillsXp >= ActivationXpNeeded)
                Activate();

            if (empire.isPlayer)
            {
                PlayerStepTriggerXp += xp;
                if (PlayerStepTriggerXp > StepXpTrigger) // todo 0 is for testing
                {
                    PlayerStepTriggerXp = 0;
                    if (GetStoryEvent(out ExplorationEvent expEvent))
                        Universe.Notifications.AddRemnantUpdateNotify(expEvent, Owner);

                    if (StoryStep == 1 && Owner.NewEspionageEnabled) // enable view overlay (scan)
                    {
                        empire.GetRelations(Owner).Espionage.IncreaseInfiltrationLevelTo(1);
                        Universe.Notifications.AddRemnantAbleToScanOrWarn(Owner, GameText.CanScanRemnantsEvent);
                    }

                    StoryStep += 1;
                }
            }
            
            //if (!Activated) // todo for testing
            //    Activate();
        }

        public float StepXpTrigger => (ShipRole.GetMaxExpValue() * StoryStep * StoryStep * 0.5f).UpperBound(ActivationXpNeeded);
        public float ProductionLimit => 300 * Level * Level * ((int)Universe.P.Difficulty + 1);  // Level 20 - 480K 
        public float ExpansionRisk => 0; // Might change this based on future story
        public bool NoPortals => !GetPortals(out _);
        public float MaxDefenseProduction => ProductionLimit
                                             * (1 + (int)Universe.P.Difficulty)
                                             * (Story is RemnantStory.AncientHelpers ? 3 : 1);

        public float BorderRisk(Empire empire)
        {
            if (!Activated || Owner.IsDefeated || !GetPortals(out Ship[] portals))
                return 0;

            int numCloseSystems = portals.Sum(p => p.System.FiveClosestSystems.Sum(s => s.HasPlanetsOwnedBy(empire) ? 1 : 0));
            return numCloseSystems * 0.05f;
        }

        public float ThreatRisk(Empire empire)
        {
            if (!Activated || Owner.IsDefeated)
                return 0;

            float risk = Level * 0.025f * ((int)Universe.P.Difficulty + 1);
            if (UsingRandomTargets())
                return risk;

            if (FindValidTarget(out Empire target) && target == empire)
                return risk * 2;

            return risk;
        }


        void Activate()
        {
            Activated = true;
            SetInitialLevelUpDate();
            Log.Info(ConsoleColor.Green, $"---- Remnants: Activation Level: {Level} ----");

            // Todo None story does not have a goal or maybe the old goal
            switch (Story)
            {
                // Todo create colonization story  
                case RemnantStory.AncientBalancers:
                case RemnantStory.AncientExterminators:
                case RemnantStory.AncientRaidersRandom:
                case RemnantStory.AncientWarMongers:
                case RemnantStory.AncientPeaceKeepers:
                case RemnantStory.AncientHelpers:
                    Owner.AI.AddGoal(new RemnantEngagements(Owner));
                    Universe.Notifications.AddRemnantsStoryActivation(Owner);
                    if (Owner.NewEspionageEnabled)
                        Universe.Notifications.AddRemnantAbleToScanOrWarn(Owner, GameText.CanWarnRemnantsEvent);
                    break;
            }

            if (Owner.NewEspionageEnabled)
            {
                // All Empires can now detect incoming threats vis SSPs
                foreach (Empire e in Universe.ActiveMajorEmpires)
                    e.GetRelations(Owner).Espionage.IncreaseInfiltrationLevelTo(3);
            }
        }

        public void SetFocusOnEmpire(Empire e)
        {
            if (!e.WeArePirates)
                FocusOnEmpire = e;
        }

        void NotifyPlayerOnLevelUp()
        {
            float espionageStr = Universe.Player.Universe.Player.GetEspionageDefenseStrVsPiratesOrRemnants(MaxLevel);
            int effectiveLevel = Owner.LegacyEspionageEnabled ? Level * 3: Level;
            if (espionageStr >= effectiveLevel)
                Universe.Notifications.AddRemnantsAreGettingStronger(Owner);
        }

        public void TryLevelUpByDate()
        {
            if (Level >= MaxLevel) 
                return;

            if (Universe.StarDate.GreaterOrEqual(NextLevelUpDate))
            {
                int turnsLevelUp = TurnsLevelUp + ExtraLevelUpEffort;
                turnsLevelUp     = (int)(turnsLevelUp * StoryTurnsLevelUpModifier() * Universe.ProductionPace);
                HibernationTurns = 0;
                NextLevelUpDate += turnsLevelUp / 10f;
                SetLevel(Level + 1);
                UpgradeAfterLevelUp();
                Log.Info(ConsoleColor.Green, $"---- Remnants: Level up to level {Level+1}. Next level up in Stardate {NextLevelUpDate} ----");
                if (Universe.StarDate.Less(NextLevelUpDate)) // do not notify on multiple initial level ups
                    NotifyPlayerOnLevelUp();
            }
        }

        void UpgradeAfterLevelUp()
        {
            if (Level % 2 == 0)
                Owner.data.ShieldPowerMod += 0.1f;

            if (Level % 4 == 0)
                Owner.data.Traits.ModHpModifier += 0.1f;

            if (Level % 5 == 0)
                Owner.data.ArmorPiercingBonus += 1;

            if (Level % 6 == 0)
                Owner.data.ExplosiveRadiusReduction += 0.15f;

            Owner.data.BaseShipLevel = Level / 3;
            EmpireHullBonuses.RefreshBonuses(Owner);
            if (Level % Owner.DifficultyModifiers.RemnantPortalCreationMod == 0 && TryCreatePortal())
                Owner.Universe.Notifications.AddRemnantsNewPortal(Owner);
        }

        public void CheckHibernation() // Start Hibernation some time before leveling up
        {
            if (Hibernating)
                HibernationTurns -= 1;

            float hibernationDate = (NextLevelUpDate - NeededHibernationTurns/10f).RoundToFractionOf10();
            if (Universe.StarDate.AlmostEqual(hibernationDate))
                HibernationTurns = NeededHibernationTurns;
        }

        private void SetLevel(int level)
        {
            Level = level.Clamped(1, MaxLevel);
        }

        float StoryTurnsLevelUpModifier() // Above 1 is slower
        {
            switch (Story)
            {
                case RemnantStory.AncientExterminators: return 1.2f;
                case RemnantStory.AncientRaidersRandom: return 0.8f;
                default:                                return 1;
            }
        }

        int TurnsLevelUp
        {
            get
            {
                int numturns = Owner.DifficultyModifiers.RemnantTurnsLevelUp;
                switch (Universe.P.ExtraRemnant)
                {
                    case ExtraRemnantPresence.VeryRare: return (int)(numturns * 0.66f);
                    case ExtraRemnantPresence.Rare:     return (int)(numturns * 0.8f);
                    default:                            return numturns;
                }
            }
        }
        int ExtraLevelUpEffort => (int)((Level-1)*Universe.RemnantPaceModifier + NeededHibernationTurns);
        public int NeededHibernationTurns => TurnsLevelUp / ((int)Universe.P.Difficulty + 2);

        void SetInitialLevelUpDate()
        {
            int turnsLevelUp = (int)(TurnsLevelUp * StoryTurnsLevelUpModifier() * Universe.P.Pace);
            NextLevelUpDate  = 1000 + turnsLevelUp/5f; // Initial Level in half rate (/5 instead of /10)
            Log.Info(ConsoleColor.Green, $"---- Remnants: Activation ----");
        }

        bool GetStoryEvent(out ExplorationEvent expEvent, bool onlyRemnantsLeft = false)
        {
            expEvent            = null;
            var potentialEvents = ResourceManager.EventsDict.Values.ToArr();
            var events = onlyRemnantsLeft ? potentialEvents.Filter(e => e.Story == Story && e.TriggerWhenOnlyRemnantsLeft)
                                          : potentialEvents.Filter(e => e.StoryStep == StoryStep 
                                             && (e.Story == Story || Story != RemnantStory.None && e.AllRemnantStories));
            if (events.Length > 0)
                expEvent = events.First();

            return expEvent != null;
        }

        public void TriggerOnlyRemnantsLeftEvent()
        {
            if (OnlyRemnantLeft)
                return;

            if (GetStoryEvent(out ExplorationEvent expEvent, true))
            {
                Universe.Notifications.AddRemnantUpdateNotify(expEvent, Owner);
                OnlyRemnantLeft = true;
                TriggerVsPlayerEndGame();
            }
            else
            {
                Activated = false; // Shutdown Remnants so win screen could be displayed if no event found
            }
        }

        void TriggerVsPlayerEndGame()
        {
            switch (Story)
            {
                case RemnantStory.AncientBalancers:
                    for (int i = 0; i <= (int)Universe.P.Difficulty; i++)
                        TryCreatePortal();
                    break;
                case RemnantStory.AncientExterminators:
                    if (GetPortals(out Ship[] portals))
                    {
                        Ship portal = Owner.Random.Item(portals);
                        for (int i = 0; i < ((int)Universe.P.Difficulty + 1) * 3; i++)
                        {
                            if (!SpawnShip(RemnantShipType.Behemoth, portal.Position, out _))
                                break;
                        }
                    }
                    break;
                case RemnantStory.AncientWarMongers:
                    if (GetPortals(out Ship[] remnantPortals))
                    {
                        Ship portal = Owner.Random.Item(remnantPortals);
                        int saturation = (5 - (int)Universe.P.Difficulty).LowerBound(2);
                        int behomothChance = (int)Universe.P.Difficulty * 25;
                        var planets = Universe.Player.GetPlanets();
                        for (int i = 0; i < planets.Count; i++)
                        {
                            RemnantShipType shipType = Owner.Random.RollDice(behomothChance)
                                ? RemnantShipType.Behemoth
                                : RemnantShipType.Exterminator;

                            Planet targetPlanet = planets[i];
                            if (i % saturation == 0 
                                && SpawnShip(shipType, portal.Position, out Ship remnantShip))
                            {
                                remnantShip.OrderToOrbit(targetPlanet, true);
                            }
                        }
                    }
                    break;
                case RemnantStory.AncientPeaceKeepers:
                case RemnantStory.AncientHelpers:
                    Activated = false;
                    break;
            }
        }

        public bool TryGetPortalForEngagement(Ship[] portals, out Array<Ship>  availablePortals)
        {
            availablePortals = new();
            if (Hibernating)
                return false;

            foreach (Ship potentialPortal in portals)
            {
                if (Owner.AI.CountGoals(g => g.IsRaid && g.TargetShip == potentialPortal) == 0)
                    availablePortals.Add(potentialPortal);
            }

            return availablePortals.Count > 0;
        }

        public bool FindValidTarget(out Empire target)
        {
            if (FocusOnEmpire != null)
            {
                target= FocusOnEmpire;
                return true;
            }

            var empiresList = GlobalStats.RestrictAIPlayerInteraction 
                                 ? Universe.ActiveNonPlayerMajorEmpires
                                 : Universe.ActiveMajorEmpires;

            target = null;
            if (empiresList.Length == 0)
                return false;

            switch (Story)
            {
                case RemnantStory.AncientHelpers: 
                case RemnantStory.AncientExterminators: target = FindWeakestByScore(empiresList, 1.25f);   break;
                case RemnantStory.AncientPeaceKeepers:  target = FindStrongestEmpireAtWar(empiresList);    break;
                case RemnantStory.AncientWarMongers:    target = FindStrongestEmpireAtPeace(empiresList);  break;
                case RemnantStory.AncientBalancers:     target = FindStrongestByScore(empiresList, 1.25f); break;
                case RemnantStory.AncientRaidersRandom: target = Owner.Random.Item(empiresList);           break;
            }

            return target != null;
        }

        public Planet GetTargetPlanetForFleetTaskWhenNoPortals(Vector2 fleetPos)
        {
            Empire targetEmpire = Owner.Remnants.FocusOnEmpire?.IsDefeated == true
                ? Owner.Remnants.FocusOnEmpire
                : Owner.Universe.Player;

            return targetEmpire.GetPlanets().FindMin(p => p.Position.SqDist(fleetPos));
        }

        public bool TargetEmpireStillValid(Empire currentTarget, bool stickToSameRandomTarget = true)
        {
            if (Hibernating)
                return false;

            if (UsingRandomTargets() && !currentTarget.IsDefeated)
                return stickToSameRandomTarget;

            FindValidTarget(out Empire expectedTarget);
            return expectedTarget == currentTarget;
        }

        bool UsingRandomTargets()
        {
            switch (Story)
            {
                case RemnantStory.AncientRaidersRandom:
                case RemnantStory.AncientPeaceKeepers:
                case RemnantStory.AncientWarMongers: return true;
            }

            return false;
        }

        Empire FindStrongestEmpireAtPeace(Empire[] empiresList)
        {
            var potentialTargets = empiresList.Filter(e => !e.IsAtWarWithMajorEmpire);
            return potentialTargets.Length == 0 ? empiresList.FindMax(e => e.TotalScore) 
                                                : potentialTargets.FindMax(e => e.TotalScore);
        }

        Empire FindStrongestEmpireAtWar(Empire[] empiresList)
        {
            var potentialTargets = empiresList.Filter(e => e.IsAtWarWithMajorEmpire);
            return potentialTargets.Length == 0 ? null : potentialTargets.FindMax(e => e.TotalScore);
        }

        Empire FindStrongestByScore(Empire[] empiresList, float ratioThreshold)
        {
            if (empiresList.Length == 1)
                return empiresList.First();

            var averageScore = empiresList.Average(e => e.TotalScore);
            Empire strongest = empiresList.FindMax(e => e.TotalScore);

            return strongest.TotalScore > averageScore * ratioThreshold  ? strongest : null;
        }

        Empire FindWeakestByScore(Empire[] empiresList, float ratioThreshold)
        {
            if (empiresList.Length == 1)
                return empiresList.First();

            var averageScore = empiresList.Average(e => e.TotalScore);
            Empire weakest   = empiresList.FindMin(e => e.TotalScore);

            return weakest.TotalScore < averageScore * ratioThreshold ? weakest : null;  
        }

        public bool AssignShipInPortalSystem(Ship portal, int bombersNeeded, float neededStr, out Array<Ship> ships)
        {
            ships = new Array<Ship>();
            if (portal.System == null)
                return false;

            var availableShips = portal.System.ShipList.Filter(s => s.Fleet == null 
                                                                    && s.Loyalty == Owner 
                                                                    && s.IsGuardian
                                                                    && !s.IsPlatformOrStation
                                                                    && !s.InCombat
                                                                    && s.AI.State != AIState.Resupply);
            if (availableShips.Length == 0)
                return false;

            if (bombersNeeded > 0)
            {
                var bombers = availableShips.Filter(s => s.DesignRole == RoleName.bomber);
                if (bombers.Length > 0)
                    ships = bombers.Take(bombersNeeded).ToArrayList();
            }
            else
            {
                float totalStr = 0;
                var nonBombers = availableShips.Filter(s => s.DesignRole != RoleName.bomber);
                var sortedNonbombers = nonBombers.Sorted(s => s.BaseStrength);
                for (int i = 0; i < sortedNonbombers.Length; i++)
                {
                    Ship s = sortedNonbombers[i];
                    ships.Add(s);
                    totalStr += s.BaseStrength;
                    if (totalStr > neededStr)
                        break;
                }
            }
            
            return ships.Count > 0;
        }

        public int NumShipsInFleet(Fleet fleet)
        {
            return fleet?.Ships.Count ?? 0;
        }

        public int NumBombersInFleet(Fleet fleet)
        {
           return fleet?.Ships.Count(s => s.DesignRole == RoleName.bomber) ?? 0;
        }

        public GoalStep ReleaseFleet(Fleet fleet, GoalStep goalStep)
        {
            if (fleet != null)
            {
                fleet.FleetTask?.DisbandTaskForce();
                fleet.FleetTask?.EndTask();
            }

            return goalStep;
        }

        public bool GetClosestPortal(Vector2 position, out Ship portal)
        {
            portal = null;
            if (!GetPortals(out Ship[] portals))
                return false;

            portal = portals.FindMin(s => s.Position.Distance(position));
            return portal != null;
        }

        public void OrderEscortPortal(Ship portal)
        {
            if (portal.System == null)
                return;

            for (int i = 0; i < portal.System.ShipList.Count; i++)
            {
                Ship ship = portal.System.ShipList[i];
                if (!ship.IsPlatformOrStation && ship.IsGuardian && !ship.InCombat && ship.AI.EscortTarget == null && ship.Fleet == null)
                {
                    ship.AI.AddEscortGoal(portal);
                    if (ship.OrdnancePercent < 1)
                        ship.ChangeOrdnance(2);
                }
            }
        }

        public void InitTargetEmpireDefenseActions(Planet planet, float starDateEta, float str)
        {
            if (planet.Owner == null || planet.Owner.IsFaction)
                return;

            if (planet.OwnerIsPlayer) // Warn the player if able
            {
                SolarSystem system = planet.System;
                if (system.PlanetList.Any(p => p.Owner == Universe.Player
                                               && p.HasBuilding(b => b.DetectsRemnantFleet)))
                {
                    string message = $"Remnant Fleet is targeting {planet.Name}\nETA - Stardate {starDateEta.String(1)}";
                    Universe.Notifications.AddIncomingRemnants(planet, message);
                }
            }
            else  // AI scramble defense
            {
                var task = MilitaryTask.CreateDefendVsRemnant(planet, planet.Owner, str);
                planet.Owner.AI.AddPendingTask(task);
            }
        }

        public bool TargetNextPlanet(Empire targetEmpire, Planet currentPlanet, int numBombers, out Planet nextPlanet)
        {
            nextPlanet = null;
            if (targetEmpire == null)
                return false;

            if (numBombers > 0 && currentPlanet.System.HasPlanetsOwnedBy(targetEmpire))
            {
                // Choose another planet owned by target empire in the same system
                nextPlanet = Owner.Random.ItemFilter(currentPlanet.System.PlanetList, p => p.Owner == targetEmpire);
                return true;
            }

            // Find a planet in another system
            var potentialPlanets = targetEmpire.GetPlanets().Filter(p => p.System != currentPlanet.System);
            if (potentialPlanets.Length == 0)
                return false;

            int numPlanetsToTake = Level <= 10 
                ? Level.UpperBound(10)  // Increase distance spread of checks by level
                : (MaxLevel - Level).Clamped(2, 10); // Decrease spread to focus on quality targets

            if (Level < 10) // Level 9 or below will go for closest planets to the portal
                nextPlanet = GetTargetPlanetByDistance(potentialPlanets, currentPlanet.Position, numPlanetsToTake);
            else // Remnants higher than level 9 will go after high level planets
                nextPlanet = GetTargetPlanetByPop(potentialPlanets, numPlanetsToTake);

            return nextPlanet != null;
        }

        Planet GetTargetPlanetByPop(Planet[] potentialPlanets, int numPlanetsToTake)
        {
            numPlanetsToTake = Math.Min(potentialPlanets.Length, numPlanetsToTake);
            if (numPlanetsToTake <= 0)
                return null;
            var planets = potentialPlanets.SortedDescending(p => p.MaxPopulation).AsSpan(0, numPlanetsToTake);
            return Owner.Random.Item(planets);
        }

        // Will be used in Future Remnant Stories
        Planet GetTargetPlanetHomeWorlds(Planet[] potentialPlanets, int numPlanetsToTake)
        {
            Planet target = Owner.Random.ItemFilter(potentialPlanets, p => p.HasCapital);
            return target ?? GetTargetPlanetByPop(potentialPlanets, numPlanetsToTake);
        }

        Planet GetTargetPlanetByDistance(Planet[] potentialPlanets, Vector2 pos, int numPlanetsToTake)
        {
            numPlanetsToTake = Math.Min(potentialPlanets.Length, numPlanetsToTake);
            if (numPlanetsToTake <= 0)
                return null;
            var planets = potentialPlanets.Sorted(p => p.Position.SqDist(pos)).AsSpan(0, numPlanetsToTake);
            return Owner.Random.Item(planets);
        }

        public float RequiredAttackFleetStr(Empire targetEmpire)
        {
            float strMultiplier = !targetEmpire.isPlayer ? 1 : 1 + (int)Owner.Universe.P.Difficulty*0.334f; // 1, 1.33, 1.66, ~2
            float str = targetEmpire.OffensiveStrength * strMultiplier 
                * Owner.GetFleetStrEmpireMultiplier(targetEmpire) / Owner.DifficultyModifiers.RemnantStrModifier;
            float effectiveLevel = Level * strMultiplier;
            return str.Clamped(min: Level * Level * 1000 * strMultiplier,
                               max: str * effectiveLevel / MaxLevel);
        }

        public bool HostileTargetingSystem(SolarSystem solarSystem)
        {
            if (Story == RemnantStory.AncientHelpers)
                return false; 

            return Owner.AnyActiveFleetsTargetingSystem(solarSystem);
        }

        public void CallGuardians(Ship portal) // One guarding from each relevant system
        {
            foreach (SolarSystem system in portal.Universe.Systems)
            {
                Ship chosenGuardian = Owner.Random.ItemFilter(system.ShipList,
                    s => s is { IsGuardian: true, InCombat: false, BaseStrength: < 1000 }
                );
                chosenGuardian?.AI.AddEscortGoal(portal);
            }
        }

        public int GetNumBombersNeeded(Planet planet)
        {
            if (Level == 1)
                return 0;

            RemnantShipType bomberType = GetBomberType(out int numBombers);
            int shieldDiv;
            switch (bomberType)
            {
                default:
                case RemnantShipType.BomberLight:  shieldDiv = 100; break;
                case RemnantShipType.BomberMedium: shieldDiv = 200; break;
                case RemnantShipType.Bomber:       shieldDiv = 400; break;
            }

            int extraBombers = (int)(planet.ShieldStrengthMax / shieldDiv);
            return (numBombers + extraBombers).UpperBound(Level*2);
        }

        RemnantShipType GetBomberType(out int numBombers)
        {
            numBombers = (int)(Level * Owner.DifficultyModifiers.RemnantNumBombers);

            if (Level <= 5)
            {
                numBombers *= (Level-1).LowerBound(1);
                return RemnantShipType.BomberLight;
            }

            if (Level <= 9)
                return RemnantShipType.BomberMedium;

            // Level 10 and above
            numBombers /= 2;
            return RemnantShipType.Bomber;
        }

        public void DisbandDefenseFleet(Fleet fleet)
        {
            float totalCost = 0;
            foreach (Ship ship in fleet.Ships)
            {
                totalCost += ship.GetCost() * ship.HealthPercent;
                ship.EmergeFromPortal();
                ship.QueueTotalRemoval();
            }

            AddToDefenseProduction(totalCost * 0.95f);
        }

        public bool TryCreatePortal()
        {
            if (CreatePortal(Universe, out Ship portal, out string systemName))
            {
                Owner.AI.AddGoal(new RemnantPortal(Owner, portal, systemName));
                return true;
            }

            return false;
        }

        bool CreatePortal(UniverseState u, out Ship portal, out string systemName)
        {
            portal             = null;
            SolarSystem system = null;
            systemName         = "";

            if (!GetRadiatingStars(u, out SolarSystem[] systems)) // Prefer stars which emit radiation
                if (!GetLoneSystem(u, out system, includeReseachable: true)) // Try a lone system
                    if (!GetUnownedNormalSystems(u, out systems)) // Fallback to any unowned system
                        return false; // Could not find a spot

            system ??= Owner.Random.Item(systems);

            Vector2 pos = system.Position.GenerateRandomPointOnCircle(20000, Random);
            systemName  = system.Name;
            return SpawnShip(RemnantShipType.Portal, pos, out portal);
        }

        public float GetHostileStrInPortalSystem(Ship portal)
        {
            return portal.System != null 
                ? Owner.Threats.GetHostileStrengthAt(portal.System.Position, portal.System.Radius)
                : Owner.Threats.GetHostileStrengthAt(portal.Position, portal.SensorRange);
        }

        public bool CreateShip(Ship portal, bool needBomber, int numShips, out Ship ship)
        {
            ship = null;
            RemnantShipType type = needBomber ? GetBomberType(out _) : SelectShipForCreation(Level, numShips);
            if (!ShipCosts.TryGetValue(type, out float cost) || Production < cost)
                return false;

            if (!SpawnShip(type, portal.Position, out ship))
                return false;

            Production -= cost;
            ship.EmergeFromPortal();
            return true;
        }

        public bool CreateDefenseShips(Vector2 pos, out Array<Ship> ships)
        {
            ships = new();
            int level = (Level * 4).UpperBound(40);
            int numShips = 0;
            while (DefenseProduction > 0)
            {
                RemnantShipType type = SelectShipForCreation(level, numShips);
                if (!ShipCosts.TryGetValue(type, out float cost) || DefenseProduction < cost)
                {
                    // reduce level if we dont have enough Defense Production to spawn ship
                    if (--level == 0)
                        break;

                    continue;
                }

                if (SpawnShip(type, pos.GenerateRandomPointInsideCircle(15_000, Owner.Random), out Ship ship))
                {
                    numShips += 1;
                    AddToDefenseProduction(-cost);
                    ships.Add(ship);
                    continue;
                }
            }

            return ships.Count > 0;
        }

        void CalculateShipCosts()
        {
            AddShipCost(Owner.data.RemnantFighter,        RemnantShipType.Fighter);
            AddShipCost(Owner.data.RemnantCorvette,       RemnantShipType.Corvette);
            AddShipCost(Owner.data.RemnantBeamCorvette,   RemnantShipType.BeamCorvette);
            AddShipCost(Owner.data.RemnantBattleCorvette, RemnantShipType.BattleCorvette);
            AddShipCost(Owner.data.RemnantSupportSmall,   RemnantShipType.SmallSupport);
            AddShipCost(Owner.data.RemnantAssimilator,    RemnantShipType.Assimilator);
            AddShipCost(Owner.data.RemnantTorpedoCruiser, RemnantShipType.TorpedoCruiser);
            AddShipCost(Owner.data.RemnantCruiser,        RemnantShipType.Cruiser);
            AddShipCost(Owner.data.RemnantCarrier,        RemnantShipType.Carrier);
            AddShipCost(Owner.data.RemnantMotherShip,     RemnantShipType.Mothership);
            AddShipCost(Owner.data.RemnantExterminator,   RemnantShipType.Exterminator);
            AddShipCost(Owner.data.RemnantInhibitor,      RemnantShipType.Inhibitor);
            AddShipCost(Owner.data.RemnantBattleship,     RemnantShipType.BattleShip);
            AddShipCost(Owner.data.RemnantBomber,         RemnantShipType.Bomber);
            AddShipCost(Owner.data.RemnantFrigate,        RemnantShipType.Frigate);
            AddShipCost(Owner.data.RemnantBomberLight,    RemnantShipType.BomberLight);
            AddShipCost(Owner.data.RemnantBomberMedium,   RemnantShipType.BomberMedium);
            AddShipCost(Owner.data.RemnantBehemoth,       RemnantShipType.Behemoth);
        }

        void AddShipCost(string shipName, RemnantShipType type)
        {
            if (shipName.IsEmpty() || !ResourceManager.GetShipTemplate(shipName, out Ship ship))
            {
                Log.Warning($"Could not find a ship named '{shipName}' in {Owner.Name}'s race xml.");
                return;
            }

            ShipCosts.Add(type, ship.ShipData.BaseCost);
        }

        RemnantShipType SelectShipForCreation(int level, int shipsInFleet) // Note Bombers are created exclusively 
        {
            int ShipGroupSize()
            {
                switch (Universe.P.Difficulty)
                {
                    default:
                    case GameDifficulty.Normal: return 20;
                    case GameDifficulty.Hard:   return 15;
                    case GameDifficulty.Brutal: return 12;
                    case GameDifficulty.Insane: return 10;
                }
            }

            int shipGroupSize  = ShipGroupSize() - (Level / 2).LowerBound(3);
            int fleetModifier  = shipsInFleet / shipGroupSize;
            int effectiveLevel = (level + (int)Universe.P.Difficulty + fleetModifier).UpperBound(level *2);
            int roll           = shipsInFleet.LowerBound(1) % shipGroupSize != 0 
                ? Random.RollDie(effectiveLevel, (fleetModifier + level / 2).LowerBound(1))
                : effectiveLevel; // get the biggest we can per complete group

            switch (roll)
            {
                case 1:
                case 2:  return RemnantShipType.Fighter;
                case 3:  
                case 4:  
                case 5:
                    switch (Random.RollDie(4))
                    {
                        default:
                        case 1: return RemnantShipType.SmallSupport;
                        case 2: return RemnantShipType.Corvette;
                        case 3: return RemnantShipType.BeamCorvette;
                        case 4: return RemnantShipType.BattleCorvette;
                    }
                case 6:  
                case 7:  return RemnantShipType.Frigate;
                case 8:  
                case 9:  
                case 10: 
                case 11: 
                    switch (Random.RollDie(3))
                    {
                        default:
                        case 1: return RemnantShipType.Cruiser;
                        case 2: return RemnantShipType.TorpedoCruiser;
                        case 3: return RemnantShipType.Assimilator;
                    }
                case 12:
                case 13:
                    switch (Random.RollDie(2))
                    {
                        default:
                        case 1: return RemnantShipType.Inhibitor;
                        case 2: return RemnantShipType.BattleShip;
                    }
                case 14: 
                case 15: 
                case 16: 
                case 17: 
                case 18:
                case 19: 
                    switch (Random.RollDie(3))
                    {
                        default:
                        case 1: return RemnantShipType.Mothership;
                        case 2: return RemnantShipType.Exterminator;
                        case 3: return RemnantShipType.Carrier;
                    }
                case 20: 
                default: return Universe.P.Difficulty == GameDifficulty.Normal && !Random.RollDice(10) 
                        ?  RemnantShipType.Exterminator
                        :  RemnantShipType.Behemoth;
            }
        }

        bool SpawnShip(RemnantShipType shipType, Vector2 where, out Ship remnantShip)
        {
            remnantShip = null;
            string shipName;
            switch (shipType)
            {
                default:
                case RemnantShipType.Fighter:        shipName = Owner.data.RemnantFighter;        break;
                case RemnantShipType.Corvette:       shipName = Owner.data.RemnantCorvette;       break;
                case RemnantShipType.BeamCorvette:   shipName = Owner.data.RemnantBeamCorvette;   break;
                case RemnantShipType.BattleCorvette: shipName = Owner.data.RemnantBattleCorvette; break;
                case RemnantShipType.SmallSupport:   shipName = Owner.data.RemnantSupportSmall;   break;
                case RemnantShipType.Assimilator:    shipName = Owner.data.RemnantAssimilator;    break;
                case RemnantShipType.Carrier:        shipName = Owner.data.RemnantCarrier;        break;
                case RemnantShipType.Mothership:     shipName = Owner.data.RemnantMotherShip;     break;
                case RemnantShipType.Exterminator:   shipName = Owner.data.RemnantExterminator;   break;
                case RemnantShipType.Portal:         shipName = Owner.data.RemnantPortal;         break;
                case RemnantShipType.Bomber:         shipName = Owner.data.RemnantBomber;         break;
                case RemnantShipType.Inhibitor:      shipName = Owner.data.RemnantInhibitor;      break;
                case RemnantShipType.BattleShip:     shipName = Owner.data.RemnantBattleship;     break;
                case RemnantShipType.Frigate:        shipName = Owner.data.RemnantFrigate;        break;
                case RemnantShipType.BomberLight:    shipName = Owner.data.RemnantBomberLight;    break;
                case RemnantShipType.BomberMedium:   shipName = Owner.data.RemnantBomberMedium;   break;
                case RemnantShipType.Cruiser:        shipName = Owner.data.RemnantCruiser;        break;
                case RemnantShipType.TorpedoCruiser: shipName = Owner.data.RemnantTorpedoCruiser; break;
                case RemnantShipType.Behemoth:       shipName = Owner.data.RemnantBehemoth;       break;
            }

            if (shipName.NotEmpty())
            {
                remnantShip = Ship.CreateShipAtPoint(Universe, shipName, Owner, where);
                if (remnantShip == null)
                    Log.Warning($"Could not spawn required Remnant ship named {shipName} for {Owner.Name}, check race xml");
                else
                    remnantShip.IsGuardian = true; // All Remnant ships are Guardians, also used when filtering original remnant ships
            }
            else
            {
                Log.Warning($"Remnant ship name was empty for {Owner.Name}, check race xml for typos");
            }

            return remnantShip != null;
        }

        public void DebugSpawnRemnant(InputState input, Vector2 pos)
        {
            if (input.EmpireToggle)
            {
                RemnantShipType shipType = input.RemnantToggle ? RemnantShipType.Exterminator : RemnantShipType.Mothership;
                SpawnShip(shipType, pos, out _);
            }
            else
            {
                Ship ship = Ship.CreateShipAtPoint(Universe, "Target Dummy", Owner, pos);
                if (ship == null)
                    Log.Warning("Could not spawn `Target Dummy` ship, it does not exist");
            }
        }

        public bool Hibernating => HibernationTurns > 0;

        public void GenerateProduction(float amount)
        {
            if (Hibernating)
                amount *= 0.2f;

            if (DefenseProduction < MaxDefenseProduction)
            {
                AddToDefenseProduction(amount * 0.1f);
                amount *= 0.9f;
            }

            Production += amount;
            float excess = Production - ProductionLimit;
            if (excess > 0)
                AddToDefenseProduction(excess);
            
            Production = Production.UpperBound(ProductionLimit);
        }

        void AddToDefenseProduction(float amount)
        {
            DefenseProduction = (DefenseProduction + amount).Clamped(0, MaxDefenseProduction);
        }

        public int NumPortals()
        {
            return Owner.OwnedShips.Count(s => s.Name == Owner.data.RemnantPortal && s.Active);
        }

        public bool RerouteGoalPortals(out Ship newPortal)
        {
            newPortal = null;
            if (!GetPortals(out Ship[] portals))
                return false;

            newPortal = portals.First();
            return newPortal != null;
        }

        public bool GetPortals(out Ship[] portals)
        {
            portals = Owner.OwnedShips.Filter(s => s.Name == Owner.data.RemnantPortal && s.Active);
            return portals.Length > 0;
        }

        float PlanetQuality(Planet planet)
        {
            float fertilityMod = 1;
            float richnessMod  = 1;
            if (Universe.Player.IsCybernetic)
            {
                fertilityMod = 0.5f;
                richnessMod  = planet.IsBarrenType ? 6f : 3f;
            }

            float quality = planet.BaseFertility * fertilityMod 
                            + planet.MineralRichness * richnessMod 
                            + planet.MaxPopulationBillionFor(Owner);

            // Boost the quality score for planets that are very rich
            if (planet.MineralRichness > 1.5f)
                quality += 2;

            if (planet.BaseFertility > 1.5f)
                quality += 2;

            return quality;
        }

        public void GenerateRemnantPresence(Planet p)
        {
            if (p.System.IsStartingSystem || p.System.PiratePresence)
                return; // Don't create Remnants on starting systems or Pirate systems

            float quality   = PlanetQuality(p);
            int dieModifier = (int)Universe.P.Difficulty * 5 - 10; // easy -10, brutal +5
            if (Story != RemnantStory.None)
                dieModifier -= 5;

            int d100 = Random.RollDie(100) + dieModifier;
            switch (Universe.P.ExtraRemnant) // Refactored by FB (including all remnant methods)
            {
                case ExtraRemnantPresence.VeryRare:   VeryRarePresence(quality, d100, p);   break;
                case ExtraRemnantPresence.Rare:       RarePresence(quality, d100, p);       break;
                case ExtraRemnantPresence.Normal:     NormalPresence(quality, d100, p);     break;
                case ExtraRemnantPresence.More:       MorePresence(quality, d100, p);       break;
                case ExtraRemnantPresence.MuchMore:   MuchMorePresence(quality, d100, p);   break;
                case ExtraRemnantPresence.Everywhere: EverywherePresence(quality, d100, p); break;
            }
        }

        void VeryRarePresence(float quality, int d100, Planet p)
        {
            if (quality > 12f && d100 >= 70)
                AddMinorFleet(p);
        }

        void RarePresence(float quality, int d100, Planet p)
        {
            if (quality > 12f && d100 >= 60)
                AddMajorFleet(p); // RedFox, changed the rare remnant to Major
        }

        void NormalPresence(float quality, int d100, Planet p)
        {
            if (quality > 18f)
            {
                if (d100 >= 10) AddMajorFleet(p);
                if (d100 >= 30) AddSupportShips(p);
                if (d100 >= 50) AddMajorFleet(p);
                if (d100 >= 70) AddFrigates(p);
            }
            if (quality > 15f)
            {
                if (d100 >= 20) AddMinorFleet(p);
                if (d100 >= 40) AddMajorFleet(p);
                if (d100 >= 60) AddSupportShips(p);
                if (d100 >= 80) AddFrigates(p);
            }
            else if (quality > 10f)
            {
                if (d100 >= 50) AddMinorFleet(p);
                if (d100 >= 60) AddMiniFleet(p);
                if (d100 >= 70) AddSupportShips(p);
                if (d100 >= 85) AddMajorFleet(p);
            }
            else if (quality > 6f)
            {
                if (d100 >= 50) AddMiniFleet(p);
                if (d100 >= 60) AddMinorFleet(p);
                if (d100 >= 70) AddSupportShips(p);
                if (d100 >= 85) AddMinorFleet(p);
            }
        }

        void MorePresence(float quality, int d100, Planet p)
        {
            NormalPresence(quality, Random.RollDie(100), p);
            if (quality >= 15f)
            {
                if (d100 >= 25) AddMinorFleet(p);
                if (d100 >= 45) AddMajorFleet(p);
                if (d100 >= 65) AddSupportShips(p);
                if (d100 >= 95) AddCarriers(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 45) AddMinorFleet(p);
                if (d100 >= 65) AddSupportShips(p);
                if (d100 >= 95) AddMajorFleet(p);
            }
            else if (quality >= 8f && d100 >= 50)
                AddMinorFleet(p);
        }

        void MuchMorePresence(float quality, int d100, Planet p)
        {
            MorePresence(quality, Random.RollDie(100), p);
            if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 > 10) AddMinorFleet(p);
                if (d100 > 20) AddSupportShips(p);
                if (d100 > 75) AddCarriers(p);
                if (d100 > 90) AddFrigates(p);
            }
            else if (quality >= 12f)
            {
                if (d100 >= 25) AddMinorFleet(p);
                if (d100 >= 30) AddSupportShips(p);
                if (d100 >= 45) AddMinorFleet(p);
                if (d100 >= 80) AddMiniFleet(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 25) AddMinorFleet(p);
                if (d100 >= 50) AddSupportShips(p);
                if (d100 >= 75) AddMajorFleet(p);
            }
            else if (quality >= 8f)
            {
                if (d100 >= 50) AddMinorFleet(p);
                if (d100 >= 75) AddMiniFleet(p);
            }
        }

        void EverywherePresence(float quality, int d100, Planet p)
        {
            MuchMorePresence(quality, Random.RollDie(100), p);
            if (quality >= 18f)
            {
                AddMajorFleet(p);
                AddMinorFleet(p);
                AddSupportShips(p);
                if (d100 >= 50) AddCarriers(p);
                if (d100 >= 70) AddFrigates(p);
                if (d100 >= 90) AddCarriers(p);
            }
            else if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 >= 40) AddSupportShips(p);
                if (d100 >= 60) AddCarriers(p);
                if (d100 >= 80) AddFrigates(p);
                if (d100 >= 95) AddCarriers(p);
            }
            else if (quality >= 12f)
            {
                AddMinorFleet(p);
                if (d100 >= 50) AddSupportShips(p);
                if (d100 >= 90) AddCarriers(p);
            }
            else if (quality >= 10f)
            {
                if (d100 >= 30) AddMinorFleet(p);
                if (d100 >= 50) AddMiniFleet(p);
                if (d100 >= 70) AddSupportShips(p);
            }
            else if (quality >= 8f)
            {
                if (d100 >= 50) AddMiniFleet(p);
                if (d100 >= 90) AddMiniFleet(p);
            }
            if (quality > 6f && d100 > 50)
                AddMiniFleet(p);
        }

        void AddMajorFleet(Planet p)
        {
            AddMinorFleet(p);
            if (Random.RollDice(50))
                AddMinorFleet(p);

            if (Random.RollDice(25))
                AddMinorFleet(p);

            if (Random.RollDice(15))
                AddCorvetteScreen(p);

            if (Random.RollDice(10))
                AddFrigates(p);

            if (Random.RollDice(5))
                AddGuardians(1, RemnantShipType.Assimilator, p);

            AddBombers(RemnantShipType.BomberMedium, 1, p);
        }

        void AddCorvetteScreen(Planet p) 
        {
            AddGuardians(Random.RollDie(5), RemnantShipType.Corvette, p);
            if (Random.RollDice(25))
                AddGuardians(Random.RollDie(3), RemnantShipType.BeamCorvette, p);

            if (Random.RollDice(25))
                AddGuardians(Random.RollDie(3), RemnantShipType.BattleCorvette, p);
        }

        void AddMinorFleet(Planet p)
        {
            int numXenoFighters = Random.RollDie(5) + 1;
            int numDrones = Random.RollDie(3);

            AddGuardians(numXenoFighters, RemnantShipType.Fighter, p);
            AddGuardians(numDrones, RemnantShipType.Corvette, p);
            AddBombers(RemnantShipType.BomberLight, 2, p);
            if (Random.RollDice(10))
                AddGuardians(1, RemnantShipType.BattleCorvette, p);
            if (Random.RollDice(10))
                AddGuardians(1, RemnantShipType.BeamCorvette, p);
        }

        void AddMiniFleet(Planet p)  //Added by Gretman
        {
            int numXenoFighters = Random.RollDie(3);
            AddGuardians(numXenoFighters, RemnantShipType.Fighter, p);
            AddBombers(RemnantShipType.BomberLight, 1, p);
            if (Random.RollDice(10)) 
                AddGuardians(1, RemnantShipType.BeamCorvette, p);
            else
                AddGuardians(1, RemnantShipType.Corvette, p);
        }

        void AddSupportShips(Planet p)  //Added by Gretman
        {
            int numSupportDrones = Random.RollDie(4);
            AddBombers(RemnantShipType.BomberLight, 3, p);
            AddGuardians(numSupportDrones, RemnantShipType.SmallSupport, p);

            if (Random.RollDice(10))
                AddGuardians(1, RemnantShipType.BattleCorvette, p);
        }

        void AddCarriers(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Carrier, p);
            if (Random.RollDice(20)) // 20% chance for another carrier
                AddGuardians(1, RemnantShipType.Carrier, p);

            AddBombers(RemnantShipType.Bomber, 1, p);
        }

        void AddFrigates(Planet p)  //Added by Gretman
        {
            AddGuardians(Random.RollDie(2), RemnantShipType.Frigate, p);
            if (Random.RollDice(25))
                AddCorvetteScreen(p);
            if (Random.RollDice(10)) 
                AddGuardians(1, RemnantShipType.TorpedoCruiser, p);
        }

        void AddBombers(RemnantShipType bomberType, int numShips, Planet p)
        {
            if (Universe.P.Difficulty is GameDifficulty.Normal)
                return;

            int numBombers = Random.RollDie(numShips);
            AddGuardians(numBombers, bomberType, p);
        }

        void AddGuardians(int numShips, RemnantShipType type, Planet p)
        {
            int divider = 20 + (10 * (int)Universe.P.Difficulty);  // harder game = earlier activation (20,30,40,50)
            for (int i = 0; i < numShips; ++i)
            {
                Vector2 pos = p.Position.GenerateRandomPointInsideCircle(p.Radius * 2, Random);
                if (SpawnShip(type, pos, out Ship ship))
                {
                    ship.OrderToOrbit(p, clearOrders:true, MoveOrder.Aggressive);
                    p.System.NewGameAddRemnantShipToList(ship);
                    ActivationXpNeeded += (ShipRole.GetExpSettings(ship).KillExp / divider) * StoryTurnsLevelUpModifier();
                }
            }
        }

        RemnantStory InitAndPickStory(EmpireAI ai)
        {
            ai.AddGoal(new RemnantInit(Owner));
            if (Universe.P.DisableRemnantStory)
                return RemnantStory.None;

            float roll = Random.RollDie(100);
            if (roll <= 10)  return RemnantStory.AncientPeaceKeepers;
            if (roll <= 20)  return RemnantStory.AncientWarMongers;
            if (roll <= 40)  return RemnantStory.AncientExterminators;
            if (roll <= 60)  return RemnantStory.AncientRaidersRandom;
            if (roll <= 70)  return RemnantStory.AncientHelpers;

            return RemnantStory.AncientBalancers;
        }

        public enum RemnantStory
        {
            None,
            AncientBalancers,
            AncientExterminators,
            AncientRaidersRandom,
            AncientWarMongers,
            AncientPeaceKeepers,
            AncientHelpers
        }
    }

    public enum RemnantShipType
    {
        Fighter,
        Corvette,
        SmallSupport,
        Carrier,
        Assimilator,
        TorpedoCruiser,
        Mothership,
        Exterminator,
        Inhibitor,
        Portal,
        Bomber,
        Frigate,
        BomberLight,
        BomberMedium,
        Cruiser,
        Behemoth,
        BeamCorvette,
        BattleCorvette,
        BattleShip
    }
}
