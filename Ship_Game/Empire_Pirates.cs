using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Commands.Goals;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public partial class Empire
    {
        bool GetCorsairOrbitals(out Array<Ship> bases, string baseName)
        {
            bases = new Array<Ship>();
            for (int i = 0; i < OwnedShips.Count; i++)
            {
                Ship ship = OwnedShips[i];
                if (ship.Name == baseName)
                    bases.Add(ship);
            }

            return bases.Count > 0;
        }

        public bool GetCorsairBases(out Array<Ship> bases) => GetCorsairOrbitals(out bases, "Corsair Asteroid Base");
        public bool GetCorsairStations(out Array<Ship> bases) => GetCorsairOrbitals(out bases, "Corsair Station");

        bool GetCorsairOrbitalsOrbitingPlanets(out Array<Ship> planetBases)
        {
            planetBases = new Array<Ship>();
            GetCorsairBases(out Array<Ship> bases);
            GetCorsairStations(out Array<Ship> stations);
            bases.AddRange(stations);

            for (int i = 0; i < bases.Count; i++)
            {
                Ship pirateBase = bases[i];
                if (pirateBase.GetTether() != null)
                    planetBases.AddUnique(pirateBase);
            }

            return planetBases.Count > 0;
        }

        public bool GetClosestCorsairBasePlanet(Vector2 fromPos, out Planet planet)
        {
            planet = null;
            if (!GetCorsairOrbitalsOrbitingPlanets(out Array<Ship> bases))
                return false;

            Ship pirateBase = bases.FindMin(b => b.Center.Distance(fromPos));
            planet          = pirateBase.GetTether();

            return planet != null;
        }

        public void CorsairsTryLevelUp()
        {
            if (this != EmpireManager.Corsairs)
                return; // Only for pirates

            if (RandomMath.RollDie(20) > PirateThreatLevel)
                PirateThreatLevel = (PirateThreatLevel + 1).Clamped(0, 20);
        }
    }
}
