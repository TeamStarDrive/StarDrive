

namespace Ship_Game.Empires
{
    public class SubSpaceProjectors
    {
        public static void SetProjectorSize(float universeWidth) => ProjectorRadius = universeWidth * .04f;
        public static float ProjectorRadius { get; private set; } = 150000f;
    }
}
