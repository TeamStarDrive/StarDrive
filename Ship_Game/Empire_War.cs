using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Ship_Game.AI.Tasks;

namespace Ship_Game
{
    public partial class Empire
    {
        public Goal[] GetDefendSystemsGoal()
        {
            return EmpireAI.Goals.Filter(g => g.type == GoalType.DefendSystem);
        }

        public bool NoEmpireDefenseGoal()
        {
            return !EmpireAI.Goals.Any(g => g.type == GoalType.EmpireDefense);
        }

        public void AddDefenseSystemGoal(SolarSystem system, int priority, float strengthWanted, int fleetCount)
        {
            EmpireAI.Goals.Add(new DefendSystem(this, system, priority, strengthWanted, fleetCount));
        }

        public bool IsAlreadyDefendingSystem(SolarSystem system)
        {
            return EmpireAI.Goals.Any(g => g.type == GoalType.DefendSystem);
        }
    }
}
