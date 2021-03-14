using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ship_Game.Commands.Goals;
using Ship_Game.GameScreens.DiplomacyScreen;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [Guid("2CC355DF-EA7A-49C8-8940-00AA0713EFE3")]
    public sealed partial class EmpireAI
    {
        private int NumberOfShipGoals  = 6;
        private int NumberTroopGoals   = 2;
        public float BuildCapacity { get; private set; }
        public float AllianceBuildCapacity { get; private set; }
        public float TroopShuttleCapacity { get; private set; }

        private readonly Empire OwnerEmpire;
        private readonly BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();
        public readonly OffensiveForcePoolManager OffensiveForcePoolManager;

        public string EmpireName;
        public DefensiveCoordinator DefensiveCoordinator;
        public BatchRemovalCollection<Goal> Goals            = new BatchRemovalCollection<Goal>();
        public ThreatMatrix ThreatMatrix;                     
        public Array<AO> AreasOfOperations                   = new Array<AO>();
        public Array<int> UsedFleets                         = new Array<int>();
        public float Toughnuts = 0;
        public int Recyclepool = 0;
        public float DefStr;
        public ExpansionAI.ExpansionPlanner ExpansionAI;

        public int PauseWarTimer { get; set; } = 0;

        public EmpireAI(Empire e, bool fromSave)
        {
            EmpireName                = e.data.Traits.Name;
            OwnerEmpire               = e;
            ThreatMatrix              = new ThreatMatrix(e);
            DefensiveCoordinator      = new DefensiveCoordinator(e);
            OffensiveForcePoolManager = new OffensiveForcePoolManager(e);
            TechChooser               = new Research.ChooseTech(e);
            ExpansionAI               = new ExpansionAI.ExpansionPlanner(OwnerEmpire);

            if (OwnerEmpire.data.EconomicPersonality != null)
                NumberOfShipGoals += OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;

            if (OwnerEmpire.isFaction && OwnerEmpire.data.IsPirateFaction)
                OwnerEmpire.SetAsPirates(fromSave, Goals);

            if (OwnerEmpire.isFaction && OwnerEmpire.data.IsRemnantFaction)
                OwnerEmpire.SetAsRemnants(fromSave, Goals);

            EmpireDefense?.RestoreFromSave(true);
            WarTasks = new StrategyAI.WarGoals.WarTasks(e);
        }

        void RunManagers()
        {
            if (OwnerEmpire.data.IsRebelFaction || OwnerEmpire.data.Defeated)
                return;

            if (!OwnerEmpire.isPlayer)
            {
                OffensiveForcePoolManager.ManageAOs();
                foreach (AO ao in AreasOfOperations)
                    ao.Update();
            }
            if (!OwnerEmpire.isFaction)
            {
                DefensiveCoordinator.ManageForcePool();
                RunEconomicPlanner();
                ExpansionAI.RunExpansionPlanner();
                RunInfrastructurePlanner();
                RunDiplomaticPlanner();
                RunResearchPlanner();
                RunAgentManager();
            }
            WarTasks.Update();
            RunMilitaryPlanner();
            RunWarPlanner();
        }

        public void RemoveFactionEndedTasks()
        {
            foreach (MilitaryTask remove in TasksToRemove)
                TaskList.RemoveRef(remove);

            TasksToRemove.Clear();
        }

        public void DebugRunResearchPlanner()
        {
            // unlock 5 techs with a focus on ship tech
            var shipTechCount = OwnerEmpire.ShipTechs.Count +3;
            var wantedTechs = 3;
            for (int i = 0; i < 15 && wantedTechs > 0; i++)
            {
                OwnerEmpire.data.TechDelayTime = 2;
                RunResearchPlanner();
                OwnerEmpire.Research.Update();
                OwnerEmpire.Research.Current.Unlock(OwnerEmpire);
                OwnerEmpire.Research.Reset();
                OwnerEmpire.UpdateShipsWeCanBuild();
                if (OwnerEmpire.ShipTechs.Count > shipTechCount)
                    wantedTechs--;
            }
        }

        public Array<Planet> GetKnownPlanets()
        {
            var knownPlanets = new Array<Planet>();
            foreach (SolarSystem s in UniverseScreen.SolarSystemList)
            {
                if (s.IsExploredBy(OwnerEmpire))
                    knownPlanets.AddRange(s.PlanetList);
            }
            return knownPlanets;
        }

        public AO FindClosestAOTo(Vector2 position)
        {
            var aos = AreasOfOperations;
            if (aos.Count == 0)
            {
                return new AO(OwnerEmpire);
            }

            AO closestAO = aos.FindMin(ao => ao.Center.SqDist(position));
            return closestAO;
        }

        public float DistanceToClosestAO(Vector2 position)
        {
            AO ao = FindClosestAOTo(position);
            if (ao == null) return OwnerEmpire.WeightedCenter.Distance(position);
            return ao.Center.Distance(position);
        }

        public AO AoContainingPosition(Vector2 location)
        {
            return AreasOfOperations.Find(ao => location.InRadius(ao.Center, ao.Radius));
        }

        public bool IsInOurAOs(Vector2 location) => AoContainingPosition(location) != null;

        public void InitializeAOsFromSave()
        {
            foreach (AO area in AreasOfOperations)
                area.InitFromSave(OwnerEmpire);
        }

        public void CheckColonizationClaims(Empire them, Relationship usToThem)
        {
            if (OwnerEmpire.isPlayer
                || OwnerEmpire.isFaction
                || !usToThem.Known
                || usToThem.AtWar)
            {
                return;
            }

            if (!GetColonizationGoalsList(OwnerEmpire, out Array<Goal> ourColonizationGoals)
                || !GetColonizationGoalsList(them, out Array<Goal> theirColonizationGoals))
            {
                return;
            }

            // Xenophobic empires will warn about claims
            // even if they decided to colonize a planet after another empire did so
            bool warnAnyway       = OwnerEmpire.IsXenophobic && usToThem.Posture != Posture.Friendly;
            float detectionChance = OwnerEmpire.ColonizationDetectionChance(usToThem, them);
            foreach (Goal ourGoal in ourColonizationGoals)
            {
                var system = ourGoal.ColonizationTarget.ParentSystem;
                if (usToThem.WarnedSystemsList.Contains(system.guid))
                    continue;

                // Non allied empires will always warn if the system is exclusively owned by them
                bool warnExclusive = !usToThem.Treaty_Alliance && system.IsExclusivelyOwnedBy(OwnerEmpire);
                foreach (Goal theirGoal in theirColonizationGoals)
                {
                    if (theirGoal.ColonizationTarget.ParentSystem != ourGoal.ColonizationTarget.ParentSystem)
                        continue;

                    if (DetectAndWarn(theirGoal, warnExclusive))
                    {
                        if (system.HasPlanetsOwnedBy(them) && theirGoal.ColonizationTarget != ourGoal.ColonizationTarget && !warnAnyway)
                            continue; // They already have colonies in this system and targeting a different planet

                        if (them.isPlayer)
                            DiplomacyScreen.Show(OwnerEmpire, "Claim System", system);

                        usToThem.WarnedSystemsList.AddUnique(system.guid);
                    }
                }
            }

            bool GetColonizationGoalsList(Empire empire, out Array<Goal> planetList)
            {
                planetList = empire.GetEmpireAI().ExpansionAI.GetColonizationGoals();
                return planetList.Count > 0;
            }

            bool DetectAndWarn(Goal goal, bool warnExclusive)
            {
                if (!RandomMath.RollDice(detectionChance)
                    && !goal.FinishedShip?.KnownByEmpires.KnownBy(OwnerEmpire) == true)
                {
                    return false;
                }

                // Detected their colonization efforts
                if (warnExclusive || warnAnyway)
                    return true;

                // If they stole planets from us, we will value our targets more.
                // If we have more planets then them, we will cut them some slack.
                Planet p           = goal.ColonizationTarget;
                float planetsRatio = (float)OwnerEmpire.GetPlanets().Count / them.GetPlanets().Count.LowerBound(1);
                float valueForUs   = p.ColonyPotentialValue(OwnerEmpire) * usToThem.NumberStolenClaims;
                float valueForThem = p.ColonyPotentialValue(them) * planetsRatio;
                float ratio        = valueForUs / valueForThem.LowerBound(1);

                return ratio > OwnerEmpire.PersonalityModifiers.ColonizationClaimRatioWarningThreshold;
            }
        }

        public void TriggerRefit()
        {
            if (OwnerEmpire.isPlayer)
                return;

            var offPool = OwnerEmpire.Pool.GetShipsFromOffensePools(onlyAO: true);
            for (int i = offPool.Count - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (ship.AI.BadGuysNear
                    || ship.AI.HasPriorityOrder
                    || ship.AI.HasPriorityTarget
                    || ship.CanBeRefitted)
                {
                    continue;
                }

                Ship newShip = ShipBuilder.PickShipToRefit(ship, OwnerEmpire);
                if (newShip != null)
                    Goals.Add(new RefitShip(ship, newShip.Name, OwnerEmpire));
            }
        }

        public void AddScrapShipGoal(Ship ship)
        {
            Goals.Add(new ScrapShip(ship, OwnerEmpire));
        }

        public void AddPlanetaryRearmGoal(Ship ship, Planet p, Ship existingSupplyShip = null)
        {
            if (existingSupplyShip == null)
                Goals.Add(new RearmShipFromPlanet(ship, p, OwnerEmpire));
            else
                Goals.Add(new RearmShipFromPlanet(ship, existingSupplyShip, p, OwnerEmpire));
        }

        public void Update()
        {
            DefStr = DefensiveCoordinator.GetForcePoolStrength();
            if (!OwnerEmpire.isFaction)
                RunManagers();
            else
                RemoveFactionEndedTasks();

            for (int i = Goals.Count - 1; i >= 0; i--)
            {
                Goals[i].Evaluate();
            }

            Goals.ApplyPendingRemovals();
        }

        public IReadOnlyList<Goal> SearchForGoals(GoalType type)
        {
            var goals = new Array<Goal>();
            for (int i = 0; i < Goals.Count; i++)
            {
                Goal goal = Goals[i];
                if (goal.type == type)
                    goals.Add(goal);
            }
            return goals;
        }


        /// <summary>
        /// Will remove Troop goals with null planet owner or no troops being built.
        /// This is a Hack since the goal should fucking deal with it
        /// </summary>
        /// <param name="numTroopGoals"></param>
        public void VerifyTroopGoals(out int numTroopGoals)
        {
            numTroopGoals = 0;
            for (int i = Goals.Count -1; i >= 0; i--)
            {
                Goal g = Goals[i];
                if (g.type != GoalType.BuildTroop)
                    continue;

                if (g.PlanetBuildingAt != null 
                    && !g.IsMainGoalCompleted
                    &&  (g.PlanetBuildingAt.Owner != OwnerEmpire || !g.PlanetBuildingAt.Construction.ContainsTroopWithGoal(g)))
                {
                    Goals.Remove(g);
                }
                else
                {
                    numTroopGoals += 1; 
                }
            }
        }

        public bool HasGoal(GoalType type)
        {
            for (int i = 0; i < Goals.Count; ++i)
                if (Goals[i].type == type) return true;
            return false;
        }

        public bool HasGoal(in Guid guid)
        {
            for (int i = 0; i < Goals.Count; ++i)
                if (Goals[i].guid == guid) return true;
            return false;
        }

        public void AddGoal(Goal goal)
        {
            Goals.Add(goal);
        }

        
        public void FindAndRemoveGoal(GoalType type, Predicate<Goal> removeIf)
        {
            for (int i = 0; i < Goals.Count; ++i)
            {
                Goal g = Goals[i];
                if (g.type == type && removeIf(g))
                {
                    Goals.QueuePendingRemoval(g);
                    return;
                }
            }
        }
    }
}