using System;
using Ship_Game;
using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public abstract class BuildShipsGoalBase : Goal
    {
        [StarData] public sealed override BuildableShip Build { get; set; }
        [StarData] public sealed override Planet PlanetBuildingAt { get; set; }
        public override IShipDesign ToBuild => Build.Template;
        float FindPlanetRetryTimer;

        protected BuildShipsGoalBase(GoalType type, Empire owner) : base(type, owner)
        {
        }

        protected enum SpacePortType { Any, Safe }

        protected bool FindPlanetToBuildShipAt(SpacePortType portType, IShipDesign ship, out Planet planet, float priority)
        {
            FindPlanetRetryTimer -= 0.016f; // fixed countdown
            if (FindPlanetRetryTimer > 0f)
            {
                planet = null;
                return false;
            }

            Planet[] spacePorts = portType == SpacePortType.Safe
                                ? Owner.SafeSpacePorts
                                : Owner.SpacePorts;

            if (Owner.FindPlanetToBuildShipAt(spacePorts, ship, out planet, priority))
            {
                return true; // OK
            }

            // search failed, so lets wait a bit before retrying this expensive operation
            FindPlanetRetryTimer = 3f;
            return false;
        }

        protected GoalStep TryBuildShip(SpacePortType portType)
        {
            if (!FindPlanetToBuildShipAt(portType, Build.Template, out Planet planet, priority: 1f))
                return GoalStep.TryAgain;
            
            PlanetBuildingAt = planet;
            planet.Construction.Enqueue(Build.Template, this);
            return GoalStep.GoToNextStep;
        }
    }
}
