using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.AI.CombatTactics
{
    internal sealed class OrbitTarget : ShipAIPlan
    {
        readonly ShipAI.Orbit OrbitDirection;
        public OrbitTarget(ShipAI ai, ShipAI.Orbit direction ) : base(ai)
        {
            OrbitDirection = direction;
        }
        public override void Execute(float elapsedTime, ShipAI.ShipGoal g)
        {
             Vector2 nextOrbitPoint = AI.SetNextOrbitPoint(AI.Target.Center, OrbitDirection, AI.Owner.MaxWeaponRange *.75f);
            AI.SubLightContinuousMoveInDirection(AI.Owner.Center.DirectionToTarget(nextOrbitPoint), elapsedTime, 0);
        }
    }
}
