using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Commands.Goals;
using Ship_Game.Data.Serialization;
using Ship_Game.Debug;
using Ship_Game.Empires.Components;
using Ship_Game.GameScreens.DiplomacyScreen;
using Ship_Game.Universe;
using System.Windows.Forms.VisualStyles;

namespace Ship_Game.Gameplay
{
    [StarDataType]
    public sealed class TrustEntry
    {
        [StarData] public int TurnTimer;
        [StarData] public int TurnsInExistence;
        [StarData] public float TrustCost;
        [StarData] public TrustEntryType Type;
    }

    [StarDataType]
    public sealed class FearEntry
    {
        [StarData] public int TurnTimer;
        [StarData] public float FearCost;
        [StarData] public int TurnsInExistence;
        [StarData] public TrustEntryType Type;
    }

    [StarDataType]
    public sealed class FederationQuest
    {
        [StarData] public QuestType type;
        [StarData] public string EnemyName;
    }

    [StarDataType]
    public partial class Relationship
    {
        [StarData] public Empire Them { get; private set; }
        [StarData] public bool Known;
        [StarData] public Posture Posture = Posture.Neutral;  // FB - use SetPosture privately or ChangeTo methods publicly

        [StarData] public FederationQuest FedQuest;
        [StarData] public int turnsSinceLastContact;
        [StarData] public int TurnsSinceLastTechTrade;
        [StarData] public int TurnsSinceLastTechDemand;
        [StarData] public int TurnsSinceLastThreathened;
        [StarData] public bool WarnedAboutShips;
        [StarData] public bool WarnedAboutColonizing;
        [StarData] public int PlayerContactStep; //  Encounter Step to use when the player contacts this faction

        [StarData] public float Anger_FromShipsInOurBorders; // FB - Use AddAngerShipsInOurBorders
        [StarData] public float Anger_TerritorialConflict; // FB - Use AddAngerTerritorialConflict
        [StarData] public float Anger_MilitaryConflict; // FB - Use AddAngerMilitaryConflict
        [StarData] public float Anger_DiplomaticConflict; // FB - Use AddAngerDiplomaticConflict

        [StarData] public int SpiesDetected;
        [StarData] public int TimesSpiedOnAlly;
        [StarData] public int SpiesKilled;
        [StarData] public float TotalAnger;
        [StarData] public bool Treaty_OpenBorders; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [StarData] public bool Treaty_NAPact; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [StarData] public bool Treaty_Trade; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [StarData] public int Treaty_Trade_TurnsExisted;
        [StarData] public bool Treaty_Alliance; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [StarData] public bool Treaty_Peace; // FB - check Empire_Relationship to see how to set it. Do not access directly!

        [StarData] public int PeaceTurnsRemaining;
        [StarData] public float Threat;
        [StarData] public float Trust;
        [StarData] public War ActiveWar;
        [StarData] public Array<War> WarHistory = new();
        [StarData] public bool haveRejectedNAPact;
        [StarData] public bool HaveRejected_TRADE;
        [StarData] public bool haveRejectedDemandTech;
        [StarData] public bool HaveRejected_OpenBorders;
        [StarData] public bool HaveRejected_Alliance;
        [StarData] public int NumberStolenClaims;

        [StarData] public Array<SolarSystem> StolenSystems = new();
        [StarData] public Array<SolarSystem> WarnedSystemsList = new();
        [StarData] public bool HaveInsulted_Military;
        [StarData] public bool HaveComplimented_Military;
        [StarData] public bool HaveWarnedTwice;
        [StarData] public bool HaveWarnedThrice;
        // TODO: remove ID
        [StarData] public int ContestedSystemId;
        [StarData] public SolarSystem ContestedSystem;
        [StarData] public bool AtWar;
        [StarData] public bool PreparingForWar; // Use prepareForWar or CancelPrepareForWar
        [StarData] public WarType PreparingForWarType = WarType.ImperialistWar;  // Use prepareForWar or CancelPrepareForWar
        [StarData] public int DefenseFleet = -1;
        [StarData] public bool HasDefenseFleet;
        [StarData] public float InitialStrength;
        [StarData] public int TurnsKnown;
        [StarData] public int TurnsAbove95; // Trust
        [StarData] public int TurnsAllied { get; private set; }
        [StarData] public int TurnsInNap { get; private set; }
        [StarData] public int TurnsInOpenBorders { get; private set; }

        [StarData] public Array<TrustEntry> TrustEntries = new();
        [StarData] public Array<FearEntry> FearEntries = new();
        [StarData] public float TrustUsed;
        [StarData] public float FearUsed;
        [StarData] public int TurnsAtWar;
        [StarData] public int FactionContactStep;  // Encounter Step to use when the faction contacts the player;
        [StarData] public bool CanAttack; // New: Bilateral condition if these two empires can attack each other
        [StarData] public bool IsHostile = true; // New: If target empire is hostile and might attack us
        [StarData] public int NumTechsWeGave; // number of tech they have given us, through tech trade or demands.
        [StarData] public bool RefusedMerge; // Refused merge or surrenders from us (mostly the player can refuse)

        [StarData] public EmpireRiskAssessment Risk;
        [StarData] public Espionage Espionage;

        [StarData] public bool DoNotSurrenderToThem;
        [StarData] public bool TheyDeclaredWarOnAlly { get; private set; } // They cannot be trusted

        [XmlIgnore] public float AvailableTrust => Trust - TrustUsed;

        [XmlIgnore] public bool CanMergeWithThem => !RefusedMerge && !DoNotSurrenderToThem;
        [XmlIgnore] public int WarAnger => (int)(TotalAnger - Trust.LowerBound(-50));

        private float FirstDemand   => 50 * Them.Universe.P.Pace;
        public float SecondDemand   =>  75 * Them.Universe.P.Pace;
        public float TechTradeTurns => 100 * Them.Universe.P.Pace;
        public float DemandTechTurns => 175 * Them.Universe.P.Pace;
        public float TryPlayerSurrenderTimer => RefusedMerge ? SecondDemand * 2 : SecondDemand;
        public float ThreatenedTurnsThreshold => 30 * Them.Universe.P.Pace;

        /// <summary>
        /// Tech transfer restriction.
        /// currently this is disabling tech content trade via diplomacy.
        /// A check here can be added to remove this for allies.
        /// </summary>
        [XmlIgnore]
        readonly Array<TechUnlockType> PreventContentExchangeOf =
                                       new Array<TechUnlockType> { TechUnlockType.Diplomacy };

        public bool AllowRacialTrade() => !PreventContentExchangeOf.Contains(TechUnlockType.Diplomacy);
        public bool HaveRejectedDemandTech // TODO check this
        {
            get => haveRejectedDemandTech;
            set
            {
                if (!(haveRejectedDemandTech = value))
                    return;

                Trust -= 20f;
                AddAngerDiplomaticConflict(20);
            }
        }

        public bool HaveRejectedNaPact
        {
            get => haveRejectedNAPact;
            set
            {
                haveRejectedNAPact = value;
                if (haveRejectedNAPact)
                    Trust -= 20f;
            }
        }
        
        [StarDataConstructor]
        Relationship() {}

        public Relationship(Empire us, Empire them)
        {
            Them = them;
            Risk = new EmpireRiskAssessment(this);
            if (us.NewEspionageEnabled)
                Espionage = new Espionage(us, them);
        }

        public void SetDeclaredWarOnAlly()
        {
            TheyDeclaredWarOnAlly = true;
        }

        public void AddTrustEntry(Offer.Attitude attitude, TrustEntryType type, float cost, int turnTimer)
        {
            turnTimer = (int)(turnTimer * Them.Universe.ProductionPace);
            if (attitude != Offer.Attitude.Threaten)
            {
                TrustEntries.Add(new TrustEntry
                {
                    TrustCost = cost,
                    TurnTimer = turnTimer,
                    Type = type
                });

                TrustUsed += cost;
            }
            else
            {
                float fearCost = Treaty_Alliance ? cost * 2 : cost;
                FearEntries.Add(new FearEntry
                {
                    FearCost = fearCost,
                    TurnTimer = turnTimer,
                    Type = type
                });

                FearUsed += fearCost;
            }
        }

        public void PrepareForWar(WarType type, Empire us)
        {
            if (us == Them)
            {
                Log.Error($"Called prepare for war vs. ourselves! Us={us} Them={Them}");
                return;
            }

            if (PreparingForWar)
                return;

            if (Them.isPlayer && GlobalStats.RestrictAIPlayerInteraction)
                return;

            us.AI.AddGoal(new PrepareForWar(us, Them));
            PreparingForWar     = true;
            PreparingForWarType = type;
        }

        public void CancelPrepareForWar()
        {
            // Note - prepare for war goal will exit by itself since it has check logic for this
            PreparingForWar = false;
        }

        public float GetTurnsForFederationWithPlayer(Empire us) => TurnsAbove95Federation(us);

        int TurnsAbove95Federation(Empire us) => ((int)(us.PersonalityModifiers.TurnsAbove95FederationNeeded 
                                                 * (int)(us.Universe.P.GalaxySize + 1) * us.Universe.P.Pace * 0.5f)).LowerBound(1);
        
        public void SetTreaty(Empire us, TreatyType treatyType, bool value)
        {
            switch (treatyType)
            {
                case TreatyType.Alliance:      Treaty_Alliance    = value; HandleAlliance(); break;
                case TreatyType.NonAggression: Treaty_NAPact      = value;                   break;
                case TreatyType.OpenBorders:   Treaty_OpenBorders = value;                   break;
                case TreatyType.Peace:         Treaty_Peace       = value; HandlePeace();    break;
                case TreatyType.Trade:         Treaty_Trade       = value; HandleTrade();    break;
            }

            if (!us.isPlayer && us.NewEspionageEnabled)
                us.AI.EspionageManager.Update(forceRun: true);

            void HandleTrade()
            {
                Treaty_Trade_TurnsExisted = 0;
            }

            void HandlePeace()
            {
                if (value)
                {
                    PeaceTurnsRemaining = 100;
                    WarnedSystemsList.Clear();
                    us.LaunchTroopsAfterPeaceSigned(Them);
                }
                else
                {
                    PeaceTurnsRemaining = 0;
                }
            }

            void HandleAlliance()
            {
                if (value) // If treaty is signed
                {
                    CancelPrepareForWar();
                    WarnedSystemsList.Clear();
                    DoNotSurrenderToThem = false;
                }

                if (Them.isPlayer)
                    us.SetAlliedWithPlayer(value);
            }
        }

        public float TradeIncome(Empire us)
        {
            float setupIncome = (0.1f * Treaty_Trade_TurnsExisted - 3f).Clamped(-3, 3);
            if (setupIncome < 0) 
                return setupIncome; // Need Several turns of investment before trade is profitable

            Empire them         = Them;
            float demandDivisor = 20; // They want our goods more if we are in better relations
            if      (Treaty_Alliance)    demandDivisor = 10;
            else if (Treaty_OpenBorders) demandDivisor = 15;

            float income         = them.MaxPopBillion / demandDivisor; // Their potential demand
            float tradeCapacity  = us.TotalPlanetsTradeValue / them.TotalPlanetsTradeValue; // Our ability to supply the demand
            tradeCapacity        = (tradeCapacity * (1 + us.data.Traits.Mercantile)).UpperBound(1);
            float maxIncome      = (income * tradeCapacity).LowerBound(3);
            float netIncome      = (0.1f * Treaty_Trade_TurnsExisted - 3f).Clamped(-3, maxIncome);

            return netIncome.RoundToFractionOf10();
        }

        public void StoleOurColonyClaim(Empire owner, Planet claimedPlanet, out bool newTheft)
        {
            newTheft = !StolenSystems.Contains(claimedPlanet.System);
            if (newTheft)
            {
                NumberStolenClaims++;
                AddAngerTerritorialConflict(5f + (float)Math.Pow(5, NumberStolenClaims));
                Trust -= owner.DifficultyModifiers.TrustLostStoleColony;
                Trust -= owner.data.DiplomaticPersonality.Territorialism / 5 * StolenSystems.Count.LowerBound(1);
                StolenSystems.AddUnique(claimedPlanet.System);
            }
        }

        public void WarnClaimThiefPlayer(Planet claimedPlanet, Empire victim)
        {
            switch (StolenSystems.Count)
            {
                case 0:                                                                                      return;
                case 1: DiplomacyScreen.Stole1stColonyClaim(claimedPlanet, victim);                          break;
                case 2: DiplomacyScreen.Stole2ndColonyClaim(claimedPlanet, victim); HaveWarnedTwice  = true; break;
                case 3: DiplomacyScreen.Stole3rdColonyClaim(claimedPlanet, victim); HaveWarnedThrice = true; break;
            }

            victim.RespondPlayerStoleColony(this);
        }

        public void DamageRelationship(Empire us, Empire them, string why, float amount, Planet p,
            bool breakTreatiesWithAllyifCaughtSpying = true)
        {
            if (us.data.DiplomaticPersonality == null || us.isPlayer)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && them.isPlayer)
                return;

            if (them.isPlayer)
                amount *= us.DifficultyModifiers.AngerMultiplierVsPlayer;

            if (us.IsHonorable || us.IsXenophobic)
                amount *= us.PersonalityModifiers.AngerMultiplierRelDamage;

            if (why != null)
            {
                if (why == "Caught Spying")
                {
                    SpiesDetected += 1;
                    AddAngerDiplomaticConflict(amount);
                    Trust -= amount;
                    CalcTotalAnger();

                    if (Treaty_Alliance)
                    {
                        TimesSpiedOnAlly += 1;
                        if (TimesSpiedOnAlly == 1)
                        {
                            if (them.isPlayer && !us.IsFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_1");

                            turnsSinceLastContact = 0;
                        }
                        else if (TimesSpiedOnAlly > 1)
                        {
                            if (them.isPlayer && !us.IsFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(us, them,
                                    breakTreatiesWithAllyifCaughtSpying ? "Caught_Spying_Ally_2" : "Caught_Spying_Ally_1");
                            }

                            if (breakTreatiesWithAllyifCaughtSpying)
                                us.BreakAllTreatiesWith(them);

                            turnsSinceLastContact = 0;
                        }
                    }
                    else if (!AtWar && them.isPlayer && !us.IsFaction)
                    {
                        if (SpiesDetected == 1)
                        {
                            if (them.isPlayer && !us.IsFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_1");

                            turnsSinceLastContact = 0;
                        }
                        else if (SpiesDetected == 2)
                        {
                            if (them.isPlayer && !us.IsFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_2");

                            turnsSinceLastContact = 0;
                        }
                        else if (SpiesDetected >= 3)
                        {
                            if (them.isPlayer && !us.IsFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_3");

                            us.BreakAllTreatiesWith(them);
                            turnsSinceLastContact = 0;
                        }
                    }
                }
                else if (why == "Caught Spying Failed")
                {
                    AddAngerDiplomaticConflict(amount);
                    Trust -= amount;
                    CalcTotalAnger();

                    SpiesKilled += 1;

                    if (Treaty_Alliance)
                    {
                        TimesSpiedOnAlly += 1;
                        if (TimesSpiedOnAlly == 1)
                        {
                            if (them.isPlayer && !us.IsFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_1");
                        }
                        else if (TimesSpiedOnAlly > 1)
                        {
                            if (them.isPlayer && !us.IsFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_2");

                            if (breakTreatiesWithAllyifCaughtSpying)
                                us.BreakAllTreatiesWith(them);

                            Posture = Posture.Hostile;
                        }
                    }
                    else if (them.isPlayer && !us.IsFaction)
                    {
                        DiplomacyScreen.ShowEndOnly(us, them, "Killed_Spy_1");
                    }
                }
                else if (why == "Insulted")
                {
                    AddAngerDiplomaticConflict(Treaty_Alliance ?  amount*2 : amount);
                    CalcTotalAnger();
                    Trust -= amount;
                }
                else if (why == "Colonized Owned System")
                {
                    var ourTargetPlanets = us.AI.SelectFromGoals((MarkForColonization c) => c.TargetPlanet);
                    foreach (Planet planet in ourTargetPlanets)
                    {
                        foreach (Planet other in them.GetPlanets())
                        {
                            if (planet.System == other.System)
                            {
                                SolarSystem sharedSys = planet.System;
                                if (us.GetRelations(them).WarnedSystemsList.Contains(sharedSys))
                                    return;
                            }
                        }
                    }

                    float expansion = us.Universe.Systems.Count / us.GetOwnedSystems().Count + them.GetOwnedSystems().Count;
                    AddAngerTerritorialConflict(amount + expansion);
                    Trust -= amount;
                    CalcTotalAnger();

                    if (Anger_TerritorialConflict < us.data.DiplomaticPersonality.Territorialism && !AtWar)
                    {
                        if (AtWar)
                            return;

                        if (them.isPlayer && !us.IsFaction)
                        {
                            if (!WarnedAboutShips)
                                DiplomacyScreen.Show(us, them, "Colonized Warning", p);
                            else if (!AtWar)
                                DiplomacyScreen.Show(us, them, "Warning Ships then Colonized", p);

                            turnsSinceLastContact  = 0;
                            WarnedAboutColonizing  = true;

                            if (p != null)
                            {
                                // TODO: remove ID
                                ContestedSystemId = p.System.Id;
                                ContestedSystem = p.System;
                            }
                        }
                    }
                }
                else if (why == "Expansion")
                {

                }
                else if (why == "Destroyed Ship")
                {
                    if (Anger_MilitaryConflict.AlmostZero() && !AtWar)
                    {
                        AddAngerMilitaryConflict(amount);
                        Trust -= amount;
                        CalcTotalAnger();
                        if (them.isPlayer && !us.IsFaction)
                        {
                            if (Anger_MilitaryConflict < 2f)
                                DiplomacyScreen.Show(us, them, "Aggression Warning");

                            Trust -= amount;
                        }
                    }

                    AddAngerMilitaryConflict(amount);
                }
            }
        }

        public float GetStrength()
        {
            return InitialStrength - Anger_FromShipsInOurBorders - Anger_TerritorialConflict - Anger_MilitaryConflict - Anger_DiplomaticConflict + Trust;
        }

        public void ImproveRelations(float trustEarned, float angerToReduce)
        {
            AddAngerDiplomaticConflict(-angerToReduce);
            Trust += trustEarned;
            CalcTotalAnger();
        }

        public void SetImperialistWar() //TODO what about AtWar?
        {
            if (ActiveWar != null)
            {
                ActiveWar.WarType = WarType.ImperialistWar;
            }
        }

        public void SetInitialStrength(float n)
        {
            InitialStrength = 50f + n;
        }

        // updates basic relationship metrics
        // but doesn't create big side-effects
        public void UpdateRelationship(Empire us, Empire them)
        {
            if (us.IsDefeated)
                return;

            if (!us.IsFaction)
                Risk.UpdateRiskAssessment(us);

            bool noAttackPlayer = GlobalStats.RestrictAIPlayerInteraction && them.isPlayer;
            if (noAttackPlayer)
            {
                IsHostile = false;
                CanAttack = false;
            }
            else
            {
                IsHostile = IsEmpireHostileToUs(us, them);
                bool canAttack = CanWeAttackThem(us, them, TotalAnger);
                float TheirAnger = them.GetRelationsOrNull(us).TotalAnger;
                if (CanTheyAttackUs(them, us, TheirAnger))
                    canAttack = true; // make sure we can also attack them

                if (canAttack) // We are now hostile as well
                    IsHostile = true;
                CanAttack = canAttack;
                // add unit test when anger is high and then when anger is low
            }
        }

        // This should be done only once per turn in Empire.UpdateRelationships
        public void AdvanceRelationshipTurn(Empire us, Empire them)
        {
            if (them.IsDefeated && AtWar)
            {
                CancelPrepareForWar();
                AtWar = false;
                ActiveWar.EndStarDate = us.Universe.StarDate;
                WarHistory.Add(ActiveWar);
                ActiveWar = null;
            }

            TurnsAtWar = AtWar ? TurnsAtWar + 1 : 0;
            Treaty_Trade_TurnsExisted = Treaty_Trade ? Treaty_Trade_TurnsExisted + 1 : 0;
            TurnsAllied = Treaty_Alliance ? TurnsAllied + 1 : 0;
            TurnsInNap = Treaty_NAPact ? TurnsInNap + 1 : 0;
            TurnsInOpenBorders = Treaty_OpenBorders ? TurnsInOpenBorders + 1 : 0;

            ++TurnsKnown;
            ++turnsSinceLastContact;
            ++TurnsSinceLastTechTrade;
            ++TurnsSinceLastTechDemand;
            ++TurnsSinceLastThreathened;

            if (AtWar && ActiveWar != null)
                ActiveWar.TurnsAtWar += 1f;

            if (us.isPlayer)
            {
                UpdatePlayerRelations(us, them);
                return;
            }

            bool wasAbsorbed = AttemptAIFederationAbsorb(aiEmpire: us);
            if (!wasAbsorbed)
            {
                DTrait dt = us.data.DiplomaticPersonality;
                UpdateThreat(us, them);
                UpdateTrust(us, them, us.data.EconomicPersonality?.EconomicPersonality() ?? EconomicPersonalityType.Generalists);
                UpdateAnger(us, them, dt);
                UpdateFear();
            }
        }
        
        void UpdatePlayerRelations(Empire us, Empire them)
        {
            if (Treaty_Peace && --PeaceTurnsRemaining <= 0)
            {
                us.EndPeaceWith(them);
                us.Universe.Notifications?.AddPeaceTreatyExpiredNotification(them);
            }
        }

        public bool CanTheyAttackUs(Empire them, Empire us, float theirAnger) => CanWeAttackThem(them, us, theirAnger);
        bool CanWeAttackThem(Empire us, Empire them, float ourAnger)
        {
            if (AtWar)
                return true;

            if (Treaty_Peace || Treaty_NAPact || Treaty_Alliance)
                return false;

            if (us.IsFaction || them.IsFaction || them.WeAreRemnants)
                return true; // Factions are below treatires since factions can have NA pact with empires

            if (!us.isPlayer)
            {
                float trustworthiness = them.isPlayer ? 50 : them.data.DiplomaticPersonality?.Trustworthiness ?? 100;
                float peacefulness    = 1.0f - them.Research.Strategy.MilitaryRatio;
                if (ourAnger > trustworthiness * peacefulness)
                    return true;
            }

            return false;
        }

        bool IsEmpireHostileToUs(Empire us, Empire them)
        {
            if (AtWar)
                return true;

            // if one of the parties is a Faction, there is hostility by default
            // unless we have Peace or NA Pacts (such as paying off Pirates)
            return (us.IsFaction || them.IsFaction)
                && !Treaty_Peace && !Treaty_NAPact;
        }

        void UpdateAnger(Empire us, Empire them, DTrait personality)
        {
            UpdateAngerBorders(us, them);
            UpdatePeace(us, them);
            float angerDissipation = Treaty_Peace ? -personality.AngerDissipation * 2 : -personality.AngerDissipation;
            AddAngerTerritorialConflict(angerDissipation);
            AddAngerShipsInOurBorders(angerDissipation);
            AddAngerDiplomaticConflict(angerDissipation);
            AddAngerMilitaryConflict(angerDissipation);
            CalcTotalAnger();
        }

        void CalcTotalAnger()
        {
            TotalAnger = (Anger_DiplomaticConflict
                         + Anger_FromShipsInOurBorders
                         + Anger_MilitaryConflict
                         + Anger_TerritorialConflict).UpperBound(100);
        }

        void UpdateFear()
        {
            foreach (FearEntry f in FearEntries)
                f.TurnsInExistence += 1;

            FearUsed = FearEntries.Sum(f => f.TurnsInExistence < f.TurnTimer ? f.FearCost : 0);
            FearEntries.RemoveAll(f => f.TurnsInExistence >= f.TurnTimer);
        }

        void UpdatePeace(Empire us, Empire them)
        {
            if (!Treaty_Peace) 
                return;

            AddAngerDiplomaticConflict(-0.1f);
            AddAngerMilitaryConflict(-0.1f);
            AddAngerShipsInOurBorders(-0.1f);
            AddAngerTerritorialConflict(-0.1f);
            if (--PeaceTurnsRemaining <= 0)
                us.EndPeaceWith(them);
        }

        void UpdateAngerBorders(Empire us, Empire them)
        {
            if (Treaty_Alliance || Treaty_OpenBorders) 
                return;

            float strShipsInBorders = us.AI.ThreatMatrix.KnownEmpireStrengthInBorders(them);
            if (strShipsInBorders > 0)
            {
                float ourStr = Treaty_NAPact ? us.CurrentMilitaryStrength * 25
                                             : us.CurrentMilitaryStrength * 50 ; // We are less concerned if we have NAP with them

                float borderAnger = (100f - Trust) * 0.01f * strShipsInBorders / ourStr.LowerBound(1);
                AddAngerShipsInOurBorders(borderAnger);
            }
        }

        // TODO: This is really funky, something is wrong with it
        bool AttemptAIFederationAbsorb(Empire aiEmpire)
        {
            if (FedQuest == null) 
                return false;

            Empire player = aiEmpire.Universe.Player;
            Empire enemyEmpire = aiEmpire.Universe.GetEmpireByName(FedQuest.EnemyName);
            if (FedQuest.type == QuestType.DestroyEnemy && enemyEmpire.IsDefeated)
            {
                DiplomacyScreen.ShowEndOnly(aiEmpire, player, "Federation_YouDidIt_KilledEnemy", enemyEmpire);
                player.AbsorbEmpire(aiEmpire);
                FedQuest = null;
                return true;
            }

            if (FedQuest.type == QuestType.AllyFriend)
            {
                if (enemyEmpire.IsDefeated)
                {
                    FedQuest = null;
                }
                else if (player.IsAlliedWith(enemyEmpire))
                {
                    DiplomacyScreen.ShowEndOnly(aiEmpire, player, "Federation_YouDidIt_AllyFriend", enemyEmpire);
                    player.AbsorbEmpire(aiEmpire);
                    FedQuest = null;
                    return true;
                }
            }
            return false;
        }

        void UpdateThreat(Empire us, Empire them)
        {
            float minimumThreat = them.isPlayer ? us.DifficultyModifiers.MinimumThreatStr * 0.5f : us.DifficultyModifiers.MinimumThreatStr;
            float ourMilScore   = (us.OffensiveStrength*0.001f).LowerBound(minimumThreat); 
            float theirMilScore = (them.OffensiveStrength*0.001f).LowerBound(minimumThreat);
            float newThreat     = ((theirMilScore - ourMilScore) / ourMilScore * 100).Clamped(-100, 100); // This will give a threat of -100 to 100
            Threat = HelperFunctions.ExponentialMovingAverage(Threat, newThreat, 0.9f);
        }

        public bool AttackForBorderViolation(DTrait personality, Empire targetEmpire, Empire attackingEmpire, bool isTrader)
        {
            if (Treaty_OpenBorders || Treaty_Peace) 
                return false;

            float borderAnger = Anger_FromShipsInOurBorders * (Anger_MilitaryConflict * 0.1f) + Anger_TerritorialConflict;

            if (isTrader)
            {
                if (Treaty_NAPact || Treaty_Trade)
                    return false;

                if (DoWeShareATradePartner(targetEmpire, attackingEmpire))
                    borderAnger *= 0.05f; // If the trader has str , this wont change anger
            }

            return borderAnger + 10 > (attackingEmpire.isPlayer ? attackingEmpire.data.BorderTolerance : personality.Territorialism);
        }

        public bool AttackForTransgressions(DTrait personality)
        {
            return !Treaty_NAPact && !Treaty_Peace && TotalAnger > (personality?.Territorialism
                ?? Them.Universe.Player.data.BorderTolerance);
        }

        void OfferTrade(Empire us)
        {
            if (TurnsKnown < FirstDemand
                || AtWar
                || Treaty_Trade
                || !Treaty_NAPact
                || HaveRejected_TRADE
                || AvailableTrust <= us.data.DiplomaticPersonality.Trade
                || turnsSinceLastContact < SecondDemand)
            {
                return;
            }

            Empire them = Them;
            if (them.isPlayer && (HaveRejected_TRADE || TotalAnger - Trust > 50))
                return;

            Offer offer1 = new Offer
            {
                TradeTreaty   = true,
                AcceptDL      = "Trade Accepted",
                RejectDL      = "Trade Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejected_TRADE,
                                               x => HaveRejected_TRADE = x)
            };

            Offer offer2 = new Offer { TradeTreaty = true };
            if (them.isPlayer)
                DiplomacyScreen.Show(us, "Offer Trade", offer2, offer1);
            else
                them.AI.AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);
        }

        void OfferNonAggression(Empire us)
        {
            if (TurnsKnown < FirstDemand
                || Treaty_NAPact
                || AvailableTrust <= us.data.DiplomaticPersonality.NAPact
                || turnsSinceLastContact < SecondDemand)
            {
                return;
            }

            Empire them = Them;
            if (them.isPlayer && HaveRejectedNaPact)
                return;

            Offer offer1 = new Offer
            {
                NAPact        = true,
                AcceptDL      = "NAPact Accepted",
                RejectDL      = "NAPact Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejectedNaPact,
                                               x => HaveRejectedNaPact = x)
            };

            Offer offer2 = new Offer { NAPact = true };
            if (them.isPlayer)
                DiplomacyScreen.Show(us, "Offer NAPact", offer2, offer1);
            else
                them.AI.AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);
        }

        void OfferOpenBorders(Empire us)
        {
            float territorialism = us.data.DiplomaticPersonality.Territorialism;
            if (turnsSinceLastContact < SecondDemand
                || AtWar
                || Trust < 40f
                || !Treaty_NAPact
                || !Treaty_Trade
                || Treaty_OpenBorders
                || AvailableTrust < us.data.DiplomaticPersonality.Territorialism
                || Anger_TerritorialConflict + Anger_FromShipsInOurBorders > 0.75f * territorialism)
            {
                return;
            }

            Empire them = Them;
            if (them.isPlayer && HaveRejected_OpenBorders)
                return;

            bool friendlyOpen      = Trust > 50f;
            Offer openBordersOffer = new Offer
            {
                OpenBorders   = true,
                AcceptDL      = "Open Borders Accepted",
                RejectDL      = friendlyOpen ? "Open Borders Friends Rejected" : "Open Borders Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejected_OpenBorders,
                                               x => HaveRejected_OpenBorders = x)
            };

            Offer ourOffer = new Offer { OpenBorders = true };
            if (them.isPlayer)
                DiplomacyScreen.Show(us, friendlyOpen ? "Offer Open Borders Friends" : "Offer Open Borders", ourOffer, openBordersOffer);
            else
                them.AI.AnalyzeOffer(ourOffer, openBordersOffer, us, Offer.Attitude.Pleading);
        }

        void OfferAlliance(Empire us)
        {
            if (TurnsAbove95 < us.PersonalityModifiers.TurnsAbove95AllianceTreshold * Them.Universe.P.Pace
                || turnsSinceLastContact < SecondDemand
                || Treaty_Alliance
                || !Treaty_Trade
                || !Treaty_NAPact
                || !Treaty_OpenBorders  
                || Anger_DiplomaticConflict >= 20
                // should maybe remove after implementing trust based on E and D personallity
                || Them.OffensiveStrength < us.OffensiveStrength * us.PersonalityModifiers.AlliancOfferStrThreshold)
            {
                return;
            }

            Empire them = Them;
            if (them.isPlayer && HaveRejected_Alliance)
                return;

            Offer offer1 = new Offer
            {
                Alliance      = true,
                AcceptDL      = "ALLIANCE_ACCEPTED",
                RejectDL      = "ALLIANCE_REJECTED",
                ValueToModify = new Ref<bool>(() => HaveRejected_Alliance,
                    x =>
                    {
                        HaveRejected_Alliance = x;
                        if (!HaveRejected_Alliance)
                            SetAlliance(true, us, them);
                    })
            };

            Offer offer2 = new Offer();
            if (them.isPlayer)
            {
                DiplomacyScreen.Show(us, "OFFER_ALLIANCE", offer2, offer1);
            }
            else
            {
                offer2.Alliance      = true;
                offer2.AcceptDL      = "ALLIANCE_ACCEPTED";
                offer2.RejectDL      = "ALLIANCE_REJECTED";
                offer2.ValueToModify = new Ref<bool>(() => HaveRejected_Alliance,
                    x => { HaveRejected_Alliance = x; });
                them.AI.AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);
            }
        }

        void Federate(Empire us, Empire them)
        {
            if (them.isPlayer
                || them.IsXenophobic && !us.IsXenophobic
                || TurnsAbove95 < TurnsAbove95Federation(us)
                || turnsSinceLastContact < 100 * Them.Universe.P.Pace
                || !Treaty_Alliance
                || TotalAnger > 10
                || Trust < 100
                || them.TotalScore < us.TotalScore * 1.5f)
            {
                return;
            }

            turnsSinceLastContact = 0; // Try again after 100 turns * Pace
            if ((Trust >= 120 && us.TotalPopBillion < them.TotalPopBillion
                || Trust >= 100 && us.TotalPopBillion < them.TotalPopBillion / 2)
                && Is3RdPartyBiggerThenUs(us, them))
            {
                us.Universe.Notifications.AddPeacefulMergerNotification(us, them);
                them.AbsorbEmpire(us);
            }
        }

        static public bool Is3RdPartyBiggerThenUs(Empire us, Empire them)
        {
            float popRatioWar = us.PersonalityModifiers.FederationPopRatioWar;
            float averageWarsGrade = us.GetAverageWarGrade();
            foreach (Empire e in us.Universe.ActiveMajorEmpires)
            {
                if (e == us || e == them)
                    continue;

                float ratio = us.IsAtWarWith(e) && averageWarsGrade < 2.5f ? popRatioWar : 5;
                if (e.TotalPopBillion / us.TotalPopBillion > ratio) // 3rd party is a potential risk
                    return true;
            }

            return false;
        }

        public void OfferMergeOrSurrenderToPlayer(Empire us, string dialogue)
        {
            var offer = new Offer
            {
                AcceptDL = "OFFER_MERGE_ACCEPTED",
                RejectDL = "OFFER_MERGE_REJECTED",
                ValueToModify = new Ref<bool>(() => RefusedMerge, x =>
                {
                    RefusedMerge = x;
                    if (!RefusedMerge)
                    {
                        us.Universe.Player.AbsorbEmpire(us);
                        us.Universe.Notifications.AddMergeWithPlayer(us);
                    }
                })
            };

            Offer ourOffer = new Offer();
            DiplomacyScreen.Show(us, dialogue, ourOffer, offer);
        }

        void ReferToMilitary(Empire us, float threatForInsult, bool compliment = true)
        {
            Empire them = Them;
            float anger = us.IsAggressive ? 0.15f : 0.1f;
            if (Threat <= threatForInsult)
            {
                if (!HaveInsulted_Military && TurnsKnown > FirstDemand)
                {
                    HaveInsulted_Military = true;
                    if (them.isPlayer)
                        DiplomacyScreen.Show(us, "Insult Military");
                }

                AddAngerDiplomaticConflict(anger);
            }
            else if (compliment && Threat > 25f && TurnsKnown > FirstDemand)
            {
                if (!HaveComplimented_Military && HaveInsulted_Military &&
                    TurnsKnown > FirstDemand && them.isPlayer)
                {
                    HaveComplimented_Military = true;
                    if (!HaveInsulted_Military || TurnsKnown <= SecondDemand)
                        DiplomacyScreen.Show(us, "Compliment Military");
                    else
                        DiplomacyScreen.Show(us, "Compliment Military Better");
                }

                AddAngerDiplomaticConflict(-anger);
            }
        }

        public void RequestPeaceNow(Empire us) => RequestPeace(us, true);

        void RequestPeace(Empire us, bool requestNow = false)
        {
            if (ActiveWar.TurnsAtWar == 0 || ActiveWar.TurnsAtWar % (int)(100 * us.Universe.P.Pace) > 0 && !requestNow)
                return;

            WarState warState    = ActiveWar.GetWarScoreState();
            Empire them          = Them;
            float warsGrade      = us.GetAverageWarGrade();
            float gradeThreshold = us.PersonalityModifiers.WarGradeThresholdForPeace;

            if (!us.IsLosingInWarWith(us.Universe.Player))
            {
                if (warsGrade > gradeThreshold && !us.IsLosingInWarWith(them))
                    return;
            }

            switch (ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    if (Anger_FromShipsInOurBorders + Anger_TerritorialConflict > us.data.DiplomaticPersonality.Territorialism)
                        return;

                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(us, them, "OFFERPEACE_LOSINGBC"); break;
                        case WarState.WinningSlightly: OfferPeace(us, them, "OFFERPEACE_FAIR"); break;
                        case WarState.Dominating:      OfferPeace(us, them, "OFFERPEACE_WINNINGBC"); break;
                    }

                    break;
                case WarType.ImperialistWar:
                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(us, them, "OFFERPEACE_PLEADING"); break;
                        case WarState.WinningSlightly: OfferPeace(us, them, "OFFERPEACE_FAIR"); break;
                        case WarState.Dominating:      OfferPeace(us, them, "OFFERPEACE_FAIR_WINNING"); break;
                        case WarState.EvenlyMatched:   OfferPeace(us, them, "OFFERPEACE_EVENLY_MATCHED"); break;
                    }

                    break;
                case WarType.DefensiveWar:
                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(us, them, "OFFERPEACE_PLEADING"); break;
                        case WarState.WinningSlightly: OfferPeace(us, them, "OFFERPEACE_FAIR"); break;
                        case WarState.Dominating:      OfferPeace(us, them, "OFFERPEACE_FAIR_WINNING"); break;
                        case WarState.EvenlyMatched:   OfferPeace(us, them, "OFFERPEACE_EVENLY_MATCHED"); break;
                    }

                    break;
            }

            turnsSinceLastContact = 0;
        }

        public void OfferPeace(Empire us, Empire them, string whichPeace)
        {
            var offerPeace = new Offer
            {
                PeaceTreaty   = true,
                AcceptDL      = "OFFERPEACE_ACCEPTED", // This will be modified in Process Peace
                RejectDL      = "OFFERPEACE_REJECTED", // This will be modified in Process Peace
                ValueToModify = new Ref<bool>(() => false, x => SetImperialistWar())
            };

            string dialogue = whichPeace;
            Offer ourOffer  = new Offer { PeaceTreaty = true };

            if (them.isPlayer)
                DiplomacyScreen.Show(us, dialogue, ourOffer, offerPeace);
            else
                them.AI.AnalyzeOffer(ourOffer, offerPeace, us, Offer.Attitude.Respectful);
        }

        void DemandTech(Empire us)
        {
            if (TurnsKnown < SecondDemand
                || TurnsSinceLastTechDemand < DemandTechTurns)
            {
                return;
            }

            TurnsSinceLastTechDemand = 0;
            Empire them = Them;
            if (!them.AI.TradableTechs(us, out Array<TechEntry> potentialDemands, true))
                return;

            TechEntry techToDemand = us.Random.Item(potentialDemands);
            Offer demandTech = new Offer();
            demandTech.TechnologiesOffered.AddUnique(techToDemand.UID);

            Offer theirDemand = new Offer
            {
                AcceptDL      = "Xeno Demand Tech Accepted",
                RejectDL      = "Xeno Demand Tech Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejectedDemandTech,
                                               x => HaveRejectedDemandTech = x)
            };

            if (them.isPlayer)
                DiplomacyScreen.Show(us, "Xeno Demand Tech", demandTech, theirDemand);
            else
                them.AI.AnalyzeOffer(theirDemand, demandTech, us, Offer.Attitude.Threaten);
        }

        void TradeTech(Empire us)
        {
            if (TurnsSinceLastTechTrade < TechTradeTurns || turnsSinceLastContact < TechTradeTurns ||
                Them.isPlayer || ActiveWar != null || Posture == Posture.Hostile)
                return;

            // always reset this to ensure trade check is done every TechTradeTurns interval
            TurnsSinceLastTechTrade = 0;

            Empire them = Them;
            Relationship themToUs = them.GetRelations(us);
            if (themToUs.Anger_DiplomaticConflict > 20)
                return;

            // Get techs we can offer them
            if (!TechsToOffer(us, them, out Array<TechEntry> ourTechs))
                return;

            // Get techs they can offer us
            if (!TechsToOffer(them, us, out Array<TechEntry> theirTechs))
                return;

            // Get final techs we offer and their techs we want to trade 
            if (!DetermineTechTrade(us, them, ourTechs, theirTechs, out string ourTechOffer, out Array<string> theirTechOffer))
                return;

            Offer ourOffer = new Offer();
            ourOffer.TechnologiesOffered.Add(ourTechOffer);
            Offer theirOffer = new Offer();
            foreach (string techName in theirTechOffer)
                theirOffer.TechnologiesOffered.Add(techName);

            Offer.Attitude ourAttitude = us.IsAggressive || us.IsRuthless || us.IsXenophobic ? Offer.Attitude.Respectful : Offer.Attitude.Pleading;
            them.AI.AnalyzeOffer(ourOffer, theirOffer, us, ourAttitude, resetTurnsSinceLastContacted: false);
        }

        bool TechsToOffer(Empire us, Empire them, out Array<TechEntry> techs)
        {
            techs = new Array<TechEntry>();
            if (!us.AI.TradableTechs(them, out Array<TechEntry> ourTechs, !us.isPlayer && !them.isPlayer))
                return false;

            IShipDesign[] theirDesigns = them.AllFactionShipDesigns;
            foreach (TechEntry entry in ourTechs)
            {
                if (them.WeCanUseThisTech(entry, theirDesigns))
                    techs.Add(entry);
            }

            return techs.Count > 0;
        }

        bool DetermineTechTrade(Empire us, Empire them, Array<TechEntry> ourTechs, Array<TechEntry> theirTechs, 
            out string ourFinalOffer, out Array<string> theirFinalOffer)
        {
            //theirFinalOffer = new Array<string>();

            TechEntry ourTech = us.Random.Item(ourTechs);
            float ourTechCost = ourTech.Tech.Cost + us.data.OngoingDiplomaticModifier * ourTech.Tech.Cost;
            ourFinalOffer     = ourTech.UID;
            if (!GetTheirTechsForOurOffer(ourTechCost, them.GetRelations(us).Posture, theirTechs, out theirFinalOffer))
                return false;

            return theirFinalOffer.Count > 0;
        }

        bool GetTheirTechsForOurOffer(float ourTechCost, Posture theirPosture, Array<TechEntry> theirTechs,  out Array<string> theirFinalTech)
        {
            theirFinalTech      = new Array<string>();
            float techCostRatio = GetTechTradeCostRatio(theirPosture);
            float theirMaxCost  = ourTechCost * techCostRatio;
            float totalCost     = 0;

            foreach (TechEntry tech in theirTechs.Sorted(t => t.Tech.Cost))
            {
                if (tech.TechCost + totalCost > theirMaxCost)
                    break;

                theirFinalTech.Add(tech.UID);
                totalCost += tech.TechCost;
            }

            return theirFinalTech.Count > 0;
        }

        float GetTechTradeCostRatio(Posture theirPosture)
        {
            float techCostRatio = 1; // Less than 1 means we want cheaper cost than what we offer
            switch (Posture)
            {
                case Posture.Friendly:
                    switch (theirPosture)
                    {
                        case Posture.Neutral: techCostRatio = 0.9f; break;
                        case Posture.Hostile: techCostRatio = 0.75f; break;
                    }

                    break;
                case Posture.Neutral:
                    switch (theirPosture)
                    {
                        case Posture.Friendly: techCostRatio = 1.1f; break;
                        case Posture.Hostile: techCostRatio = 0.9f; break;
                    }

                    break;
            }

            return techCostRatio;
        }

        void WarnAboutShips(Empire us)
        {
            Empire them = Them;
            if (Anger_FromShipsInOurBorders > us.data.DiplomaticPersonality.Territorialism / 4f
                && !AtWar && !WarnedAboutShips)
            {
                if (them.isPlayer && turnsSinceLastContact > FirstDemand)
                    if (!WarnedAboutColonizing)
                        DiplomacyScreen.Show(us, them, "Warning Ships");
                    else if (ContestedSystem != null)
                        DiplomacyScreen.Show(us, them, "Warning Colonized then Ships", ContestedSystem);

                turnsSinceLastContact = 0;
                WarnedAboutShips = true;
            }
        }

        public void RequestHelpFromAllies(Empire us, Empire enemy, int contactThreshold)
        {
            if (ActiveWar == null || ActiveWar.TurnsAtWar < 25 && !enemy.isPlayer) // They Accepted Peace or too soon
                return;

            var allies = new Array<Empire>();
            foreach (Relationship rel in us.AllRelations)
            {
                if (rel.Treaty_Alliance
                    && rel.Them.IsKnown(enemy)
                    && !rel.Them.IsAtWarWith(enemy)
                    && !rel.Them.IsPeaceTreaty(enemy))
                {
                    allies.Add(rel.Them);
                }
            }

            foreach (Empire ally in allies)
            {
                Relationship usToAlly = us.GetRelations(ally);
                if (!ActiveWar.AlliesCalled.Contains(ally.data.Traits.Name)
                    && usToAlly.turnsSinceLastContact > (ally.isPlayer ? contactThreshold * 2 : contactThreshold))
                {
                    us.AI.CallAllyToWar(ally, enemy);
                    if (ally.IsAtWarWith(enemy))
                        ActiveWar.AlliesCalled.Add(ally.data.Traits.Name);

                    usToAlly.turnsSinceLastContact = 0;
                }
            }
        }

        void AssessDiplomaticAnger(Empire us)
        {
            if (!Known)
                return;

            Empire them = Them;
            if (us.IsAggressive && Threat < -15f && !us.IsAtWar)
            {
                float angerMod = -Threat / 15; // every -15 threat will give +0.1 anger
                AddAngerMilitaryConflict(us.data.DiplomaticPersonality.AngerDissipation + 0.1f * angerMod);
            }

            if (Anger_MilitaryConflict > 80 && !PreparingForWar && !AtWar && !Treaty_Peace)
            {
                if (us.TryConfirmPrepareForWarType(them, WarType.DefensiveWar, out WarType warType))
                    PrepareForWar(warType, us);

                return;
            }

            if (!PreparingForWar
                && Anger_TerritorialConflict + Anger_FromShipsInOurBorders >= us.data.DiplomaticPersonality.Territorialism
                && !AtWar 
                && !Treaty_OpenBorders 
                && !Treaty_Peace 
                && them.CurrentMilitaryStrength  * us.PersonalityModifiers.GoToWarTolerance < us.OffensiveStrength)
            {
                if (us.TryConfirmPrepareForWarType(them, WarType.BorderConflict, out WarType warType))
                    PrepareForWar(warType, us);

                return;
            }

            WarnAboutShips(us);
        }

        bool TheyArePotentialTargetHonorable(Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar || TurnsKnown < SecondDemand)
                return false;

            if (Treaty_Alliance && TheyDeclaredWarOnAlly && Trust < 50 && Threat < 10)
                return true;

            if (Threat > -10 && Threat < 10 && !Treaty_Alliance && TotalAnger > 75 && !them.IsAtWar)
                return true;

            return false;
        }

        bool TheyArePotentialTargetsPacifist(Empire us)
        {
            if (us.Universe.P.Difficulty is GameDifficulty.Normal || Treaty_Peace || AtWar || PreparingForWar || TurnsKnown < SecondDemand)
                return false;

            if (!Treaty_Alliance &&  us.Universe.GetAllies(us).Count > 0 && TheyDeclaredWarOnAlly && Trust < 0 && Threat < -10)
                return true;

            return false;
        }

        bool TheyArePotentialTargetCunning(Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar || TurnsKnown < SecondDemand)
                return false;

            if (Threat < -50 && them.GetAverageWarGrade() < 5 || Threat < -25 && them.GetAverageWarGrade() < 3)
                return true;

            if (!Treaty_Alliance && Threat < 0 && them.AtWarCount > 1 && them.GetAverageWarGrade() < 7)
                return true;

            return false;
        }

        bool TheyArePotentialTargetRuthless(Empire us, Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar || Threat > 0f || TurnsKnown < SecondDemand)
                return false;

            if (Threat < -50f && !Treaty_Alliance)
                return true;

            // Ruthless will break alliances if the other party does not have strong military but valuable colonies
            if (Threat < -30f && us.TotalColonyValues < them.TotalColonyValues)
                return true;

            return false;
        }

        bool TheyArePotentialTargetAggressive(Empire us, Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar)
                return false;

            if (Threat < -40f && TurnsKnown > SecondDemand && !Treaty_Alliance)
            {
                if (TotalAnger > 75f || us.MaxColonyValue > them.MaxColonyValue*1.25f)
                    return true;
            }
            else if (Threat <= -75f)
            {
                return true;
            }

            return false;
        }

        bool TheyArePotentialTargetXenophobic(Empire us, Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar || Posture == Posture.Friendly)
                return false;

            return them.ExpansionScore > us.ExpansionScore * 1.25f && TotalAnger > 20f;
        }

        void UpdateTreatiesByTrust(Empire us, Empire them)
        {
            if (turnsSinceLastContact < 10)
                return;

            if (Treaty_OpenBorders && Trust < 5)
            {
                if (them.isPlayer)
                    DiplomacyScreen.Show(us, "TRUST_LOW_OPEN_BORDERS");
                us.BreakAllianceAndOpenBordersWith(them);
                turnsSinceLastContact = 0;
            }
            else if (Treaty_Alliance && Trust < 40)
            {
                if (them.isPlayer)
                    DiplomacyScreen.Show(us, "TRUST_LOW_ALLIANCE");
                us.BreakAllianceAndOpenBordersWith(them);
                turnsSinceLastContact = 0;
            }
        }

        // Pacifist, Cunning, Honorable
        public void DoConservative(Empire us, Empire them, out bool theyArePotentialTargets)
        {
            switch (Posture)
            {
                case Posture.Friendly:
                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    OfferOpenBorders(us);
                    OfferAlliance(us);
                    Federate(us, them);
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    AssessDiplomaticAnger(us);
                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    if (us.GetAverageWarGrade() <= us.PersonalityModifiers.WarGradeThresholdForPeace)
                        RequestPeace(us);

                    RequestHelpFromAllies(us, them, 50);
                    break;
                case Posture.Hostile:
                    AssessDiplomaticAnger(us);
                    ChangeToNeutralIfPossible(us);
                    if (them.isPlayer && Threat < -75 && us.IsCunning && us.Universe.P.Difficulty > GameDifficulty.Normal)
                        DemandTech(us);
                    break;
            }

            switch (us.Personality)
            {
                case PersonalityType.Cunning:   theyArePotentialTargets = TheyArePotentialTargetCunning(them);   break;
                case PersonalityType.Honorable: theyArePotentialTargets = TheyArePotentialTargetHonorable(them); break;
                case PersonalityType.Pacifist:  theyArePotentialTargets = TheyArePotentialTargetsPacifist(us);   break;
                default:                        theyArePotentialTargets = false;                                 break;
            }

            UpdateTreatiesByTrust(us, them);
        }

        public void DoRuthless(Empire us, Empire them, out bool theyArePotentialTargets)
        {
            switch (Posture)
            {
                case Posture.Friendly:
                    if (them.isPlayer && Threat < -95 && us.Universe.P.Difficulty > GameDifficulty.Hard)
                        DemandTech(us);

                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    Federate(us, them);
                    OfferAlliance(us);
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    if (them.isPlayer && Threat < -75 && us.Universe.P.Difficulty > GameDifficulty.Normal)
                        DemandTech(us);

                    AssessDiplomaticAnger(us);
                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    ReferToMilitary(us, threatForInsult: -20, compliment: false);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestHelpFromAllies(us, them, 50);
                    break;
                case
                    Posture.Hostile:
                    ReferToMilitary(us, threatForInsult: -15, compliment: false);
                    if (them.isPlayer && Threat < -65)
                        DemandTech(us);

                    AssessDiplomaticAnger(us);
                    ChangeToNeutralIfPossible(us);
                    break;
            }

            theyArePotentialTargets = TheyArePotentialTargetRuthless(us, them);
            UpdateTreatiesByTrust(us, them);
        }

        public void DoAggressive(Empire us, Empire them, out bool theyArePotentialTargets)
        {
            theyArePotentialTargets = false;
            AssessDiplomaticAnger(us);
            ReferToMilitary(us, threatForInsult: -10);
            switch (Posture)
            {
                case Posture.Friendly:
                    if (them.isPlayer && Threat < -95 && us.Universe.P.Difficulty > GameDifficulty.Hard)
                        DemandTech(us);

                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    OfferOpenBorders(us);
                    OfferAlliance(us);
                    Federate(us, them);
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    if (them.isPlayer && Threat < -75 && us.Universe.P.Difficulty > GameDifficulty.Normal)
                        DemandTech(us);

                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us);
                    RequestHelpFromAllies(us, them, 50);
                    break;
                case Posture.Hostile:
                    if (them.isPlayer && Threat < -35)
                        DemandTech(us);

                    theyArePotentialTargets = TheyArePotentialTargetAggressive(us, them);
                    break;
            }

            UpdateTreatiesByTrust(us, them);
        }

        public void DoXenophobic(Empire us, Empire them, out bool theyArePotentialTargets)
        {
            AssessDiplomaticAnger(us);
            switch (Posture)
            {
                case Posture.Friendly:
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToNeutralIfPossible(us);
                    Federate(us, them);
                    OfferAlliance(us);
                    break;
                case Posture.Neutral:
                    if (them.isPlayer && Threat < -75 && us.Universe.P.Difficulty > GameDifficulty.Normal)
                        DemandTech(us);

                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us);
                    break;
                case Posture.Hostile:
                    if (them.isPlayer && Threat < -45)
                        DemandTech(us);
                    ChangeToNeutralIfPossible(us);
                    break;
            }

            theyArePotentialTargets = TheyArePotentialTargetXenophobic(us, them);
            UpdateTreatiesByTrust(us, them);
        }

        void ChangeToFriendlyIfPossible(Empire us)
        {
            switch (us.Personality)
            {
                case PersonalityType.Cunning:
                case PersonalityType.Honorable:
                case PersonalityType.Pacifist:
                    if (TurnsKnown > FirstDemand && Treaty_NAPact || Trust > 50f && TotalAnger < 10)
                        break;

                    return;
                case PersonalityType.Aggressive:
                    if (TurnsKnown > SecondDemand && Threat > 0 && Trust > 50f && TotalAnger < 10)
                        break;

                    return;
                case PersonalityType.Ruthless:
                    if (TurnsKnown > SecondDemand && Threat < 0 && Trust > 50f && TotalAnger < 10)
                        break;

                    return;
                case PersonalityType.Xenophobic:
                    if (TurnsKnown > FirstDemand && Trust > 50f && TotalAnger < 10 && !HaveRejectedDemandTech)
                        break;

                    return;
            }

            ChangeToFriendly();
        }

        void ChangeToNeutralIfPossible(Empire us)
        {
            if (AtWar)
                return;

            switch (us.Personality)
            {
                case PersonalityType.Cunning:
                case PersonalityType.Honorable:
                case PersonalityType.Pacifist:
                    if (TotalAnger > 10)
                    {
                        if (!Treaty_NAPact && Trust < 50 || Treaty_NAPact && Trust < 10)
                            break; // Friendly to Neutral
                    }
                    else
                    {
                        if (Treaty_NAPact && Trust > 10)
                            break; // Hostile to Neutral
                    }

                    return;
                case PersonalityType.Aggressive:
                    if (Threat > -15 && TotalAnger < 50 && Trust > 15)
                        break;  // Hostile to Neutral

                    if (Threat < -15 && TotalAnger > 50 && Trust < 15)
                        break; // Friendly to Neutral

                    return;
                case PersonalityType.Ruthless:
                    if (Trust > 10 && TotalAnger < 20)
                        break; // Hostile to Neutral

                    if (Trust < 10 && TotalAnger > 20)
                        break; // Friendly to Neutral

                    return;
                case PersonalityType.Xenophobic:
                    if (Trust > 10 && TotalAnger < 10)
                        break; // Hostile to Neutral

                    if (Trust < 10 && TotalAnger > 10)
                        break; // Friendly to Neutral

                    return;
            }

            ChangeToNeutral();
        }

        void ChangeToHostileIfPossible(Empire us)
        {
            switch (us.Personality)
            {
                case PersonalityType.Cunning:
                case PersonalityType.Honorable:
                case PersonalityType.Pacifist:
                    if ((!Treaty_NAPact || Trust.AlmostEqual(0)) && TotalAnger > 50)
                        break;

                    return;
                case PersonalityType.Aggressive:
                    if (Threat < -45 || TotalAnger > 50 || Treaty_NAPact && TotalAnger > 75)
                        break;

                    return;
                case PersonalityType.Ruthless:
                    if (Trust < 10 && TotalAnger > 20)
                        break;

                    return;
                case PersonalityType.Xenophobic:
                    if (Trust < 10 && TotalAnger > 10)
                        break;

                    return;
            }

            ChangeToHostile();
        }

        public void SetAlliance(bool ally, Empire us, Empire them)
        {
            if (ally)
                us.SignAllianceWith(them);
            else
                us.BreakAllianceWith(them);
        }

        public void LostAShip(Ship ourShip)
        {
            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(ourShip);
            float angerToAdd = ourShip.ShipData.IsColonyShip ? 10 : (killedExpSettings.KillExp / 5).LowerBound(1);
            AddAngerMilitaryConflict(angerToAdd);
            ActiveWar?.ShipWeLost(ourShip);

        }
        public void KilledAShip(Ship theirShip) => ActiveWar?.ShipWeKilled(theirShip);

        public void LostAColony(Planet colony, Empire attacker)
        {
            ActiveWar?.PlanetWeLost(attacker, colony);
            AddAngerMilitaryConflict(colony.ColonyValue);
        }

        public void WonAColony(Planet colony, Empire loser)
        {
            ActiveWar?.PlanetWeWon(loser, colony);
        }

        public static bool DoWeShareATradePartner(Empire them, Empire us)
        {
            var theirTrade = us.Universe.GetTradePartners(them);
            var ourTrade = us.Universe.GetTradePartners(them);
            foreach (var trade in theirTrade)
            {
                if (ourTrade.ContainsRef(trade))
                    return true;
            }

            return false;
        }

        public DebugTextBlock DebugWar(Empire us)
        {
            Color color = Them.EmpireColor;
            var debug = new DebugTextBlock
            {
                Header      = $"Relation To: {Them.data.PortraitName}",
                HeaderColor = color,
            };

           debug.AddLine(ActiveWar == null
                ? $" ReadyForWar: {us.ShouldGoToWar(this, Them)}"
                : " At War", color);

            debug.AddLine($" WarType: {PreparingForWarType}", color);
            debug.AddLine($" WarStatus: {ActiveWar?.GetWarScoreState()}", color);
            debug.AddLine($" {us.data.PortraitName} Strength: {us.CurrentMilitaryStrength}", color);
            debug.AddLine($" {Them.data.PortraitName} Strength: {Them.CurrentMilitaryStrength}", color);
            debug.AddLine($" WarAnger: {WarAnger}", color);
            debug.AddLine($" Previous Wars: {WarHistory.Count}", color);

            ActiveWar?.WarDebugData(ref debug);
            us.AI.DebugDrawTasks(ref debug, Them, warTasks: true);
            return debug;
        }

        public void AddAngerDiplomaticConflict(float amount)
        {
            Anger_DiplomaticConflict = (Anger_DiplomaticConflict + amount).Clamped(0, 100);
        }

        public void AddAngerMilitaryConflict(float amount)
        {
            Anger_MilitaryConflict = (Anger_MilitaryConflict + amount).Clamped(0, 100);
        }

        public void ResetAngerMilitaryConflict()
        {
            AddAngerMilitaryConflict(-Anger_MilitaryConflict);
        }

        public void AddAngerShipsInOurBorders(float amount)
        {
            Anger_FromShipsInOurBorders = (Anger_FromShipsInOurBorders + amount).Clamped(0, 100);
        }

        public void AddAngerTerritorialConflict(float amount)
        {
            Anger_TerritorialConflict = (Anger_TerritorialConflict + amount).Clamped(0, 100);
        }

        public void ChangeToFriendly()
        {
            Posture = Posture.Friendly;
        }

        public void ChangeToNeutral()
        {
            Posture = Posture.Neutral;
        }

        public void ChangeToHostile()
        {
            Posture = Posture.Hostile;
        }
    }

    public enum TreatyType
    {
        Alliance,
        NonAggression,
        OpenBorders,
        Peace,
        Trade
    }
}