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
        }

        public void DecreaseInfiltrationLevelTo(byte value)
        {
            Level = value;
            LevelProgress = Level > 0 ? LevelCost(Level) : 0;
        }

        public void DecreaseProgrees(float value)
        {
            if (Level == 0)
                return;

            LevelProgress = (LevelProgress - value).LowerBound(0);
            if (LevelProgress < LevelCost(Level))
                Level--;
        }

        public void IncreaseProgrees(float taxedResearch, int totalWeight)
        {
            if (AtMaxLevel)
                return;

            float progressToIncrease = GetProgressToIncrease(taxedResearch, totalWeight);
            LevelProgress = (LevelProgress + progressToIncrease).UpperBound(LevelCost(MaxLevel));
            if (LevelProgress >= NextLevelCost)
                IncreaseInfiltrationLevel();
        }

        public float GetProgressToIncrease(float taxedResearch, int totalWeight)
        {
            return taxedResearch
                   * (Weight / totalWeight.LowerBound(1))
                   * (Them.TotalPopBillion / Owner.TotalPopBillion.LowerBound(0.1f))
                   * (1 - Them.EspionageDefenseRatio * 0.75f);
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
            // 2 - 150
            // 3 - 450
            // 4 - 1350
            // 5 - 4050
            return level == 0 ? 0 : (int)(50 * Math.Pow(3, level-1) * Owner.Universe.SettingsResearchModifier);
        }

        public int NextLevelCost => LevelCost(Level+1);
        public bool ShowDefenseRatio => Level >= 2;
        public bool AtMaxLevel => Level >= MaxLevel;

        public float RelativeProgressPercent => (LevelProgress - LevelCost(Level)) / (NextLevelCost - LevelCost(Level)) * 100;
    }
}
