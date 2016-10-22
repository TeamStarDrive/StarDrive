using System;
using Ship_Game.Gameplay;

namespace Ship_Game
{
    public sealed class CombatAI
    {
        public float VultureWeight = 1;

        //public float AssistWeight = 0.1f;          //Not referenced in code, removing to save memory

        public float SelfDefenseWeight = 1f;

        //public float OthersDefenseWeight = 1f;          //Not referenced in code, removing to save memory

        public float SmallAttackWeight = 1f;

        public float MediumAttackWeight = 1f;

        public float LargeAttackWeight = 3f;

        public float PreferredEngagementDistance = 1500f;
        public float PirateWeight = 0;
        //public float minWeaponRange = 0;          //Not referenced in code, removing to save memory
        //public float MaxWeaponRange = 0;          //Not referenced in code, removing to save memory
        public Ship_Game.Gameplay.Ship owner = null;

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
                this.owner = ship;
                UpdateCombatAI(ship);
            }
            ;

        }
        public void UpdateCombatAI(Ship_Game.Gameplay.Ship ship)
        {


            if (ship.Size > 0 )
            {
                byte pd =0;
                byte mains=0;
                float FireRate =0;
                foreach(Weapon w in ship.Weapons)
                {
                    if(w.isBeam || w.isMainGun || w.moduleAttachedTo.XSIZE*w.moduleAttachedTo.YSIZE >4)
                        mains++;
                    if(w.SalvoCount>2 || w.Tag_PD )
                        pd++;
                    FireRate += w.fireDelay;
                }
                if (ship.Weapons.Count > 0)
                    FireRate /= ship.Weapons.Count;

                int Size = ship.Size;                
                this.LargeAttackWeight = mains>2 ?3: FireRate >.5 ?2:0;
                this.SmallAttackWeight = mains == 0 && FireRate <.1 && pd >1 ? 3 : 0;
                this.MediumAttackWeight = mains <3 && FireRate > .1 ? 3 : 0;
                float stlspeed = ship.velocityMaximum;
                if ( ship.loyalty.isFaction || stlspeed >500) 
                    this.VultureWeight = 2;
                if (ship.loyalty.isFaction )
                    this.PirateWeight = 3;
            
            
            }



        }
  
    }
}