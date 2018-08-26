using System;

namespace Ship_Game
{
    public sealed class EconomicResearchStrategy
    {
        public string Name;

        public Array<Tech> TechPath = new Array<Tech>();

        public byte MilitaryPriority = 5;
        public byte ExpansionPriority = 5;
        public byte ResearchPriority = 5;
        public byte IndustryPriority = 5;

        public float PriorityRatio(float priority) => Math.Max( priority  / (MilitaryPriority + ExpansionPriority + ResearchPriority + IndustryPriority ) , .1f);
        public float MilitaryRatio => PriorityRatio(MilitaryPriority);
        public float ExpansionRatio => PriorityRatio(ExpansionPriority);
        public float ResearchRatio => PriorityRatio(ResearchPriority);
        public float IndustryRatio => PriorityRatio(IndustryPriority);
        public struct Tech
        {
            public string id;
        }
    }
}