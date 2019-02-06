using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    /// <summary>
    /// This is a proxy workaround for the current buggy implementation
    /// of `Empire.Universe?.Property` where Universe can be null.
    ///
    /// At some point this can be redesigned as non-global
    /// </summary>
    public static class CurrentGame
    {
        // @note GamePace is used all over the place while UniverseData is still being constructed
        //       So we need to pull it out of UniverseScreen
        public static float Pace { get; private set; } = 1f;
        public static UniverseData.GameDifficulty Difficulty { get; private set; }

        public static void StartNew(UniverseData data, float pace)
        {
            Difficulty = data.difficulty;
            Pace = pace;
            RandomEventManager.ActiveEvent = null;
        }
    }
}
