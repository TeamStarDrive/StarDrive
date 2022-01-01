

namespace Ship_Game.Empires
{
    public class SubSpaceProjectors
    {
        public float Radius { get; }

        public SubSpaceProjectors(float universeWidth)
        {
            float sspRadius = GlobalStats.Unsupported_ProjectorRadius;
            Radius = sspRadius < 1000 ? universeWidth * 0.04f : sspRadius;
        }
    }
}
