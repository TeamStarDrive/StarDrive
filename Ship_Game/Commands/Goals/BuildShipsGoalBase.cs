using System;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public abstract class BuildShipsGoalBase : Goal
    {
        IShipDesign ShipTemplate;
        float FindPlanetRetryTimer;

        protected BuildShipsGoalBase(GoalType type, Empire owner) : base(type, owner)
        {
        }

        protected bool GetShipTemplate(string uid, out IShipDesign template)
        {
            if (ShipTemplate == null)
            {
                ResourceManager.Ships.GetDesign(uid, out ShipTemplate);
            }
            return (template = ShipTemplate) != null;
        }

        protected bool GetFreighter(out IShipDesign freighterTemplate)
        {
            if (ShipTemplate == null)
            {
                ShipTemplate = ShipBuilder.PickFreighter(Owner, Owner.FastVsBigFreighterRatio)?.ShipData;
                if (ShipTemplate == null)
                    throw new Exception($"PickFreighter failed for {Owner.Name}."+
                                        "This is a FATAL bug in data files, where Empire is not able to build any freighters!");
            }
            return (freighterTemplate = ShipTemplate) != null;
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
            if (!GetShipTemplate(ToBuildUID, out IShipDesign template))
                return GoalStep.GoalFailed;

            if (!FindPlanetToBuildShipAt(portType, template, out Planet planet, priority: 1f))
                return GoalStep.TryAgain;

            planet.Construction.Enqueue(template, this);
            
            return GoalStep.GoToNextStep;
        }
    }
}
