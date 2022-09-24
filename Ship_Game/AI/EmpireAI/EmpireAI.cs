using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Compnonents;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [StarDataType]
    public sealed partial class EmpireAI
    {
        [StarData] int NumberOfShipGoals = 6;
        public float BuildCapacity { get; private set; }
        public float CivShipBudget => OwnerEmpire.data.FreightBudget;
        public float AllianceBuildCapacity { get; private set; }

        UniverseState UState => OwnerEmpire.Universum;

        [StarData] readonly Empire OwnerEmpire;
        public OffensiveForcePoolManager OffensiveForcePoolManager;

        public DefensiveCoordinator DefensiveCoordinator;
        public IReadOnlyList<Goal> Goals => GoalsList;

        [StarData] readonly Array<Goal> GoalsList;
        [StarData] public Array<int> UsedFleets;
        [StarData] public Array<AO> AreasOfOperations;
        [StarData] public ThreatMatrix ThreatMatrix;
        [StarData] public float DefStr;
        [StarData] public ExpansionAI.ExpansionPlanner ExpansionAI;
        BudgetPriorities BudgetSettings;

        [StarDataConstructor]
        EmpireAI() {}

        public EmpireAI(Empire e)
        {
            OwnerEmpire = e;
            ThreatMatrix = new(e);
            ExpansionAI = new(OwnerEmpire);
            GoalsList = new();
            UsedFleets = new();
            AreasOfOperations = new();

            InitializeManagers(e);

            if (OwnerEmpire.data.EconomicPersonality != null)
                NumberOfShipGoals += OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;

            if (OwnerEmpire.IsFaction && OwnerEmpire.data.IsPirateFaction)
                OwnerEmpire.SetAsPirates(this);

            if (OwnerEmpire.IsFaction && OwnerEmpire.data.IsRemnantFaction)
                OwnerEmpire.SetAsRemnants(this);
        }

        void InitializeManagers(Empire e)
        {
            DefensiveCoordinator = new(e.Universum.CreateId(), e, "DefensiveCoordinator");
            TechChooser = new(e);
            OffensiveForcePoolManager = new(e);
            BudgetSettings = new(e);
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
            InitializeManagers(OwnerEmpire);
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
            if (!OwnerEmpire.IsFaction)
            {
                DefensiveCoordinator.ManageForcePool();
                RunEconomicPlanner();
                ExpansionAI.RunExpansionPlanner();
                RunInfrastructurePlanner();
                RunDiplomaticPlanner();
                RunResearchPlanner();
                RunAgentManager();
                if (OwnerEmpire.Universum?.Debug == true && OwnerEmpire.Universum?.StarDate % 50 == 0)
                {
                    int techScore     = 0;
                    int totalStrength = 0;
                    int maxStrength   = 0;
                    int maxTechScore  = 0;
                    Log.Write($"------- ship list -----{OwnerEmpire.Universum?.StarDate} Ship list for {OwnerEmpire.Name}");
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
                    Log.Write($"------- ship list -----{OwnerEmpire.Universum?.StarDate} Ship list for {OwnerEmpire.Name}");
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

        public Array<Planet> GetKnownPlanets(UniverseState universe)
        {
            var knownPlanets = new Array<Planet>();
            foreach (SolarSystem s in universe.Systems)
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
                var ao = new AO(OwnerEmpire.Universum, OwnerEmpire);
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

        public void CheckColonizationClaims(Empire them, Relationship usToThem)
        {
            if (OwnerEmpire.isPlayer
                || OwnerEmpire.IsFaction
                || !usToThem.Known
                || usToThem.AtWar)
            {
                return;
            }

            if (!GetColonizationGoalsList(OwnerEmpire, out MarkForColonization[] ourColonizationGoals) ||
                !GetColonizationGoalsList(them, out MarkForColonization[] theirColonizationGoals))
            {
                return;
            }

            // Xenophobic empires will warn about claims
            // even if they decided to colonize a planet after another empire did so
            bool warnAnyway       = OwnerEmpire.IsXenophobic && usToThem.Posture != Posture.Friendly;
            float detectionChance = OwnerEmpire.ColonizationDetectionChance(usToThem, them);
            Relationship themToUs = them.GetRelations(OwnerEmpire);
            foreach (MarkForColonization ourGoal in ourColonizationGoals)
            {
                Planet ourColonizeP = ourGoal.TargetPlanet;
                var system = ourColonizeP.ParentSystem;
                if (usToThem.WarnedSystemsList.Contains(system.Id))
                    continue; // Already warned them

                // Non allied empires will always warn if the system is exclusively owned by them
                bool warnExclusive = !usToThem.Treaty_Alliance && system.IsExclusivelyOwnedBy(OwnerEmpire);
                foreach (MarkForColonization theirGoal in theirColonizationGoals)
                {
                    Planet theirColonizeP = theirGoal.TargetPlanet;
                    if (theirColonizeP.ParentSystem != system)
                        continue;

                    if (DetectAndWarn(theirGoal, warnExclusive))
                    {
                        if (system.HasPlanetsOwnedBy(them)
                            && theirColonizeP.ParentSystem == them.Capital?.ParentSystem
                            && theirColonizeP != ourColonizeP
                            && !warnAnyway)
                        {
                            continue; // They already have colonies in this system and targeting a different planet, or its their home system
                        }

                        if (them.isPlayer)
                            DiplomacyScreen.Show(OwnerEmpire, "Claim System", system);

                        usToThem.WarnedSystemsList.AddUnique(system.Id);
                    }
                }
            }

            bool GetColonizationGoalsList(Empire empire, out MarkForColonization[] planetList)
            {
                planetList = empire.AI.ExpansionAI.GetColonizationGoals();
                return planetList.Length > 0;
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

                    if (themToUs.WarnedSystemsList.Contains(goal.TargetPlanet.ParentSystem.Id))
                        return false; // They warned us, so no need to warn them

                    // If they stole planets from us, we will value our targets more.
                    // If we have more pop then them, we will cut them some slack.
                    Planet p = goal.TargetPlanet;
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
                    AddGoal(new RefitShip(ship, newShip.Name, OwnerEmpire));
                    foreach (Planet p in OwnerEmpire.GetPlanets())
                        p.Construction.RefitShipsBeingBuilt(ship, newShip);
                }
            }
        }

        public void AddScrapShipGoal(Ship ship, bool immediateScuttle)
        {
            AddGoal(new ScrapShip(ship, OwnerEmpire, immediateScuttle));
        }

        public void AddPlanetaryRearmGoal(Ship ship, Planet p, Ship existingSupplyShip = null)
        {
            if (existingSupplyShip == null)
                AddGoal(new RearmShipFromPlanet(ship, p, OwnerEmpire));
            else
                AddGoal(new RearmShipFromPlanet(ship, existingSupplyShip, p, OwnerEmpire));
        }

        public void CancelColonization(Planet p)
        {
            Goal goal = FindGoal(g => g.IsColonizationGoal(p));
            if (goal != null)
            {
                goal.FinishedShip?.AI.OrderOrbitNearest(true);
                goal.PlanetBuildingAt?.Construction.Cancel(goal);
                RemoveGoal(goal);
            }
        }

        public void Update()
        {
            DefStr = DefensiveCoordinator.GetForcePoolStrength();
            if (!OwnerEmpire.IsFaction)
                RunManagers();
            else
                RemoveFactionEndedTasks();

            for (int i = GoalsList.Count - 1; i >= 0; i--)
            {
                GoalsList[i].Evaluate();
                if (GoalsList.Count == 0)
                    break; // setting an empire as defeated within a goal clears the goals
            }
        }

        public IReadOnlyList<Goal> SearchForGoals(GoalType type)
        {
            var goals = new Array<Goal>();
            for (int i = 0; i < GoalsList.Count; i++)
            {
                Goal goal = GoalsList[i];
                if (goal.Type == type)
                    goals.Add(goal);
            }
            return goals;
        }

        public int NumTroopGoals() => GoalsList.Filter(g => g.Type == GoalType.BuildTroop).Length;

        public bool HasGoal(GoalType type)
        {
            for (int i = 0; i < GoalsList.Count; ++i)
                if (GoalsList[i].Type == type) return true;
            return false;
        }

        public bool HasGoal(Goal goal)
        {
            for (int i = 0; i < GoalsList.Count; ++i)
                if (GoalsList[i] == goal) return true;
            return false;
        }

        public bool HasGoal(Predicate<Goal> predicate)
        {
            return GoalsList.Any(predicate);
        }

        public Goal FindGoal(Predicate<Goal> predicate)
        {
            return GoalsList.Find(predicate);
        }

        public Goal[] FindGoals(Predicate<Goal> predicate)
        {
            return GoalsList.Filter(predicate);
        }

        public T[] FindGoals<T>() where T : Goal
        {
            return GoalsList.FilterSelect(g => g is T, g => (T)g);
        }

        public V[] SelectFromGoals<T, V>(Func<T, V> selector) where T : Goal
        {
            return GoalsList.FilterSelect(g => g is T, g => selector((T)g));
        }

        public int CountGoals(Predicate<Goal> predicate)
        {
            return GoalsList.Count(predicate);
        }

        public void AddGoal(Goal goal)
        {
            GoalsList.Add(goal);
        }

        public void RemoveGoal(Goal goal)
        {
            GoalsList.Remove(goal);
        }

        public void ClearGoals()
        {
            GoalsList.Clear();
        }

        public void FindAndRemoveGoal(GoalType type, Predicate<Goal> removeIf)
        {
            for (int i = 0; i < GoalsList.Count; ++i)
            {
                Goal g = GoalsList[i];
                if (g.Type == type && removeIf(g))
                {
                    RemoveGoal(g);
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