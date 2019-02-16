

namespace Ship_Game.Empires
{
    public class SubSpaceProjectors
    {
        public float Radius { get; }
  
        public SubSpaceProjectors(float universeWidth)
        {
            Radius = universeWidth * 0.04f;
        }
    }
}
