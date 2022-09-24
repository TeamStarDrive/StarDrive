using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    [StarDataType]
    public class War
    {
        [StarData] public WarType WarType;
        [StarData] public float OurStartingStrength;
        [StarData] public float TheirStartingStrength;
        [StarData] public float OurStartingGroundStrength;
        [StarData] public int InitialColoniesValue;
        [StarData] public float TheirStartingGroundStrength;
        [StarData] public float StrengthKilled;
        [StarData] public float StrengthLost;
        [StarData] public int ColoniesValueWon;
        [StarData] public int ColoniesValueLost;
        [StarData] public Array<string> AlliesCalled = new();
        [StarData] public Array<int> ContestedSystemsIds = new();
        [StarData] public float TurnsAtWar;
        [StarData] public float EndStarDate;
        [StarData] public float StartDate;
        [StarData] Empire Us;
        [StarData] public string UsName;
        [StarData] public string ThemName;
        [StarData] public bool Initialized;
        [StarData] readonly WarScore Score;
        [StarData] public Map<int, int> SystemAssaultFailures = new();
        [StarData] public int StartingNumContestedSystems;

        public WarState GetBorderConflictState(Array<Planet> coloniesOffered) => Score.GetBorderConflictState(coloniesOffered);
        public WarState GetWarScoreState() => WarType == WarType.BorderConflict ? Score.GetBorderConflictState() : Score.GetWarScoreState();

        [StarData] public Empire Them { get; private set; }
        [StarData] public SolarSystem[] ContestedSystems { get; private set; }
        
        [JsonIgnore][XmlIgnore] public float LostColonyPercent  => (float)ColoniesValueLost / (1 + InitialColoniesValue + ColoniesValueWon);
        [JsonIgnore][XmlIgnore] public float TotalThreatAgainst => Them.CurrentMilitaryStrength / Us.CurrentMilitaryStrength.LowerBound(0.01f);
        [JsonIgnore][XmlIgnore] public const float MaxWarGrade = 10;
        [JsonIgnore][XmlIgnore] public float SpaceWarKd
        {
            get
            {
                float minStr      = 10000 * ((int)Us.Universum.Difficulty + 1);
                float ourStr      = Us.CurrentMilitaryStrength.LowerBound(minStr);
                float theirStr    = Them.CurrentMilitaryStrength.LowerBound(minStr);
                float killPercent = StrengthKilled / theirStr;
                float lossPercent = StrengthLost / ourStr;

                // start checking kill ratio only after 5% kills/loses 
                return killPercent > 0.05f || lossPercent > 0.05f ? killPercent / lossPercent : 1;
            }
        }

        readonly Array<SolarSystem> HistoricLostSystems = new();
        public IReadOnlyList<SolarSystem> GetHistoricLostSystems() => HistoricLostSystems;
        Relationship OurRelationToThem;

        public float GetPriority()
        {
            if (Them.IsFaction) 
                return 11; // This might be changed in the future, if we want more meaningful wars vs factions

            var warState = Score.GetWarScoreState();
            if (Us != Them)
            {
                var strength      = Them.KnownEmpireStrength(Us);
                float strengthMod = (Us.OffensiveStrength / strength.LowerBound(1)).Clamped(0.3f,3);

                if (Them.isPlayer && ColoniesValueLost - ColoniesValueWon < 0 && strengthMod > 1)
                    return 0;

                int warHistory    = OurRelationToThem.WarHistory.Count + 1;
                int upperBound    = Them.isPlayer ? Us.DifficultyModifiers.PlayerWarPriorityLimit : 10;
                float priority    = 10 - (((int)warState * strengthMod * warHistory).Clamped(0, upperBound));
                return priority;
            }
            return 0;
        }

        public War()
        {
            Score = new WarScore(this, Us);
        }

        public War(Empire us, Empire them, float starDate, WarType warType)
        {
            StartDate = starDate;
            Us        = us;
            Them      = them;
            UsName    = us.data.Traits.Name;
            ThemName  = them.data.Traits.Name;
            WarType   = warType;

            OurStartingStrength         = us.CurrentMilitaryStrength;
            OurStartingGroundStrength   = us.CurrentTroopStrength;
            InitialColoniesValue        = us.GetTotalPlanetsWarValue();
            TheirStartingStrength       = them.CurrentMilitaryStrength;
            TheirStartingGroundStrength = them.CurrentTroopStrength;
            ContestedSystems            = Us.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them));
            ContestedSystemsIds       = FindContestedSystemGUIDs();
            StartingNumContestedSystems = ContestedSystemsIds.Count;
            OurRelationToThem           = us.GetRelationsOrNull(them);
            Score                       = new WarScore(this, Us);

            PopulateHistoricLostSystems(us.Universum);
            if (!Us.isPlayer && !Us.IsFaction && !them.IsFaction)
                Us.AI.AddGoal(new WarManager(Us, Them, WarType));

            //WarTheaters = new TheatersOfWar(this);
        }

        public static War CreateInstance(Empire owner, Empire target, WarType warType)
        {
            var war = new War(owner, target, owner.Universum.StarDate, warType);
            return war;
        }

        void PopulateHistoricLostSystems(UniverseState us)
        {
            if (OurRelationToThem == null) return;
            foreach (var lostSystem in OurRelationToThem.GetPlanetsLostFromWars(us))
            {
                if (lostSystem.OwnerList.Contains(Them))
                    HistoricLostSystems.AddUniqueRef(lostSystem);
            }
        }

        public void ChangeWarType(WarType type)
        {
            WarType = type;
        }

        Array<int> FindContestedSystemGUIDs()
        {
            var contestedSystemIds = new Array<int>();
            for (int x = 0; x < ContestedSystems.Length; x++)
                contestedSystemIds.Add(ContestedSystems[x].Id);
            return contestedSystemIds;
        }

        public void RestoreFromSave(UniverseState us, bool activeWar)
        {
            Us = EmpireManager.GetEmpireByName(UsName);
            Them = EmpireManager.GetEmpireByName(ThemName);

            ContestedSystems = new SolarSystem[ContestedSystemsIds.Count];
            for (int i = 0; i < ContestedSystemsIds.Count; i++)
            {
                int systemId = ContestedSystemsIds[i];
                SolarSystem solarSystem = Us.Universum.GetSystem(systemId);
                ContestedSystems[i] = solarSystem;
            }
            // The Us == Them is used in EmpireDefense and relations should be null
            OurRelationToThem = Us.GetRelationsOrNull(Them);
            
            if (activeWar)
                PopulateHistoricLostSystems(us);
        }

        public void ShipWeLost(Ship target)
        {
            if (target.IsHangarShip || target.IsHomeDefense || Them != target.LastDamagedBy?.GetLoyalty()) 
                return;

            StrengthLost += target.GetStrength();
        }

        public void ShipWeKilled(Ship target)
        {
            if (Them != target.Loyalty || target.IsHangarShip || target.IsHomeDefense)
                return;

            StrengthKilled += target.GetStrength();
        }

        public void PlanetWeLost(Empire attacker, Planet colony)
        {
            if (attacker != Them)
                return;

            ColoniesValueLost += (int)colony.ColonyWarValueTo(Us);
        }

        public void PlanetWeWon(Empire loser, Planet colony)
        {
            if (loser != Them) 
                return;

            ColoniesValueWon += (int)colony.ColonyWarValueTo(Us);
        }

        public void SystemAssaultFailed(SolarSystem system)
        {
            SystemAssaultFailures.TryGetValue(system.Id, out int count);
            SystemAssaultFailures[system.Id] = ++count;
        }

        public float GetGrade()
        {
            switch (GetWarScoreState())
            {
                default:
                case WarState.ColdWar:
                case WarState.EvenlyMatched:
                case WarState.NotApplicable:   return MaxWarGrade * 0.5f;
                case WarState.WinningSlightly: return MaxWarGrade * 0.75f;
                case WarState.Dominating:      return MaxWarGrade;
                case WarState.LosingSlightly:  return MaxWarGrade * 0.25f;
                case WarState.LosingBadly:     return 1;
            }
        }

        public void WarDebugData(ref DebugTextBlock debug)
        {
            debug.AddLine($"Duration Years: {Us.Universum.StarDate - StartDate:n1}");
            debug.AddLine($"ThreatRatio = {(int)(TotalThreatAgainst * 100):p0}");
            debug.AddLine($"StartDate {StartDate}");
            debug.AddLine($"killed: {StrengthKilled:n0} Lost: {StrengthLost:n0} Ratio: {(int)(SpaceWarKd * 100):p0}");
            debug.AddLine($"Colonies Value Won : {ColoniesValueWon} Lost : {ColoniesValueLost} Ratio: % {(int)(LostColonyPercent * 100):n0}");
        }
    }
}