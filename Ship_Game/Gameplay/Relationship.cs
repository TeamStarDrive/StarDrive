using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Commands.Goals;
using Ship_Game.Debug;
using Ship_Game.Empires.Components;
using Ship_Game.GameScreens.DiplomacyScreen;

namespace Ship_Game.Gameplay
{
    public sealed class TrustEntry
    {
        [Serialize(0)] public int TurnTimer;
        [Serialize(1)] public int TurnsInExistence;
        [Serialize(2)] public float TrustCost;
        [Serialize(3)] public TrustEntryType Type;
    }

    public sealed class FearEntry
    {
        [Serialize(0)] public int TurnTimer;
        [Serialize(1)] public float FearCost;
        [Serialize(2)] public float TurnsInExistence;
        [Serialize(3)] public TrustEntryType Type;
    }

    public sealed class FederationQuest
    {
        [Serialize(0)] public QuestType type;
        [Serialize(1)] public string EnemyName;
    }

    public sealed class Relationship : IDisposable
    {
        [Serialize(0)] public FederationQuest FedQuest;
        [Serialize(1)] public Posture Posture = Posture.Neutral;  // FB - use SetPosture privately or ChangeTo methods publicly
        [Serialize(2)] public string Name;
        [Serialize(3)] public bool Known;
        [Serialize(4)] public float IntelligenceBudget;
        [Serialize(5)] public float IntelligencePenetration;
        [Serialize(6)] public int turnsSinceLastContact;
        [Serialize(7)] public bool WarnedAboutShips;
        [Serialize(8)] public bool WarnedAboutColonizing;
        [Serialize(9)] public int PlayerContactStep; //  Encounter Step to use when the player contacts this faction

        [Serialize(10)] public float Anger_FromShipsInOurBorders; // FB - Use AddAngerShipsInOurBorders
        [Serialize(11)] public float Anger_TerritorialConflict; // FB - Use AddAngerTerritorialConflict
        [Serialize(12)] public float Anger_MilitaryConflict; // FB - Use AddAngerMilitaryConflict
        [Serialize(13)] public float Anger_DiplomaticConflict; // FB - Use AddAngerDiplomaticConflict

        [Serialize(14)] public int SpiesDetected;
        [Serialize(15)] public int TimesSpiedOnAlly;
        [Serialize(16)] public int SpiesKilled;
        [Serialize(17)] public float TotalAnger;
        [Serialize(18)] public bool Treaty_OpenBorders; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [Serialize(19)] public bool Treaty_NAPact; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [Serialize(20)] public bool Treaty_Trade; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [Serialize(21)] public int Treaty_Trade_TurnsExisted;
        [Serialize(22)] public bool Treaty_Alliance; // FB - check Empire_Relationship to see how to set it. Do not access directly!
        [Serialize(23)] public bool Treaty_Peace; // FB - check Empire_Relationship to see how to set it. Do not access directly!

        [Serialize(24)] public int PeaceTurnsRemaining;
        [Serialize(25)] public float Threat;
        [Serialize(26)] public float Trust;
        [Serialize(27)] public War ActiveWar;
        [Serialize(28)] public Array<War> WarHistory = new Array<War>();
        [Serialize(29)] public bool haveRejectedNAPact;
        [Serialize(30)] public bool HaveRejected_TRADE;
        [Serialize(31)] public bool haveRejectedDemandTech;
        [Serialize(32)] public bool HaveRejected_OpenBorders;
        [Serialize(33)] public bool HaveRejected_Alliance;
        [Serialize(34)] public int NumberStolenClaims;

        [Serialize(35)] public Array<Guid> StolenSystems = new Array<Guid>();
        [Serialize(36)] public bool HaveInsulted_Military;
        [Serialize(37)] public bool HaveComplimented_Military;
        [Serialize(38)] public bool XenoDemandedTech;
        [Serialize(39)] public Array<Guid> WarnedSystemsList = new Array<Guid>();
        [Serialize(40)] public bool HaveWarnedTwice;
        [Serialize(41)] public bool HaveWarnedThrice;
        [Serialize(42)] public Guid contestedSystemGuid;
        [Serialize(43)] public bool AtWar;
        [Serialize(44)] public bool PreparingForWar; // Use prepareForWar or CancelPrepareForWar
        [Serialize(45)] public WarType PreparingForWarType = WarType.ImperialistWar;  // Use prepareForWar or CancelPrepareForWar
        [Serialize(46)] public int DefenseFleet = -1;
        [Serialize(47)] public bool HasDefenseFleet;
        [Serialize(48)] public float InvasiveColonyPenalty;
        [Serialize(49)] public float AggressionAgainstUsPenalty;
        [Serialize(50)] public float InitialStrength;
        [Serialize(51)] public int TurnsKnown;
        [Serialize(52)] public int TurnsAbove95; // Trust
        [Serialize(53)] public int TurnsAllied;

        [Serialize(54)] public BatchRemovalCollection<TrustEntry> TrustEntries = new BatchRemovalCollection<TrustEntry>();
        [Serialize(55)] public BatchRemovalCollection<FearEntry> FearEntries = new BatchRemovalCollection<FearEntry>();
        [Serialize(56)] public float TrustUsed;
        [Serialize(57)] public float FearUsed;
        [Serialize(58)] public float TheyOweUs;
        [Serialize(59)] public float WeOweThem;
        [Serialize(60)] public int TurnsAtWar;
        [Serialize(61)] public int FactionContactStep;  // Encounter Step to use when the faction contacts the player;
        [Serialize(62)] public bool CanAttack ; // New: Bilateral condition if these two empires can attack each other
        [Serialize(63)] public bool IsHostile = true; // New: If target empire is hostile and might attack us
        [Serialize(64)] public int NumTechsWeGave; // number of tech they have given us, through tech trade or demands.
        [Serialize(65)] public EmpireInformation.InformationLevel IntelligenceLevel = EmpireInformation.InformationLevel.Full;

        [XmlIgnore][JsonIgnore] public EmpireRiskAssessment Risk;
        [XmlIgnore][JsonIgnore] public Empire Them => EmpireManager.GetEmpireByName(Name);
        [XmlIgnore][JsonIgnore] public float AvailableTrust => Trust - TrustUsed;
        [XmlIgnore][JsonIgnore] Empire Player => Empire.Universe.PlayerEmpire;
        [XmlIgnore][JsonIgnore] public EmpireInformation KnownInformation;
        [XmlIgnore][JsonIgnore] public int WarAnger => (int)(TotalAnger - Trust.LowerBound(-50));

        private readonly int FirstDemand   = 50;
        public readonly int SecondDemand   = 75;
        public readonly int TechTradeTurns = 100;

        /// <summary>
        /// Tech transfer restriction.
        /// currently this is disabling tech content trade via diplomacy.
        /// A check here can be added to remove this for allies.
        /// </summary>
        [XmlIgnore][JsonIgnore]
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
                TotalAnger += 20f; // TODO check this
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

        public Relationship(string name)
        {
            Name = name;
            Risk = new EmpireRiskAssessment(this);
            KnownInformation = new EmpireInformation(this);
        }

        public Relationship()
        {
        }

        public void AddTrustEntry(Offer.Attitude attitude, TrustEntryType type, float cost, int turnTimer = 250)
        {
            if (attitude != Offer.Attitude.Threaten)
            {
                TrustEntries.Add(new TrustEntry
                {
                    TrustCost = cost,
                    TurnTimer = turnTimer,
                    Type = type
                });
            }
            else
            {
                FearEntries.Add(new FearEntry
                {
                    FearCost = cost,
                    TurnTimer = turnTimer,
                    Type = type
                });
            }
        }

        public void PrepareForWar(WarType type, Empire us)
        {
            if (PreparingForWar)
                return;

            if (Them.isPlayer && GlobalStats.RestrictAIPlayerInteraction)
                return;

            us.GetEmpireAI().AddGoal(new PrepareForWar(us, Them));
            PreparingForWar     = true;
            PreparingForWarType = type;
        }

        public void CancelPrepareForWar()
        {
            // Note - prepare for war goal will exit by itself since it has check logic for this
            PreparingForWar = false;
        }

        public float GetTurnsForFederationWithPlayer(Empire us) => TurnsAbove95Federation(us);

        int TurnsAbove95Federation(Empire us) => us.PersonalityModifiers.TurnsAbove95FederationNeeded 
                                                 * (int)(CurrentGame.GalaxySize + 1);
        
        public void SetTreaty(Empire us, TreatyType treatyType, bool value)
        {
            switch (treatyType)
            {
                case TreatyType.Alliance:      Treaty_Alliance    = value; HandleAlliance();              break;
                case TreatyType.NonAggression: Treaty_NAPact      = value;                                break;
                case TreatyType.OpenBorders:   Treaty_OpenBorders = value;                                break;
                case TreatyType.Peace:         Treaty_Peace       = value; HandlePeace();                 break;
                case TreatyType.Trade:         Treaty_Trade       = value; Treaty_Trade_TurnsExisted = 0; break;
            }

            // Local Method
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

            // Local Method
            void HandleAlliance()
            {
                if (value) // If treaty is signed
                {
                    CancelPrepareForWar();
                    WarnedSystemsList.Clear();
                }
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

        public SolarSystem[] GetPlanetsLostFromWars()
        {
            var lostSystems = new Array<SolarSystem>();
            for (int i = 0; i < WarHistory.Count; i++)
            {
                var war = WarHistory[i];
                var owner = EmpireManager.GetEmpireByName(war.UsName);
                if (war.ContestedSystemsGUIDs.IsEmpty) continue;
                var systems = SolarSystem.GetSolarSystemsFromGuids(war.ContestedSystemsGUIDs);
                for (int j = 0; j < systems.Count; j++)
                {
                    SolarSystem system = systems[j];
                    if (!system.OwnerList.Contains(owner))
                        lostSystems.AddUniqueRef(system);
                }
            }

            return lostSystems.ToArray();
        }

        public void StoleOurColonyClaim(Empire owner, Planet claimedPlanet, out bool newTheft)
        {
            NumberStolenClaims++;
            AddAngerTerritorialConflict(5f + (float)Math.Pow(5, NumberStolenClaims));
            Trust -= owner.DifficultyModifiers.TrustLostStoleColony;
            Trust -= owner.data.DiplomaticPersonality.Territorialism/5 * StolenSystems.Count.LowerBound(1);
            newTheft = !StolenSystems.Contains(claimedPlanet.ParentSystem.guid);
            StolenSystems.AddUnique(claimedPlanet.ParentSystem.guid);
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

        public void DamageRelationship(Empire us, Empire them, string why, float amount, Planet p)
        {
            if (us.data.DiplomaticPersonality == null || us.isPlayer)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction &&  them == Empire.Universe.PlayerEmpire)
                return;

            if (them.isPlayer)
                amount *= us.DifficultyModifiers.Anger;

            if (us.IsHonorable || us.IsXenophobic)
                amount *= 2;

            if (why != null)
            {
                if (why == "Caught Spying")
                {
                    SpiesDetected            += 1;
                    AddAngerDiplomaticConflict(amount);
                    TotalAnger               += amount;
                    Trust                    -= amount;

                    if (Treaty_Alliance)
                    {
                        TimesSpiedOnAlly += 1;
                        if (TimesSpiedOnAlly == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_1");

                            turnsSinceLastContact = 0;
                        }
                        else if (TimesSpiedOnAlly > 1)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_2");

                            us.BreakAllTreatiesWith(them);
                            turnsSinceLastContact = 0;
                        }
                    }
                    else if (SpiesDetected == 1 && !AtWar && Empire.Universe.PlayerEmpire == them && !us.isFaction)
                    {
                        if (SpiesDetected == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_1");

                            turnsSinceLastContact = 0;
                        }
                        else if (SpiesDetected == 2)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_2");

                            turnsSinceLastContact = 0;
                        }
                        else if (SpiesDetected >= 3)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_3");

                            us.BreakAllTreatiesWith(them);
                            turnsSinceLastContact = 0;
                        }
                    }
                }
                else if (why == "Caught Spying Failed")
                {
                    AddAngerDiplomaticConflict(amount);
                    TotalAnger               += amount;
                    Trust                    -= amount;

                    SpiesKilled += 1;

                    if (Treaty_Alliance)
                    {
                        TimesSpiedOnAlly += 1;
                        if (TimesSpiedOnAlly == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_1");
                        }
                        else if (TimesSpiedOnAlly > 1)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_Ally_2");

                            us.BreakAllTreatiesWith(them);
                            Posture = Posture.Hostile;
                        }
                    }
                    else if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                    {
                        DiplomacyScreen.ShowEndOnly(us, them, "Killed_Spy_1");
                    }
                }
                else if (why == "Insulted")
                {
                    AddAngerDiplomaticConflict(amount);
                    TotalAnger               += amount;
                    Trust                    -= amount;
                }
                else if (why == "Colonized Owned System")
                {
                    Array<Planet> ourTargetPlanets = new Array<Planet>();
                    Array<Planet> theirTargetPlanets = new Array<Planet>();
                    foreach (Goal g in us.GetEmpireAI().Goals)
                    {
                        if (g.type != GoalType.Colonize)
                            continue;

                        ourTargetPlanets.Add(g.ColonizationTarget);
                    }

                    foreach (Planet theirPlanet in them.GetPlanets())
                    {
                        theirTargetPlanets.Add(theirPlanet);
                    }

                    bool matchFound = false;
                    SolarSystem sharedSystem = null;
                    foreach (Planet planet in ourTargetPlanets)
                    {
                        foreach (Planet other in theirTargetPlanets)
                        {
                            if (p == null || other == null || p.ParentSystem != other.ParentSystem)
                            {
                                continue;
                            }
                            sharedSystem = p.ParentSystem;
                            matchFound = true;
                            break;
                        }
                        if (!matchFound || !us.GetRelations(them).WarnedSystemsList.Contains(sharedSystem.guid))
                        {
                            continue;
                        }
                        return;
                    }

                    float expansion = UniverseScreen.SolarSystemList.Count / us.GetOwnedSystems().Count + them.GetOwnedSystems().Count;
                    AddAngerTerritorialConflict(amount + expansion);
                    Trust -= amount;

                    if (Anger_TerritorialConflict < us.data.DiplomaticPersonality.Territorialism && !AtWar)
                    {
                        if (AtWar)
                            return;

                        if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                        {
                            if (!WarnedAboutShips)
                                DiplomacyScreen.Show(us, them, "Colonized Warning", p);
                            else if (!AtWar)
                                DiplomacyScreen.Show(us, them, "Warning Ships then Colonized", p);

                            turnsSinceLastContact  = 0;
                            WarnedAboutColonizing  = true;

                            if (p != null)
                                contestedSystemGuid = p.ParentSystem.guid;
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
                        if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
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

        public bool GetContestedSystem(out SolarSystem contested)
        {
            return Empire.Universe.SolarSystemDict.TryGetValue(contestedSystemGuid, out contested);
        }

        public float GetStrength()
        {
            return InitialStrength - Anger_FromShipsInOurBorders - Anger_TerritorialConflict - Anger_MilitaryConflict - Anger_DiplomaticConflict + Trust;
        }

        public void ImproveRelations(float trustEarned, float angerToReduce)
        {
            AddAngerDiplomaticConflict(-angerToReduce);
            TotalAnger               -= angerToReduce;
            Trust                    += trustEarned;
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
            Trust           = n;
            InitialStrength = 50f + n;
        }

        void UpdateIntelligence(Empire us, Empire them) // Todo - not sure what this does
        {
            // Moving towards adding intelligence. 
            // everything after the update is not used.
            // what should happen is that the information level is figured out.
            // then knowninformation is updated with the intelligence level. 
            KnownInformation.Update(IntelligenceLevel);
            if (us.Money < IntelligenceBudget || IntelligencePenetration > 100f)
                return;

            us.AddMoney(-IntelligenceBudget);
            int moleCount = 0;
            var theirPlanets = them.GetPlanets();
            foreach (Mole mole in us.data.MoleList)
            {
                foreach (Planet p in theirPlanets)
                {
                    if (p.guid != mole.PlanetGuid)
                        continue;
                    moleCount++;
                }
            }
            IntelligencePenetration += (IntelligenceBudget + IntelligenceBudget * (0.1f * moleCount + us.data.SpyModifier)) / 30f;
            if (IntelligencePenetration > 100f)
                IntelligencePenetration = 100f;
        }

        // updates basic relationship metrics
        // but doesn't create big side-effects
        public void UpdateRelationship(Empire us, Empire them)
        {
            if (us.data.Defeated)
                return;

            Risk.UpdateRiskAssessment(us);

            bool noAttackPlayer = GlobalStats.RestrictAIPlayerInteraction && them.isPlayer;
            if (!noAttackPlayer)
            {
                IsHostile = IsEmpireHostileToUs(us, them);
                bool canAttack = CanWeAttackThem(us, them);
                if (CanAttack != canAttack)
                {
                    CanAttack = canAttack;
                    if (canAttack) // make sure enemy can also attack us
                        them.GetRelations(us).CanAttack = true;
                }
            }
        }

        // This should be done only once per turn in Empire.UpdateRelationships
        public void AdvanceRelationshipTurn(Empire us, Empire them)
        {
            if (them.data.Defeated && AtWar)
            {
                CancelPrepareForWar();
                AtWar = false;
                ActiveWar.EndStarDate = Empire.Universe.StarDate;
                WarHistory.Add(ActiveWar);
                ActiveWar = null;
            }

            TurnsAtWar = AtWar ? TurnsAtWar + 1 : 0;
            Treaty_Trade_TurnsExisted = Treaty_Trade ? Treaty_Trade_TurnsExisted + 1 : 0;
            TurnsAllied               = Treaty_Alliance ? TurnsAllied + 1 : 0;
            TurnsKnown               += 1;
            turnsSinceLastContact    += 1;
            
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
                UpdateIntelligence(us, them);
                UpdateTrust(us, them, dt);
                UpdateAnger(us, them, dt);
                UpdateFear();

                InitialStrength       += dt.NaturalRelChange;
                TurnsKnown            += 1;
                turnsSinceLastContact += 1;
            }
        }
        
        void UpdatePlayerRelations(Empire us, Empire them)
        {
            UpdateIntelligence(us, them);
            if (Treaty_Peace && --PeaceTurnsRemaining <= 0)
            {
                us.EndPeaceWith(them);
                Empire.Universe.NotificationManager?.AddPeaceTreatyExpiredNotification(them);
            }
        }
        
        bool CanWeAttackThem(Empire us, Empire them)
        {
            if (!Known || AtWar)
                return true;

            if (Treaty_Peace || Treaty_NAPact || Treaty_Alliance)
                return false;

            if (us.isFaction || them.isFaction || them.WeAreRemnants)
                return true;

            if (!us.isPlayer)
            {
                float trustworthiness = them.data.DiplomaticPersonality?.Trustworthiness ?? 100;
                float peacefulness    = 1.0f - them.Research.Strategy.MilitaryRatio;
                if (TotalAnger < trustworthiness * peacefulness)
                    return false;
            }

            return true;
        }

        bool IsEmpireHostileToUs(Empire us, Empire them)
        {
            if (AtWar)
                return true;

            // if one of the parties is a Faction, there is hostility by default
            // unless we have Peace or NA Pacts (such as paying off Pirates)
            return (us.isFaction || them.isFaction)
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

            TotalAnger = (Anger_DiplomaticConflict
                         + Anger_FromShipsInOurBorders
                         + Anger_MilitaryConflict
                         + Anger_TerritorialConflict).UpperBound(100);
        }

        void UpdateFear()
        {
            foreach (FearEntry te in FearEntries)
            {
                te.TurnsInExistence += 1f;
                if (te.TurnsInExistence >= te.TurnTimer)
                    FearEntries.QueuePendingRemoval(te);
                else
                    FearUsed += te.FearCost;
            }

            FearEntries.ApplyPendingRemovals();
        }

        void UpdateTrust(Empire us, Empire them, DTrait personality)
        {
            TrustUsed = 0f;
            foreach (TrustEntry te in TrustEntries)
            {
                te.TurnsInExistence += 1;
                if (te.TurnsInExistence >= te.TurnTimer)
                    TrustEntries.QueuePendingRemoval(te);
                else
                    TrustUsed += te.TrustCost;
            }

            TrustEntries.ApplyPendingRemovals();
            float trustToAdd = 0;
            switch (Posture)
            {
                case Posture.Friendly:                      trustToAdd += personality.TrustGainedAtPeace;     break;
                case Posture.Neutral when !us.IsXenophobic: trustToAdd += personality.TrustGainedAtPeace / 2; break;
                case Posture.Hostile when !us.IsXenophobic: trustToAdd += personality.TrustGainedAtPeace / 5; break;
                case Posture.Hostile:                                                                         return;
            }

            float trustGain = GetTrustGain();
            if (Treaty_NAPact)      trustToAdd += trustGain;
            if (Treaty_OpenBorders) trustToAdd += trustGain;
            if (Treaty_Trade)       trustToAdd += trustGain;

            if (us.IsXenophobic
                && them.GetPlanets().Count >  us.GetPlanets().Count * 1.2f )
            {
                trustToAdd -= 0.1f;
            }

            Trust        += trustToAdd * TrustMultiplier();
            Trust        = Trust.Clamped(-50, Treaty_Alliance ? 150 : 100);
            TurnsAbove95 = Trust > 95 ? TurnsAbove95 + 1 : 0;

            float GetTrustGain()
            {
                float gain = 0.0125f;

                switch (us.Personality)
                {
                    case PersonalityType.Aggressive when them.IsAggressive:
                    case PersonalityType.Aggressive when them.IsXenophobic: gain *= 0.7f;  break;
                    case PersonalityType.Aggressive when them.IsRuthless:   gain *= 0.85f; break;
                    case PersonalityType.Ruthless   when them.IsAggressive: gain *= 0.75f; break;
                    case PersonalityType.Ruthless   when them.IsRuthless:   gain *= 0.7f;  break;
                    case PersonalityType.Ruthless   when them.IsXenophobic: gain *= 0.8f;  break;
                    case PersonalityType.Xenophobic when them.IsAggressive:
                    case PersonalityType.Xenophobic when them.IsRuthless:   gain *= 0.5f;  break;
                    case PersonalityType.Xenophobic when them.IsXenophobic: gain *= 0.2f;  break;
                    case PersonalityType.Honorable  when them.IsHonorable:  gain *= 2f;    break;
                    case PersonalityType.Honorable  when them.IsPacifist:   gain *= 1.1f;  break;
                    case PersonalityType.Honorable  when them.IsCunning:    gain *= 1.05f; break;
                    case PersonalityType.Pacifist   when them.IsHonorable:  gain *= 1.2f;  break;
                    case PersonalityType.Pacifist   when them.IsPacifist:   gain *= 2f;    break;
                    case PersonalityType.Pacifist   when them.IsCunning:    gain *= 1.1f;  break;
                    case PersonalityType.Cunning    when them.IsHonorable:
                    case PersonalityType.Cunning    when them.IsPacifist:   gain *= 1.1f;  break;
                    case PersonalityType.Cunning    when them.IsCunning:    gain *= 0.8f;  break;
                    case PersonalityType.Xenophobic:                        gain *= 0.6f;  break;
                    case PersonalityType.Aggressive:                        gain *= 0.75f; break;
                    case PersonalityType.Ruthless:                          gain *= 0.7f;  break;
                    case PersonalityType.Pacifist:                          gain *= 1.25f; break;
                    case PersonalityType.Honorable:                         gain *= 1.1f;  break;
                }
 
                if (them.isPlayer)
                {
                    gain /= ((int)CurrentGame.Difficulty).LowerBound(1);
                }

                return gain;
            }

            float TrustMultiplier() // Based on number of planet they stole from us
            {
                if (NumberStolenClaims == 0 || !them.isPlayer) // AI has their internal trust gain
                    return 1;

                return us.PersonalityModifiers.PlanetStoleTrustMultiplier / NumberStolenClaims;
            }
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

            float strShipsInBorders = us.GetEmpireAI().ThreatMatrix.StrengthOfAllEmpireShipsInBorders(us, them);
            if (strShipsInBorders > 0)
            {
                float ourStr = Treaty_NAPact ? us.CurrentMilitaryStrength * 25
                                             : us.CurrentMilitaryStrength * 50 ; // We are less concerned if we have NAP with them

                float borderAnger = (100f - Trust) / 100f * strShipsInBorders / ourStr.LowerBound(1);
                AddAngerShipsInOurBorders(borderAnger);
            }
        }

        // TODO: This is really funky, something is wrong with it
        bool AttemptAIFederationAbsorb(Empire aiEmpire)
        {
            if (FedQuest == null) 
                return false;

            Empire player = Empire.Universe.PlayerEmpire;
            Empire enemyEmpire = EmpireManager.GetEmpireByName(FedQuest.EnemyName);
            if (FedQuest.type == QuestType.DestroyEnemy && enemyEmpire.data.Defeated)
            {
                DiplomacyScreen.ShowEndOnly(aiEmpire, player, "Federation_YouDidIt_KilledEnemy", enemyEmpire);
                player.AbsorbEmpire(aiEmpire);
                FedQuest = null;
                return true;
            }

            if (FedQuest.type == QuestType.AllyFriend)
            {
                if (enemyEmpire.data.Defeated)
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
            float ourMilScore   = 10 + us.MilitaryScore; // The 2.3 is to reduce fluctuations for small numbers
            float theirMilScore = 10 + them.MilitaryScore;
            Threat = (theirMilScore - ourMilScore) / ourMilScore * 100; // This will give a threat of -100 to 100
        }

        public bool AttackForBorderViolation(DTrait personality, Empire targetEmpire, Empire attackingEmpire, bool isTrader)
        {
            if (Treaty_OpenBorders || Treaty_Peace) 
                return false;

            float borderAnger = Anger_FromShipsInOurBorders * (Anger_MilitaryConflict * 0.1f) + Anger_TerritorialConflict;

            if (isTrader)
            {
                if (Treaty_Trade)
                    return false;

                if (DoWeShareATradePartner(targetEmpire, attackingEmpire))
                    borderAnger *= 0.05f; // If the trader has str , this wont change anger
            }

            return borderAnger + 10 > (attackingEmpire.isPlayer ? attackingEmpire.data.BorderTolerance : personality.Territorialism);
        }

        public bool AttackForTransgressions(DTrait personality)
        {
            return !Treaty_NAPact && !Treaty_Peace && TotalAnger > (personality?.Territorialism
                ?? EmpireManager.Player.data.BorderTolerance);
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
            if (them == Player)
                DiplomacyScreen.Show(us, "Offer Trade", offer2, offer1);
            else
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);

            turnsSinceLastContact = 0;
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
            if (them == Empire.Universe.PlayerEmpire)
                DiplomacyScreen.Show(us, "Offer NAPact", offer2, offer1);
            else
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);

            turnsSinceLastContact = 0;
        }

        void OfferOpenBorders(Empire us)
        {
            float territorialism = us.data.DiplomaticPersonality.Territorialism;
            if (turnsSinceLastContact < SecondDemand
                || AtWar
                || Trust < 20f
                || !Treaty_NAPact
                || !Treaty_Trade
                || Treaty_OpenBorders
                || AvailableTrust < us.data.DiplomaticPersonality.Territorialism / 2f
                || Anger_TerritorialConflict + Anger_FromShipsInOurBorders < 0.75f * territorialism)
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
                them.GetEmpireAI().AnalyzeOffer(ourOffer, openBordersOffer, us, Offer.Attitude.Pleading);

            turnsSinceLastContact = 0;
        }

        void OfferAlliance(Empire us)
        {
            if (TurnsAbove95 < 100
                || turnsSinceLastContact < 100
                || Treaty_Alliance
                || !Treaty_Trade
                || !Treaty_NAPact
                || !Treaty_OpenBorders
                || Anger_DiplomaticConflict >= 20)
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
            if (them == Empire.Universe.PlayerEmpire)
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
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);
            }

            turnsSinceLastContact = 0;
        }

        void Federate(Empire us, Empire them)
        {
            if (them.isPlayer
                || TurnsAbove95 < TurnsAbove95Federation(us)
                || turnsSinceLastContact < 100
                || !Treaty_Alliance
                || TotalAnger > 0
                || Trust < 150
                || us.TotalScore * 1.5f < them.TotalScore)
            {
                return;
            }

            turnsSinceLastContact = 0; // Try again after 100 turns
            Relationship themToUs = us.GetRelations(them);
            if ((themToUs.Trust >= 150 || themToUs.Trust >= 100 && them.GetPlanets().Count < us.GetPlanets().Count / 5)
                && Is3RdPartyBiggerThenUs())
            {
                Empire.Universe.NotificationManager.AddPeacefulMergerNotification(us, them);
                us.AbsorbEmpire(them);
            }

            // Local Method
            bool Is3RdPartyBiggerThenUs()
            {
                float popRatioWar = us.PersonalityModifiers.FederationPopRatioWar;
                foreach (Empire e in EmpireManager.ActiveMajorEmpires)
                {
                    if (e == us || e == them)
                        continue;

                    float ratio = us.IsAtWarWith(e) ? popRatioWar : 1.6f;
                    if (e.TotalPopBillion / us.TotalPopBillion > ratio) // 3rd party is a potential risk
                        return true;
                }

                return false;
            }
        }

        void ReferToMilitary(Empire us, float threatForInsult, bool compliment = true)
        {
            Empire them = Them;
            float anger = us.IsAggressive ? 15 : 5;
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
            if (ActiveWar.TurnsAtWar == 0 || ActiveWar.TurnsAtWar % 100 > 0 && !requestNow)
                return;

            WarState warState    = ActiveWar.GetWarScoreState();
            Empire them          = Them;
            float warsGrade      = us.GetAverageWarGrade();
            float gradeThreshold = us.PersonalityModifiers.WarGradeThresholdForPeace;

            if (!us.IsLosingInWarWith(EmpireManager.Player))
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

            if (them == Empire.Universe.PlayerEmpire)
                DiplomacyScreen.Show(us, dialogue, ourOffer, offerPeace);
            else
                them.GetEmpireAI().AnalyzeOffer(ourOffer, offerPeace, us, Offer.Attitude.Respectful);
        }

        void DemandTech(Empire us)
        {
            if (TurnsKnown < FirstDemand
                || Treaty_NAPact
                || HaveRejectedDemandTech
                || XenoDemandedTech)
            {
                return;
            }

            Empire them = Them;
            if (!them.GetEmpireAI().TradableTechs(us, out Array<TechEntry> potentialDemands, true))
                return;

            TechEntry techToDemand = potentialDemands.RandItem();
            Offer demandTech       = new Offer();

            demandTech.TechnologiesOffered.AddUnique(techToDemand.UID);
            XenoDemandedTech  = true;
            Offer theirDemand = new Offer
            {
                AcceptDL      = "Xeno Demand Tech Accepted",
                RejectDL      = "Xeno Demand Tech Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejectedDemandTech,
                                               x => HaveRejectedDemandTech = x)
            };

            if (them == Player)
                DiplomacyScreen.Show(us, "Xeno Demand Tech", demandTech, theirDemand);
            else
                them.GetEmpireAI().AnalyzeOffer(theirDemand, demandTech, us, Offer.Attitude.Threaten);

            turnsSinceLastContact = 0;
        }

        void TradeTech(Empire us)
        {
            Empire them = Them;
            if (them == Player || ActiveWar != null || turnsSinceLastContact < TechTradeTurns || Posture == Posture.Hostile)
                return;

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
            them.GetEmpireAI().AnalyzeOffer(ourOffer, theirOffer, us, ourAttitude);
            turnsSinceLastContact = 0;
        }

        bool TechsToOffer(Empire us, Empire them, out Array<TechEntry> techs)
        {
            techs = new Array<TechEntry>();
            if (!us.GetEmpireAI().TradableTechs(them, out Array<TechEntry> ourTechs, !us.isPlayer && !them.isPlayer))
                return false;

            var theirDesigns = them.GetOurFactionShips();
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

            TechEntry ourTech = ourTechs.RandItem();
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
                if (tech.Tech.ActualCost + totalCost > theirMaxCost)
                    break;

                theirFinalTech.Add(tech.UID);
                totalCost += tech.Tech.ActualCost;
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
                    else if (GetContestedSystem(out SolarSystem contested))
                        DiplomacyScreen.Show(us, them, "Warning Colonized then Ships", contested);

                turnsSinceLastContact = 0;
                WarnedAboutShips = true;
            }
        }

        public void RequestHelpFromAllies(Empire us, Empire enemy, int contactThreshold)
        {
            if (ActiveWar == null) // They Accepted Peace
                return;

            var allies = new Array<Empire>();
            foreach ((Empire them, Relationship rel) in us.AllRelations)
            {
                if (rel.Treaty_Alliance
                    && them.IsKnown(enemy) && !them.IsAtWarWith(enemy) && !them.IsPeaceTreaty(enemy))
                {
                    allies.Add(them);
                }
            }

            foreach (Empire ally in allies)
            {
                Relationship usToAlly = us.GetRelations(ally);
                if (!ActiveWar.AlliesCalled.Contains(ally.data.Traits.Name)
                    && usToAlly.turnsSinceLastContact > (ally.isPlayer ? contactThreshold * 2 : contactThreshold))
                {
                    us.GetEmpireAI().CallAllyToWar(ally, enemy);
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

        bool TheyArePotentialTargetRuthless(Empire us, Empire them)
        {
            if (!Treaty_Peace || AtWar || PreparingForWar )
                return false;

            if (Threat > 0f || TurnsKnown < SecondDemand)
                return false;

            if (Threat < -75f && !Treaty_Alliance)
                return true;

            // Ruthless will break alliances if the other party does not have strong military but valuable colonies
            if (Threat < -75f && us.TotalColonyValues < them.TotalColonyValues)
                return true;

            return false;
        }

        bool TheyArePotentialTargetAggressive(Empire us, Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar)
                return false;

            if (Threat < -40f && TurnsKnown > SecondDemand && !Treaty_Alliance)
            {
                if (TotalAnger > 75f || us.MaxColonyValue < them.MaxColonyValue)
                    return true;
            }
            else if (Threat <= -75f && TotalAnger > 20f)
            {
                return true;
            }

            return false;
        }

        bool TheyArePotentialTargetXenophobic(Empire us, Empire them)
        {
            if (Treaty_Peace || AtWar || PreparingForWar || Posture == Posture.Friendly)
                return false;

            return them.GetPlanets().Count > us.GetPlanets().Count * 1.25f && TotalAnger > 20f;
        }

        // Pacifist, Cunning, Honorable
        public void DoConservative(Empire us, Empire them)
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

                    RequestHelpFromAllies(us, them, FirstDemand);
                    break;
                case Posture.Hostile:
                    AssessDiplomaticAnger(us);
                    ChangeToNeutralIfPossible(us);
                    break;
            }
        }

        public void DoRuthless(Empire us, Empire them, out bool theyArePotentialTargets)
        {
            switch (Posture)
            {
                case Posture.Friendly:
                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
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
                    ReferToMilitary(us, threatForInsult: -20, compliment: false);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestHelpFromAllies(us, them, FirstDemand);
                    break;
                case
                    Posture.Hostile:
                    ReferToMilitary(us, threatForInsult: -15, compliment: false);
                    AssessDiplomaticAnger(us);
                    ChangeToNeutralIfPossible(us);
                    break;
            }

            theyArePotentialTargets = TheyArePotentialTargetRuthless(us, them);
        }

        public void DoAggressive(Empire us, Empire them, out bool theyArePotentialTargets)
        {
            theyArePotentialTargets = false;
            AssessDiplomaticAnger(us);
            ReferToMilitary(us, threatForInsult: 0);
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
                    OfferNonAggression(us);
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us);
                    RequestHelpFromAllies(us, them, FirstDemand);
                    break;
                case Posture.Hostile:
                    theyArePotentialTargets = TheyArePotentialTargetAggressive(us, them);
                    break;
            }
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
                    break;
                case Posture.Neutral:
                    if (them.isPlayer)
                        DemandTech(us);

                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us);
                    break;
                case Posture.Hostile:
                    if (them.isPlayer)
                        DemandTech(us);
                    ChangeToNeutralIfPossible(us);
                    break;
            }

            theyArePotentialTargets = TheyArePotentialTargetXenophobic(us, them);
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
            float angerToAdd = ourShip.isColonyShip ? 10 : (killedExpSettings.KillExp / 5).LowerBound(1);
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
            var theirTrade = EmpireManager.GetTradePartners(them);
            var ourTrade = EmpireManager.GetTradePartners(them);
            foreach (var trade in theirTrade)
            {
                if (ourTrade.ContainsRef(trade))
                    return true;
            }

            return false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Relationship() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            Risk = null;
            TrustEntries?.Dispose(ref TrustEntries);
            FearEntries?.Dispose(ref FearEntries);
        }

        public void RestoreWarsFromSave()
        {
            ActiveWar?.RestoreFromSave(true);
            
            foreach (var war in WarHistory)
                war.RestoreFromSave(false);
        }

        public DebugTextBlock DebugWar(Empire us)
        {
            Color color = EmpireManager.GetEmpireByName(Name).EmpireColor;
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
            us.GetEmpireAI().DebugDrawTasks(ref debug, Them, warTasks: true);
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