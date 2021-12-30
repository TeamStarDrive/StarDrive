using System;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public sealed class Mole
    {
        [StarData] public Guid PlanetGuid;

        public static Mole PlantMole(Empire owner, Empire target, out string targetPlanetName)
        {
            targetPlanetName = "";
            Array<Planet> potentials = new Array<Planet>();
            foreach (Planet p in target.GetPlanets())
            {
                if (!p.IsExploredBy(owner))
                {
                    continue;
                }
                bool GoodPlanet = true;
                foreach (Mole m in target.data.MoleList)
                {
                    if (m.PlanetGuid != p.guid)
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
                int Random = (int)RandomMath.RandomBetween(0f, potentials.Count + 0.7f);
                if (Random > potentials.Count - 1)
                {
                    Random = potentials.Count - 1;
                }
                mole = new Mole
                {
                    PlanetGuid = potentials[Random].guid
                };

                targetPlanetName = potentials[Random].Name;
                owner.data.MoleList.Add(mole);
            }
            return mole;
        }
    }
}