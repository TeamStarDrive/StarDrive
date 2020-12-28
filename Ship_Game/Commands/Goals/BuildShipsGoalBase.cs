using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public abstract class BuildShipsGoalBase : Goal
    {
        Ship ShipTemplate;
        float FindPlanetRetryTimer;

        protected BuildShipsGoalBase(GoalType type) : base(type)
        {
        }

        protected bool GetShipTemplate(string uid, out Ship template)
        {
            if (ShipTemplate == null)
            {
                ResourceManager.GetShipTemplate(uid, out ShipTemplate);
            }
            return (template = ShipTemplate) != null;
        }

        protected bool GetFreighter(out Ship freighterTemplate)
        {
            if (ShipTemplate == null)
            {
                ShipTemplate = ShipBuilder.PickFreighter(empire, empire.FastVsBigFreighterRatio);
            }
            return (freighterTemplate = ShipTemplate) != null;
        }

        protected enum SpacePortType { Any, Safe }

        protected bool FindPlanetToBuildShipAt(SpacePortType portType, Ship ship, out Planet planet)
        {
            FindPlanetRetryTimer -= 0.016f; // fixed countdown
            if (FindPlanetRetryTimer > 0f)
            {
                planet = null;
                return false;
            }

            Planet[] spacePorts = portType == SpacePortType.Safe
                                ? empire.SafeSpacePorts
                                : empire.SpacePorts;

            if (empire.FindPlanetToBuildAt(spacePorts, ship, out planet))
            {
                return true; // OK
            }

            // search failed, so lets wait a bit before retrying this expensive operation
            FindPlanetRetryTimer = 3f;
            return false;
        }

        protected GoalStep FindPlanetToBuildAt(SpacePortType portType)
        {
            if (!GetShipTemplate(ToBuildUID, out Ship template))
                return GoalStep.GoalFailed;

            if (!FindPlanetToBuildShipAt(portType, template, out Planet planet))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(template, this);
            return GoalStep.GoToNextStep;
        }
    }
}
