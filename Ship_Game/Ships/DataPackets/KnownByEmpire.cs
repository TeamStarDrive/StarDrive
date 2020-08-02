using System.Web.UI;

namespace Ship_Game.Ships.DataPackets
{
    /// <summary>
    /// Should be run before any ship updates are done.
    /// </summary>
    public class KnownByEmpire
    {
        public bool KnownByPlayer => SeenByID[EmpireManager.Player.Id-1] > 0;
        float[] SeenByID;
        public const float KnownDuration = 2;

        public KnownByEmpire()
        {
            SeenByID = new float[EmpireManager.NumEmpires];
        }

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

        public void SetSeen(Empire empire)          => SeenByID[empire.Id-1] = KnownDuration;
        public bool KnownBy(Empire empire)          => SeenByID[empire.Id-1] > 0;
        public void SetSeenByPlayer()               => SetSeen(EmpireManager.Player);
    }
}