using System;
using System.ComponentModel;

namespace Ship_Game.AI
{
    /// <summary>
    /// CombatState is used to determine how the ship moves in combat.
    /// The order of the enum CANNOT be changed due to the save process mapping the integer values to integer values. 
    /// </summary>
    public enum CombatState
    {
        /// <summary> Face the target and move backwards or forwards to maintain max weapons range </summary>
        Artillery,
        /// <summary> Maintain a left facing to the target at max weapons range. </summary>
        BroadsideLeft,
        /// <summary> Maintain a right facing to the target at max weapons range. </summary>
        BroadsideRight,
        /// <summary> Orbit target maintaining a left facing to the target at max weapons range. </summary>
        OrbitLeft,
        /// <summary> Orbit target maintaining a right facing to the target at max weapons range. </summary>
        OrbitRight,
        /// <summary> move in to attack and then move away in a looping pattern. </summary>
        AttackRuns,
        /// <summary> Don't move under any circumstance. Code for this behavior should use the ships sensor range.  </summary>
        HoldPosition,
        /// <summary> Flee to another system. Code for this behavior should use the ships sensor range.  </summary>
        Evade,
        /// <summary> Launch or land troops for invasion. Code for this behavior should use the ships sensor range.  </summary>
        AssaultShip,
        /// <summary> Do not use any combat movement. Code for this behavior should use the ships sensor range.  </summary>
        OrbitalDefense,
        /// <summary> For a carrier use the min between max weapons range and hangar range. </summary>
        ShortRange,
        /// <summary> No combat stance. Take no action in combat </summary>
        None
    }

    /// <summary>
    /// Group CombatStates .
    /// </summary>
    public static class CombatStanceType
    {
        public enum StanceType
        {
            /// <summary>
            /// Combat Movement using ranges generally based on Ship weapons and modules. 
            /// </summary>
            RangedCombatMovement,

            /// <summary>
            /// Non combat Movement or no movement generally using ranges based on ship sensors. 
            /// </summary>
            NonCombatMovement,
            None
        }

        /// <summary>
        /// Converts CombatState to a StanceType.
        /// </summary>
        public static StanceType ToStanceType(CombatState combatState)
        {
            switch (combatState)
            {
                case CombatState.Artillery:
                case CombatState.BroadsideLeft:
                case CombatState.BroadsideRight:
                case CombatState.OrbitLeft:
                case CombatState.OrbitRight:
                case CombatState.AttackRuns:
                case CombatState.ShortRange:
                case CombatState.AssaultShip:
                    return StanceType.RangedCombatMovement;
                case CombatState.HoldPosition:
                case CombatState.Evade:
                case CombatState.OrbitalDefense:
                    return StanceType.NonCombatMovement;
            }
            return StanceType.None;
        }
    }
}