using System;
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

namespace Ship_Game
{
    using static RandomMath;
    using static HelperFunctions;

    [StarDataType]
    public class Remnants
    {
        public const int MaxLevel = 20;
        [StarData] public readonly Empire Owner;
        public UniverseState Universe => Owner.Universe ?? throw new NullReferenceException("Remnants.Owner.Universe must not be null");

        [StarData] public float StoryTriggerKillsXp { get; private set; }
        [StarData] public float PlayerStepTriggerXp { get; private set; }
        [StarData] public float NextLevelUpDate { get; private set; }
        [StarData] public bool Activated { get; private set; }
        [StarData] public RemnantStory Story { get; private set; }
        [StarData] public float Production { get; private set; }
        [StarData] public int Level { get; private set; } = 1;
        [StarData] public Map<RemnantShipType, float> ShipCosts { get; private set; } = new();
        [StarData] public int StoryStep { get; private set; } = 1;
        [StarData] public bool OnlyRemnantLeft { get; private set; }
        [StarData] public int HibernationTurns { get; private set; } // Remnants will not attack or gain production if above 0
        [StarData] public float ActivationXpNeeded { get; private set; } // xp of killed Remnant ships needed to for story activation

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

        public void IncrementKills(Empire empire, int xp)
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

                    StoryStep += 1;
                }
            }
            
            //if (!Activated) // todo for testing
            //    Activate();
        }

        float StepXpTrigger => (ShipRole.GetMaxExpValue() * StoryStep * StoryStep * 0.5f).UpperBound(ActivationXpNeeded);

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
                    Owner.AI.AddGoal(new RemnantEngagements(Owner));
                    Universe.Notifications.AddRemnantsStoryActivation(Owner);
                    break;
            }

        }

        void NotifyPlayerOnLevelUp()
        {
            float espionageStr = Universe.Player.GetSpyDefense();
            if (espionageStr <= Level * 3)
                return; // not enough espionage strength to learn about Remnant activities

            Universe.Notifications.AddRemnantsAreGettingStronger(Owner);
        }

        public bool TryLevelUpByDate(out int newLevel)
        {
            newLevel = 0;
            if (Universe.StarDate.GreaterOrEqual(NextLevelUpDate))
            {
                int turnsLevelUp = TurnsLevelUp + ExtraLevelUpEffort;
                turnsLevelUp     = (int)(turnsLevelUp * StoryTurnsLevelUpModifier() * Universe.ProductionPace);
                HibernationTurns = 0;
                NextLevelUpDate += turnsLevelUp / 10f;

                if (Level < MaxLevel)
                {
                    Log.Info(ConsoleColor.Green, $"---- Remnants: Level up to level {Level+1}. Next level up in Stardate {NextLevelUpDate} ----");
                    if (Universe.StarDate.Less(NextLevelUpDate)) // do not notify on multiple initial level ups
                        NotifyPlayerOnLevelUp();
                }

                SetLevel(Level + 1);
                newLevel = Level;
                Upgrade();
                return true;
            }

            CheckHibernation();
            return false;
        }

        void Upgrade()
        {
            if (Level % 2 == 0)
                Owner.data.ShieldPowerMod += 0.1f;

            if (Level % 4 == 0)
                Owner.data.Traits.ModHpModifier += 0.1f;

            if (Level % 6 == 0)
                Owner.data.ArmorPiercingBonus += 1;

            if (Level % 8 == 0)
                Owner.data.ExplosiveRadiusReduction += 0.15f;

            Owner.data.BaseShipLevel = Level / 3;
            EmpireHullBonuses.RefreshBonuses(Owner);
        }

        void CheckHibernation() // Start Hibernation some time before leveling up
        {
            float hibernationDate = (NextLevelUpDate - NeededHibernationTurns / 10f).RoundToFractionOf10();
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
                case RemnantStory.AncientBalancers:     return 1f;
                case RemnantStory.AncientRaidersRandom: return 0.9f;
                default:                                return 1;
            }
        }

        int TurnsLevelUp                  => Owner.DifficultyModifiers.RemnantTurnsLevelUp;
        int ExtraLevelUpEffort            => (int)((Level-1) * Universe.RemnantPaceModifier + NeededHibernationTurns);
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
                        CreatePortal();
                    break;
                case RemnantStory.AncientExterminators:
                    if (!GetPortals(out Ship[] portals))
                        return;
                    Ship portal = portals.RandItem();
                    for (int i = 0; i < ((int)Universe.P.Difficulty + 1) * 3; i++)
                    {
                        if (!SpawnShip(RemnantShipType.Exterminator, portal.Position, out _))
                            return;
                    }

                    break;
            }
        }

        public bool CanDoAnotherEngagement()
        {
            if (Hibernating)
                return false;

            if (Owner.AI.HasGoal(g => g.IsRaid && g is FleetGoal))
                return false;  // Limit building fleet to 1 at a time

            int ongoingRaids = Owner.AI.CountGoals(g => g.IsRaid);
            return ongoingRaids < NumPortals();
        }

        public bool FindValidTarget(out Empire target)
        {
            var empiresList = GlobalStats.RestrictAIPlayerInteraction 
                                 ? Universe.ActiveNonPlayerMajorEmpires
                                 : Universe.ActiveMajorEmpires;

            target = null;
            if (empiresList.Length == 0)
                return false;

            switch (Story)
            {
                case RemnantStory.AncientBalancers:     target = FindStrongestByAveragePopAndStr(empiresList); break;
                case RemnantStory.AncientExterminators: target = FindWeakestEmpire(empiresList);               break;
                case RemnantStory.AncientRaidersRandom: target = empiresList.RandItem();                       break;
            }

            return target != null;
        }

        public bool TargetEmpireStillValid(Empire currentTarget, Ship portal, bool checkOnlyDefeated = false)
        {
            if (Hibernating)
                return false;

            if (checkOnlyDefeated && !currentTarget.IsDefeated)
                return true;

            FindValidTarget(out Empire expectedTarget);
            return expectedTarget == currentTarget;
        }

        Empire FindStrongestByAveragePopAndStr(Empire[] empiresList)
        {
            if (empiresList.Length == 1)
                return empiresList.First();

            var averagePop    = empiresList.Average(e => e.TotalPopBillion);
            var averageStr    = empiresList.Average(e => e.CurrentMilitaryStrength);
            Empire bestEmpire = null;
            float ratioOverAverage = 0;
            foreach (Empire empire in empiresList)
            {
                float ratio = (empire.TotalPopBillion / averagePop +
                               empire.CurrentMilitaryStrength / averageStr) * 0.5f;
                if (ratio > ratioOverAverage)
                {
                    ratioOverAverage = ratio;
                    bestEmpire = empire;
                }
            }


            return bestEmpire != null && ratioOverAverage > 1.25f ? bestEmpire : null;
        }

        Empire FindWeakestEmpire(Empire[] empiresList)
        {
            if (empiresList.Length == 1)
                return empiresList.First();

            var potentialTargets = empiresList.Filter(e =>
            {
                var planets = e.GetPlanets();
                return planets.Count > 1 || planets.Count == 1 && Level > planets[0].Level + 1;
            });

            return potentialTargets.Length == 0 ? null : potentialTargets.FindMin(e => e.CurrentMilitaryStrength);
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
                                                                    && !s.InCombat);
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
                for (int i = 0; i < nonBombers.Length; i++)
                {
                    Ship s = nonBombers[i];
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
            if (fleet == null)
                return goalStep;

            fleet.FleetTask?.DisbandTaskForce(fleet);
            fleet.FleetTask?.EndTask();
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
                    ship.AI.AddEscortGoal(portal);
            }
        }

        public void InitTargetEmpireDefenseActions(Planet planet, float starDateEta, float str)
        {
            if (planet.Owner == null || planet.Owner.IsFaction)
                return;

            if (planet.OwnerIsPlayer) // Warn the player is able
            {
                SolarSystem system = planet.ParentSystem;
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

            if (numBombers > 0 && currentPlanet.ParentSystem.HasPlanetsOwnedBy(targetEmpire))
            {
                // Choose another planet owned by target empire in the same system
                nextPlanet = currentPlanet.ParentSystem.PlanetList.Filter(p => p.Owner == targetEmpire).RandItem();
                return true;
            }

            // Find a planet in another system
            var potentialPlanets = targetEmpire.GetPlanets().Filter(p => p.ParentSystem != currentPlanet.ParentSystem);
            if (potentialPlanets.Length == 0)
                return false;

            int numPlanetsToTake = Level <= 10 
                ? Level.UpperBound(10)  // Increase distance spread of checks by level
                : (MaxLevel - Level).Clamped(2, 10); // Decrease spread to focus on quality targets

            if (Level <= 5) // Level 5 or below will go for closest planets to the portal
                nextPlanet = GetTargetPlanetByDistance(potentialPlanets, currentPlanet.Position, numPlanetsToTake);
            else // Remnants higher than level 5 will go after high level planets
                nextPlanet = GetTargetPlanetByPop(potentialPlanets, numPlanetsToTake);

            return nextPlanet != null;
        }

        Planet GetTargetPlanetByPop(Planet[] potentialPlanets, int numPlanetsToTake)
        {
            var filteredList = potentialPlanets.SortedDescending(p => p.MaxPopulation).TakeItems(numPlanetsToTake);

            return filteredList.Length > 0 ? filteredList.RandItem() : null;
        }

        // Will be used in Future Remnant Stories
        Planet GetTargetPlanetHomeWorlds(Planet[] potentialPlanets, int numPlanetsToTake)
        {
            var filteredList = potentialPlanets.Filter(p => p.HasCapital);

            return filteredList.Length > 0
                ? filteredList.RandItem()
                : GetTargetPlanetByPop(potentialPlanets, numPlanetsToTake);
        }

        Planet GetTargetPlanetByDistance(Planet[] potentialPlanets, Vector2 pos, int numPlanetsToTake)
        {
            var filteredList = potentialPlanets.Sorted(p => p.Position.Distance(pos)).TakeItems(numPlanetsToTake);

            return filteredList.Length > 0 ? filteredList.RandItem() : null;
        }

        public void CallGuardians(Ship portal) // One guarding from each relevant system
        {
            foreach (SolarSystem system in portal.Universe.Systems)
            {
                var guardians = system.ShipList.Filter(s => s != null && s.IsGuardian && !s.InCombat && s.BaseStrength < 1000);
                if (guardians.Length > 0)
                {
                    Ship chosenGuardian = guardians.RandItem();
                    chosenGuardian.AI.AddEscortGoal(portal);
                }
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

            if (Level <= 6)
            {
                numBombers *= 2;
                return RemnantShipType.BomberLight;
            }

            if (Level <= 11)
                return RemnantShipType.BomberMedium;

            // Level 12 and above
            numBombers /= 2;
            return RemnantShipType.Bomber;
        }

        public bool CreatePortal()
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
                if (!GetLoneSystem(u, out system)) // Try a lone system
                    if (!GetUnownedSystems(u, out systems)) // Fallback to any unowned system
                        return false; // Could not find a spot

            if (system == null)
                system = systems.RandItem();

            Vector2 pos = system.Position.GenerateRandomPointOnCircle(20000);
            systemName  = system.Name;
            return SpawnShip(RemnantShipType.Portal, pos, out portal);
        }

        public bool CreateShip(Ship portal, bool needBomber, int numShips, out Ship ship)
        {
            ship = null;
            RemnantShipType type = needBomber ? GetBomberType(out _) : SelectShipForCreation(numShips);
            if (!ShipCosts.TryGetValue(type, out float cost) || Production < cost)
                return false;

            if (!SpawnShip(type, portal.Position, out ship))
                return false;

            GenerateProduction(-cost);
            ship.EmergeFromPortal();
            return true;
        }

        void CalculateShipCosts()
        {
            AddShipCost(Owner.data.RemnantFighter,        RemnantShipType.Fighter);
            AddShipCost(Owner.data.RemnantCorvette,       RemnantShipType.Corvette);
            AddShipCost(Owner.data.RemnantSupportSmall,   RemnantShipType.SmallSupport);
            AddShipCost(Owner.data.RemnantAssimilator,    RemnantShipType.Assimilator);
            AddShipCost(Owner.data.RemnantTorpedoCruiser, RemnantShipType.TorpedoCruiser);
            AddShipCost(Owner.data.RemnantCruiser,        RemnantShipType.Cruiser);
            AddShipCost(Owner.data.RemnantCarrier,        RemnantShipType.Carrier);
            AddShipCost(Owner.data.RemnantMotherShip,     RemnantShipType.Mothership);
            AddShipCost(Owner.data.RemnantExterminator,   RemnantShipType.Exterminator);
            AddShipCost(Owner.data.RemnantInhibitor,      RemnantShipType.Inhibitor);
            AddShipCost(Owner.data.RemnantBomber,         RemnantShipType.Bomber);
            AddShipCost(Owner.data.RemnantFrigate,        RemnantShipType.Frigate);
            AddShipCost(Owner.data.RemnantBomberLight,    RemnantShipType.BomberLight);
            AddShipCost(Owner.data.RemnantBomberMedium,   RemnantShipType.BomberMedium);
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

        RemnantShipType SelectShipForCreation(int shipsInFleet) // Note Bombers are created exclusively 
        {
            int fleetModifier  = shipsInFleet / 12;
            int effectiveLevel = Level + (int)Universe.P.Difficulty + fleetModifier;
            effectiveLevel     = effectiveLevel.UpperBound(Level * 2);
            int roll           = RollDie(effectiveLevel, (fleetModifier + Level / 2).LowerBound(1));
            switch (roll)
            {
                case 1:
                case 2:  return RemnantShipType.Fighter;
                case 3:
                case 4:  return RemnantShipType.SmallSupport;
                case 5:
                case 6:  return RemnantShipType.Corvette;
                case 7:
                case 8:  return RemnantShipType.Frigate;
                case 9:
                case 10: return RemnantShipType.Cruiser;
                case 11:
                case 12: return RemnantShipType.TorpedoCruiser;
                case 13:
                case 14: return RemnantShipType.Inhibitor;
                case 15:
                case 16: return RemnantShipType.Carrier;
                case 17:
                case 18: return RemnantShipType.Mothership;
                default: return RemnantShipType.Exterminator;
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
                case RemnantShipType.SmallSupport:   shipName = Owner.data.RemnantSupportSmall;   break;
                case RemnantShipType.Assimilator:    shipName = Owner.data.RemnantAssimilator;    break;
                case RemnantShipType.Carrier:        shipName = Owner.data.RemnantCarrier;        break;
                case RemnantShipType.Mothership:     shipName = Owner.data.RemnantMotherShip;     break;
                case RemnantShipType.Exterminator:   shipName = Owner.data.RemnantExterminator;   break;
                case RemnantShipType.Portal:         shipName = Owner.data.RemnantPortal;         break;
                case RemnantShipType.Bomber:         shipName = Owner.data.RemnantBomber;         break;
                case RemnantShipType.Inhibitor:      shipName = Owner.data.RemnantInhibitor;      break;
                case RemnantShipType.Frigate:        shipName = Owner.data.RemnantFrigate;        break;
                case RemnantShipType.BomberLight:    shipName = Owner.data.RemnantBomberLight;    break;
                case RemnantShipType.BomberMedium:   shipName = Owner.data.RemnantBomberMedium;   break;
                case RemnantShipType.Cruiser:        shipName = Owner.data.RemnantCruiser;        break;
                case RemnantShipType.TorpedoCruiser: shipName = Owner.data.RemnantTorpedoCruiser; break;
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

        public void TryGenerateProduction(float amount)
        {
            if (Hibernating)
            {
                HibernationTurns -= 1;
                amount /= 5;
            }

            GenerateProduction(amount);
        }

        void GenerateProduction(float amount)
        {
            int limit = 200 * ((int)Universe.P.Difficulty).LowerBound(1);
            Production = (Production + amount).UpperBound(Level * Level * limit); // Level 20 - 240K 
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
            if (p.ParentSystem.IsStartingSystem || p.ParentSystem.PiratePresence)
                return; // Don't create Remnants on starting systems or Pirate systems

            float quality   = PlanetQuality(p);
            int dieModifier = (int)Universe.P.Difficulty * 5 - 10; // easy -10, brutal +5
            if (Story != RemnantStory.None)
                dieModifier -= 5;

            int d100 = RollDie(100) + dieModifier;
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
                if (d100 >= 70) AddFrigate(p);
            }
            if (quality > 15f)
            {
                if (d100 >= 20) AddMinorFleet(p);
                if (d100 >= 40) AddMajorFleet(p);
                if (d100 >= 60) AddSupportShips(p);
                if (d100 >= 80) AddFrigate(p);
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
            NormalPresence(quality, RollDie(100), p);
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
            MorePresence(quality, RollDie(100), p);
            if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 > 10) AddMinorFleet(p);
                if (d100 > 20) AddSupportShips(p);
                if (d100 > 75) AddCarriers(p);
                if (d100 > 90) AddFrigate(p);
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
            MuchMorePresence(quality, RollDie(100), p);
            if (quality >= 18f)
            {
                AddMajorFleet(p);
                AddMinorFleet(p);
                AddSupportShips(p);
                if (d100 >= 50) AddCarriers(p);
                if (d100 >= 70) AddFrigate(p);
                if (d100 >= 90) AddCarriers(p);
            }
            else if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 >= 40) AddSupportShips(p);
                if (d100 >= 60) AddCarriers(p);
                if (d100 >= 80) AddFrigate(p);
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
            if (RollDice(50))
                AddMinorFleet(p);

            if (RollDice(25))
                AddMinorFleet(p);

            if (RollDice(10))
                AddFrigate(p);

            if (RollDice(5))
                AddGuardians(1, RemnantShipType.Assimilator, p);
        }

        void AddMinorFleet(Planet p)
        {
            int numXenoFighters = RollDie(5) + 1;
            int numDrones = RollDie(3);

            AddGuardians(numXenoFighters, RemnantShipType.Fighter, p);
            AddGuardians(numDrones, RemnantShipType.Corvette, p);
        }

        void AddMiniFleet(Planet p)  //Added by Gretman
        {
            int numXenoFighters = RollDie(3);

            AddGuardians(numXenoFighters, RemnantShipType.Fighter, p);
            AddGuardians(1, RemnantShipType.Corvette, p);
        }

        void AddSupportShips(Planet p)  //Added by Gretman
        {
            int numSupportDrones = RollDie(4);
            AddGuardians(numSupportDrones, RemnantShipType.SmallSupport, p);
        }

        void AddCarriers(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Carrier, p);
            if (RollDice(20)) // 20% chance for another carrier
                AddGuardians(1, RemnantShipType.Carrier, p);
        }

        void AddFrigate(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Frigate, p);
            if (RollDice(10)) // 10% chance a torpedo cruiser
                AddGuardians(1, RemnantShipType.TorpedoCruiser, p);
        }

        void AddGuardians(int numShips, RemnantShipType type, Planet p)
        {
            int divider = 7 * ((int)Universe.P.Difficulty).LowerBound(1); // harder game = earlier activation
            for (int i = 0; i < numShips; ++i)
            {
                Vector2 pos = p.Position.GenerateRandomPointInsideCircle(p.Radius * 2);
                if (SpawnShip(type, pos, out Ship ship))
                {
                    ship.OrderToOrbit(p, clearOrders:true, MoveOrder.Aggressive);
                    p.ParentSystem.NewGameAddRemnantShipToList(ship);
                    ActivationXpNeeded += (ShipRole.GetExpSettings(ship).KillExp / divider) * StoryTurnsLevelUpModifier();
                }
            }
        }

        RemnantStory InitAndPickStory(EmpireAI ai)
        {
            ai.AddGoal(new RemnantInit(Owner));
            if (Universe.P.DisableRemnantStory)
                return RemnantStory.None;

            switch (RollDie(3)) // todo for now 3 stories
            {
                default:
                case 1: return RemnantStory.AncientBalancers;
                case 2: return RemnantStory.AncientExterminators;
                case 3: return RemnantStory.AncientRaidersRandom;
                case 4: return RemnantStory.AncientColonizers;
            }
        }

        public enum RemnantStory
        {
            None,
            AncientBalancers,
            AncientExterminators,
            AncientRaidersRandom,
            AncientColonizers
        }
    }

    /*
    public void CheckArmageddon()
    {
        if (Armageddon)
        {
            if (!Paused) ArmageddonTimer -= elapsedTime;
            if (ArmageddonTimer < 0.0)
            {
                ArmageddonTimer = 300f;
                ++ArmageddonCounter;
                if (ArmageddonCounter > 5)
                    ArmageddonCounter = 5;
                for (int i = 0; i < ArmageddonCounter; ++i)
                {
                    Ship exterminator = Ship.CreateShipAtPoint("Remnant Exterminator", EmpireManager.Remnants,
                            player.GetWeightedCenter() + new Vector2(RandomMath.RandomBetween(-500000f, 500000f),
                                RandomMath.RandomBetween(-500000f, 500000f)));
                    exterminator.AI.DefaultAIState = AIState.Exterminate;
                }
            }
        }
    }*/

    public enum RemnantShipType
    {
        Fighter,
        Corvette,
        SmallSupport,
        Carrier,
        Assimilator, // Assimilators are Guardians which are created at game start only
        TorpedoCruiser,
        Mothership,
        Exterminator,
        Inhibitor,
        Portal,
        Bomber,
        Frigate,
        BomberLight,
        BomberMedium,
        Cruiser
    }
}
