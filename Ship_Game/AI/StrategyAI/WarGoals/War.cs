using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    public class War
    {
        public Guid WarGuid = Guid.NewGuid();
        public WarType WarType;
        public float OurStartingStrength;
        public float TheirStartingStrength;
        public float OurStartingGroundStrength;
        public int InitialColoniesValue;
        public float TheirStartingGroundStrength;
        public float StrengthKilled;
        public float StrengthLost;
        public int ColoniesValueWon;
        public int ColoniesValueLost;
        public Array<string> AlliesCalled = new Array<string>();
        public Array<Guid> ContestedSystemsGUIDs = new Array<Guid>();
        public float TurnsAtWar;
        public float EndStarDate;
        public float StartDate;
        private Empire Us;
        public string UsName;
        public string ThemName;
        public bool Initialized;
        readonly WarScore Score;
        public Map<Guid, int> SystemAssaultFailures = new Map<Guid, int>();
        public TheatersOfWar WarTheaters;
        public int StartingNumContestedSystems;

        public WarState GetBorderConflictState(Array<Planet> coloniesOffered) => Score.GetBorderConflictState(coloniesOffered);
        public WarState GetWarScoreState() => WarType == WarType.BorderConflict ? Score.GetBorderConflictState() : Score.GetWarScoreState();

        [JsonIgnore][XmlIgnore] public Empire Them { get; private set; }
        [JsonIgnore][XmlIgnore] public SolarSystem[] ContestedSystems { get; private set; }
        [JsonIgnore][XmlIgnore] public float LostColonyPercent  => (float)ColoniesValueLost / (1 + InitialColoniesValue + ColoniesValueWon);
        [JsonIgnore][XmlIgnore] public float TotalThreatAgainst => Them.CurrentMilitaryStrength / Us.CurrentMilitaryStrength.LowerBound(0.01f);
        [JsonIgnore][XmlIgnore] public const float MaxWarGrade = 10;
        [JsonIgnore][XmlIgnore] public float SpaceWarKd
        {
            get
            {
                float minStr      = 10000 * ((int)CurrentGame.Difficulty + 1);
                float ourStr      = Us.CurrentMilitaryStrength.LowerBound(minStr);
                float theirStr    = Them.CurrentMilitaryStrength.LowerBound(minStr);
                float killPercent = StrengthKilled / theirStr;
                float lossPercent = StrengthLost / ourStr;

                // start checking kill ratio only after 5% kills/loses 
                return killPercent > 0.05f || lossPercent > 0.05f ? killPercent / lossPercent : 1;
            }
        }

        [JsonIgnore][XmlIgnore] public int LowestTheaterPriority;

        int ContestedSystemCount => ContestedSystems.Count(s => s.OwnerList.Contains(Them));

        readonly Array<SolarSystem> HistoricLostSystems = new Array<SolarSystem>();
        public IReadOnlyList<SolarSystem> GetHistoricLostSystems() => HistoricLostSystems;
        Relationship OurRelationToThem;

        public float GetPriority()
        {
            if (Them.isFaction) 
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
            InitialColoniesValue         = us.GetTotalPlanetsWarValue();
            TheirStartingStrength       = them.CurrentMilitaryStrength;
            TheirStartingGroundStrength = them.CurrentTroopStrength;
            ContestedSystems            = Us.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them));
            ContestedSystemsGUIDs       = FindContestedSystemGUIDs();
            StartingNumContestedSystems = ContestedSystemsGUIDs.Count;
            OurRelationToThem           = us.GetRelationsOrNull(them);
            Score                       = new WarScore(this, Us);

            PopulateHistoricLostSystems();
            Us.GetEmpireAI().AddGoal(new WarManager(Us, Them, WarType));
            //WarTheaters = new TheatersOfWar(this);
        }

        public static War CreateInstance(Empire owner, Empire target, WarType warType)
        {
            var war = new War(owner, target, Empire.Universe.StarDate, warType);
            return war;
        }

        void PopulateHistoricLostSystems()
        {
            if (OurRelationToThem == null) return;
            foreach (var lostSystem in OurRelationToThem.GetPlanetsLostFromWars())
            {
                if (lostSystem.OwnerList.Contains(Them))
                    HistoricLostSystems.AddUniqueRef(lostSystem);
            }
        }

        public void ChangeWarType(WarType type)
        {
            WarType = type;
        }

        public SolarSystem[] GetTheirBorderSystems() => Them.GetOurBorderSystemsTo(Us, true)
                                .Filter(s => Us.GetEmpireAI().IsInOurAOs(s.Position));
        public SolarSystem[] GetTheirNearSystems() => Them.GetOurBorderSystemsTo(Us, true).ToArray();

        Array<Guid> FindContestedSystemGUIDs()
        {
            var contestedSystemGUIDs = new Array<Guid>();
            var systems = ContestedSystems;
            for (int x = 0; x < systems.Length; x++) contestedSystemGUIDs.Add(systems[x].guid);
            return contestedSystemGUIDs;
        }

        public void SetCombatants(Empire u, Empire t)
        {
            Us = u;
            Them = t;
        }

        public void RestoreFromSave(bool activeWar)
        {
            ContestedSystems = new SolarSystem[ContestedSystemsGUIDs.Count];
            for (int i = 0; i < ContestedSystemsGUIDs.Count; i++)
            {
                var guid = ContestedSystemsGUIDs[i];
                SolarSystem solarSystem = Empire.Universe.SolarSystemDict[guid];
                ContestedSystems[i] = solarSystem;
            }
            Us   = EmpireManager.GetEmpireByName(UsName);
            Them = EmpireManager.GetEmpireByName(ThemName);
            // The Us == Them is used in EmpireDefense and relations should be null
            OurRelationToThem = Us.GetRelationsOrNull(Them);
            
            if (activeWar)
            {
                PopulateHistoricLostSystems();
                if (WarTheaters == null) WarTheaters = new TheatersOfWar(this);
                WarTheaters.RestoreFromSave(this);
            }
            else
            {
                WarTheaters = null;
            }
        }

        public WarState ConductWar()
        {
            LowestTheaterPriority = WarTheaters.MinPriority();
            WarTheaters.Evaluate();

            return Score.GetWarScoreState();
        }

        public void ShipWeLost(Ship target)
        {
            if (target.IsHangarShip || target.IsHomeDefense || Them != target.LastDamagedBy?.GetLoyalty()) 
                return;

            StrengthLost += target.GetStrength();
        }

        public void ShipWeKilled(Ship target)
        {
            if (Them != target.loyalty || target.IsHangarShip || target.IsHomeDefense)
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
            SystemAssaultFailures.TryGetValue(system.guid, out int count);
            SystemAssaultFailures[system.guid] = ++count;
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
            debug.AddLine($"Duration Years: {Empire.Universe.StarDate - StartDate:n1}");
            debug.AddLine($"ThreatRatio = {(int)(TotalThreatAgainst * 100):p0}");
            debug.AddLine($"StartDate {StartDate}");
            debug.AddLine($"killed: {StrengthKilled:n0} Lost: {StrengthLost:n0} Ratio: {(int)(SpaceWarKd * 100):p0}");
            debug.AddLine($"Colonies Value Won : {ColoniesValueWon} Lost : {ColoniesValueLost} Ratio: % {(int)(LostColonyPercent * 100):n0}");
        }
    }
}