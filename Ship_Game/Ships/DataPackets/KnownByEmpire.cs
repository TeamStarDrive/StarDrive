using System;
using System.Web.UI;

namespace Ship_Game.Ships.DataPackets
{
    /// <summary>
    /// Should be run before any ship updates are done.
    /// </summary>
    public class KnownByEmpire
    {
        Ship Owner;
        float[] SeenByID;

        /// <summary>
        /// Gets a value indicating whether ship is [known by player]. Used for ui stuff.
        /// use KnownBy(empire) for everything but UI
        /// </summary>
        /// <value>
        ///   <c>true</c> if [known by player]; otherwise, <c>false</c>.
        /// </value>
        public bool KnownByPlayer => SeenByID[EmpireManager.Player.Id-1] + KnownDuration > 0;

        /// <summary>
        /// The known duration. how long the object will be known for. 0.5 = roughly half a second.
        /// </summary>
        public float KnownDuration => Owner.loyalty.MaxContactTimer;

        public KnownByEmpire(Ship ship)
        {
            SeenByID = new float[EmpireManager.NumEmpires];
            for (int i = 0; i < SeenByID.Length; i++)
                SeenByID[i] = -100;
            Owner = ship;
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
        /// </summary>
        public void Update(FixedSimTime timeStep)
        {
            float[] seenById = GetSeenByID();
            for (int i = 0; i < seenById.Length; i++)
            {
                seenById[i] -= timeStep.FixedTime;
            }
        }

        /// <summary>
        /// Sets if the ship has been seen by an empire;
        /// </summary>
        /// <param name="empire">The empire.</param>
        public void SetSeen(Empire empire)
        {
            float[] seenById = GetSeenByID();
            seenById[empire.Id-1] = KnownDuration;
        }

        public bool KnownBy(Empire empire)
        {
            float[] seenById = GetSeenByID();
            return seenById[empire.Id-1] + KnownDuration > 0;
        }

        /// <summary>
        /// Sets the ship as seen by player. Unlike "knownByPlayer" this can be used anywhere. 
        /// </summary>
        public void SetSeenByPlayer() => SetSeen(EmpireManager.Player);
    }
}