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
        public float Percent; // Percentage workers allocated [0.0-1.0]
        public bool PercentLock; // Percentage slider locked by user

        // Per Turn: Raw value produced before we apply any taxes or consume stuff
        public float GrossIncome { get; protected set; }

        // Per Turn: NetIncome = GrossIncome - (taxes + consumption)
        public float NetIncome { get; protected set; }

        // Per Turn: GrossIncome assuming we have MaxPopulation
        public float MaxPotential { get; protected set; }

        // Per Turn: Flat income added; no taxes applied
        public float FlatBonus { get; protected set; }

        // Per Turn: NetFlatBonus = FlatBonus - tax
        public float NetFlatBonus { get; protected set; }

        // Per Turn: Resource yield per allocated colonist; no taxes applied
        public float YieldPerColonist { get; protected set; }

        // Per Turn: NetYieldPerColonist = YieldPerColonist - taxes
        public float NetYieldPerColonist { get; protected set; }


        protected ColonyResource(Planet planet)
        {
            Planet = planet;
        }

        protected abstract void RecalculateModifiers();

        public virtual void Update(float consumption)
        {
            FlatBonus        = 0f;
            YieldPerColonist = 0f;
            RecalculateModifiers();

            float products = YieldPerColonist * Percent * Planet.PopulationBillion;
            MaxPotential = YieldPerColonist * Planet.MaxPopulationBillion;
            GrossIncome = FlatBonus + products;

            // taxes get applied before consumption
            // because government gets to eat their pie first :)))
            // @note Taxes affect all aspects of life: Food, Prod and Research.
            float tax = Planet.Owner.data.TaxRate;
            NetIncome    = GrossIncome  - (GrossIncome*tax + consumption);
            NetFlatBonus = NetFlatBonus - (NetFlatBonus*tax);
            NetYieldPerColonist = YieldPerColonist - (YieldPerColonist*tax);
        }
    }


    public class ColonyFood : ColonyResource
    {
        public ColonyFood(Planet planet) : base(planet)
        {
        }

        protected override void RecalculateModifiers()
        {
            float plusPerColonist = 0f;
            foreach (Building b in Planet.BuildingList)
            {
                plusPerColonist += b.PlusFoodPerColonist;
                FlatBonus       += b.PlusFlatFoodAmount;
            }
            YieldPerColonist = Planet.Fertility * (1 + plusPerColonist);
        }

        public override void Update(float consumption)
        {
            base.Update(Planet.IsCybernetic ? 0f : consumption);
        }
    }

    public class ColonyProd : ColonyResource
    {
        public ColonyProd(Planet planet) : base(planet)
        {
        }

        protected override void RecalculateModifiers()
        {
            float richness = Planet.MineralRichness;
            float plusPerColonist = 0f;
            foreach (Building b in Planet.BuildingList)
            {
                plusPerColonist += b.PlusProdPerColonist;
                FlatBonus += b.PlusProdPerRichness * richness;
                FlatBonus += b.PlusFlatProductionAmount;
            }
            float factionMod = Planet.Owner.data.Traits.ProductionMod;
            YieldPerColonist = richness * (1 + plusPerColonist + factionMod);
        }

        public override void Update(float consumption)
        {
            base.Update(Planet.IsCybernetic ? consumption : 0f);
        }
    }

    public class ColonyRes : ColonyResource
    {
        public ColonyRes(Planet planet) : base(planet)
        {
        }

        protected override void RecalculateModifiers()
        {
            float plusPerColonist = 0f;
            foreach (Building b in Planet.BuildingList)
            {
                plusPerColonist += b.PlusResearchPerColonist;
                FlatBonus   += b.PlusFlatResearchAmount;
            }
            float factionMod = Planet.Owner.data.Traits.ResearchMod;
            YieldPerColonist = plusPerColonist * factionMod;
        }

        public override void Update(float consumption)
        {
            base.Update(0f/*research not consumed*/);
        }
    }
}
