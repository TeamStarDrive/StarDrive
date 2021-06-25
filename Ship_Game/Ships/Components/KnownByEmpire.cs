using System;
using System.Web.UI;

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
        public bool KnownByPlayer => (SeenByID[EmpireManager.Player.Id-1] + Empire.MaxContactTimer) > 0;

        public KnownByEmpire()
        {
            SeenByID = new float[EmpireManager.NumEmpires];
            for (int i = 0; i < SeenByID.Length; i++)
                SeenByID[i] = -100;
        }

        float[] GetSeenByID()
        {
            float[] seenById = SeenByID;
            if (seenById.Length != EmpireManager.NumEmpires)
            {
                var newArray = new float[EmpireManager.NumEmpires];
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
            float[] seenById = GetSeenByID();
            for (int i = 0; i < seenById.Length; i++)
            {
                seenById[i] -= timeStep.FixedTime;
            }
            seenById[owner.Id-1] = Empire.MaxContactTimer;
        }

        /// <summary>
        /// Sets if the ship has been seen by an empire;
        /// </summary>
        /// <param name="empire">The empire.</param>
        public void SetSeen(Empire empire)
        {
            float[] seenById = GetSeenByID();
            seenById[empire.Id-1] = Empire.MaxContactTimer;
        }

        public bool KnownBy(Empire empire)
        {
            float[] seenById = GetSeenByID();
            return (seenById[empire.Id-1] + Empire.MaxContactTimer) > 0;
        }

        /// <summary>
        /// Sets the ship as seen by player. Unlike "knownByPlayer" this can be used anywhere. 
        /// </summary>
        public void SetSeenByPlayer() => SetSeen(EmpireManager.Player);
    }
}