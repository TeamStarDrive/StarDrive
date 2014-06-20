using System;

namespace Ship_Game
{
	public class CombatAI
	{
		public float VultureWeight = 1f;

		public float AssistWeight = 0.1f;

		public float SelfDefenseWeight = 1f;

		public float OthersDefenseWeight = 1f;

		public float SmallAttackWeight = 1f;

		public float MediumAttackWeight = 1f;

		public float LargeAttackWeight = 1f;

		public float PreferredEngagementDistance = 1500f;

		public CombatAI()
		{
		}
	}
}