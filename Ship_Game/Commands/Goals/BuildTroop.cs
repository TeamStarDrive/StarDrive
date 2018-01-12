using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.AI;

namespace Ship_Game.Commands.Goals
{
    public class BuildTroop : Goal
    {
        public const string ID = "Build Troop";
        public override string UID => ID;

        public BuildTroop() : base(GoalType.BuildTroop)
        {
        }
        public BuildTroop(Troop toCopy, Empire owner, Planet p) : base(GoalType.BuildTroop)
        {
            PlanetBuildingAt = p;
            ToBuildUID = toCopy.Name;
            empire = owner;
        }

        public override void Evaluate()
        {
            if (Held)
                return;

            switch (Step)
            {
                case 0:
                    if (ToBuildUID != null)
                    {
                        Troop troopTemplate = ResourceManager.GetTroopTemplate(ToBuildUID);
                        PlanetBuildingAt.ConstructionQueue.Add(new QueueItem()
                        {
                            isTroop = true,
                            QueueNumber = PlanetBuildingAt.ConstructionQueue.Count,
                            troopType = ToBuildUID,
                            Goal = this,
                            Cost = troopTemplate.GetCost()
                        });
                    }
                    else Log.Info("Missing Troop {0}", ToBuildUID);
                    Step = 1;
                    break;

                case 1:
                {
                    break;
                }
                case 2:
                    this.empire.GetGSAI().Goals.QueuePendingRemoval(this);
                    break;
            }
        }
    }
}
