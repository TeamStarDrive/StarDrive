using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Gameplay;

namespace Ship_Game.Commands.MilitaryTasks
{
    public class CohesiveClearAreaOfEnemies : MilitaryTask //@proposal add all the code that is associated with the task here. 
    {
        public CohesiveClearAreaOfEnemies(AO ao)
        {
            AO = ao.Position;
            AORadius = ao.Radius;
            type = TaskType.CohesiveClearAreaOfEnemies;
            WhichFleet = ao.WhichFleet;
            IsCoreFleetTask = true;
            SetEmpire(ao.GetCoreFleet().Owner);
        }
    }
}
