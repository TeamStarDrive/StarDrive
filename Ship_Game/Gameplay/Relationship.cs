using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using System.Xml.Serialization;
using Ship_Game.AI.StrategyAI.WarGoals;
using Ship_Game.Debug;
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

        [JsonIgnore] private SolarSystem contestedSystem;

        [Serialize(43)] public bool AtWar;
        [Serialize(44)] public bool PreparingForWar;
        [Serialize(45)] public WarType PreparingForWarType = WarType.ImperialistWar;
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

        [XmlIgnore][JsonIgnore] public EmpireRiskAssessment Risk;
        [XmlIgnore][JsonIgnore] public Empire Them => EmpireManager.GetEmpireByName(Name);
        [XmlIgnore][JsonIgnore] public float AvailableTrust => Trust - TrustUsed;
        [XmlIgnore][JsonIgnore] Empire Player => Empire.Universe.PlayerEmpire;

        private readonly int FirstDemand   = 20;
        private readonly int SecondDemand  = 75;
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
        }

        public Relationship()
        {
        }


        public void SetTreaty(Empire us, TreatyType treatyType, bool value)
        {
            switch (treatyType)
            {
                case TreatyType.Alliance:      Treaty_Alliance    = value;                                break;
                case TreatyType.NonAggression: Treaty_NAPact      = value;                                break;
                case TreatyType.OpenBorders:   Treaty_OpenBorders = value;                                break;
                case TreatyType.Peace:         Treaty_Peace       = value; SetPeace();                    break;
                case TreatyType.Trade:         Treaty_Trade       = value; Treaty_Trade_TurnsExisted = 0; break;
            }

            void SetPeace()
            {
                if (value)
                {
                    PeaceTurnsRemaining = 100;
                    us.LaunchTroopsAfterPeaceSigned(Them);
                }
                else
                {
                    PeaceTurnsRemaining = 0;
                }
            }
        }

        public float TradeIncome() => (0.25f * Treaty_Trade_TurnsExisted - 3f).Clamped(-3f, 3f);

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

        public void StoleOurColonyClaim(Empire owner, Planet claimedPlanet)
        {
            NumberStolenClaims++;
            AddAngerTerritorialConflict(5f + (float)Math.Pow(5, NumberStolenClaims));
            StolenSystems.AddUnique(claimedPlanet.ParentSystem.guid);
        }

        public void WarnClaimThiefPlayer(Planet claimedPlanet, Empire victim)
        {
            bool newTheft = !StolenSystems.Contains(claimedPlanet.ParentSystem.guid);

            if (newTheft && !HaveWarnedTwice)
            {
                DiplomacyScreen.Stole1stColonyClaim(claimedPlanet, victim);
                return;
            }

            if (!HaveWarnedTwice)
            {
                DiplomacyScreen.Stole2ndColonyClaim(claimedPlanet, victim);
                HaveWarnedTwice = true;
                return;
            }

            if (newTheft || !HaveWarnedThrice)
                DiplomacyScreen.Stole3rdColonyClaim(claimedPlanet, victim);
            HaveWarnedThrice = true;
        }

        public void DamageRelationship(Empire us, Empire them, string why, float amount, Planet p)
        {
            if (us.data.DiplomaticPersonality == null)
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
                            {
                                contestedSystem = p.ParentSystem;
                                contestedSystemGuid = p.ParentSystem.guid;
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

        private void UpdateIntelligence(Empire us, Empire them) // Todo - not sure what this does
        {
            if (!(us.Money > IntelligenceBudget) || !(IntelligencePenetration < 100f))
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

        public void UpdatePlayerRelations(Empire us, Empire them)
        {
            UpdateIntelligence(us, them);
            if (Treaty_Peace && --PeaceTurnsRemaining <= 0)
            {
                us.EndPeaceWith(them);
                Empire.Universe.NotificationManager.AddPeaceTreatyExpiredNotification(them);
            }
        }
        
        public void UpdateRelationship(Empire us, Empire them)
        {
            if (us.data.Defeated)
                return;

            if (them.data.Defeated && AtWar)
            {
                AtWar                 = false;
                PreparingForWar       = false;
                ActiveWar.EndStarDate = Empire.Universe.StarDate;
                WarHistory.Add(ActiveWar);
                ActiveWar             = null;
            }

            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == them)
                return;

            TurnsAtWar = AtWar ? TurnsAtWar + 1 : 0;
            Risk.UpdateRiskAssessment(us);

            if (us.isFaction)
                return;

            Treaty_Trade_TurnsExisted = Treaty_Trade ? Treaty_Trade_TurnsExisted + 1 : 0;
            TurnsAllied               = Treaty_Alliance ? TurnsAllied + 1 : 0;
            TurnsKnown               += 1;
            turnsSinceLastContact    += 1;

            if (us.isPlayer)
            {
                UpdatePlayerRelations(us, them);
                return;
            }

            if (AtWar && ActiveWar != null)
                ActiveWar.TurnsAtWar += 1f;

            UpdateFederationQuest(us, out bool questCompleted);
            if (questCompleted)
                return; // Empire was absorbed

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

        void UpdateAnger(Empire us, Empire them, DTrait personality)
        {
            UpdateAngerBorders(us, them);
            UpdatePeace(us, them);
            AddAngerTerritorialConflict(-personality.AngerDissipation);
            AddAngerShipsInOurBorders(-personality.AngerDissipation);
            AddAngerDiplomaticConflict(-personality.AngerDissipation);
            AddAngerMilitaryConflict(-personality.AngerDissipation);

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
                if (te.TurnTimer != 0 && !(te.TurnsInExistence <= 250f))
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
                if (te.TurnTimer != 0 && te.TurnsInExistence > 250)
                    TrustEntries.QueuePendingRemoval(te);
                else
                    TrustUsed += te.TrustCost;
            }

            TrustEntries.ApplyPendingRemovals();

            switch (Posture)
            {
                case Posture.Friendly:
                    Trust = (Trust + personality.TrustGainedAtPeace).UpperBound(Treaty_Alliance ? 150 : 100);
                    break;
                case Posture.Hostile:
                    Trust -= personality.TrustGainedAtPeace;
                    break;
            }

            if (Treaty_NAPact) Trust      += 0.0125f;
            if (Treaty_OpenBorders) Trust += 0.025f;
            if (Treaty_Trade) Trust       += 0.025f;

            if (us.Personality == PersonalityType.Xenophobic
                && them.GetPlanets().Count >  us.GetPlanets().Count * 1.2f )
            {
                Trust -= 0.1f;
            }

            TurnsAbove95 = Trust > 95 ? TurnsAbove95 + 1 : 0;
            Trust        = Trust.Clamped(0, Treaty_Alliance ? 150 : 100);
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
                int multiplier = Treaty_NAPact ? 1 : 2; // We are less concerned if we have NAP with them
                float borderAnger = (100f - Trust) / 100f * strShipsInBorders / (us.MilitaryScore * multiplier);
                AddAngerShipsInOurBorders(borderAnger);
            }
        }

        void UpdateFederationQuest(Empire us, out bool success)
        {
            success = false;
            if (FedQuest == null) 
                return;

            Empire enemyEmpire = EmpireManager.GetEmpireByName(FedQuest.EnemyName);
            if (FedQuest.type == QuestType.DestroyEnemy && enemyEmpire.data.Defeated)
            {
                DiplomacyScreen.ShowEndOnly(us, Empire.Universe.PlayerEmpire, "Federation_YouDidIt_KilledEnemy", enemyEmpire);
                Empire.Universe.PlayerEmpire.AbsorbEmpire(us);
                FedQuest = null;
                success  = true;
                return;
            }

            if (FedQuest.type == QuestType.AllyFriend)
            {
                if (enemyEmpire.data.Defeated)
                {
                    FedQuest = null;
                }
                else if (Empire.Universe.PlayerEmpire.GetRelations(enemyEmpire).Treaty_Alliance)
                {
                    DiplomacyScreen.ShowEndOnly(us, Empire.Universe.PlayerEmpire, "Federation_YouDidIt_AllyFriend",
                        EmpireManager.GetEmpireByName(FedQuest.EnemyName));
                    Empire.Universe.PlayerEmpire.AbsorbEmpire(us);
                    FedQuest = null;
                    success  = true;
                }
            }
        }

        void UpdateThreat(Empire us, Empire them)
        {
            float ourMilScore   = 2300f + us.MilitaryScore; // The 2300 is to reduce fluctuations for small numbers
            float theirMilScore = 2300f + them.MilitaryScore;
            Threat              = (theirMilScore - ourMilScore) / ourMilScore * 100; // This will give a threat of -100 to 100

        }

        public bool AttackForBorderViolation(DTrait personality, Empire targetEmpire, Empire attackingEmpire, bool isTrader = true)
        {
            if (Treaty_OpenBorders) 
                return false;

            float borderAnger = Anger_FromShipsInOurBorders * (Anger_MilitaryConflict * 0.1f) + Anger_TerritorialConflict;
            if (isTrader)
            {
                if (Treaty_Trade)
                    return false;
                if (DoWeShareATradePartner(targetEmpire, attackingEmpire)) 
                    borderAnger *= 0.1f;
            }

            return borderAnger + 10 > (personality?.Territorialism  ?? EmpireManager.Player.data.BorderTolerance);
        }

        public bool AttackForTransgressions(DTrait personality)
        {
            return !Treaty_NAPact && TotalAnger  > (personality?.Territorialism
                ?? EmpireManager.Player.data.BorderTolerance);
        }

        void OfferTrade(Empire us)
        {
            if (TurnsKnown < FirstDemand
                || AtWar
                || Treaty_Trade
                || HaveRejected_TRADE
                || AvailableTrust <= us.data.DiplomaticPersonality.Trade
                || turnsSinceLastContact < SecondDemand)
            {
                return;
            }

            Offer offer1 = new Offer
            {
                TradeTreaty   = true,
                AcceptDL      = "Trade Accepted",
                RejectDL      = "Trade Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejected_TRADE,
                                               x => HaveRejected_TRADE = x)
            };

            Empire them  = Them;
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
                || HaveRejectedNaPact
                || turnsSinceLastContact < SecondDemand)
            {
                return;
            }

            Offer offer1 = new Offer
            {
                NAPact        = true,
                AcceptDL      = "NAPact Accepted",
                RejectDL      = "NAPact Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejectedNaPact,
                                               x => HaveRejectedNaPact = x)
            };

            Empire them  = Them;
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
                || HaveRejected_OpenBorders
                || !Treaty_NAPact
                || !Treaty_Trade
                || Treaty_OpenBorders
                || AvailableTrust < us.data.DiplomaticPersonality.Territorialism / 2f
                || Anger_TerritorialConflict + Anger_FromShipsInOurBorders < 0.75f * territorialism)
            {
                return;
            }

            bool friendlyOpen      = Trust > 50f;
            Offer openBordersOffer = new Offer
            {
                OpenBorders   = true,
                AcceptDL      = "Open Borders Accepted",
                RejectDL      = friendlyOpen ? "Open Borders Friends Rejected" : "Open Borders Rejected",
                ValueToModify = new Ref<bool>(() => HaveRejected_OpenBorders,
                                               x => HaveRejected_OpenBorders = x)
            };

            Empire them    = Them;
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
                || turnsSinceLastContact < FirstDemand
                || Treaty_Alliance
                || !Treaty_Trade
                || !Treaty_NAPact
                || HaveRejected_Alliance
                || TotalAnger >= 20)
            {
                return;
            }

            Empire them  = Them;
            Offer offer1 = new Offer
            {
                Alliance      = true,
                AcceptDL      = "ALLIANCE_ACCEPTED",
                RejectDL      = "ALLIANCE_REJECTED",
                ValueToModify = new Ref<bool>(() => HaveRejected_Alliance,
                    x =>
                    {
                        HaveRejected_Alliance = x;
                        SetAlliance(!HaveRejected_Alliance, us, them);
                    })
            };

            Offer offer2 = new Offer();
            if (them == Empire.Universe.PlayerEmpire)
                DiplomacyScreen.Show(us, "OFFER_ALLIANCE", offer2, offer1);
            else
                them.GetEmpireAI().AnalyzeOffer(offer2, offer1, us, Offer.Attitude.Respectful);

            turnsSinceLastContact = 0;
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

        void RequestPeace(Empire us, bool onlyBadly = false)
        {
            if ((ActiveWar.TurnsAtWar % 100).NotZero())
                return;

            Empire them = Them;
            WarState warState;
            switch (ActiveWar.WarType)
            {
                case WarType.BorderConflict:
                    if (Anger_FromShipsInOurBorders + Anger_TerritorialConflict > us.data.DiplomaticPersonality.Territorialism)
                        return;

                    warState = ActiveWar.GetBorderConflictState();
                    if (CheckLosingBadly("OFFERPEACE_LOSINGBC"))
                        break;

                    switch (warState)
                    {
                        case WarState.LosingSlightly:
                        case WarState.LosingBadly:     OfferPeace(us, them, "OFFERPEACE_LOSINGBC"); break;
                        case WarState.WinningSlightly: OfferPeace(us, them, "OFFERPEACE_FAIR"); break;
                        case WarState.Dominating:      OfferPeace(us, them, "OFFERPEACE_WINNINGBC"); break;
                    }

                    break;
                case WarType.ImperialistWar:
                    warState = ActiveWar.GetWarScoreState();
                    if (CheckLosingBadly("OFFERPEACE_PLEADING"))
                        break;

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
                    warState = ActiveWar.GetBorderConflictState();
                    if (CheckLosingBadly("OFFERPEACE_PLEADING"))
                        break;

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

            // Aggressive / Xenophobic / Ruthless will request peace only if losing badly
            bool CheckLosingBadly(string whichPeace)
            {
                if (!onlyBadly)
                    return false;

                if (warState == WarState.LosingBadly)
                    OfferPeace(us, them, whichPeace);

                return true;
            }
        }

        void OfferPeace(Empire us, Empire them, string whichPeace)
        {
            var offerPeace = new Offer
            {
                PeaceTreaty   = true,
                AcceptDL      = "OFFERPEACE_ACCEPTED",
                RejectDL      = "OFFERPEACE_REJECTED",
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
            if (!them.GetEmpireAI().TradableTechs(us, out Array<TechEntry> potentialDemands))
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

            turnsSinceLastContact = 0;

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
        }

        bool TechsToOffer(Empire us, Empire them, out Array<TechEntry> techs)
        {
            techs = new Array<TechEntry>();
            if (!us.GetEmpireAI().TradableTechs(them, out Array<TechEntry> ourTechs))
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
            float ourTechCost = ourTech.Tech.ActualCost;
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

            foreach (TechEntry tech in theirTechs.Sorted(t => t.Tech.ActualCost))
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

            if (!WarnedAboutShips || AtWar || !us.IsEmpireAttackable(them))
                return;

            int territorialism = us.data.DiplomaticPersonality.Territorialism;
            if (them.CurrentMilitaryStrength < us.CurrentMilitaryStrength * (1f - territorialism * 0.01f) && !Treaty_Peace)
                us.GetEmpireAI().DeclareWarOn(them, WarType.ImperialistWar);
        }

        void AssessDiplomaticAnger(Empire us)
        {
            if (!Known)
                return;

            Empire them = Them;
            if (us.Personality == PersonalityType.Aggressive && Threat < -15f)
            {
                float angerMod = -Threat / 15;// every -15 threat will give +0.1 anger
                AddAngerDiplomaticConflict(us.data.DiplomaticPersonality.AngerDissipation + 0.1f * angerMod);
            }

            if (Anger_MilitaryConflict >= 5 && !AtWar && !Treaty_Peace)
            {
                us.GetEmpireAI().DeclareWarOn(them, WarType.DefensiveWar);
                return;
            }

            if (Anger_TerritorialConflict + Anger_FromShipsInOurBorders >= us.data.DiplomaticPersonality.Territorialism
                && !AtWar && !Treaty_OpenBorders && !Treaty_Peace)
            {
                PreparingForWar     = true;
                PreparingForWarType = WarType.BorderConflict;
                return;
            }

            WarnAboutShips(us);
        }

        bool TheyArePotentialTargetRuthless(Empire us, Empire them)
        {
            if (Treaty_Peace || ActiveWar != null && ActiveWar.WarType != WarType.DefensiveWar)
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
            if (Treaty_Peace || ActiveWar != null && ActiveWar.WarType != WarType.DefensiveWar)
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
            if (Treaty_Peace || ActiveWar != null && ActiveWar.WarType != WarType.DefensiveWar || Posture == Posture.Friendly)
                return false;

            return them.GetPlanets().Count > us.GetPlanets().Count * 1.25f && TotalAnger > 20f;
        }

        // Pacifist, Cunning, Honorable
        public void DoConservative(Empire us, Empire them)
        {
            switch (Posture)
            {
                case Posture.Friendly:
                    OfferTrade(us);
                    TradeTech(us);
                    OfferNonAggression(us);
                    OfferOpenBorders(us);
                    OfferAlliance(us);
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    AssessDiplomaticAnger(us);
                    OfferTrade(us);
                    TradeTech(us);
                    OfferNonAggression(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us);
                    us.GetEmpireAI().RequestHelpFromAllies(this, them, FirstDemand);
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
                    OfferTrade(us);
                    OfferNonAggression(us);
                    TradeTech(us);
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    AssessDiplomaticAnger(us);
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    ReferToMilitary(us, threatForInsult: -20, compliment: false);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    us.GetEmpireAI().RequestHelpFromAllies(this, them, FirstDemand);
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
                    OfferTrade(us);
                    TradeTech(us);
                    OfferNonAggression(us);
                    OfferOpenBorders(us);
                    OfferAlliance(us);
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    OfferTrade(us);
                    TradeTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us, onlyBadly: true);
                    us.GetEmpireAI().RequestHelpFromAllies(this, them, FirstDemand);
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
                    ChangeToNeutralIfPossible(us);
                    break;
                case Posture.Neutral:
                    DemandTech(us);
                    ChangeToFriendlyIfPossible(us);
                    ChangeToHostileIfPossible(us);
                    break;
                case Posture.Hostile when ActiveWar != null:
                    RequestPeace(us, onlyBadly: true);
                    break;
                case Posture.Hostile:
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
                    if (!Treaty_NAPact || Trust.AlmostEqual(0) && TotalAnger > 50)
                        break;

                    return;
                case PersonalityType.Aggressive:
                    if (Threat < -45 || TotalAnger > 50 || Treaty_NAPact & TotalAnger > 75)
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

            AddAngerMilitaryConflict(killedExpSettings.KillExp);
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

        public DebugTextBlock DebugWar()
        {
            var debug = new DebugTextBlock
            {
                Header      = $"Relation To: {Name}",
                HeaderColor = EmpireManager.GetEmpireByName(Name).EmpireColor
            };

            debug.AddLine($"Total Anger: {(int)TotalAnger}");
            debug.AddLine($" Border: {(int)Anger_FromShipsInOurBorders}");
            debug.AddLine($" Military: {(int)Anger_MilitaryConflict}");
            debug.AddLine($" Territory: {(int)Anger_TerritorialConflict}");
            debug.AddLine($" Diplomatic: {(int)Anger_DiplomaticConflict}");
            debug.AddLine($" Trust: {(int)Trust} TrustUsed: {(int)TrustUsed}");

            ActiveWar?.WarDebugData(ref debug);
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

        void SetPosture(Posture posture)
        {
            Posture = posture;
        }

        public void ChangeToFriendly()
        {
            SetPosture(Posture.Friendly);
        }

        public void ChangeToNeutral()
        {
            SetPosture(Posture.Neutral);
        }

        public void ChangeToHostile()
        {
            SetPosture(Posture.Hostile);
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