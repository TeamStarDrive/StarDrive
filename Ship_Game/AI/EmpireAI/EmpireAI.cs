using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ship_Game.AI.Compnonents;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.GameScreens.DiplomacyScreen;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [Guid("2CC355DF-EA7A-49C8-8940-00AA0713EFE3")]
    public sealed partial class EmpireAI
    {
        private int NumberOfShipGoals  = 6;
        public float BuildCapacity { get; private set; }
        public float AvailableBuildCapacity => BuildCapacity - OwnerEmpire.TotalWarShipMaintenance - OwnerEmpire.TotalTroopShipMaintenance;
        public float CivShipBudget => OwnerEmpire.data.FreightBudget;
        public float AvailableCivShipBudget => OwnerEmpire.data.FreightBudget - OwnerEmpire.TotalCivShipMaintenance;
        public float AllianceBuildCapacity { get; private set; }

        private readonly Empire OwnerEmpire;
        public readonly OffensiveForcePoolManager OffensiveForcePoolManager;

        public string EmpireName;
        public DefensiveCoordinator DefensiveCoordinator;
        public BatchRemovalCollection<Goal> Goals = new BatchRemovalCollection<Goal>();
        public ThreatMatrix ThreatMatrix;                     
        public Array<AO> AreasOfOperations = new Array<AO>();
        public Array<int> UsedFleets = new Array<int>();
        public float DefStr;
        public ExpansionAI.ExpansionPlanner ExpansionAI;

        public EmpireAI(Empire e, bool fromSave)
        {
            EmpireName                = e.data.Traits.Name;
            OwnerEmpire               = e;
            ThreatMatrix              = new ThreatMatrix(e);
            DefensiveCoordinator      = new DefensiveCoordinator(e, "DefensiveCoordinator");
            OffensiveForcePoolManager = new OffensiveForcePoolManager(e);
            TechChooser               = new Research.ChooseTech(e);
            ExpansionAI               = new ExpansionAI.ExpansionPlanner(OwnerEmpire);
            BudgetSettings            = new BudgetPriorities(e);
            if (OwnerEmpire.data.EconomicPersonality != null)
                NumberOfShipGoals += OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;

            if (OwnerEmpire.isFaction && OwnerEmpire.data.IsPirateFaction)
                OwnerEmpire.SetAsPirates(fromSave, Goals);

            if (OwnerEmpire.isFaction && OwnerEmpire.data.IsRemnantFaction)
                OwnerEmpire.SetAsRemnants(fromSave, Goals);
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
                if (Empire.Universe?.Debug == true && Empire.Universe?.StarDate % 50 == 0)
                {
                    int techScore     = 0;
                    int totalStrength = 0;
                    int maxStrength   = 0;
                    int maxTechScore  = 0;
                    Log.Write($"------- ship list -----{Empire.Universe?.StarDate} Ship list for {OwnerEmpire.Name}");
                    foreach (var logit in OwnerEmpire.ShipsWeCanBuild)
                    {
                        var template = ResourceManager.GetShipTemplate(logit, false);
                        Log.Write(ConsoleColor.Green ,$"{template.BaseHull.Role}, {template.DesignRole}, '{logit}'");
                        int strength   = (int)template.GetStrength();
                        techScore     += template.ShipData.TechsNeeded.Count;
                        totalStrength += strength;
                        maxStrength    = Math.Max(maxStrength, strength);
                        maxTechScore   = Math.Max(maxTechScore, techScore);
                    }
                    Log.Write($"ShipTechCount= {techScore} MaxShipTechs={maxTechScore} MaxShipStrength= {maxStrength}");
                    Log.Write($"PlanetBudget= {OwnerEmpire.data.ColonyBudget:0.0}/{OwnerEmpire.TotalBuildingMaintenance:0.0} Population= {OwnerEmpire.TotalPopBillion:0.0} Planets= {OwnerEmpire.NumPlanets}");
                    Log.Write($"------- ship list -----{Empire.Universe?.StarDate} Ship list for {OwnerEmpire.Name}");
                }
            }

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
            int shipTechCount = OwnerEmpire.ShipTechs.Count + 3;
            int wantedTechs = 3;
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
                var ao = new AO(OwnerEmpire);
                AreasOfOperations.Add(ao);
                return ao;
            }

            AO closestAO = aos.FindMin(ao => ao.Center.SqDist(position));
            return closestAO;
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
            Relationship themToUs = them.GetRelations(OwnerEmpire);
            foreach (Goal ourGoal in ourColonizationGoals)
            {
                var system = ourGoal.ColonizationTarget.ParentSystem;
                if (usToThem.WarnedSystemsList.Contains(system.guid))
                    continue; // Already warned them

                // Non allied empires will always warn if the system is exclusively owned by them
                bool warnExclusive = !usToThem.Treaty_Alliance && system.IsExclusivelyOwnedBy(OwnerEmpire);
                foreach (Goal theirGoal in theirColonizationGoals)
                {
                    if (theirGoal.ColonizationTarget.ParentSystem != system)
                        continue;

                    if (DetectAndWarn(theirGoal, warnExclusive))
                    {
                        if (system.HasPlanetsOwnedBy(them)
                            && theirGoal.ColonizationTarget.ParentSystem == them.Capital?.ParentSystem
                            && theirGoal.ColonizationTarget != ourGoal.ColonizationTarget
                            && !warnAnyway)
                        {
                            continue; // They already have colonies in this system and targeting a different planet, or its their home system
                        }

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

            // Local method
            bool DetectAndWarn(Goal goal, bool warnExclusive)
            {
                if (RandomMath.RollDice(detectionChance)
                    || goal.FinishedShip != null && goal.FinishedShip.KnownByEmpires.KnownBy(OwnerEmpire))
                {
                    // Detected their colonization efforts
                    if (warnExclusive || warnAnyway)
                        return true;

                    if (themToUs.WarnedSystemsList.Contains(goal.ColonizationTarget.ParentSystem.guid))
                        return false; // They warned us, so no need to warn them

                    // If they stole planets from us, we will value our targets more.
                    // If we have more pop then them, we will cut them some slack.
                    Planet p = goal.ColonizationTarget;
                    float popRatio = OwnerEmpire.MaxPopBillion / them.MaxPopBillion.LowerBound(1);
                    float valueToUs = p.ColonyPotentialValue(OwnerEmpire) * (usToThem.NumberStolenClaims + 1);
                    float valueToThem = p.ColonyPotentialValue(them) * popRatio;
                    float ratio = valueToUs / valueToThem.LowerBound(1);

                    return ratio > OwnerEmpire.PersonalityModifiers.ColonizationClaimRatioWarningThreshold;
                }

                return false;
            }
        }

        public void TriggerRefit()
        {
            if (OwnerEmpire.isPlayer)
                return;

            var offPool = OwnerEmpire.AIManagedShips.GetShipsFromOffensePools(onlyAO: true);
            for (int i = offPool.Count - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (ship.AI.BadGuysNear
                    || ship.AI.HasPriorityOrder
                    || ship.AI.HasPriorityTarget
                    || !ship.CanBeRefitted)
                {
                    continue;
                }

                Ship newShip = ShipBuilder.PickShipToRefit(ship, OwnerEmpire);
                if (newShip != null)
                {
                    Goals.Add(new RefitShip(ship, newShip.Name, OwnerEmpire));
                    foreach (Planet p in OwnerEmpire.GetPlanets())
                        p.Construction.RefitShipsBeingBuilt(ship, newShip);
                }
            }
        }

        public void AddScrapShipGoal(Ship ship, bool immediateScuttle)
        {
            Goals.Add(new ScrapShip(ship, OwnerEmpire, immediateScuttle));
        }

        public void AddPlanetaryRearmGoal(Ship ship, Planet p, Ship existingSupplyShip = null)
        {
            if (existingSupplyShip == null)
                Goals.Add(new RearmShipFromPlanet(ship, p, OwnerEmpire));
            else
                Goals.Add(new RearmShipFromPlanet(ship, existingSupplyShip, p, OwnerEmpire));
        }

        public void CancelColonization(Planet p)
        {
            Goal goal = Goals.Find(g => g.type == GoalType.Colonize && g.ColonizationTarget == p);
            if (goal != null)
            {
                goal.FinishedShip?.AI.OrderOrbitNearest(true);
                goal.PlanetBuildingAt?.Construction.Cancel(goal);
                Goals.QueuePendingRemoval(goal);
                Goals.ApplyPendingRemovals();
            }
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
                if (OwnerEmpire.data.Defeated)
                    break; // setting an empire as defeated within a goal clears the goals
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

        public int NumTroopGoals() => Goals.Filter(g => g.type == GoalType.BuildTroop).Length;

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

        public void DebugDrawTasks(ref DebugTextBlock debug, Empire enemy, bool warTasks)
        {
            var prioritizedTasks = TaskList.Sorted(t => t.Priority);
            for (int i = 0; i < prioritizedTasks.Length; i++)
            {
                MilitaryTask task = prioritizedTasks[i];
                if (warTasks && (!task.IsWarTask || task.TargetEmpire != enemy))
                    continue;

                task.DebugDraw(ref debug);
            }
        }
    }
}