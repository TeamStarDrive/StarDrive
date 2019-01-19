using System;
using Microsoft.Xna.Framework;
using Ship_Game.AI;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    public class BuildConstructionShip : Goal
    {
        public const string ID = "BuildConstructionShip";
        public override string UID => ID;

        public BuildConstructionShip() : base(GoalType.DeepSpaceConstruction)
        {
            Steps = new Func<GoalStep>[]
            {
                FindPlanetToBuildAt,
                WaitMainGoalCompletion
            };
        }

        public BuildConstructionShip(Vector2 buildPosition, string platformUid, Empire owner) : this()
        {
            BuildPosition = buildPosition;
            ToBuildUID = platformUid;
            empire = owner;
            Evaluate();
        }

        private GoalStep FindPlanetToBuildAt()
        {
            var shipyardPlanets = new Array<Planet>();
            foreach (Planet planet in empire.GetPlanets())
                if (planet.HasSpacePort)
                    shipyardPlanets.Add(planet);

            if (shipyardPlanets.IsEmpty)
                return GoalStep.TryAgain;

            // pick planet with best chance for quickly completing our production
            Planet shipyard = shipyardPlanets.FindMin(p =>
            {
                // @todo We should use production based estimation instead
                float distance = p.Center.Distance(BuildPosition);
                return distance + distance*p.NumConstructing;
            });

            if (!ResourceManager.GetShipTemplate(ToBuildUID, out Ship toBuild))
            {
                Log.Error($"BuildConstructionShip: no ship to build with uid={ToBuildUID ?? "null"}");
                return GoalStep.GoalFailed;
            }

            string constructionShip = empire.data.ConstructorShip;
            if (!ResourceManager.GetShipTemplate(constructionShip, out beingBuilt))
            {
                Log.Error($"BuildConstructionShip: no construction ship with uid={constructionShip}");
                return GoalStep.GoalFailed;
            }

            var queueItem = new QueueItem(shipyard)
            {
                isShip        = true,
                Goal          = this,
                NotifyOnEmpty = false,
                DisplayName = "Construction Ship",
                QueueNumber = shipyard.NumConstructing,
                sData       = beingBuilt.shipData,
                Cost        = toBuild.GetCost(empire)
            };

            shipyard.ConstructionQueue.Add(queueItem);
            PlanetBuildingAt = shipyard;
            return GoalStep.GoToNextStep;
        }

    }
}
