using SDGraphics;
using SDUtils;
using Ship_Game.AI;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Data.Serialization;
using Ship_Game.Gameplay;
using Ship_Game.Utils;

namespace Ship_Game
{
    public partial class Empire
    {
        [StarData] public bool CanBeScannedByPlayer { get; private set; } = true;
        [StarData] public int EspionageDefenseWeight { get; private set; } = 100;
        [StarData] public float EspionageDefenseRatio { get; private set; } = 1;


        void SetCanBeScannedByPlayer(bool value)
        {
            CanBeScannedByPlayer = value;
        }

        public int CalcTotalEspionageWeight()
        {
            return Universe.ActiveMajorEmpires.Filter(e => e != this)
                .Sum(e => GetRelations(e).Espionage.GetWeight()) + EspionageDefenseWeight;
        }

        public void SetEspionageDefenseWeight(int value)
        {
            EspionageDefenseWeight = value;
        }

        public void UpdateEspionage(float taxedResearch)
        {
            if (Universe.P.UseLegacyEspionage)
                return;

            int totalWeight = CalcTotalEspionageWeight();
            foreach (Empire empire in Universe.ActiveMajorEmpires.Filter(e => e != this))
                GetRelations(empire).Espionage.IncreaseProgrees(taxedResearch, totalWeight);

            EspionageDefenseRatio = EspionageDefenseWeight / totalWeight.LowerBound(1);
        }
    }
}
