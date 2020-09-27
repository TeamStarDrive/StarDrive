using System;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Ships;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.Fleets;

namespace Ship_Game
{
    using static RandomMath;
    using static HelperFunctions;
    public class Remnants
    {
        public const int MaxLevel = 20;
        public readonly Empire Owner;
        public readonly BatchRemovalCollection<Goal> Goals;
        public float StoryTriggerKillsXp { get; private set; }
        public float PlayerStepTriggerXp { get; private set; }
        public bool Activated { get; private set; }
        public static bool Armageddon;
        public RemnantStory Story { get; private set; }
        public float Production { get; private set; }
        public int Level { get; private set; }
        public Map<RemnantShipType, float> ShipCosts { get; } = new Map<RemnantShipType, float>();
        public int StoryStep { get; private set; } = 1;

        public Remnants(Empire owner, bool fromSave, BatchRemovalCollection<Goal> goals)
        {
            Owner = owner;
            Goals = goals;

            Owner.data.FuelCellModifier      = 1.4f;
            Owner.data.FTLPowerDrainModifier = 0.8f;

            if (!fromSave)
                Story = InitAndPickStory(goals);

            CalculateShipCosts();
        }

        public void RestoreFromSave(SavedGame.EmpireSaveData sData)
        {
            Activated           = sData.RemnantStoryActivated;
            StoryTriggerKillsXp = sData.RemnantStoryTriggerKillsXp;
            PlayerStepTriggerXp = sData.RemnantPlayerStepTriggerXp;
            Story               = (RemnantStory)sData.RemnantStoryType;
            Production          = sData.RemnantProduction;
            Level               = sData.RemnantLevel;
            StoryStep           = sData.RemnantStoryStep;
        }

        public void IncrementKills(Empire empire, int xp)
        {
            if (!Activated)
                StoryTriggerKillsXp += xp;

            if (!Activated && StoryTriggerKillsXp >= 30)
                Activate();

            if (empire.isPlayer)
            {
                float stepTrigger = ShipRole.GetMaxExpValue() * StoryStep;
                PlayerStepTriggerXp += xp;
                if (PlayerStepTriggerXp > stepTrigger) // todo 0 is for testing
                {
                    PlayerStepTriggerXp = 0;
                    if (GetStoryEvent(out ExplorationEvent expEvent))
                        Empire.Universe.NotificationManager.AddRemnantUpdateNotify(expEvent, Owner);

                    StoryStep += 1;
                }
            }


            if (!Activated) // todo for testing
                Activate();
        }

        void Activate()
        {
            Activated = true;
            SetInitialLevel();

            // Todo None story does not have a goal or maybe the old goal

            if (Story != RemnantStory.AncientColonizers)
                Goals.Add(new RemnantEngagements(Owner));
            //else
               // Todo create colonization story  
        }

        void NotifyPlayerOnLevelUp()
        {
            float espionageStr = EmpireManager.Player.GetSpyDefense();
            if (espionageStr <= Level * 3)
                return; // not enough espionage strength to learn about pirate activities

            Empire.Universe.NotificationManager.AddRemnantsAreGettingStronger(Owner);
        }

        public bool TryLevelUpByDate(out int newLevel)
        {
            newLevel         = 0;
            int turnsLevelUp = Owner.DifficultyModifiers.RemnantTurnsLevelUp;
            int turnsPassed  = (int)(Empire.Universe.StarDate * 10);
            if (turnsPassed % turnsLevelUp == 0) // 500 turns on Normal
            {
                if (Level < MaxLevel)
                {
                    Log.Info(ConsoleColor.Green, $"---- Remnants: Level up to level {Level} ----");
                    NotifyPlayerOnLevelUp();
                }

                newLevel = Level = (Level + 1).UpperBound(MaxLevel);
                return true;
            }

            return false;
        }

        void SetInitialLevel()
        {
            int turnsLevelUp = Owner.DifficultyModifiers.RemnantTurnsLevelUp;
            int turnsPassed  = (int)((Empire.Universe.StarDate - 1000) * 10);
            Level            = (int)Math.Floor(turnsPassed / (decimal)turnsLevelUp);
            Level            = Level.LowerBound(1);
            Log.Info(ConsoleColor.Green, $"---- Remnants: Activation Level: {Level} ----");
        }

        bool GetStoryEvent(out ExplorationEvent expEvent)
        {
            expEvent = null;
            var events = ResourceManager.EventsDict.Values.ToArray().Filter(e => e.StoryStep == StoryStep 
                                                                            && (e.Story == Story || Story != RemnantStory.None && e.AllRemnantStories));
            if (events.Length > 0)
                expEvent = events.First();

            return expEvent != null;
        }

        public bool CanDoAnotherEngagement(out int numRaids)
        {
            numRaids = Goals.Count(g => g.IsRaid);
            return numRaids < Level;
        }

        public bool FindValidTarget(Ship portal, out Empire target)
        {
            target = null;
            switch (Story)
            {
                case RemnantStory.AncientBalancers:
                    target = EmpireManager.MajorEmpires.FindMaxFiltered(e => !e.data.Defeated, e => e.TotalScore);
                    break;
                case RemnantStory.AncientExterminators: 
                    target = EmpireManager.MajorEmpires.FindMinFiltered(e => !e.data.Defeated, e => e.TotalScore);
                    break;
                case RemnantStory.AncientRaidersClosest:
                    target = EmpireManager.MajorEmpires.FindMaxFiltered(e => !e.data.Defeated, e => portal.Center.Distance(e.WeightedCenter));
                    break;
                case RemnantStory.AncientRaidersRandom:
                    var potentialEmpires = EmpireManager.MajorEmpires.Filter(e => !e.data.Defeated);
                    if (potentialEmpires.Length > 0)
                        target = potentialEmpires.RandItem();

                    break;
                default: 
                    return false;
            }

            return target != null;
        }

        public bool TargetEmpireStillValid(Empire currentTarget, Ship portal, bool checkOnlyDefeated = false)
        {
            if (checkOnlyDefeated && !currentTarget.data.Defeated)
                return true;

            FindValidTarget(portal, out Empire expectedTarget);
            return expectedTarget == currentTarget;
        }

        public bool AssignShipInPortalSystem(Ship portal, int bombersNeeded, out Ship ship)
        {
            ship = null;
            if (portal.System == null)
                return false;

            var availableShips = portal.System.ShipList.Filter(s => s.fleet == null 
                                                                    && s.loyalty == Owner 
                                                                    && s.IsGuardian
                                                                    && !s.IsPlatformOrStation
                                                                    && !s.InCombat);
            if (availableShips.Length > 0)
                if (bombersNeeded > 0)
                {
                    var bombers = availableShips.Filter(s => s.Name == Owner.data.RemnantBomber);
                    if (bombers.Length > 0)
                        ship = bombers.First();
                }
                else
                {
                    ship = availableShips.RandItem();
                }

            return ship != null;
        }

        public int NumBombersInFleet(Fleet fleet)
        {
           return fleet?.Ships.Count(s => s.Name == Owner.data.RemnantBomber) ?? 0;
        }

        public void ReleaseFleet(Fleet fleet)
        {
            if (fleet == null)
                return;

            fleet.FleetTask?.DisbandFleet(fleet);
            fleet.FleetTask?.EndTask();
        }

        public bool GetClosestPortal(Vector2 position, out Ship portal)
        {
            portal = null;
            if (!GetPortals(out Ship[] portals))
                return false;

            portal = portals.FindMin(s => s.Center.Distance(position));
            return portal != null;
        }

        public void OrderEscortPortal(Ship portal)
        {
            if (portal.System == null)
                return;

            for (int i = 0; i < portal.System.ShipList.Count; i++)
            {
                Ship ship = portal.System.ShipList[i];
                if (!ship.IsPlatformOrStation && ship.IsGuardian && !ship.InCombat && ship.AI.EscortTarget == null && ship.fleet == null)
                    ship.AI.AddEscortGoal(portal);
            }
        }

        public bool SelectTargetPlanetByLevel(Empire targetEmpire, out Planet targetPlanet)
        {
            targetPlanet           = null;
            int desiredPlanetLevel = (RollDie(5) - 5 + Level).LowerBound(1);
            var potentialPlanets   = targetEmpire.GetPlanets().Filter(p => p.Level == desiredPlanetLevel);
            if (potentialPlanets.Length == 0) // Try lower level planets if not found exact level
                potentialPlanets = targetEmpire.GetPlanets().Filter(p => p.Level < desiredPlanetLevel);

            if (potentialPlanets.Length == 0)
                return false; // Could not find a target planet by planet level

            targetPlanet = potentialPlanets.RandItem();
            return true;
        }

        public bool SelectTargetClosestPlanet(Ship portal, Empire targetEmpire, out Planet targetPlanet)
        {
            targetPlanet = null;
            var potentialPlanets = targetEmpire.GetPlanets();
            if (potentialPlanets.Count == 0) // No planets - might be defeated
                return false;

            targetPlanet = potentialPlanets.FindMin(p => p.Center.Distance(portal.Center));
            return targetPlanet != null;
        }

        public bool TargetNextPlanet(Empire targetEmpire, Planet currentPlanet, int numBombers, out Planet nextPlanet)
        {
            nextPlanet           = null;

            if (numBombers > 0 && currentPlanet.ParentSystem.IsOwnedBy(targetEmpire))
            {
                nextPlanet = currentPlanet.ParentSystem.PlanetList.Filter(p => p.Owner == targetEmpire).RandItem();
                return true;
            }

            var potentialPlanets = targetEmpire.GetPlanets().Filter(p => p.ParentSystem != currentPlanet.ParentSystem);
            if (potentialPlanets.Length == 0)
                return false;

            int numPlanets     = 5.UpperBound(potentialPlanets.Length);
            var closestPlanets = potentialPlanets.Sorted(p => p.Center.Distance(currentPlanet.Center)).Take(numPlanets).ToArray();
            nextPlanet         = closestPlanets.RandItem();
            return nextPlanet != null;
        }

        public void CallGuardians(Ship portal) // One guarding from each relevant system
        {
            foreach (SolarSystem system in UniverseScreen.SolarSystemList)
            {
                var guardians = system.ShipList.Filter(s => s.IsGuardian && !s.InCombat);
                if (guardians.Length > 0)
                {
                    Ship chosenGuardian = guardians.RandItem();
                    chosenGuardian.AI.AddEscortGoal(portal);
                }
            }
        }

        public int GetNumBombersNeeded(Planet planet)
        {
            return RollDice((Level - 1) * 10) ? planet.Level : 0;
        }

        public bool CreatePortal(out Ship portal, out string systemName)
        {
            portal             = null;
            SolarSystem system = null;
            systemName         = "";

            if (!GetRadiatingStars(out SolarSystem[] systems)) // Prefer stars which emit radiation
                if (!GetLoneSystem(out system)) // Try a lone system
                    if (!GetUnownedSystems(out systems)) // Fallback to any unowned system
                        return false; // Could not find a spot

            if (system == null)
                system = systems.RandItem();

            Vector2 pos = system.Position.GenerateRandomPointOnCircle(20000);
            systemName  = system.Name;
            return SpawnShip(RemnantShipType.Portal, pos, out portal);
        }

        public bool CreateShip(Ship portal, bool needBomber, out Ship ship)
        {
            ship = null;
            RemnantShipType type = needBomber ? RemnantShipType.Bomber : SelectShipForCreation();
            if (!ShipCosts.TryGetValue(type, out float cost) || Production < cost)
                return false;

            if (!SpawnShip(type, portal.Center, out ship))
                return false;

            GenerateProduction(-cost);
            ship.EmergeFromPortal();
            return true;
        }

        void CalculateShipCosts()
        {
            AddShipCost(Owner.data.RemnantFighter, RemnantShipType.Fighter);
            AddShipCost(Owner.data.RemnantCorvette, RemnantShipType.Corvette);
            AddShipCost(Owner.data.RemnantSupportSmall, RemnantShipType.SmallSupport);
            AddShipCost(Owner.data.RemnantAssimilator, RemnantShipType.Assimilator);
            AddShipCost(Owner.data.RemnantCarrier, RemnantShipType.Carrier);
            AddShipCost(Owner.data.RemnantMotherShip, RemnantShipType.Mothership);
            AddShipCost(Owner.data.RemnantExterminator, RemnantShipType.Exterminator);
            AddShipCost(Owner.data.RemnantInhibitor, RemnantShipType.Inhibitor);
            AddShipCost(Owner.data.RemnantBomber, RemnantShipType.Bomber);
        }

        void AddShipCost(string shipName, RemnantShipType type)
        {
            Ship ship  = ResourceManager.GetShipTemplate(shipName);
            float cost = ship.BaseCost;
            ShipCosts.Add(type, cost);
        }

        RemnantShipType SelectShipForCreation() // Note Bombers are created exclusively 
        {
            int roll = RollDie(Level + 3, Level/2).LowerBound(1);
            switch (roll)
            {
                case 1:
                case 2:  return RemnantShipType.Fighter;
                case 3:  return RemnantShipType.SmallSupport;
                case 4:  return RemnantShipType.Corvette;
                case 5:
                case 6:  return RemnantShipType.Assimilator;
                case 7:
                case 8:  return RemnantShipType.Cruiser;
                case 9:
                case 10: return RemnantShipType.Inhibitor;
                case 11: 
                case 12: return RemnantShipType.Carrier;
                case 13:
                case 14: return RemnantShipType.Mothership;
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
                case RemnantShipType.Fighter:      shipName = Owner.data.RemnantFighter;      break;
                case RemnantShipType.Corvette:     shipName = Owner.data.RemnantCorvette;     break;
                case RemnantShipType.SmallSupport: shipName = Owner.data.RemnantSupportSmall; break;
                case RemnantShipType.Assimilator:  shipName = Owner.data.RemnantAssimilator;  break;
                case RemnantShipType.Carrier:      shipName = Owner.data.RemnantCarrier;      break;
                case RemnantShipType.Mothership:   shipName = Owner.data.RemnantMotherShip;   break;
                case RemnantShipType.Exterminator: shipName = Owner.data.RemnantExterminator; break;
                case RemnantShipType.Portal:       shipName = Owner.data.RemnantPortal;       break;
                case RemnantShipType.Bomber:       shipName = Owner.data.RemnantBomber;       break;
                case RemnantShipType.Inhibitor:    shipName = Owner.data.RemnantInhibitor;    break;
            }

            if (shipName.NotEmpty())
            {
                remnantShip = Ship.CreateShipAtPoint(shipName, Owner, where);
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

        public void GenerateProduction(float amount)
        {
            Production = (Production + amount).UpperBound(Level * Level * 1000); // Level 20 - 400k
        }

        public int NumPortals()
        {
            return Owner.GetShips().Count(s => s.Name == Owner.data.RemnantPortal && s.Active);
        }

        public bool GetPortals(out Ship[] portals)
        {
            portals = Owner.GetShips().Filter(s => s.Name == Owner.data.RemnantPortal && s.Active);
            return portals.Length > 0;
        }

        float PlanetQuality(Planet planet)
        {
            float fertilityMod = 1;
            float richnessMod  = 1;
            if (EmpireManager.Player.IsCybernetic)
            {
                fertilityMod = 0.5f;
                richnessMod  = planet.IsBarrenType ? 6f : 3f;
            }

            float quality = planet.BaseFertility * fertilityMod 
                            + planet.MineralRichness * richnessMod 
                            + planet.MaxPopulationBillionFor(EmpireManager.Remnants);

            // Boost the quality score for planets that are very rich
            if (planet.MineralRichness > 1.5f)
                quality += 2;

            if (planet.BaseFertility > 1.5f)
                quality += 2;

            return quality;
        }

        public void GenerateRemnantPresence(Planet p)
        {
            if (p.ParentSystem.isStartingSystem || p.ParentSystem.PiratePresence)
                return; // Don't create Remnants on starting systems or Pirate systems

            float quality   = PlanetQuality(p);
            int dieModifier = (int)CurrentGame.Difficulty * 5 - 5; // easy -5, brutal +10
            int d100        = RollDie(100) + dieModifier;

            switch (GlobalStats.ExtraRemnantGS) // Added by Gretman, Refactored by FB (including all remnant methods)
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
            if (quality > 15f)
            {
                if (d100 >= 30) AddMinorFleet(p);
                if (d100 >= 50) AddMajorFleet(p);
                if (d100 >= 70) AddSupportShips(p);
                if (d100 >= 90) AddTorpedoShips(p);
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
                if (d100 > 90) AddTorpedoShips(p);
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
                if (d100 >= 70) AddTorpedoShips(p);
                if (d100 >= 90) AddCarriers(p);
            }
            else if (quality >= 15f)
            {
                AddMajorFleet(p);
                if (d100 >= 40) AddSupportShips(p);
                if (d100 >= 60) AddCarriers(p);
                if (d100 >= 80) AddTorpedoShips(p);
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

            if (RollDice(10 + (int)CurrentGame.Difficulty))
                AddGuardians(1, RemnantShipType.Inhibitor, p);
        }

        void AddCarriers(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Carrier, p);
            if (RollDice(20)) // 20% chance for another carrier
                AddGuardians(1, RemnantShipType.Carrier, p);
        }

        void AddTorpedoShips(Planet p)  //Added by Gretman
        {
            AddGuardians(1, RemnantShipType.Cruiser, p);
            if (RollDice(10)) // 10% chance for another torpedo cruiser
                AddGuardians(1, RemnantShipType.Cruiser, p);
        }

        void AddGuardians(int numShips, RemnantShipType type, Planet p)
        {
            for (int i = 0; i < numShips; ++i)
            {
                Vector2 pos = p.Center.GenerateRandomPointInsideCircle(p.ObjectRadius * 2);
                if (SpawnShip(type, pos, out Ship ship))
                    ship.OrderToOrbit(p);
            }
        }

        RemnantStory InitAndPickStory(BatchRemovalCollection<Goal> goals)
        {
            switch (RollDie(3)) // todo 3 is for testing  should be 6
            {
                default:
                case 1: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientBalancers;
                case 2: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientExterminators;
                case 3: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientRaidersRandom;
                case 4: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientRaidersClosest;
                case 5: goals.Add(new RemnantAI(Owner)); return RemnantStory.AncientColonizers;
                case 6: goals.Add(new RemnantAI(Owner)); return RemnantStory.None;
            }
        }

        public enum RemnantStory
        {
            None,
            AncientBalancers,
            AncientExterminators,
            AncientRaidersRandom,
            AncientRaidersClosest,
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
        Assimilator,
        Cruiser,
        Mothership,
        Exterminator,
        Inhibitor,
        Portal,
        Bomber
    }
}
