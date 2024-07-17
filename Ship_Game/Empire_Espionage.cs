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
        [StarData] public int EspionageDefenseWeight { get; private set; } = 50;
        [StarData] public float EspionageDefenseRatio { get; private set; } = 1;
        [StarData] public float TotalMoneyLeechedLastTurn { get; private set; }
        [StarData] public float EspionageBudgetMultiplier { get; private set; } = 1; // 1-5
        public float EspionagePointsPerTurn => TotalPopBillion * EspionageBudgetMultiplier;
        public float EspionageCost => TotalPopBillion * (EspionageBudgetMultiplier - 1);


        public void SetCanBeScannedByPlayer(bool value)
        {
            CanBeScannedByPlayer = value;
        }

        public void UpdateMoneyLeechedLastTurn()
        {
            if (Universe.P.UseLegacyEspionage)
                return;

            TotalMoneyLeechedLastTurn = 0;
            foreach (Empire e in Universe.ActiveMajorEmpires.Filter(e => e != this))
                TotalMoneyLeechedLastTurn += GetEspionage(e).ExtractMoneyLeechedThisTurn();
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

        public void SetEspionageBudgetMultiplier(float value)
        {
            EspionageBudgetMultiplier = value;
        }

        public void UpdateEspionage()
        {
            if (Universe.P.UseLegacyEspionage)
                return;

            int totalWeight = CalcTotalEspionageWeight();
            foreach (Empire empire in Universe.ActiveMajorEmpires.Filter(e => e != this))
                GetEspionage(empire).Update(totalWeight);

            UpdateEspionageDefenseRatio(totalWeight);
        }

        public void UpdateEspionageDefenseRatio(int totalWeight)
        {
            EspionageDefenseRatio = (float)EspionageDefenseWeight / totalWeight.LowerBound(1);
        }

        public void UpdateEspionageDefenseRatio()
        {
            UpdateEspionageDefenseRatio(CalcTotalEspionageWeight());
        }

        public Espionage GetEspionage(Empire targetEmpire) => GetRelations(targetEmpire).Espionage;

        public void AddRebellion(Planet targetPlanet, int numTroops)
        {
            Empire rebels = null;
            if (!data.RebellionLaunched)
                rebels = Universe.CreateRebelsFromEmpireData(data, this);

            if (rebels == null)
                rebels = Universe.GetEmpireByName(data.RebelName);

            for (int i = 0; i < numTroops; i++)
            {
                foreach (string troopType in ResourceManager.TroopTypes)
                {
                    if (WeCanBuildTroop(troopType) && ResourceManager.TryCreateTroop(troopType, rebels, out Troop t))
                    {

                        t.Name = rebels.data.TroopName.Text;
                        t.Description = rebels.data.TroopDescription.Text;
                        if (targetPlanet.GetFreeTiles(t.Loyalty) == 0 &&
                            !targetPlanet.BumpOutTroop(Universe.Corsairs) &&
                            !t.TryLandTroop(targetPlanet)) // Let's say the rebels are pirates :)
                        {
                            t.Launch(targetPlanet); // launch the rebels
                        }

                        break;
                    }
                }
            }
        }

        public bool TryGetRebels(out Empire rebels)
        {
            rebels = Universe.GetEmpireByName(data.RebelName);
            return rebels != null;
        }
    }
}
