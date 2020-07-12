using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public float TroopShuttleCapacity { get; private set; }

        private readonly Empire OwnerEmpire;
        private readonly BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();
        private readonly OffensiveForcePoolManager OffensiveForcePoolManager;

        public string EmpireName;
        public DefensiveCoordinator DefensiveCoordinator;
        public BatchRemovalCollection<Goal> Goals            = new BatchRemovalCollection<Goal>();
        public ThreatMatrix ThreatMatrix                     = new ThreatMatrix();
        public Array<AO> AreasOfOperations                   = new Array<AO>();
        public Array<int> UsedFleets                         = new Array<int>();
        public float Toughnuts = 0;
        public int Recyclepool = 0;
        public float DefStr;
        public ExpansionAI.ExpansionPlanner ExpansionAI;

        public EmpireAI(Empire e, bool fromSave)
        {
            EmpireName                = e.data.Traits.Name;
            OwnerEmpire               = e;
            DefensiveCoordinator      = new DefensiveCoordinator(e);
            OffensiveForcePoolManager = new OffensiveForcePoolManager(e);
            TechChooser               = new Research.ChooseTech(e);
            ExpansionAI               = new ExpansionAI.ExpansionPlanner(OwnerEmpire);

            if (OwnerEmpire.data.EconomicPersonality != null)
                NumberOfShipGoals = NumberOfShipGoals + OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;

            string name = OwnerEmpire.data.Traits.Name;

            if (OwnerEmpire.isFaction && OwnerEmpire.data.IsPirateFaction && !GlobalStats.DisablePirates)
                OwnerEmpire.SetAsPirates(fromSave, Goals);

            if (name == "The Remnant" && !fromSave)
                Goals.Add(new RemnantAI(OwnerEmpire));
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
            RunMilitaryPlanner();
            RunWarPlanner();
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
            if (ao == null) return OwnerEmpire.GetWeightedCenter().Distance(position);
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

        public void RunEventChecker(KeyValuePair<Empire, Relationship> them)
        {
            if (OwnerEmpire == Empire.Universe.PlayerEmpire || OwnerEmpire.isFaction || !them.Value.Known)
                return;

            var ourTargetPlanets = new Array<Planet>();
            var theirTargetPlanets = new Array<Planet>();
            foreach (Goal g in Goals)
            {
                if (g.type == GoalType.Colonize)
                    ourTargetPlanets.Add(g.ColonizationTarget);
            }
            foreach (Goal g in them.Key.GetEmpireAI().Goals)
            {
                if (g.type == GoalType.Colonize)
                    theirTargetPlanets.Add(g.ColonizationTarget);
            }
            SolarSystem sharedSystem = null;

            foreach (Ship s in them.Key.GetShips())
            {
                if (s.AI.State == AIState.Colonize && s.AI.ColonizeTarget != null)
                    theirTargetPlanets.Add(s.AI.ColonizeTarget);
            }

            foreach (Planet p in ourTargetPlanets)
            {
                bool matchFound = false;
                foreach (Planet other in theirTargetPlanets)
                {
                    if (p == null || other == null || p.ParentSystem != other.ParentSystem)
                        continue;

                    sharedSystem = p.ParentSystem;
                    matchFound = true;
                    break;
                }
                if (matchFound)
                    break;
            }

            if (sharedSystem != null && !them.Value.AtWar && !them.Value.WarnedSystemsList.Contains(sharedSystem.guid))
            {
                bool theyAreThereAlready = false;
                foreach (Planet p in sharedSystem.PlanetList)
                {
                    if (p.Owner != null && p.Owner == Empire.Universe.PlayerEmpire)
                        theyAreThereAlready = true;
                }
                if (!theyAreThereAlready)
                {
                    if (them.Key == Empire.Universe.PlayerEmpire)
                    {
                        DiplomacyScreen.Show(OwnerEmpire, "Claim System", sharedSystem);
                    }
                    them.Value.WarnedSystemsList.Add(sharedSystem.guid);
                }
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
                if (ship.AI.BadGuysNear || ship.AI.HasPriorityOrder || ship.AI.HasPriorityTarget)
                    continue;

                Ship newShip = ShipBuilder.PickShipToRefit(ship, OwnerEmpire);
                if (newShip != null)
                    Goals.Add(new RefitShip(ship, newShip.Name, OwnerEmpire));
            }
        }

        public void Update()
        {
            DefStr = DefensiveCoordinator.GetForcePoolStrength();
            if (!OwnerEmpire.isFaction)
                RunManagers();

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

        public bool HasGoal(GoalType type)
        {
            for (int i = 0; i < Goals.Count; ++i)
                if (Goals[i].type == type) return true;
            return false;
        }

        public void AddGoal(Goal goal)
        {
            Goals.Add(goal);
        }

        public void RemoveGoal(GoalType type, Predicate<Goal> removeIf)
        {
            for (int i = 0; i < Goals.Count; ++i)
            {
                Goal g = Goals[i];
                if (g.type == type && removeIf(g))
                {
                    Goals.RemoveAt(i);
                    return;
                }
            }
        }
    }
}