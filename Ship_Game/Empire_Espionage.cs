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


        public void SetCanBeScannedByPlayer(bool value)
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
                GetRelations(empire).Espionage.Update(taxedResearch, totalWeight);

            EspionageDefenseRatio = EspionageDefenseWeight / totalWeight.LowerBound(1);
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
    }
}
