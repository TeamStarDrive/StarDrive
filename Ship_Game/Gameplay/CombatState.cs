using System;

namespace Ship_Game.Gameplay
{
	public enum CombatState
	{
		Artillery,
        BroadsideLeft,
        BroadsideRight,
		OrbitLeft,
		OrbitRight,
		AttackRuns,
		HoldPosition,
		Evade,
		AssaultShip,
		OrbitalDefense
	}
}