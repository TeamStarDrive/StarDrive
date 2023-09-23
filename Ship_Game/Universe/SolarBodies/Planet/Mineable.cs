using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using System;
using System.Drawing;
using System.Xml.Serialization;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    [StarDataType]
    public class Mineable
    {
        public const int MaximumMiningStations = 4;
        [StarData] readonly int NameTranslationIndex;
        [StarData] readonly int DescriptionIndex;
        [StarData] readonly string IconName;
        [StarData] public readonly Planet P;
        [StarData] public readonly ExoticResource ResourceType;

        [StarData] public Empire Owner { get; private set; }
        [StarData] public float RefiningRatio { get; private set; } // how much  of the resource is processed per turn
        [StarData] public float Richness { get; private set; } // how much of the resource is exctracted per turn

        public string CargoName => Enum.GetName(typeof(ExoticResource), ResourceType);
        public LocalizedText TranslatedResourceName => new(NameTranslationIndex);
        public LocalizedText ResourceDescription => new(DescriptionIndex);
        public SubTexture ExoticResourceIcon => ResourceManager.Texture($"NewUI/{IconName}");
        public static SubTexture Icon => ResourceManager.Texture($"NewUI/icon_exotic_resource");

        public bool CanAddMiningStationFor(Empire empire) => (Owner == empire || Owner == null) && MiningOpsSize < MaximumMiningStations;

        public bool MiningOpsExistFor(Empire empire) => Owner == empire && MiningOpsSize > 0;
        int MiningOpsSize => Owner != null ? Owner.AI.CountGoals(g => g.IsMiningOpsGoal(P)) : 0;


        public bool AreMiningOpsPresentBy(Empire empire)
        {
            return P.OrbitalStations.Any(o => o.IsMiningStation && o.Loyalty == empire);
        }

        public bool AreMiningOpsPresent()
        {
            return P.OrbitalStations.Any(o => o.IsMiningStation);
        }

        public void ChangeOwner(Empire empire)
        {
            Owner = empire;
        }


        public Mineable(Planet planet)
        {
            P = planet;
            planet.Universe.AddMineablePlanet(planet);
            ResourceType = GetRandomResourceType(planet.Universe);
            ExoticResourceStats stats = new ExoticResourceStats(ResourceType);
            RefiningRatio = stats.RefiningRatio;
            Richness = planet.Universe.Random.RollDie(stats.MaxRichness);
            NameTranslationIndex = stats.NameIndex;
            DescriptionIndex = stats.DescriptionIndex;
            IconName = stats.IconName;
        }

        public Mineable()
        {
        }

        struct ExoticResourceStats
        {
            public readonly float RefiningRatio; // How much is processed per 1 foot in the processing unit module
            public readonly byte MaxRichness; // Speed of mining per turn
            public readonly int NameIndex;
            public readonly int DescriptionIndex;
            public readonly int Weight;
            public readonly string IconName;

            public ExoticResourceStats(ExoticResource type)
            {
                switch (type)
                {
                    default:
                    case ExoticResource.Pulsefield: 
                        RefiningRatio = 1;
                        MaxRichness = 10;
                        NameIndex = 4953;
                        DescriptionIndex = 4954;
                        Weight = 5;
                        IconName = "Exotic_Pulsefield";
                        return;
                }
            }
        }

        ExoticResource GetRandomResourceType(UniverseState universe)
        {
            Array<ExoticResource> resourcesPool = new();
            foreach (ExoticResource resource in Enum.GetValues(typeof(ExoticResource)))
            {
                ExoticResourceStats resouceStats = new ExoticResourceStats(resource);
                int weight = resouceStats.Weight;
                for (int i = 0; i < weight; i++) 
                    resourcesPool.Add(resource);
            }

            return universe.Random.Item(resourcesPool);
        }
    }
    public enum ExoticResource
    {
        Pulsefield,
        Aetherium,
        Fortifume,
        Luxarium,
        Energon,
        NanoGas
    }
}
