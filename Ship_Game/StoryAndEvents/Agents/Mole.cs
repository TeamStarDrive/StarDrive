using System;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public sealed class Mole
    {
        [StarData] public int PlanetId;

        public static Mole PlantMole(Empire owner, Empire target, out string targetPlanetName)
        {
            targetPlanetName = "";
            var potentials = new Array<Planet>();
            foreach (Planet p in target.GetPlanets())
            {
                if (!p.IsExploredBy(owner))
                {
                    continue;
                }
                bool GoodPlanet = true;
                foreach (Mole m in target.data.MoleList)
                {
                    if (m.PlanetId != p.Id)
                    {
                        continue;
                    }
                    GoodPlanet = false;
                    break;
                }
                if (!GoodPlanet)
                {
                    break;
                }
                potentials.Add(p);
            }
            if (potentials.Count == 0)
            {
                potentials = new Array<Planet>(target.GetPlanets());
            }
            Mole mole = null;
            if (potentials.Count > 0)
            {
                int Random = (int)RandomMath.Float(0f, potentials.Count + 0.7f);
                if (Random > potentials.Count - 1)
                {
                    Random = potentials.Count - 1;
                }
                mole = new Mole
                {
                    PlanetId = potentials[Random].Id
                };

                targetPlanetName = potentials[Random].Name;
                owner.data.MoleList.Add(mole);
            }
            return mole;
        }
    }
}