using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public abstract class BuildShipsGoalBase : Goal
    {
        ShipDesign ShipTemplate;
        float FindPlanetRetryTimer;

        protected BuildShipsGoalBase(GoalType type) : base(type)
        {
        }

        protected bool GetShipTemplate(string uid, out ShipDesign template)
        {
            if (ShipTemplate == null)
            {
                ResourceManager.Ships.GetDesign(uid, out ShipTemplate);
            }
            return (template = ShipTemplate) != null;
        }

        protected bool GetFreighter(out ShipDesign freighterTemplate)
        {
            if (ShipTemplate == null)
            {
                var design = ShipBuilder.PickFreighter(empire, empire.FastVsBigFreighterRatio);
                if (design == null)
                {
                    freighterTemplate = null;
                    return false;
                }
                ShipTemplate = design.shipData;
            }
            return (freighterTemplate = ShipTemplate) != null;
        }

        protected enum SpacePortType { Any, Safe }

        protected bool FindPlanetToBuildShipAt(SpacePortType portType, ShipDesign ship, out Planet planet, float priority)
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

            if (empire.FindPlanetToBuildShipAt(spacePorts, ship, out planet, priority))
            {
                return true; // OK
            }

            // search failed, so lets wait a bit before retrying this expensive operation
            FindPlanetRetryTimer = 3f;
            return false;
        }

        protected GoalStep TryBuildShip(SpacePortType portType)
        {
            if (!GetShipTemplate(ToBuildUID, out ShipDesign template))
                return GoalStep.GoalFailed;

            if (!FindPlanetToBuildShipAt(portType, template, out Planet planet, priority: 1f))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(template, this);
            
            return GoalStep.GoToNextStep;
        }
    }
}
