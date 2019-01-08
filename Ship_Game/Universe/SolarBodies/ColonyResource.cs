using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Universe.SolarBodies
{
    public abstract class ColonyResource
    {
        protected readonly Planet Planet;

        public float Percentage; // Percentage workers allocated [0.0-1.0]
        public bool PercentageLock; // Percentage slider locked by user

        public float GrossIncome;
        public float NetIncome; // GrossIncome - planet.Consumption
        protected float PlusFlatBonus;
        protected float PlusPerColonist;
        protected float BonusModifier = 1f;

        protected ColonyResource(Planet planet)
        {
            Planet = planet;
        }

        public virtual void RecalculateModifiers()
        {
            PlusFlatBonus   = 0f;
            PlusPerColonist = 0f;
            BonusModifier   = 1f;
        }

        public virtual void Update()
        {
        }
    }


    public class ColonyFood : ColonyResource
    {
        public ColonyFood(Planet planet) : base(planet)
        {
        }

        public override void RecalculateModifiers()
        {
            base.RecalculateModifiers();
            foreach (Building b in Planet.BuildingList)
            {
                PlusPerColonist += b.PlusFoodPerColonist;
                PlusFlatBonus += b.PlusFlatFoodAmount;
            }
        }

        public override void Update()
        {
            base.Update();


        }
    }

    public class ColonyProd : ColonyResource
    {
        public ColonyProd(Planet planet) : base(planet)
        {
        }

        public override void RecalculateModifiers()
        {
            base.RecalculateModifiers();

            BonusModifier = Planet.Owner.data.Traits.ProductionMod;
            foreach (Building b in Planet.BuildingList)
            {
                PlusPerColonist += b.PlusProdPerColonist;
                PlusFlatBonus += b.PlusProdPerRichness * Planet.MineralRichness;
                PlusFlatBonus += b.PlusFlatProductionAmount;
            }
        }

        public override void Update()
        {
            base.Update();


        }
    }

    public class ColonyRes : ColonyResource
    {
        public ColonyRes(Planet planet) : base(planet)
        {
        }

        public override void RecalculateModifiers()
        {
            base.RecalculateModifiers();
            
            BonusModifier = Planet.Owner.data.Traits.ProductionMod;
            foreach (Building b in Planet.BuildingList)
            {
                PlusPerColonist += b.PlusFoodPerColonist;
            }
        }

        public override void Update()
        {
            base.Update();


        }
    }
}
