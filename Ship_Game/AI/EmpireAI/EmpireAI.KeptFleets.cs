using SDUtils;
using Ship_Game.Fleets;

// ReSharper disable once CheckNamespace
namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        // the cap's floor follows the most it has been; over the cap, the weakest idle fleet joins the nearest until the count is back at it
        void JoinIdleFleetsOverTheCap()
        {
            if (!OwnerEmpire.KeepsFleets)
                return;

            OwnerEmpire.UpdatePeakFleetCap();

            for (int over = OwnerEmpire.KeptFleetCount() - OwnerEmpire.KeptFleetCapForJoins(); over > 0; --over)
            {
                Fleet weakest = null;
                float weakestStrength = float.MaxValue;
                foreach (Fleet fleet in OwnerEmpire.ActiveFleets)
                {
                    if (fleet.IsIdle && fleet.GetStrength() < weakestStrength)
                    {
                        weakest = fleet;
                        weakestStrength = fleet.GetStrength();
                    }
                }

                if (weakest == null)
                    return;

                Fleet[] nearest = OwnerEmpire.IdleFleetsNearest(weakest.AveragePosition(force: true));
                Fleet joinTo = nearest.Find(f => f != weakest);
                if (joinTo == null)
                    return;

                joinTo.TakeInIdleFleet(weakest);
            }
        }
    }
}
