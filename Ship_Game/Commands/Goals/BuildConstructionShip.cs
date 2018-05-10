using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI;

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
                WaitMainGoalCompletion,
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
            Array<Planet> list = new Array<Planet>();
            foreach (Planet planet in this.empire.GetPlanets())
            {
                if (planet.HasShipyard)
                    list.Add(planet);
            }
            IOrderedEnumerable<Planet> orderedEnumerable = ((IEnumerable<Planet>)list).OrderBy<Planet, float>((Func<Planet, float>)(planet => planet.ConstructionQueue.Count));

            int TotalPlanets = (orderedEnumerable).Count<Planet>();
            if (TotalPlanets <= 0)
                return GoalStep.TryAgain;

            int leastque = (orderedEnumerable).ElementAt<Planet>(0).ConstructionQueue.Count;
            float leastdist = float.MaxValue;
            int bestplanet = 0;

            for (short looper = 0; looper < TotalPlanets; looper++)
            {
                if ((orderedEnumerable).ElementAt<Planet>(looper).ConstructionQueue.Count > leastque)
                    break;
                float currentdist = Vector2.Distance(orderedEnumerable.ElementAt(looper).Center, this.BuildPosition);
                if (currentdist < leastdist)
                {
                    bestplanet = looper;
                    leastdist = currentdist;
                }
            }

            PlanetBuildingAt = orderedEnumerable.ElementAt(bestplanet);
            QueueItem queueItem = new QueueItem();
            queueItem.isShip = true;
            queueItem.DisplayName = "Construction Ship";
            queueItem.QueueNumber = PlanetBuildingAt.ConstructionQueue.Count;
            queueItem.sData = ResourceManager.ShipsDict[EmpireManager.Player.data.CurrentConstructor].shipData;
            queueItem.Goal = this;
            queueItem.Cost = ResourceManager.ShipsDict[this.ToBuildUID].GetCost(this.empire);
            queueItem.NotifyOnEmpty = false;
            if (!string.IsNullOrEmpty(this.empire.data.CurrentConstructor) && ResourceManager.ShipsDict.ContainsKey(this.empire.data.CurrentConstructor))
            {
                this.beingBuilt = ResourceManager.ShipsDict[this.empire.data.CurrentConstructor];
            }
            else
            {
                this.beingBuilt = null;
                string empiredefaultShip = this.empire.data.DefaultConstructor;
                if (string.IsNullOrEmpty(empiredefaultShip))
                {
                    empiredefaultShip = this.empire.data.DefaultSmallTransport;
                }
                ResourceManager.ShipsDict.TryGetValue(empiredefaultShip, out this.beingBuilt);
                this.empire.data.DefaultConstructor = empiredefaultShip;
            }
            orderedEnumerable.ElementAt(bestplanet).ConstructionQueue.Add(queueItem); //Gretman

            return GoalStep.GoToNextStep;
        }

    }
}
