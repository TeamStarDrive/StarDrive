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
        public static UniverseData.GameDifficulty Difficulty { get; private set; }

        public static void StartNew(UniverseData data)
        {
            Difficulty = data.difficulty;
        }
    }
}
