using SDGraphics;
using Ship_Game.Data.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game
{
    [StarDataType]
    public class Espionage
    {
        public const byte MaxLevel = 5;
        [StarData] public byte Level;
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        [StarData] public float LevelProgress { get; private set; }
        [StarData] int Weight;

        [StarDataConstructor]
        public Espionage() { }

        public Espionage(Empire us, Empire them) 
        {
            Owner = us;
            Them  = them;
        }

        void IncreaseInfiltrationLevel()
        {
            Level++;
            LevelProgress = 0;
            Them.SetCanBeScannedByPlayer(Level > 0);
        }

        public void DecreaseInfiltrationLevelTo(byte value)
        {
            Level = value;
            LevelProgress = 0;
            Them.SetCanBeScannedByPlayer(Level > 0);
        }

        public void DecreaseProgrees(float value)
        {
            if (Level == 0)
                return;

            LevelProgress -= value;
            if (LevelProgress < 0)
                DecreaseInfiltrationLevelTo((byte)(Level-1));
        }

        public void IncreaseProgress(float taxedResearch, int totalWeight)
        {
            if (AtMaxLevel)
                return;

            float progressToIncrease = GetProgressToIncrease(taxedResearch, totalWeight);
            LevelProgress = (LevelProgress + progressToIncrease).UpperBound(LevelCost(MaxLevel));
            if (LevelProgress >= NextLevelCost)
                IncreaseInfiltrationLevel();
        }

        public float GetProgressToIncrease(float taxedResearch, float totalWeight)
        {
            float lala = taxedResearch;
            lala *= (Weight / totalWeight.LowerBound(1));
            lala *= (Them.TotalPopBillion / Owner.TotalPopBillion.LowerBound(0.1f));
            lala *= (1 - Them.EspionageDefenseRatio * 0.75f);

            return taxedResearch
                   * (Weight / totalWeight.LowerBound(1))
                   * (Them.TotalPopBillion / Owner.TotalPopBillion.LowerBound(0.1f))
                   * (1 - Them.EspionageDefenseRatio*0.75f);
        }

        public void SetWeight(int value)
        {
            Weight = value;
        }

        public int GetWeight()
        {
            return !AtMaxLevel ? Weight : 0;
        }

        public int LevelCost(int level)
        {
            // 1 - 50
            // 2 - 100
            // 3 - 200
            // 4 - 400
            // 5 - 800
            return level == 0 ? 0 : (int)(50 * Math.Pow(2, level-1) * Owner.Universe.SettingsResearchModifier);
        }

        public int NextLevelCost => LevelCost(Level+1);

        public bool CanViewPersonality   => Level >= 1;
        public bool CanViewNumPlanets    => Level >= 1;
        public bool CanViewPop           => Level >= 1;
        public bool CanViewTheirTreaties => Level >= 1;

        public bool CanViewDefenseRatio => Level >= 2;
        public bool CanViewNumShips     => Level >= 2;
        public bool CanViewBonuses      => Level >= 2;
        public bool CanViewTechType     => Level >= 2;
        public bool CanViewArtifacts    => Level >= 2;

        public bool CanViewMoneyAndMaint => Level >= 3;
        public bool CanViewResearchTopic => Level >= 3;

        public bool AtMaxLevel => Level >= MaxLevel;
        public float ProgressPercent => LevelProgress/NextLevelCost * 100;

        public string TheirInfiltrationLevel()
        {
            if (Level <= 1)
                return "Unknown";

            int theirInfiltrationLevel = Them.GetRelations(Owner).Espionage.Level;
            if (Level <= 2)
                return theirInfiltrationLevel > 0 ? "Exists" : "Probably None";

            if (Level <= 4)
                return theirInfiltrationLevel > 3 ? "Deep" : "Shallow";

            return $"{theirInfiltrationLevel}";
        }
    }
}
