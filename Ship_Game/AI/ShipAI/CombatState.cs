using System.ComponentModel;

namespace Ship_Game.AI
{
    /// <summary>
    /// CombatState is used to determine how the ship moves in combat.
    /// The items should be in order of shortest to longest desired range. 
    /// </summary>
    public enum CombatState
    {
        /// <summary> For a carrier use the min between max weapons range and hangar range. </summary>
        ShortRange,
        /// <summary> move in to attack and then move away in a looping pattern. </summary>
        AttackRuns,
        /// <summary> Maintain a left facing to the target at max weapons range. </summary>
        BroadsideLeft,
        /// <summary> Maintain a right facing to the target at max weapons range. </summary>
        BroadsideRight,
        /// <summary> Orbit target maintaining a left facing to the target at max weapons range. </summary>
        OrbitLeft,
        /// <summary> Orbit target maintaining a right facing to the target at max weapons range. </summary>
        OrbitRight,
        /// <summary> Face the target and move backwards or forwards to maintain max weapons range </summary>
        Artillery,
        /// <summary> Launch or land troops for invasion. Code for this behavior should use the ships sensor range.  </summary>
        AssaultShip,
        /// <summary> Don't move under any circumstance. Code for this behavior should use the ships sensor range.  </summary>
        HoldPosition,
        /// <summary> Flee to another system. Code for this behavior should use the ships sensor range.  </summary>
        Evade,
        /// <summary> Do not use any combat movement. Code for this behavior should use the ships sensor range.  </summary>
        OrbitalDefense
    }
}