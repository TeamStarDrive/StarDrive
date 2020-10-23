namespace Ship_Game
{
    public sealed class SolarSystemData
    {
        public string Name;
        public string SunPath = "star_yellow";
        public Array<Ring> RingList = new Array<Ring>();

        public struct Ring
        {
            public string Planet; //Planet Name
            public string SpecialDescription;
            public int WhichPlanet; //planet Type
            public string Asteroids; //true or false.
            public string HasRings; //Planet has rings.
            public bool HomePlanet; //is an empire home planet. 
            public float planetScale; //1 or 0 is normal. larger or smaller than 1 increases or decreases planet size. 
            public string Owner; //who ones the planet. 
            public string Station; //Has a space port
            public Array<Moon> Moons; //an array of moons.
            public Array<string> BuildingList; //buildings on planet.
            public float MaxPopDefined; // pop per tile, disregarding planet scale
            public int OrbitalDistance; // custom orbital radius override
            public int OrbitalWidth; // custom orbital spacing override
            //Using a separate boolean to ensure that modders can define an unusual 0-habitability planet (e.g. 0 tile Terran); otherwise would have to disregard 0.
            public bool UniqueHabitat;
            public int UniqueHabPC;

            public override string ToString()
            {
                if (WhichPlanet != 0)
                    return $"{Planet} Rings:{HasRings} {ResourceManager.Planet(WhichPlanet)}";
                return $"{Planet} Ast:{Asteroids} Rings:{HasRings}";
            }
        }

        public struct Moon
        {
            public int WhichMoon;
            public float MoonScale;

            public override string ToString() => $"Moon  P:{ResourceManager.Planet(WhichMoon)}";
        }
    }
}