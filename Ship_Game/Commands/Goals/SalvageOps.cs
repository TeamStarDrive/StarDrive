using System;
using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    // an empire's one salvage station in a system: ordered, built by a constructor, then kept while it lasts
    [StarDataType]
    public class SalvageOps : Goal
    {
        [StarData] public SolarSystem TargetSystem;
        [StarData] public sealed override Ship TargetShip { get; set; }
        [StarData] public Vector2 StaticBuildPos;
        [StarData] IShipDesign StationToBuild;
        Ship SalvageStation => TargetShip;
        public override IShipDesign ToBuild => StationToBuild;

        public override bool IsSalvageOpsGoal(SolarSystem system) => system != null && TargetSystem == system;

        [StarDataConstructor]
        public SalvageOps(Empire owner) : base(GoalType.SalvageOps, owner)
        {
            Steps = new Func<GoalStep>[]
            {
                BuildStationConstructor,
                WaitForConstructor,
                Salvage
            };
        }

        public SalvageOps(Empire owner, SolarSystem system, Vector2 buildPos, IShipDesign stationToBuild) : this(owner)
        {
            StarDateAdded = owner.Universe.StarDate;
            TargetSystem = system;
            StaticBuildPos = buildPos;
            StationToBuild = stationToBuild;
        }

        // only in a system with a salvageable derelict the empire knows of, and one per empire per system
        public static bool CanOrderIn(Empire empire, SolarSystem system)
        {
            return system != null && empire.Universe.P.SalvageOps
                && empire.Universe.Derelicts.AnyKnownIn(empire, system)
                && !empire.AI.HasGoal(g => g?.IsSalvageOpsGoal(system) == true);
        }

        public const float Clearance = 2000f; // from every salvageable derelict and every other salvage station

        // from any thread: inside the system, clear of its star, radiation and planets' orbits, and of its derelicts and the salvage stations among `ships`
        public static bool IsValidSite(SolarSystem system, Vector2 pos, Ship[] ships)
        {
            if (pos.OutsideRadius(system.Position, system.Radius * DeepSpaceBuildingWindow.SystemPlacementRadius)
                || pos.InRadius(system.Position, DeepSpaceBuildingWindow.MinimumBuildDistanceFromSun)
                || !system.InSafeDistanceFromRadiation(pos))
                return false;

            for (int i = 0; i < system.PlanetList.Count; ++i)
                if (pos.InRadius(system.PlanetList[i].Position, DeepSpaceBuildingWindow.TetherRadius(system.PlanetList[i])))
                    return false;

            Derelict[] derelicts = system.Universe.Derelicts.Current;
            for (int i = 0; i < derelicts.Length; ++i)
                if (derelicts[i].System == system && derelicts[i].CanBeSalvaged && pos.InRadius(derelicts[i].RestPosition, Clearance))
                    return false;

            for (int i = 0; i < ships.Length; ++i)
                if (ships[i].IsSalvageStation && ships[i].Active && !ships[i].Dying && pos.InRadius(ships[i].Position, Clearance))
                    return false;

            return true;
        }

        // an order ends, with any constructor it has, when salvage ops are off (a save loaded under a mod without salvage content), and before its station is built once its system has no derelict left to salvage
        protected override GoalStep? PreEvaluate()
        {
            if (Owner.Universe.P.SalvageOps && (SalvageStation != null || Owner.DerelictSystems.Contains(TargetSystem)))
                return null;

            Owner.AI.CancelSalvageStationConstruction(TargetSystem);
            return GoalStep.GoalFailed;
        }

        GoalStep BuildStationConstructor()
        {
            // orders placed together from the UI reach the sim thread together, and the first one holds the system
            if (Owner.AI.FindGoal(g => g.IsSalvageOpsGoal(TargetSystem)) != this)
                return GoalStep.GoalFailed;

            StationToBuild ??= ShipBuilder.PickSalvageStation(Owner);
            if (StationToBuild == null)
                return GoalStep.GoalFailed;

            if (!Owner.FindPlanetToBuildShipAt(Owner.SafeSpacePorts, StationToBuild, out Planet planetToBuildAt, portQuality: 1f))
                return GoalStep.TryAgain;

            Owner.AI.AddGoal(new BuildOrbital(planetToBuildAt, TargetSystem, StationToBuild.Name, Owner, StaticBuildPos));
            return GoalStep.GoToNextStep;
        }

        GoalStep WaitForConstructor()
        {
            if (SalvageStation != null)
            {
                if (Owner.isPlayer)
                    Owner.Universe.Notifications?.AddSalvageStationBuiltNotification(SalvageStation, TargetSystem);
                return GoalStep.GoToNextStep;
            }

            return ConstructionGoalInProgress ? GoalStep.TryAgain : GoalStep.GoalFailed;
        }

        GoalStep Salvage()
        {
            if (SalvageStation is not { Active: true } || SalvageStation.Loyalty != Owner)
                return GoalStep.GoalFailed;

            return GoalStep.TryAgain;
        }

        bool ConstructionGoalInProgress
            => Owner.AI.HasGoal(g => g.IsBuildingOrbitalFor(TargetSystem) && g.ToBuild?.IsSalvageStation == true);
    }
}
