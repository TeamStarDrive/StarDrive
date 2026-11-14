using SDGraphics;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class DeepSpaceBuildGoal : Goal
    {
        [StarData] public sealed override BuildableShip Build { get; set; }
        [StarData] public Vector2 StaticBuildPos;
        [StarData] public Planet TetherPlanet; // not the same as TargetPlanet
        [StarData] public Vector2 TetherOffset;
        [StarData] public SolarSystem TargetSystem;

        // Pathfinder detour chain for the construction ship's dispatch to BuildPosition,
        // pre-computed when OrderDeepSpaceBuild dispatches the ship. Empty when the
        // straight line is clear or routing is disabled. DetourIndex advances as the
        // constructor passes each detour during execution.
        [StarData] public Vector2[] Detours;
        [StarData] public int DetourIndex;

        public override IShipDesign ToBuild => Build.Template;
        public override bool IsBuildingOrbitalFor(Planet planet) => TetherPlanet != null && TetherPlanet == planet;
        public override bool IsBuildingOrbitalFor(SolarSystem system) => TargetSystem == system;

        [StarDataConstructor]
        public DeepSpaceBuildGoal(GoalType type, Empire owner) : base(type, owner)
        {
        }

        protected void Initialize(string shipUid, Vector2 buildPos, SolarSystem targetSystem)
        {
            Build = new(shipUid);
            StaticBuildPos = buildPos;
            TargetSystem = targetSystem;
            if (buildPos.IsNaN())
                Log.Error($"NaN StaticBuildPos={buildPos}");
        }

        protected void Initialize(string shipUid, Vector2 buildPos, Planet planet, Vector2 offset)
        {
            Initialize(shipUid, buildPos, planet.System);
            TetherPlanet = planet;
            TetherOffset = offset;
            if (offset.IsNaN())
                Log.Error($"NaN TetherOffset={offset}");
        }
        public override bool IsDeploymentGoal => true;

        bool TetherPlanetTakenByOtherEmpire => TetherPlanet?.Owner != null && TetherPlanet.Owner != Owner;

        protected override GoalStep? PreEvaluate()
        {
            DropFinishedShipNoLongerOurs();
            if (TetherPlanetTakenByOtherEmpire)
            {
                // We no longer own the planet this orbital was for: scrap the in-flight
                // constructor (if one was already built) and abandon the goal. The base
                // Evaluate removes the goal once we return GoalFailed.
                FinishedShip?.AI.OrderScuttleShip();
                return GoalStep.GoalFailed;
            }

            return null;
        }

        public override Vector2 BuildPosition
        {
            get
            {
                if (TetherPlanet != null) return TetherPlanet.Position + TetherOffset;
                return StaticBuildPos;
            }
        }

        public override Vector2 MovePosition
        {
            get
            {
                Planet targetPlanet = TetherPlanet ?? TargetPlanet;
                if (targetPlanet != null)
                    return targetPlanet.Position + TetherOffset;
                return BuildPosition;
            }
        }

        public Vector2 GetThrustTarget(Vector2 shipPosition)
            => GravityWellRouter.GetThrustTarget(Detours, ref DetourIndex, BuildPosition, shipPosition);
    }
}
