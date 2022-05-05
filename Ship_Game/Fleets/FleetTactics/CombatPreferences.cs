using SDGraphics;
using Ship_Game.Ships;

namespace Ship_Game.Fleets.FleetTactics
{
    public class CombatPreferences
    {
        /// <summary>
        /// Defend 
        /// </summary>
        public void SetTacticDefense(Array<Ship> flankShips, float aoRadius = 7500f)
        {
            foreach (var ship in flankShips)
            {
                var node = ship.AI.FleetNode;
                ChangeNodeWeight(ref node.VultureWeight, 0f);
                ChangeNodeWeight(ref node.AttackShieldedWeight, 0f);
                ChangeNodeWeight(ref node.AssistWeight, 0.0f);
                ChangeNodeWeight(ref node.DefenderWeight, 1.0f);
                ChangeNodeWeight(ref node.SizeWeight, 0.5f);
                ChangeNodeWeight(ref node.ArmoredWeight, 0f);
                ChangeNodeWeight(ref node.DPSWeight, 0.25f);
                ChangeNodeWeight(ref node.OrdersRadius, aoRadius);
            }
        }

        /// <summary>
        /// Attack the dangerous targets
        /// </summary>
        public void SetTacticAttack(Array<Ship> flankShips, float aoRadius = 7500f)
        {
            foreach (var ship in flankShips)
            {
                var node = ship.AI.FleetNode;
                ChangeNodeWeight(ref node.VultureWeight, 0.0f);
                ChangeNodeWeight(ref node.AttackShieldedWeight, 0.0f);
                ChangeNodeWeight(ref node.DPSWeight, 1.0f);
                ChangeNodeWeight(ref node.ArmoredWeight, 0.0f);
                ChangeNodeWeight(ref node.AssistWeight, 1.0f);
                ChangeNodeWeight(ref node.DefenderWeight, 0.0f);
                ChangeNodeWeight(ref node.SizeWeight, .75f);
                ChangeNodeWeight(ref node.OrdersRadius, aoRadius);
            }
        }

        /// <summary>
        /// Attack wounded smaller targets.
        /// Defend nearby ships as well. 
        /// </summary>
        public void SetTacticIntercept(Array<Ship> flankShips, float aoRadius = 7500f)
        {
            foreach (var ship in flankShips)
            {
                var node = ship.AI.FleetNode;
                ChangeNodeWeight(ref node.VultureWeight, 1.0f);
                ChangeNodeWeight(ref node.AttackShieldedWeight, 0.0f);
                ChangeNodeWeight(ref node.AssistWeight, 0.0f);
                ChangeNodeWeight(ref node.DefenderWeight, 0.0f);
                ChangeNodeWeight(ref node.SizeWeight, 0.0f);
                ChangeNodeWeight(ref node.ArmoredWeight, 0.0f);
                ChangeNodeWeight(ref node.DPSWeight, 0.5f);
                ChangeNodeWeight(ref node.OrdersRadius, aoRadius);
            }
        }

        void ChangeNodeWeight(ref float currentValue, float newValue)
        {
            if (currentValue.AlmostEqual(0.5f))
                currentValue = newValue;
        }
    }
}