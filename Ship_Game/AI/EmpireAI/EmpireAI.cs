using Ship_Game.AI.Tasks;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.Components;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Ship_Game.Fleets;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    [StarDataType]
    public sealed partial class EmpireAI : IDisposable
    {
        [StarData] int NumberOfShipGoals = 6;
        [StarData] public float BuildCapacity { get; private set; }
        public float CivShipBudget => OwnerEmpire.AI.FreightBudget;
        public float AllianceBuildCapacity { get; private set; }

        UniverseState UState => OwnerEmpire.Universe;

        [StarData] readonly Empire OwnerEmpire;

        public DefensiveCoordinator DefensiveCoordinator;
        public IReadOnlyList<Goal> Goals => GoalsList;

        [StarData] readonly Array<Goal> GoalsList;
        [StarData] public ThreatMatrix ThreatMatrix;
        [StarData] public ExpansionAI.ExpansionPlanner ExpansionAI;
        [StarData] public ExpansionAI.ResearchStationPlanner ResearchStationsAI;
        [StarData] public ExpansionAI.MiningOpsPlanner MiningOpsAI;
        [StarData] public SpaceRoadsManager SpaceRoadsManager;
        BudgetPriorities BudgetSettings;

        // Debug settings
        public bool Disabled; // disables EmpireAI and FleetAI

        [StarDataConstructor]
        EmpireAI() {}

        public EmpireAI(Empire e)
        {
            OwnerEmpire = e;
            ThreatMatrix = new(e);
            ExpansionAI = new(OwnerEmpire);
            ResearchStationsAI = new(OwnerEmpire);
            MiningOpsAI = new(OwnerEmpire);
            GoalsList = new();

            InitializeManagers(e);
            SpaceRoadsManager = new(e);
            if (OwnerEmpire.data.EconomicPersonality != null)
                NumberOfShipGoals += OwnerEmpire.data.EconomicPersonality.ShipGoalsPlus;

            if (OwnerEmpire.IsFaction && OwnerEmpire.data.IsPirateFaction)
                OwnerEmpire.SetAsPirates(this);

            if (OwnerEmpire.IsFaction && OwnerEmpire.data.IsRemnantFaction)
                OwnerEmpire.SetAsRemnants(this);
        }

        void InitializeManagers(Empire e)
        {
            DefensiveCoordinator = new(e.Universe.CreateId(), e, "DefensiveCoordinator");
            TechChooser = new(e);
            BudgetSettings = new(e);
        }

        ~EmpireAI() { Dispose(false); }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            DefensiveCoordinator.Dispose();
        }

        [StarDataDeserialized]
        void OnDeserialized()
        {
            InitializeManagers(OwnerEmpire);
        }

        public void Update()
        {
            if (Disabled) // AI has been disabled for debugging purposes
                return;

            if (!OwnerEmpire.IsFaction)
                RunManagers();
            else
                RemoveFactionEndedTasks();

            UpdateFleetsPosAndSpeed();
            OwnerEmpire.UpdateExoticConsumpsions(); // must be done after RunManager since
                                                    // this resets data needed or managers (like mining ops num)
            for (int i = GoalsList.Count - 1; i >= 0; i--)
            {
                GoalsList[i].Evaluate();
                if (GoalsList.Count == 0)
                    break; // setting an empire as defeated within a goal clears the goals
            }
        }

        void UpdateFleetsPosAndSpeed()
        {
            foreach (Fleet fleet in OwnerEmpire.ActiveFleets)
            {
                fleet.AveragePosition();
                fleet.UpdateSpeedLimit();
            }
        }

        void RunManagers()
        {
            if (OwnerEmpire.data.IsRebelFaction || OwnerEmpire.IsDefeated)
                return;

            if (!OwnerEmpire.IsFaction)
            {
                DefensiveCoordinator.ManageForcePool();
                RunEconomicPlanner();
                ExpansionAI.RunExpansionPlanner();

                if (ResearchStationsAI == null)
                    ResearchStationsAI = new(OwnerEmpire);

                ResearchStationsAI?.RunResearchStationPlanner(); // the null check here is for save competability

                if (MiningOpsAI == null)
                    MiningOpsAI = new(OwnerEmpire);

                MiningOpsAI.RunMiningOpsPlanner(); // the null check here is for save competability
                SpaceRoadsManager.Update();
                RunDiplomaticPlanner();
                RunResearchPlanner();
                RunAgentManager();
                if (OwnerEmpire.Universe?.Debug == true && OwnerEmpire.Universe?.StarDate % 50 == 0)
                {
                    int techScore     = 0;
                    int totalStrength = 0;
                    int maxStrength   = 0;
                    int maxTechScore  = 0;
                    Log.Write($"------- ship list -----{OwnerEmpire.Universe?.StarDate} Ship list for {OwnerEmpire.Name}");
                    foreach (IShipDesign design in OwnerEmpire.ShipsWeCanBuild)
                    {
                        Log.Write(ConsoleColor.Green ,$"{design.BaseHull.Role}, {design.Role}, '{design}'");
                        int strength   = (int)design.BaseStrength;
                        techScore     += design.TechsNeeded.Count;
                        totalStrength += strength;
                        maxStrength    = Math.Max(maxStrength, strength);
                        maxTechScore   = Math.Max(maxTechScore, techScore);
                    }
                    Log.Write($"ShipTechCount= {techScore} MaxShipTechs={maxTechScore} MaxShipStrength= {maxStrength}");
                    Log.Write($"PlanetBudget= {ColonyBudget:0.0}/{OwnerEmpire.TotalBuildingMaintenance:0.0} Population= {OwnerEmpire.TotalPopBillion:0.0} Planets= {OwnerEmpire.NumPlanets}");
                    Log.Write($"------- ship list -----{OwnerEmpire.Universe?.StarDate} Ship list for {OwnerEmpire.Name}");
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

        public void RemoveTaskFromGoals(MilitaryTask task)
        {
            foreach (Goal goal in Goals)
                goal.RemoveTask(task);
        }

        public void RemoveFleetFromGoals(Fleet fleet)
        {
            foreach (Goal goal in Goals)
                goal.RemoveFleet(fleet);
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
            Relationship themToUs = them.GetRelations(OwnerEmpire);
            foreach (MarkForColonization ourGoal in ourColonizationGoals)
            {
                Planet ourColonizeP = ourGoal.TargetPlanet;
                var system = ourColonizeP.System;
                if (usToThem.WarnedSystemsList.Contains(system))
                    continue; // Already warned them

                // Non allied empires will always warn if the system is exclusively owned by them
                bool warnExclusive = !usToThem.Treaty_Alliance && system.IsExclusivelyOwnedBy(OwnerEmpire);
                foreach (MarkForColonization theirGoal in theirColonizationGoals)
                {
                    Planet theirColonizeP = theirGoal.TargetPlanet;
                    if (theirColonizeP.System != system)
                        continue;

                    if (DetectAndWarn(theirGoal, warnExclusive))
                    {
                        if (system.HasPlanetsOwnedBy(them)
                            && theirColonizeP.System == them.Capital?.System
                            && theirColonizeP != ourColonizeP
                            && !warnAnyway)
                        {
                            continue; // They already have colonies in this system and targeting a different planet, or its their home system
                        }

                        if (them.isPlayer)
                            DiplomacyScreen.Show(OwnerEmpire, "Claim System", system);

                        usToThem.WarnedSystemsList.AddUnique(system);
                    }
                }
            }

            bool GetColonizationGoalsList(Empire empire, out MarkForColonization[] planetList)
            {
                planetList = empire.AI.ExpansionAI.GetColonizationGoals();
                return planetList.Length > 0;
            }

            bool DetectAndWarn(Goal goal, bool warnExclusive)
            {
                if (goal.FinishedShip?.KnownByEmpires.KnownBy(OwnerEmpire) == true || OwnerEmpire.Random.RollDice(DetectChanceByTreaty()))
                {
                    // Detected their colonization efforts
                    if (warnExclusive || warnAnyway)
                        return true;

                    if (themToUs.WarnedSystemsList.Contains(goal.TargetPlanet.System))
                        return false; // They warned us, so no need to warn them

                    // If they stole planets from us, we will value our targets more.
                    // If we have more max pop then them, we will cut them some slack and vice versa
                    // If we have nice relations, we will cut them some slack as well
                    // If the planet is closer to them, consider that as well
                    Planet p = goal.TargetPlanet;
                    float sqDistToUs = p.System.Position.SqDist(OwnerEmpire.WeightedCenter);
                    float sqDistToThem = p.System.Position.SqDist(them.WeightedCenter);
                    float distRatio = (sqDistToThem / sqDistToUs.LowerBound(1)).Clamped(0.1f, 3);
                    float popRatio = them.MaxPopBillion / OwnerEmpire.MaxPopBillion.LowerBound(1);
                    float valueToUs = p.ColonyPotentialValue(OwnerEmpire) * (usToThem.NumberStolenClaims + 1)
                        * popRatio * distRatio * ValueToUsMultiplerByRelations();
                    float valueToThem = p.ColonyPotentialValue(them);
                    float ratio = valueToUs / valueToThem.LowerBound(1);

                    return ratio > OwnerEmpire.PersonalityModifiers.ColonizationClaimRatioWarningThreshold;
                }

                return false;
            }

            float ValueToUsMultiplerByRelations()
            {
                float multiplier = 1;
                if      (usToThem.Treaty_Alliance)    multiplier = 0.25f;
                else if (usToThem.Treaty_OpenBorders) multiplier = 0.5f;
                else if (usToThem.Treaty_Trade)       multiplier = 0.75f;
                return multiplier;
            }

            float DetectChanceByTreaty()
            {
                float chance = 0.075f;
                if      (usToThem.Treaty_Alliance)    chance = 0.75f;
                else if (usToThem.Treaty_OpenBorders) chance = 0.5f;
                else if (usToThem.Treaty_Trade)       chance = 0.25f;
                else if (usToThem.Treaty_NAPact)      chance = 0.15f;
                return chance;

            }
        }

        public void TriggerRefit()
        {
            if (OwnerEmpire.isPlayer)
                return;

            var offPool = OwnerEmpire.EmpireShips.OwnedShips;
            for (int i = offPool.Length - 1; i >= 0; i--)
            {
                Ship ship = offPool[i];
                if (ship.AI.BadGuysNear
                    || ship.IsFreighter
                    || ship.AI.HasPriorityOrder
                    || ship.AI.HasPriorityTarget
                    || !ship.CanBeRefitted
                    || ship.IsResearchStation
                    || ship.IsMiningStation
                    || ship.IsMiningShip
                    || ship.ShipData.IsColonyShip
                    || ship.IsConstructor)
                {
                    continue;
                }

                IShipDesign newShip = ShipBuilder.PickShipToRefit(ship, OwnerEmpire);
                if (newShip != null)
                {
                    AddGoalAndEvaluate(new RefitShip(ship, newShip, OwnerEmpire));
                    foreach (Planet p in OwnerEmpire.GetPlanets())
                        p.Construction.RefitShipsBeingBuilt(ship, newShip);
                }
            }
        }

        public void AddScrapShipGoal(Ship ship, bool immediateScuttle)
        {
            AddGoalAndEvaluate(new ScrapShip(ship, OwnerEmpire, immediateScuttle));
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

        public void AddDeployResearchStationGoal(Planet p)
        {
            string researchStationName = OwnerEmpire.data.CurrentResearchStation;
            if (!OwnerEmpire.isPlayer || OwnerEmpire.AutoPickBestResearchStation)
            {
                IShipDesign bestResearchStation = ShipBuilder.PickResearchStation(OwnerEmpire);
                if (bestResearchStation != null)
                    researchStationName = bestResearchStation.Name;
            }

            AddGoal(new BuildOrbital(p, researchStationName, OwnerEmpire));
        }

        public void CancelResearchStation(Planet p)
        {
            Goal stationGoal = FindGoal(g => g.IsResearchStationGoal(p));
            if (stationGoal != null)
                RemoveGoal(stationGoal);

            Goal constructionGoal = FindGoal(g => g.IsBuildingOrbitalFor(p) && g.Build.Template.IsResearchStation);
            if (constructionGoal != null)
            {
                constructionGoal.FinishedShip?.AI.OrderScrapShip();
                constructionGoal.PlanetBuildingAt?.Construction.Cancel(constructionGoal);
                RemoveGoal(constructionGoal);
            }
        }

        public void CancelMiningStation(Planet p)
        {
            Goal stationGoal = FindGoal(g => g.IsMiningOpsGoal(p) && g.TargetShip == null);
            if (stationGoal != null)
                RemoveGoal(stationGoal);

            Goal constructionGoal = FindGoal(g => g.IsBuildingOrbitalFor(p) && g.Build.Template.IsMiningStation);
            if (constructionGoal != null)
            {
                constructionGoal.FinishedShip?.AI.OrderScrapShip();
                constructionGoal.PlanetBuildingAt?.Construction.Cancel(constructionGoal);
                RemoveGoal(constructionGoal);
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

        public void AddGoalAndEvaluate(Goal goal)
        {
            GoalsList.Add(goal);
            goal.Evaluate();
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