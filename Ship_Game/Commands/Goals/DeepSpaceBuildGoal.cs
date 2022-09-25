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

        public override IShipDesign ToBuild => Build.Template;
        public override bool IsBuildingOrbitalFor(Planet planet) => TetherPlanet == planet;

        [StarDataConstructor]
        public DeepSpaceBuildGoal(GoalType type, Empire owner) : base(type, owner)
        {
        }

        protected void Initialize(string shipUid, Vector2 buildPos)
        {
            Build = new(shipUid);
            StaticBuildPos = buildPos;
        }

        protected void Initialize(string shipUid, Vector2 buildPos, Planet planet, Vector2 offset)
        {
            Initialize(shipUid, buildPos);
            TetherPlanet = planet;
            TetherOffset = offset;
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
    }
}
