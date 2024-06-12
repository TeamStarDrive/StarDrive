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
        const byte MaxLevel = 5;
        [StarData] public byte Level;
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        [StarData] public float LevelProgress { get; private set; }
        [StarData] int Weight;

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
            if (Level >= MaxLevel)
                return;

            float progressToIncrease = taxedResearch * (Weight / totalWeight);
            LevelProgress = (LevelProgress + progressToIncrease).UpperBound(LevelCost(MaxLevel));
            if (LevelProgress >= NextLevelCost)
                IncreaseInfiltrationLevel();
        }

        public void SetWeight(int value)
        {
            Weight = value;
        }

        public int GetWeight()
        {
            return Level < MaxLevel ? Weight : 0;
        }

        public int LevelCost(int level)
        {
            // 1 - 50
            // 2 - 150
            // 3 - 450
            // 4 - 1350
            // 5 - 4050
            return (int)(50 * Math.Pow(3, level-1) * Owner.Universe.SettingsResearchModifier);
        }

        public int NextLevelCost => LevelCost(Level+1);
    }
}
