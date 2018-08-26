using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public static class Vector
    {
        // +Y is downwards on the screen
        public static Vector2 Down() => new Vector2(0f, +1f);

        // -Y is downwards on the screen
        public static Vector2 Up() => new Vector2(0f, -1f);
    }
}
