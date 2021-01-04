using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class Planet
    {
        public enum GoodState
        {
            STORE,
            IMPORT,
            EXPORT
        }

        public ColonyStorage Storage;
        public ColonyResource Food;
        public ColonyResource Prod;
        public ColonyResource Res;
        public ColonyMoney    Money;

        public float FoodHere
        {
            get => Storage.Food;
            set => Storage.Food = value;
        }

        public float ProdHere
        {
            get => Storage.Prod;
            set => Storage.Prod = value;
        }

        public float Population
        {
            get => Storage.Population;
            set
            {
                Storage.Population = value;
                PopulationBillion = value / 1000f;
            }
        }

        public void SetWorkerPercentages(float farmerPercent, float workerPercent, float researchPercent)
        {
            Food.Percent = farmerPercent.NaNChecked(0.4f, "SetWorkerPercentages farmer");
            Prod.Percent = workerPercent.NaNChecked(0.4f, "SetWorkerPercentages worker");
            Res.Percent  = researchPercent.NaNChecked(0.2f, "SetWorkerPercentages research");
            if (Owner != null)
                Food.AutoBalanceWorkers();
        }

        public float PopulationBillion { get; private set; }
        public float PlusFlatPopulationPerTurn { get; private set; }

        public bool HasProduction    => Prod.GrossIncome > 1.0f;
        public float PopulationRatio => MaxPopulation.AlmostZero() ? 0 : Storage.Population / MaxPopulation;

        public string PopulationStringForPlayer
        {
            get
            {
                float maxPopForPlayer = MaxPopulationBillionFor(EmpireManager.Player);
                int numDecimalsPop    = PopulationBillion.GreaterOrEqual(0.1f) ? 1 : 2;
                int numDecimalsPopMax = maxPopForPlayer % 1 > 0.95f || maxPopForPlayer % 1 < 0.05f ? 1 : 2;
                string popString      = $"{PopulationBillion.String(numDecimalsPop)} / {maxPopForPlayer.String(numDecimalsPopMax)}";

                if (PopulationRatio.NotZero())
                    popString += $" ({(PopulationRatio * 100).String()}%)";

                return popString;
            }
        }

        public float GetGoodHere(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FoodHere;
                case Goods.Production: return ProdHere;
                case Goods.Colonists:  return Population;
                default:               return 0;
            }
        }

        public GoodState FS = GoodState.STORE;      //I dont like these names, but changing them will affect a lot of files
        public GoodState PS = GoodState.STORE;
        public bool ImportFood => FS == GoodState.IMPORT;
        public bool ImportProd => PS == GoodState.IMPORT;
        public bool ExportFood => FS == GoodState.EXPORT;
        public bool ExportProd => PS == GoodState.EXPORT;

        GoodState ColonistsTradeState
        {
            get
            {
                if (Quarantine && Owner.isPlayer)
                    return GoodState.STORE;

                bool needFood = ShortOnFood();
                if (needFood && Population > 500f)                  return GoodState.EXPORT;
                if (!needFood && PopulationRatio < 0.8f)            return GoodState.IMPORT;
                if (MaxPopulation > 2000 && PopulationRatio > 0.9f) return GoodState.EXPORT;

                return GoodState.STORE;
            }
        }

        public bool ShortOnFood()
        {
            if (Owner?.isFaction ?? true)
                return false;

            if (Owner.NonCybernetic)
            {
                if (TurnsToEmptyStorage(Food.NetIncome, FoodHere + IncomingFood) < AverageImportTurns)
                    return true;
            }
            else if (TurnsToEmptyStorage(Prod.NetIncome, ProdHere + IncomingProd) < AverageImportTurns)
                return true;

            return false;
        }

        float TurnsToEmptyStorage(float output, float storage)
        {
            if (output.GreaterOrEqual(0))
                return 1000;

            return storage / -output;
        }

        public GoodState GetGoodState(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FS;
                case Goods.Production: return PS;
                case Goods.Colonists:  return ColonistsTradeState;
                default:               return 0;
            }
        }

        public bool IsExporting()
        {
            foreach (Goods good in Enum.GetValues(typeof(Goods)))
                if (GetGoodState(good) == GoodState.EXPORT)
                    return true;
            return false;
        }

        private string ImportsDescr()
        {
            if (!ImportFood && !ImportProd) return "";
            if (ImportFood && !ImportProd) return "(IMPORT FOOD)";
            if (ImportProd && !ImportFood) return "(IMPORT PROD)";
            return "(IMPORT ALL)";
        }

        public float StorageRatio(Goods goods)
        {
            switch (goods)
            {
                case Goods.Food:       return Storage.FoodRatio;
                case Goods.Production: return Storage.ProdRatio;
                case Goods.Colonists:  return PopulationRatio;
                default:               return 1;
            }
        }

        void DetermineFoodState(float importThreshold, float exportThreshold)
        {
            if (IsCybernetic) return;

            if (Owner.NumPlanets == 1)
            {
                FS = GoodState.STORE; // Easy out for solo planets
                return;
            }

            if (Food.FlatBonus > PopulationBillion) // Account for possible overproduction from FlatFood
            {
                float offsetAmount = (Food.FlatBonus - PopulationBillion) * 0.05f;
                offsetAmount       = offsetAmount.Clamped(0.00f, 0.15f);
                importThreshold    = (importThreshold - offsetAmount).Clamped(0.1f, 1f);
                exportThreshold    = (exportThreshold - offsetAmount).Clamped(0.1f, 1f);
            }

            float ratio               = Storage.FoodRatio;
            bool belowImportThreshold = ratio < importThreshold && Food.NetIncome.Less(Storage.Max / 50);

            // This will allow a buffer for import / export, so they dont constantly switch between them
            if      (ShortOnFood() || belowImportThreshold)                  FS = GoodState.IMPORT; 
            else if (Food.NetMaxPotential + Food.NetFlatBonus < 0)           FS = GoodState.STORE; // We are struggling to produce food but not short on food
            else if (FS == GoodState.IMPORT && ratio >= importThreshold * 2) FS = GoodState.STORE;  // Until you reach 2x importThreshold, then switch to Store
            else if (FS == GoodState.EXPORT && ratio <= exportThreshold / 2) FS = GoodState.STORE;  // If we were exporting, and drop below half exportThreshold, stop exporting
            else if (ratio > exportThreshold)                                FS = GoodState.EXPORT; // Until we get back to the Threshold, then export
        }

        void DetermineProdState(float importThreshold, float exportThreshold)
        {
            if (Owner.NumPlanets == 1)
            {
                PS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (Prod.FlatBonus > 0)
            {
                if (IsCybernetic)  //Account for excess food for the filthy Opteris
                {
                    if (Prod.FlatBonus > PopulationBillion)
                    {
                        float offsetAmount = (Prod.FlatBonus - PopulationBillion) * 0.05f;
                        offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                        importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                        exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                    }
                }
                else
                {
                    float offsetAmount = Prod.FlatBonus * 0.05f;
                    offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                    importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                    exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                }
            }

            float ratio = Storage.ProdRatio;
            if (ratio < importThreshold) PS = GoodState.IMPORT;
            else if (PS == GoodState.IMPORT && ratio >= importThreshold * 2) PS = GoodState.STORE;
            else if (PS == GoodState.EXPORT && ratio <= exportThreshold / 2) PS = GoodState.STORE;
            else if (ratio > exportThreshold) PS = GoodState.EXPORT;
        }
    }
}
