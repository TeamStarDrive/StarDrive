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

		public float LargeAttackWeight = 3f;

		public float PreferredEngagementDistance = 1500f;

		public CombatAI()
		{
		}
        public CombatAI(Ship_Game.Gameplay.Ship ship)
        {
            int Size = ship.Size;
            if (Size == 0)
            {
                return;
            }
            else
            {
                UpdateCombatAI(ship);
            }
;

        }
        public void UpdateCombatAI(Ship_Game.Gameplay.Ship ship)
        {
            int Size = ship.Size;
            this.LargeAttackWeight = Size / 100 > 1 ? Size / 200 > 1 ? 4 : 3 : .5f;  
            this.SmallAttackWeight = 30 / Size > 1 ? 3 : 0;
            this.MediumAttackWeight = Size / 100 < 1 && 30 / Size > 1 ? 3 : .5f ;
            float stlspeed = ship.GetSTLSpeed();
            if (stlspeed > 500) this.VultureWeight = (int)(stlspeed / 250);
        }
	}
}