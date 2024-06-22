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
        [StarData] Mole StickyMole;
        [StarData] public float TotalMoneyLeeched { get; private set; }

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
            EnablePassiveEffects();
        }

        public void WipeoutInfiltration() => SetInfiltrationLevelTo(0);

        public void ReduceInfiltrationLevel() => SetInfiltrationLevelTo((byte)(Level.LowerBound(1) - 1));

        public void SetInfiltrationLevelTo(byte value)
        {
            Level = value.LowerBound(0);
            LevelProgress = 0;
            RemoveMissions();
            EnablePassiveEffects();
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

            if (!CanPlanetStickyMole && StickyMole != null)
            {
                Owner.data.MoleList.Remove(StickyMole);
                StickyMole = null;
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

        void EnablePassiveEffects()
        {
            if (Level >= 1)
                Them.SetCanBeScannedByPlayer(true); // This ability cannot be lost after it was achieved.

            if (CanPlanetStickyMole && StickyMole == null)
            {
                StickyMole = Mole.PlantStickyMoleAtHomeworld(Owner, Them, out string targetPlanetName);
                if (StickyMole != null)
                {
                    string message = $"{Localizer.Token(GameText.NewSuccessfullyInfiltratedAColony)} {targetPlanetName}";
                    Owner.Universe.Notifications.AddAgentResult(true, message, Owner);
                }
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

        public void AddLeechedMoney(float money)
        {
            Owner.AddMoney(money);
            TotalMoneyLeeched += money;
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
        bool CanPlanetStickyMole         => Level >= 3;

        public bool CanLeechTech => Level >= 4;

        public bool CanLeechMoney => Level >= 5;



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
                case InfiltrationMissionType.PlantMole: Missions.Add(new InfiltrationMissionPlantMole(Owner, Them, LevelCost(Level), Level)); break;
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

        public bool IsMissionActive(InfiltrationMissionType type) => Missions.Any(m => m.Type == type);
    }
}
