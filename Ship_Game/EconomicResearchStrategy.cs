using System;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public sealed class EconomicResearchStrategy
    {
        [StarData] public readonly string Name;
        [StarData] public readonly string[] TechPath = Empty<string>.Array;

        [StarData] public readonly byte MilitaryPriority = 5;
        [StarData] public readonly byte ExpansionPriority = 5;
        [StarData] public readonly byte ResearchPriority = 5;
        [StarData] public readonly byte IndustryPriority = 5;

        float PriorityRatio(float priority) 
            => Math.Max(priority / (MilitaryPriority + ExpansionPriority + ResearchPriority + IndustryPriority), 0.1f);

        public float MilitaryRatio => PriorityRatio(MilitaryPriority);
        public float ExpansionRatio => PriorityRatio(ExpansionPriority);
        public float ResearchRatio => PriorityRatio(ResearchPriority);
        public float IndustryRatio => PriorityRatio(IndustryPriority);
    }
}