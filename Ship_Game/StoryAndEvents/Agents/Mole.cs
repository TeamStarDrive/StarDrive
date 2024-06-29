using System.Linq;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public sealed class Mole
    {
        [StarData] public int PlanetId;
        [StarData] public bool Sticky { get; set; } // cannot be removed with counter espionage in new espionage system

        public static Mole PlantMole(Empire owner, Empire target, out Planet targetPlanet)
        {
            targetPlanet = null;
            if (target.IsDefeated) 
                return null;

            var potentials = target.GetPlanets().Filter(p => p.IsExploredBy(owner) 
                                                             && !owner.data.MoleList.Any(m => m.PlanetId == p.Id));

            if (potentials.Length == 0)
                potentials = target.GetPlanets().ToArray();

            Planet targetplanet = target.Random.Item(potentials);
            Mole mole = new()
            {
                PlanetId = targetplanet.Id,
            };

            owner.data.MoleList.Add(mole);
            return mole;
        }

        public static Mole PlantStickyMoleAtHomeworld(Empire owner, Empire target, out Planet targetPlanet)
        {
            targetPlanet = null;
            var planets = target.GetPlanets().Filter(p => p.IsHomeworld || p.HasCapital);

            targetPlanet = planets.Length == 0 ? target.GetPlanets().FindMax(p => p.PopulationBillion)
                                                : target.Random.Item(planets);

            Mole mole = new()
            {
                PlanetId = targetPlanet.Id, Sticky = true,  
            };

            owner.data.MoleList.Add(mole);
            return mole;
        }
    }
}