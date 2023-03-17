using Microsoft.Xna.Framework.Graphics;
using SDGraphics;

namespace Ship_Game.Universe.SolarBodies
{
    public struct DistanceDisplay
    {
        public readonly string Text;
        public readonly Color Color;
        Distances PlanetDistance;

        public DistanceDisplay(float distance) : this()
        {
            DeterminePlanetDistanceCategory(distance);
            switch (PlanetDistance)
            {
                case Distances.Local:   Text = "Local";   Color = Color.Green;         break;
                case Distances.Near:    Text = "Near";    Color = Color.YellowGreen;   break;
                case Distances.Midway:  Text = "Midway";  Color = Color.DarkGoldenrod; break;
                case Distances.Distant: Text = "Distant"; Color = Color.DarkRed;       break;
                default:                Text = "Beyond";  Color = Color.DarkGray;      break;
            }
        }

        void DeterminePlanetDistanceCategory(float distance)
        {
            if      (distance.LessOrEqual(140))  PlanetDistance = Distances.Local;
            else if (distance.LessOrEqual(1200)) PlanetDistance = Distances.Near;
            else if (distance.LessOrEqual(3000)) PlanetDistance = Distances.Midway;
            else if (distance.LessOrEqual(6000)) PlanetDistance = Distances.Distant;
            else                                 PlanetDistance = Distances.Beyond;
        }

        enum Distances
        {
            Local,
            Near,
            Midway,
            Distant,
            Beyond
        }
    }
}
