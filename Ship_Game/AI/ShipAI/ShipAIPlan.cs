using System;
using Ship_Game.Ships;
using Ship_Game.Utils;

namespace Ship_Game.AI;

internal abstract class ShipAIPlan : IDisposable
{
    public ShipAI AI;
    public Ship Owner;
    public RandomBase Random => Owner.Loyalty.Random;

    protected ShipAIPlan(ShipAI ai)
    {
        AI = ai;
        Owner = ai.Owner;
    }

    ~ShipAIPlan() { Dispose(false); }

    public abstract void Execute(FixedSimTime timeStep, ShipAI.ShipGoal g);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        AI = null;
        Owner = null;
    }
}
