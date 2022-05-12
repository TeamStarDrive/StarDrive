using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Empires
{
    /// <summary>
    /// Contains constants which control how empires behave
    /// Affected categories are:
    ///  - Sensor Known Timers
    ///  - Projector Influence Time
    ///  - Threat Scanning Timers
    /// </summary>
    public static class EmpireConstants
    {
        /// <summary>
        /// How long a visible entity should be known for.
        /// If set to 1.0, then we see Ships for 1 extra second after they exit sensor range
        /// </summary>
        public const float KnownContactTimer = 1.0f;

        /// <summary>
        /// How often ships choose targets.
        /// This is not particular expensive, and relies on existing scan data
        /// This should always be less or equal to EnemyScanInterval
        /// </summary>
        public const float TargetSelectionInterval = 1.0f;

        /// <summary>
        /// How often PD-capable ships should scan for projectiles
        /// This should not be too often, otherwise late-game will suffer badly
        /// </summary>
        public const float ProjectileScanInterval = 0.75f;

        /// <summary>
        /// How often to scan friendlies
        /// We don't need to do this very often
        /// </summary>
        public const float FriendScanInterval = 5.0f;

        /// <summary>
        /// Enemy scan doesn't have to be done too often either
        /// </summary>
        public const float EnemyScanInterval = 1.0f;
    }
}
