using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Ship_Game.Commands.Goals;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [Guid("2CC355DF-EA7A-49C8-8940-00AA0713EFE3")]
    public sealed partial class EmpireAI : IDisposable
    {
        private int NumberOfShipGoals  = 6;
        private int NumberTroopGoals   = 2;
        public float BuildCapacity { get; private set; }

        private readonly Empire OwnerEmpire;
        private readonly BatchRemovalCollection<SolarSystem> MarkedForExploration = new BatchRemovalCollection<SolarSystem>();
        private readonly OffensiveForcePoolManager OffensiveForcePoolManager;

        public string EmpireName;
        public DefensiveCoordinator DefensiveCoordinator;
        public BatchRemovalCollection<Goal> Goals            = new BatchRemovalCollection<Goal>();
        public ThreatMatrix ThreatMatrix                     = new ThreatMatrix();
        public Array<AO> AreasOfOperations                   = new Array<AO>();
        public Array<int> UsedFleets                         = new Array<int>();
        public BatchRemovalCollection<MilitaryTask> TaskList = new BatchRemovalCollection<MilitaryTask>();
        public Array<MilitaryTask> TasksToAdd                = new Array<MilitaryTask>();
        public float Toughnuts                               = 0;
        public int Recyclepool                               = 0;
        public float DefStr;
        public ExpansionAI.ExpansionPlanner ExpansionAI;

        public EmpireAI(Empire e)
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
            switch (name)
            {
                case "The Remnant": Goals.Add(new RemnantAI(OwnerEmpire)); break;
                case "Corsairs":    Goals.Add(new CorsairAI(OwnerEmpire)); break;
            }
        }

        public void AddToTaskList(MilitaryTask task) => TaskList.Add(task);

        public void RemoveFromTaskList(MilitaryTask task)
        {
            if (task == null)
                Log.Error("Attempting to Remove null task from Empire TaskList");
            TaskList.Remove(task);
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

        public void RemoveShipFromForce(Ship ship) => RemoveShipFromForce(ship, null);
        public void RemoveShipFromForce(Ship ship, AO ao)
        {
            if (ship == null) return;
            OwnerEmpire.ForcePoolRemove(ship);
            if (ao == null)
                foreach (var aos in AreasOfOperations)
                    aos.RemoveShip(ship);
            else
                ao.RemoveShip(ship);

            DefensiveCoordinator.Remove(ship);
        }


        public void AssignShipToForce(Ship toAdd)
        {
            toAdd.ClearFleet();
            if (OwnerEmpire.GetShipsFromOffensePools(true).ContainsRef(toAdd))
            {
                //@TODO fix the cause of having ships already in forcepool when a ship is being added to the force pool
                //this is broken. its checking all AO pools and force pool but only removes from force pool.
                OwnerEmpire.ForcePoolRemove(toAdd);
            }

            int numWars = OwnerEmpire.AtWarCount;

            float baseDefensePct = 0.1f;
            baseDefensePct = baseDefensePct + 0.15f * numWars;
            if(toAdd.DesignRole < ShipData.RoleName.fighter || toAdd.BaseStrength <=0
                                                            || toAdd.WarpThrust <= 0f || !toAdd.BaseCanWarp)
            {
                OwnerEmpire.GetForcePool().AddUnique(toAdd);
                return;
            }

            if (baseDefensePct > 0.35f)
                baseDefensePct = 0.35f;

            bool needDef = OwnerEmpire.CurrentMilitaryStrength * baseDefensePct - DefStr >0 && DefensiveCoordinator.DefenseDeficit >0;

            if (needDef)
            {
                DefensiveCoordinator.AddShip(toAdd);
                return;
            }
            //need to rework this better divide the ships.
            AO area = AreasOfOperations.FindMin(ao => toAdd.Position.SqDist(ao.Center));
            if (!area?.AddShip(toAdd) ?? false)
                OwnerEmpire.GetForcePool().Add(toAdd);
        }

        private Vector2 FindAveragePosition(Empire e)
        {
            IReadOnlyList<Planet> planets = e.GetPlanets();
            if (planets.Count <= 0)
                return Vector2.Zero;

            var avgPos = Vector2.Zero;
            foreach (Planet p in planets)
                avgPos += p.Center;

            avgPos /= planets.Count;
            return avgPos;
        }

        public float GetDistanceFromOurAO(Planet p)
        {
            IOrderedEnumerable<AO> sortedList =
                from area in AreasOfOperations
                orderby Vector2.Distance(p.Center, area.Center)
                select area;
            if (!sortedList.Any())
                return 0f;

            return Vector2.Distance(p.Center, sortedList.First().Center);
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

        public void InitialzeAOsFromSave(UniverseData data)
        {
            foreach (AO area in AreasOfOperations)
                area.InitFromSave(data, OwnerEmpire);
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
            them.Key.GetShips().ForEach(ship =>
            {
                if (ship.AI.State != AIState.Colonize || ship.AI.ColonizeTarget == null)
                    return;

                theirTargetPlanets.Add(ship.AI.ColonizeTarget);
            }, false, false);

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

            var offPool = OwnerEmpire.GetShipsFromOffensePools(onlyAO: true);
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


        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~EmpireAI() { Destroy(); }

        void Destroy()
        {
            TaskList?.Dispose(ref TaskList);
            DefensiveCoordinator?.Dispose(ref DefensiveCoordinator);
            Goals?.Dispose(ref Goals);
        }
    }
}