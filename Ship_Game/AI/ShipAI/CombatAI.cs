using Ship_Game.Gameplay;

namespace Ship_Game.AI
{
    public sealed class CombatAI
    {
        public float VultureWeight = 1;
        public float SelfDefenseWeight = 1f;
        public float SmallAttackWeight = 1f;
        public float MediumAttackWeight = 1f;
        public float LargeAttackWeight = 3f;
        public float PreferredEngagementDistance = 1500f;
        public float PirateWeight;          

        public Ship Owner;

        public CombatAI()
        {
        }

        public CombatAI(Ship ship)
        {
            int size = ship.Size;
            if (size == 0) return;
            Owner = ship;
            UpdateCombatAI(ship);
        }

        public void UpdateCombatAI(Ship ship)
        {
            if (ship.Size <= 0) return;
            FleetDataNode node = Owner?.AI.FleetNode;
            if (Owner?.fleet != null)
            {                
                VultureWeight               = node.VultureWeight;
                PreferredEngagementDistance = node.OrdersRadius;
                SelfDefenseWeight           = node.DefenderWeight;

            }
            else
                PreferredEngagementDistance = ship.maxWeaponsRange * 0.75f;

            byte pd        = 0;
            byte mains     = 0;
            float fireRate = 0;
            foreach(Weapon w in ship.Weapons)
            {
                if(w.isBeam || w.isMainGun || w.Module.XSIZE*w.Module.YSIZE > 4)
                    mains++;
                if(w.SalvoCount > 2 || w.Tag_PD )
                    pd++;
                fireRate += w.fireDelay;
            }
            if (ship.Weapons.Count > 0)
                fireRate /= ship.Weapons.Count;

            LargeAttackWeight  = mains > 2 ? 3 : fireRate > 0.5 ? 2 : 0;
            LargeAttackWeight *= .5f + node?.SizeWeight ?? .5f;
            SmallAttackWeight  = mains == 0 && fireRate < .1 && pd > 1 ? 3 : 0;            
            SmallAttackWeight *= 1.5f - node?.SizeWeight ?? .5f;
            MediumAttackWeight = mains < 3 && fireRate > .1 ? 3 : 0;
            if (ship.loyalty.isFaction || ship.velocityMaximum > 500)
                VultureWeight  = 2;
            if (ship.loyalty.isFaction)
                PirateWeight   = 3;
        }  
    }
}