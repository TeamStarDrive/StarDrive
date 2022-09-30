using System;
using SDUtils;
using Ship_Game.Empires;
using Ship_Game.Universe;

namespace Ship_Game.Ships.Components
{
    /// <summary>
    /// Should be run before any ship updates are done.
    /// </summary>
    public class KnownByEmpire
    {
        float[] SeenByID;

        /// <summary>
        /// Gets a value indicating whether ship is [known by player]. Used for ui stuff.
        /// use KnownBy(empire) for everything but UI
        /// </summary>
        /// <value>
        ///   <c>true</c> if [known by player]; otherwise, <c>false</c>.
        /// </value>
        public bool KnownByPlayer(UniverseState us) => SeenByID[us.Player.Id-1] > 0f;

        public KnownByEmpire(UniverseState us)
        {
            SeenByID = us != null ? new float[us.NumEmpires] : Empty<float>.Array;
            for (int i = 0; i < SeenByID.Length; i++)
                SeenByID[i] = -100;
        }

        float[] GetSeenByID(UniverseState us)
        {
            float[] seenById = SeenByID;
            if (seenById.Length < us.NumEmpires)
            {
                var newArray = new float[us.NumEmpires];
                Array.Copy(seenById, newArray, seenById.Length);
                SeenByID = newArray;
                return newArray;
            }
            return seenById;
        }

        /// <summary>
        /// Updates visibility timers of all known empires
        /// Resets visibility for owner
        /// </summary>
        public void Update(FixedSimTime timeStep, Empire owner)
        {
            float[] seenById = GetSeenByID(owner.Universe);
            for (int i = 0; i < seenById.Length; i++)
            {
                seenById[i] -= timeStep.FixedTime;
            }
            seenById[owner.Id-1] = EmpireConstants.KnownContactTimer;
        }

        /// <summary>
        /// Sets if the ship has been seen by an empire;
        /// </summary>
        /// <param name="empire">The empire.</param>
        public void SetSeen(Empire empire)
        {
            float[] seenById = GetSeenByID(empire.Universe);
            seenById[empire.Id-1] = EmpireConstants.KnownContactTimer;
        }

        /// <summary>
        /// TRUE if the empire is marked as seen, including grace time
        /// </summary>
        public bool KnownBy(Empire empire)
        {
            float[] seenById = GetSeenByID(empire.Universe);
            return seenById[empire.Id-1] > 0f;
        }

        /// <summary>
        /// TRUE if empire is set
        /// </summary>
        public bool IsSet(Empire empire)
        {
            float[] seenById = GetSeenByID(empire.Universe);
            return seenById[empire.Id-1] > 0f;
        }
    }
}