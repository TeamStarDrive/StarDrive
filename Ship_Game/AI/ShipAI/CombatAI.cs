using System;
using Ship_Game.AI.ShipMovement;
using Ship_Game.Ships;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework;

namespace Ship_Game.AI
{
    public sealed class CombatAI
    {
        ShipAI AI;
        ShipAIPlan CombatTactic;
        CombatState CurrentCombatStance;

        public CombatAI(ShipAI ai)
        {
            AI = ai;
            CurrentCombatStance = ai.CombatState;
            SetCombatTactics(CurrentCombatStance);
        }

        public void SetCombatTactics(CombatState combatState)
        {
            if (CurrentCombatStance != combatState)
            {
                CurrentCombatStance = combatState;
                CombatTactic = null;
                AI.Owner.ShipStatusChanged = true; // FIX: force DesiredCombatRange update
            }

            if (CombatTactic == null)
            {
                switch (combatState)
                {
                    case CombatState.Artillery:
                        CombatTactic = new CombatTactics.Artillery(AI);
                        break;
                    case CombatState.BroadsideLeft:
                        CombatTactic = new CombatTactics.BroadSides(AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.BroadsideRight:
                        CombatTactic = new CombatTactics.BroadSides(AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.OrbitLeft:
                        CombatTactic = new CombatTactics.OrbitTarget(AI, OrbitPlan.OrbitDirection.Left);
                        break;
                    case CombatState.OrbitRight:
                        CombatTactic = new CombatTactics.OrbitTarget(AI, OrbitPlan.OrbitDirection.Right);
                        break;
                    case CombatState.AttackRuns:
                        CombatTactic = new CombatTactics.AttackRun(AI);
                        break;
                    case CombatState.HoldPosition:
                        CombatTactic = new CombatTactics.HoldPosition(AI);
                        break;
                    case CombatState.Evade:
                        CombatTactic = new CombatTactics.Evade(AI);
                        break;
                    case CombatState.OrbitalDefense:
                        break;
                    case CombatState.GuardMode: // in guard mode use Artillery, to preserve fleet formation
                    case CombatState.AssaultShip:
                    case CombatState.ShortRange:
                        CombatTactic = new CombatTactics.Artillery(AI);
                        break;
                }

            }
        }

        public void ExecuteCombatTactic(FixedSimTime timeStep)
        {
            CombatTactic?.Execute(timeStep, null);
        }
    }
}