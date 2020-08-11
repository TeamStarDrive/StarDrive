using System.Web.UI;

namespace Ship_Game.Ships.DataPackets
{
    /// <summary>
    /// Should be run before any ship updates are done.
    /// </summary>
    public class KnownByEmpire
    {
        /// <summary>
        /// The draw time buffer. we aren't locked here so the draw might read while the value is being updated.
        /// this gives a little buffer to help the draw routine deal with that. 
        /// </summary>

        /// <summary>
        /// Gets a value indicating whether ship is [known by player]. Used for ui stuff.
        /// use KnownBy(empire) for everything but UI
        /// </summary>
        /// <value>
        ///   <c>true</c> if [known by player]; otherwise, <c>false</c>.
        /// </value>
        public bool KnownByPlayer => SeenByID[EmpireManager.Player.Id-1] + KnownDuration > 0;
        float[] SeenByID;
        /// <summary>
        /// The known duration. how long the object will be known for. .5 = roughly half a second. 
        /// </summary>
        public const float KnownDuration = 1f;

        public KnownByEmpire()
        {
            SeenByID = new float[EmpireManager.NumEmpires];
        }

        /// <summary>
        /// Updates the specified elapsed time.
        /// </summary>
        /// <param name="elapsedTime">The elapsed time.</param>
        public void Update(float elapsedTime)
        {
            if (SeenByID.Length != EmpireManager.NumEmpires)
            {
                var newArray = new Array<float>(SeenByID);
                newArray.Resize(EmpireManager.NumEmpires);
                SeenByID = newArray.ToArray();
            }

            for (int i = 0; i < EmpireManager.NumEmpires; i++)
            {
                SeenByID[i] -= elapsedTime;
            }
        }

        /// <summary>
        /// Sets if the ship has been seen by an empire;
        /// </summary>
        /// <param name="empire">The empire.</param>
        /// <param name="timer">The timer.</param>
        public void SetSeen(Empire empire, float timer = KnownDuration) => SeenByID[empire.Id-1] = timer;
        public bool KnownBy(Empire empire)             => SeenByID[empire.Id-1] + KnownDuration > 0;

        /// <summary>
        /// Sets the ship as seen by player. Unlike "knownByPlayer" this can be used anywhere. 
        /// </summary>
        public void SetSeenByPlayer()                  => SetSeen(EmpireManager.Player);
    }
}