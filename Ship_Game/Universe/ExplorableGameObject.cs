using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDUtils;

namespace Ship_Game.Universe
{
    public class ExplorableGameObject : GameObject
    {
                // this is a sparse map where [Empire.Id-1] is the index
        Empire[] ExploredBy = Empty<Empire>.Array;

        public ExplorableGameObject(int id, GameObjectType type) : base(id, type)
        {
        }

        public bool IsExploredBy(Empire empire)  => ExploredBy.FlatMapIsSet(empire);
        public void SetExploredBy(Empire empire) => ExploredBy.FlatMapSet(ref ExploredBy, empire);
        public void SetExploredBy(string empireName) => SetExploredBy(EmpireManager.GetEmpireByName(empireName));
        public void SetExploredBy(string[] empireNames)
        {
            if (empireNames == null)
                return;
            foreach (string empireName in empireNames)
                SetExploredBy(empireName);
        }

        public Empire[] ExploredByEmpires
        {
            get
            {
                // probe first:
                int count = 0;
                for (int i = 0; i < ExploredBy.Length; ++i)
                    if (ExploredBy[i] != null)
                        ++count;

                // single alloc
                var empires = new Empire[count];
                for (int i = 0, n = 0; i < ExploredBy.Length; ++i)
                {
                    Empire empire = ExploredBy[i];
                    if (empire != null)
                        empires[n++] = empire;
                }
                return empires;
            }
        }
    }
}
