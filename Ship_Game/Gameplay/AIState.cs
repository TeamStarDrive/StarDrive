using System;

namespace Ship_Game.Gameplay
{
	public enum AIState
	{
		DoNothing,
		Combat,
		HoldPosition,
		ManualControl,
		AwaitingOrders,
		AttackTarget,
		Escort,
		SystemTrader,
		AttackRunner,
		Orbit,
		PatrolSystem,
		PassengerTransport,
		Flee,
		Colonize,
		MoveTo,
		PirateRaiderCarrier,
		Explore,
		SystemDefender,
		AwaitingOffenseOrders,
		Resupply,
		Rebase,
		Bombard,
		Boarding,
		ReturnToHangar,
		MineAsteroids,
		Ferrying,
		Refit,
		Scrap,
		Intercept,
		FormationWarp,
		AssaultPlanet,
		Exterminate,
		Scuttle
	}
}