using System.Collections.Generic;
using System.Xml.Serialization;
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
        [StarData] public float TurnsAtWar;
        [StarData] public float EndStarDate;
        [StarData] public float StartDate;
        [StarData] Empire Us;
        [StarData] public bool Initialized;
        [StarData] readonly WarScore Score;
        [StarData] public Map<int, int> SystemAssaultFailures = new();
        [StarData] public int StartingNumContestedSystems;

        public WarState GetBorderConflictState(Array<Planet> coloniesOffered) => Score.GetBorderConflictState(coloniesOffered);
        public WarState GetWarScoreState() => WarType == WarType.BorderConflict ? Score.GetBorderConflictState() : Score.GetWarScoreState();

        [StarData] public Empire Them { get; private set; }
        [StarData] public SolarSystem[] ContestedSystems { get; private set; }
        
        public float LostColonyPercent  => (float)ColoniesValueLost / (1 + InitialColoniesValue + ColoniesValueWon);
        public float TotalThreatAgainst => Them.CurrentMilitaryStrength / Us.CurrentMilitaryStrength.LowerBound(0.01f);
        public const float MaxWarGrade = 10;
        public float SpaceWarKd
        {
            get
            {
                float minStr      = 10000 * ((int)Us.Universe.P.Difficulty + 1);
                float ourStr      = Us.CurrentMilitaryStrength.LowerBound(minStr);
                float theirStr    = Them.CurrentMilitaryStrength.LowerBound(minStr);
                float killPercent = StrengthKilled / theirStr;
                float lossPercent = StrengthLost / ourStr;

                // start checking kill ratio only after 5% kills/loses 
                return killPercent > 0.05f || lossPercent > 0.05f ? killPercent / lossPercent : 1;
            }
        }

        [StarData] Relationship OurRelationToThem;

        public War()
        {
            Score = new WarScore(this, Us);
        }

        public War(Empire us, Empire them, float starDate, WarType warType)
        {
            StartDate = starDate;
            Us        = us;
            Them      = them;
            WarType   = warType;

            OurStartingStrength         = us.CurrentMilitaryStrength;
            OurStartingGroundStrength   = us.CurrentTroopStrength;
            InitialColoniesValue        = us.GetTotalPlanetsWarValue();
            TheirStartingStrength       = them.CurrentMilitaryStrength;
            TheirStartingGroundStrength = them.CurrentTroopStrength;
            ContestedSystems = Us.GetOwnedSystems().Filter(s => s.OwnerList.Contains(Them));
            StartingNumContestedSystems = ContestedSystems.Length;
            OurRelationToThem           = us.GetRelationsOrNull(them);
            Score                       = new WarScore(this, Us);

            if (!Us.isPlayer && !Us.IsFaction && !them.IsFaction)
                Us.AI.AddGoal(new WarManager(Us, Them, WarType));
        }

        public static War CreateInstance(Empire owner, Empire target, WarType warType)
        {
            var war = new War(owner, target, owner.Universe.StarDate, warType);
            return war;
        }

        public void ChangeWarType(WarType type)
        {
            WarType = type;
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
            debug.AddLine($"Duration Years: {Us.Universe.StarDate - StartDate:n1}");
            debug.AddLine($"ThreatRatio = {(int)(TotalThreatAgainst * 100):p0}");
            debug.AddLine($"StartDate {StartDate}");
            debug.AddLine($"killed: {StrengthKilled:n0} Lost: {StrengthLost:n0} Ratio: {(int)(SpaceWarKd * 100):p0}");
            debug.AddLine($"Colonies Value Won : {ColoniesValueWon} Lost : {ColoniesValueLost} Ratio: % {(int)(LostColonyPercent * 100):n0}");
        }
    }
}