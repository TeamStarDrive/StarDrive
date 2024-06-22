using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;
using System;

namespace Ship_Game
{
    // This is a new Espionage System that uses Infiltration Level instead of Agents
    [StarDataType]
    public class Espionage
    {
        public const byte MaxLevel = 5;
        [StarData] public byte Level;
        [StarData] readonly Empire Owner;
        [StarData] public readonly Empire Them;
        [StarData] public float LevelProgress { get; private set; }
        [StarData] int Weight;
        [StarData] Array<InfiltrationMission> Missions = new();

        [StarDataConstructor]
        public Espionage() { }

        public Espionage(Empire us, Empire them) 
        {
            Owner = us;
            Them  = them;
        }


        // Used for Remnants and Pirates
        public void IncreaseInfiltrationLevelTo(byte value)
        {
            for (byte i = 1; i <= value; i++)
                IncreaseInfiltrationLevel();
        }

        void IncreaseInfiltrationLevel()
        {
            if (Level == MaxLevel) 
                return;

            Level++;
            LevelProgress = 0;
            Them.SetCanBeScannedByPlayer(true); // This ability cannot be lost after it was achieved.
        }

        public void WipeoutInfiltration() => SetInfiltrationLevelTo(0);

        public void ReduceInfiltrationLevel() => SetInfiltrationLevelTo((byte)(Level.LowerBound(1) - 1));

        public void SetInfiltrationLevelTo(byte value)
        {
            Level = value.LowerBound(0);
            LevelProgress = 0;
            RemoveMissions();
        }

        public void DecreaseProgress(float value)
        {
            if (Level == 0)
                return;

            LevelProgress -= value;
            if (LevelProgress < 0)
                SetInfiltrationLevelTo((byte)(Level-1));
        }

        public void Update(float taxedResearch, int totalWeight)
        {
            RemoveMissions();
            float progressToIncrease = GetProgressToIncrease(taxedResearch, totalWeight);
            UpdateMissions(Missions.Count > 0 ? progressToIncrease / Missions.Count : 0);

            if (AtMaxLevel)
                return;

            LevelProgress = (LevelProgress + progressToIncrease).UpperBound(LevelCost(MaxLevel));
            if (LevelProgress >= NextLevelCost)
                IncreaseInfiltrationLevel();
        }

        void RemoveMissions()
        {
            for (int i = Missions.Count - 1; i >= 0; i--)
            {
                InfiltrationMission mission = Missions[i];
                if (mission.Level > Level)
                    Missions.Remove(mission);
            }
        }

        void UpdateMissions(float progress)
        {
            for (int i = 0; i < Missions.Count; i++)
            {
                InfiltrationMission mission = Missions[i];
                mission.Update(progress);
            }
        }

        public float GetProgressToIncrease(float taxedResearch, float totalWeight)
        {
            float activeMissionRatio = Missions.Count > 0 ? 0.5f : 1;
            return taxedResearch
                   * (Weight / totalWeight.LowerBound(1))
                   * (Them.TotalPopBillion / Owner.TotalPopBillion.LowerBound(0.1f))
                   * (1 - Them.EspionageDefenseRatio*0.75f)
                   * activeMissionRatio;
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
            // default costs
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

        public bool CanViewNumShips     => Level >= 2;
        public bool CanViewTechType     => Level >= 2;
        public bool CanViewArtifacts    => Level >= 2;
        public bool CanViewRanks        => Level >= 2;
        public bool ProjectorsCanAlert  => Level >= 2;

        public bool CanViewDefenseRatio  => Level >= 3;
        public bool CanViewMoneyAndMaint => Level >= 3;
        public bool CanViewResearchTopic => Level >= 3;
        public bool CanViewBonuses       => Level >= 3;


        public bool AtMaxLevel => Level >= MaxLevel;
        public float ProgressPercent => LevelProgress/NextLevelCost * 100;

        public string InfiltrationLevelSummary()
        {
            if (Level <= 1)
                return "Unknown";

            int theirInfiltrationLevel = Them.GetRelations(Owner).Espionage.Level;
            if (Level <= 2)
                return theirInfiltrationLevel > 0 ? "Exists" : "Probably None";

            if (Level <= 4)
                return theirInfiltrationLevel == 0 ? "None" 
                                                   : theirInfiltrationLevel > 3 ? "Deep" : "Shallow";

            return $"{theirInfiltrationLevel}";
        }

        public void AddMission(InfiltrationMissionType type)
        {
            if (Missions.Any(m => m.Type == type))
                Log.Error($"Mission type {type} already exists for {Owner}");

            switch (type) 
            {
                case InfiltrationMissionType.PlantMole: AddMissionPlantMole(); break;
            }
        }

        public void RemoveMission(InfiltrationMissionType type) 
        {
            for (int i = Missions.Count - 1; i >= 0; i--)
            {
                InfiltrationMission mission = Missions[i];
                if (mission.Type == type)
                    Missions.Remove(mission);
            }
        }

        void AddMissionPlantMole()
        {
            InfiltrationMissionPlantMole planetMole = new(Owner, Them, LevelCost(Level), Level);
            Missions.Add(planetMole);
        }

        public bool IsPlantingMole => Missions.Any(m => m.Type == InfiltrationMissionType.PlantMole);
    }
}
