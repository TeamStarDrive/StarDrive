using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using System;
using System.Xml.Serialization;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    [StarDataType]
    public class Mineable
    {
        [StarData] public readonly Planet P;
        [StarData] public readonly int NameTranslationIndex;
        [StarData] public readonly int DescriptionIndex;

        [StarData] public Empire Owner { get; private set; }
        [StarData] public float ProcessingRatio { get; private set; }
        [StarData] public float Richness { get; private set; }
        public LocalizedText ResourceName => new(NameTranslationIndex);
        public LocalizedText ResourceText => new(DescriptionIndex);

        public Mineable(Planet planet)
        {
            P = planet;
            planet.Universe.AddMineablePlanet(planet);
            ExoticResourceStats stats = new ExoticResourceStats(ExoticResource.Pulsefield);
            ProcessingRatio = stats.ProcessingRatio;
            Richness = planet.Universe.Random.RollDie(stats.MaxRichness);
            NameTranslationIndex = stats.NameIndex;
            DescriptionIndex = stats.DescriptionIndex;
        }

        public Mineable()
        {
        }

        struct ExoticResourceStats
        {
            public readonly float ProcessingRatio; // How much is processed per 1 foot in the processing unit module
            public readonly byte MaxRichness; // Speed of mining per turn
            public readonly int NameIndex;
            public readonly int DescriptionIndex;

            public ExoticResourceStats(ExoticResource type)
            {
                switch (type)
                {
                    default:
                    case ExoticResource.Pulsefield: 
                        ProcessingRatio = 1;
                        MaxRichness = 10;
                        NameIndex = 4440;
                        DescriptionIndex = 4441;
                        return;
                }
            }
        }

        enum ExoticResource
        {
            Pulsefield,
            Aetherium,
            Fortifume,
            Luxarium,
            Energon,
            NanoGas
        }
    }
}
