using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.Gameplay;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game.AI.StrategyAI.WarGoals
{
    [StarDataType]
    public class War
    {
        public const int PlayerProgressDefaultTimer = 120;

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
        [StarData] public bool PlayerAlliedAskedUsToJoin;
        [StarData] public int CheckPlayerProgressTimer = PlayerProgressDefaultTimer;
        [StarData] public int PlayerContributionWarnings;
        [StarData] Relationship OurRelationToThem;


        public WarState GetBorderConflictState(Array<Planet> coloniesOffered) => Score.GetBorderConflictState(coloniesOffered);
        public WarState GetWarScoreState() => WarType == WarType.BorderConflict ? Score.GetBorderConflictState() : Score.GetWarScoreState();

        [StarData] public Empire Them { get; private set; } // setters required for serializer
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

        public War()
        {
            Score = new WarScore(this, Us);
        }

        public War(Empire us, Empire them, float starDate, WarType warType, bool playerRequestedUsToJoin)
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
            PlayerAlliedAskedUsToJoin   = playerRequestedUsToJoin;

            if (!Us.isPlayer && !Us.IsFaction && !them.IsFaction)
                Us.AI.AddGoal(new WarManager(Us, Them, WarType));
        }

        public static War CreateInstance(Empire owner, Empire target, WarType warType, bool playerRequestedUsToJoin = false)
        {
            var war = new War(owner, target, owner.Universe.StarDate, warType, playerRequestedUsToJoin);
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

        public void MonitorPlayerContribution()
        {
            Empire player = Us.Universe.Player;
            if (!PlayerAlliedAskedUsToJoin || !Us.IsAlliedWith(player) || Them.isPlayer || !Them.IsAtWarWith(player))
                return;

            if (--CheckPlayerProgressTimer <= 0)
            {
                CheckPlayerProgressTimer = PlayerProgressDefaultTimer;
                if (Them.TryGetActiveWarWithPlayer(out War theirWarWithPlayer))
                {
                    float strRatioBySize = (OurStartingStrength + Us.OffensiveStrength) * 0.5f 
                        / ((theirWarWithPlayer.TheirStartingStrength + player.OffensiveStrength) * 0.5f).LowerBound(100);
                    float contributiopnRatio = StrengthKilled / (theirWarWithPlayer.StrengthLost*strRatioBySize).LowerBound(100);
                    if (contributiopnRatio > Us.PersonalityModifiers.PlayerWarContributionRatioThreshold)
                    {
                        // we are contributing more net effort than the player
                        PlayerContributionWarnings++;
                        if (PlayerContributionWarnings > Us.PersonalityModifiers.PlayerWarContributionMaxWarnings)
                        {
                            DiplomacyScreen.Show(Us, player, "PLAYER_ALLIED_WAR_CONTRIBUTION_ACTION");
                            OurRelationToThem.AddAngerDiplomaticConflict(100);
                            OurRelationToThem.Trust = 0;
                            if (!Us.PersonalityModifiers.CanWeSurrenderToPlayerAfterBetrayal)
                                OurRelationToThem.DoNotSurrenderToThem = true;
                            switch (Us.Personality)
                            {
                                case PersonalityType.Aggressive:
                                    player.AddToDiplomacyContactView(Us, "DECLAREWAR");
                                    break;
                                case PersonalityType.Ruthless:
                                    OurRelationToThem.RequestPeaceNow(Us);
                                    Us.BreakAllianceWith(player);
                                    if (Us.IsPeaceTreaty(Them))
                                        Us.GetRelations(player).PrepareForWar(WarType.ImperialistWar, Us);
                                    break;
                                case PersonalityType.Xenophobic:
                                    Us.BreakAllTreatiesWith(player);
                                    OurRelationToThem.Trust = -100;
                                    break;
                                case PersonalityType.Cunning:
                                    OurRelationToThem.RequestPeaceNow(Us);
                                    if (Us.IsPeaceTreaty(Them))
                                        Us.BreakAllianceWith(player);
                                    break;
                                case PersonalityType.Pacifist:
                                    OurRelationToThem.RequestPeaceNow(Us);
                                    Us.BreakAllTreatiesWith(player);
                                    break;
                                case PersonalityType.Honorable:
                                    OurRelationToThem.Trust = -50;
                                    OurRelationToThem.RequestPeaceNow(Us);
                                    Us.BreakAllTreatiesWith(player);
                                    if (!Us.IsPeaceTreaty(Them))
                                        Us.GetRelations(player).PrepareForWar(WarType.ImperialistWar, Us);
                                    break;
                            }
                        }
                        else
                        {
                            DiplomacyScreen.Show(Us, player, "PLAYER_ALLIED_WAR_CONTRIBUTION_WARNING");
                            OurRelationToThem.AddAngerDiplomaticConflict(50);
                            OurRelationToThem.Trust -= 50;
                        }
                    }
                }
            }
        }
    }
}