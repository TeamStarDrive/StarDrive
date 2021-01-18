using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Ship_Game.AI.Tasks;
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
        public int OurStartingColonies;
        public float TheirStartingGroundStrength;
        public float StrengthKilled;
        public float StrengthLost;
        public float TroopsKilled;
        public float TroopsLost;
        public int ColoniesWon;
        public int ColoniesLost;
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

        public WarState GetBorderConflictState() => Score.GetBorderConflictState();
        public WarState GetBorderConflictState(Array<Planet> coloniesOffered) => 
            Score.GetBorderConflictState(coloniesOffered);
        public WarState GetWarScoreState() => Score.GetWarScoreState();

        [JsonIgnore][XmlIgnore]
        public Empire Them { get; private set; }
        public int StartingNumContestedSystems;
        [JsonIgnore][XmlIgnore] public SolarSystem[] ContestedSystems { get; private set; }
        [JsonIgnore][XmlIgnore] public float LostColonyPercent  => ColoniesLost / (OurStartingColonies + 0.01f + ColoniesWon);
        [JsonIgnore][XmlIgnore] public float TotalThreatAgainst => Them.CurrentMilitaryStrength / Us.CurrentMilitaryStrength.LowerBound(0.01f);
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

        int GetMinPriority() => WarTheaters.MinPriority() - (int)GetWarScoreState();

        public int GetPriority()
        {
            if (Them.isFaction) return 1;
            var warState = Score.GetWarScoreState();
            if (Us != Them)
            {
                var strength = Us.GetRelationsOrNull(Them)?.KnownInformation.OffensiveStrength ?? Us.CurrentMilitaryStrength;
                float strengthMod = Us.CurrentMilitaryStrength / strength.LowerBound(1);
                return 8 - (int)((int)warState * strengthMod).UpperBound(8);
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
            OurStartingColonies         = us.GetPlanets().Count;
            TheirStartingStrength       = them.CurrentMilitaryStrength;
            TheirStartingGroundStrength = them.CurrentTroopStrength;
            ContestedSystems            = Us.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them));
            ContestedSystemsGUIDs       = FindContestedSystemGUIDs();
            StartingNumContestedSystems = ContestedSystemsGUIDs.Count;
            OurRelationToThem           = us.GetRelationsOrNull(them);
            Score                       = new WarScore(this, Us);
            PopulateHistoricLostSystems();
            WarTheaters = new TheatersOfWar(this);
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
            if (attacker != Them) return;
            ColoniesLost++;
        }

        public void PlanetWeWon(Empire loser, Planet colony)
        {
            if (loser != Them) return;
            ColoniesWon++;
        }

        public void SystemAssaultFailed(SolarSystem system)
        {
            SystemAssaultFailures.TryGetValue(system.guid, out int count);
            SystemAssaultFailures[system.guid] = ++count;
        }

        public void WarDebugData(ref DebugTextBlock debug)
        {
            string pad = "     ";
            string pad2 = pad + "  *";
            debug.AddLine($"Duration Years: {Empire.Universe.StarDate - StartDate:n1}");
            debug.AddLine($"War Value: {WarTheaters.WarValue:n1}");
            debug.AddLine($"ThreatRatio = {(int)(TotalThreatAgainst * 100):p0}");
            debug.AddLine($"StartDate {StartDate}");
            debug.AddLine($"killed: {StrengthKilled:n0} Lost: {StrengthLost:n0} Ratio: {(int)(SpaceWarKd * 100):p0}");
            debug.AddLine($"Colonies Won : {ColoniesWon} Lost : {ColoniesLost} Ratio: % {(int)(LostColonyPercent * 100):n0}");
            
            

            debug = WarTheaters.DebugText(debug, pad, pad2);

            debug.AddLine($"Contested Systems: {ContestedSystemCount}");
            foreach (var system in ContestedSystems)
            {
                bool ourForcesPresent   = system.OwnerList.Contains(Us);
                bool theirForcesPresent = system.OwnerList.Contains(Them);
                int value               = (int)system.PlanetList.Sum(p => p.ColonyBaseValue(Us));
                bool hasFleetTask = Us.GetEmpireAI().WarTasks.IsAlreadyAssaultingSystem(system);
            }
        }
    }
}