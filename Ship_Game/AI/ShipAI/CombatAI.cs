using System;
using SDUtils;
using Ship_Game.AI.ShipMovement;

namespace Ship_Game.AI;

public sealed class CombatAI : IDisposable
{
    readonly ShipAI AI;
    ShipAIPlan CombatTactic;
    CombatState CurrentCombatStance;

    public CombatAI(ShipAI ai)
    {
        AI = ai;
        CurrentCombatStance = ai.CombatState;
        SetCombatTactics(CurrentCombatStance);
    }

    public void Dispose()
    {
        Mem.Dispose(ref CombatTactic);
    }

    public void SetCombatTactics(CombatState combatState)
    {
        if (CurrentCombatStance != combatState)
        {
            CurrentCombatStance = combatState;
            Mem.Dispose(ref CombatTactic);
            AI.Owner.ShipStatusChanged = true; // FIX: force DesiredCombatRange update
        }

        if (CombatTactic == null)
        {
            switch (combatState)
            {
                case CombatState.Artillery:
                    SetCombatTactic(new CombatTactics.Artillery(AI));
                    break;
                case CombatState.BroadsideLeft:
                    SetCombatTactic(new CombatTactics.BroadSides(AI, OrbitPlan.OrbitDirection.Left));
                    break;
                case CombatState.BroadsideRight:
                    SetCombatTactic(new CombatTactics.BroadSides(AI, OrbitPlan.OrbitDirection.Right));
                    break;
                case CombatState.OrbitLeft:
                    SetCombatTactic(new CombatTactics.OrbitTarget(AI, OrbitPlan.OrbitDirection.Left));
                    break;
                case CombatState.OrbitRight:
                    SetCombatTactic(new CombatTactics.OrbitTarget(AI, OrbitPlan.OrbitDirection.Right));
                    break;
                case CombatState.AttackRuns:
                    SetCombatTactic(new CombatTactics.AttackRun(AI));
                    break;
                case CombatState.HoldPosition:
                    SetCombatTactic(new CombatTactics.HoldPosition(AI));
                    break;
                case CombatState.Evade:
                    SetCombatTactic(new CombatTactics.Evade(AI));
                    break;
                case CombatState.OrbitalDefense:
                    break;
                case CombatState.GuardMode: // in guard mode use Artillery, to preserve fleet formation
                case CombatState.AssaultShip:
                case CombatState.ShortRange:
                    SetCombatTactic(new CombatTactics.Artillery(AI));
                    break;
            }
        }
    }

    void SetCombatTactic(ShipAIPlan tactic)
    {
        CombatTactic = tactic;
    }

    public void ExecuteCombatTactic(FixedSimTime timeStep)
    {
        CombatTactic?.Execute(timeStep, null);
    }
}
