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
        [Serialize(1)] public Posture Posture = Posture.Neutral; // FB - use SetPosture privately or ChangeTo methods publicly
        [Serialize(2)] public string Name;
        [Serialize(3)] public bool Known;
        [Serialize(4)] public float IntelligenceBudget;
        [Serialize(5)] public float IntelligencePenetration;
        [Serialize(6)] public int turnsSinceLastContact;
        [Serialize(7)] public bool WarnedAboutShips;
        [Serialize(8)] public bool WarnedAboutColonizing;
        [Serialize(9)] public int PlayerContactStep; //  Encounter Step to use when the player contacts this faction

        [Serialize(10)] public float Anger_FromShipsInOurBorders;
        [Serialize(11)] public float Anger_TerritorialConflict;
        [Serialize(12)] public float Anger_MilitaryConflict;
        [Serialize(13)] public float Anger_DiplomaticConflict;

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
        [XmlIgnore] [JsonIgnore] public EmpireRiskAssessment Risk;
        [XmlIgnore][JsonIgnore]
        public Empire Them => EmpireManager.GetEmpireByName(Name);

        /// <summary>
        /// Tech transfer restriction.
        /// currently this is disabling tech content trade via diplomacy.
        /// A check here can be added to remove this for allies.
        /// </summary>
        [XmlIgnore][JsonIgnore]
        readonly Array<TechUnlockType> PreventContentExchangeOf =
                                         new Array<TechUnlockType>
                                         {
                                             TechUnlockType.Diplomacy
                                         };

        public bool AllowRacialTrade() => !PreventContentExchangeOf.Contains(TechUnlockType.Diplomacy);
        public bool HaveRejectedDemandTech
        {
            get => haveRejectedDemandTech;
            set
            {
                if (!(haveRejectedDemandTech = value))
                    return;
                Trust -= 20f;
                TotalAnger += 20f;
                Anger_DiplomaticConflict += 20f;
            }
        }

        public bool HaveRejectedNapact
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


        public void SetTreaty(TreatyType treatyType, bool value)
        {
            switch (treatyType)
            {
                case TreatyType.Alliance:      Treaty_Alliance    = value;                                        break;
                case TreatyType.NonAggression: Treaty_NAPact      = value;                                        break;
                case TreatyType.OpenBorders:   Treaty_OpenBorders = value;                                        break;
                case TreatyType.Peace:         Treaty_Peace       = value; PeaceTurnsRemaining = value ? 100 : 0; break;
                case TreatyType.Trade:         Treaty_Trade       = value; Treaty_Trade_TurnsExisted = 0;         break;
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

        public bool WarnedSystemListContains(Planet claimedPlanet) => WarnedSystemsList.Any(guid => guid == claimedPlanet.ParentSystem.guid);

        public void StoleOurColonyClaim(Empire owner, Planet claimedPlanet)
        {
            NumberStolenClaims++;
            Anger_TerritorialConflict += 5f + (float) Math.Pow(5, NumberStolenClaims);
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

            amount *= us.DifficultyModifiers.Anger;

            if (us.IsHonorable || us.IsXenophobic)
                amount *= 2;

            if (why != null)
            {
                if (why == "Caught Spying")
                {
                    SpiesDetected            += 1;
                    Anger_DiplomaticConflict += amount;
                    TotalAnger               += amount;
                    Trust                    -= amount;

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
                    else if (SpiesDetected == 1 && !AtWar && Empire.Universe.PlayerEmpire == them && !us.isFaction)
                    {
                        if (SpiesDetected == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_1");
                        }
                        else if (SpiesDetected == 2)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_2");
                        }
                        else if (SpiesDetected >= 3)
                        {
                            if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                                DiplomacyScreen.ShowEndOnly(us, them, "Caught_Spying_3");

                            us.BreakAllTreatiesWith(them);
                            Posture = Posture.Hostile;
                        }
                    }
                }
                else if (why == "Caught Spying Failed")
                {
                    Anger_DiplomaticConflict += amount;
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
                    Anger_DiplomaticConflict += amount;
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
                    Anger_TerritorialConflict += amount + expansion;
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
                        Anger_MilitaryConflict += amount;
                        Trust -= amount;
                        if (Empire.Universe.PlayerEmpire == them && !us.isFaction)
                        {
                            if (Anger_MilitaryConflict < 2f)
                                DiplomacyScreen.Show(us, them, "Aggression Warning");

                            Trust -= amount;
                        }
                    }

                    Anger_MilitaryConflict += amount;
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

        public void ImproveRelations(float trustEarned, float diploAngerMinus)
        {
            Anger_DiplomaticConflict -= diploAngerMinus;
            TotalAnger               -= diploAngerMinus;
            Trust                    += trustEarned;
            if (Trust > 100f && !Treaty_Alliance)
            {
                Trust = 100f;
                return;
            }
            if (Trust > 150f && Treaty_Alliance)
                Trust = 150f;
        }

        public void SetImperialistWar()
        {
            if (ActiveWar != null)
            {
                ActiveWar.WarType = WarType.ImperialistWar;
            }
        }

        public void SetInitialStrength(float n)
        {
            Trust = n;
            InitialStrength = 50f + n;
        }

        private void UpdateIntelligence(Empire us, Empire them)
        {
            if (!(us.Money > IntelligenceBudget) || !(IntelligencePenetration < 100f))
                return;

            us.AddMoney(-IntelligenceBudget);
            int molecount = 0;
            var theirPlanets = them.GetPlanets();
            foreach (Mole mole in us.data.MoleList)
            {
                foreach (Planet p in theirPlanets)
                {
                    if (p.guid != mole.PlanetGuid)
                        continue;
                    molecount++;
                }
            }
            IntelligencePenetration += (IntelligenceBudget + IntelligenceBudget * (0.1f * molecount + us.data.SpyModifier)) / 30f;
            if (IntelligencePenetration > 100f)
                IntelligencePenetration = 100f;
        }

        public void UpdatePlayerRelations(Empire us, Empire them)
        {
            UpdateIntelligence(us, them);
            if (Treaty_Trade)
                Treaty_Trade_TurnsExisted++;
            
            if (Treaty_Peace && --PeaceTurnsRemaining <= 0)
            {
                us.EndPeachWith(them);
                Empire.Universe.NotificationManager.AddPeaceTreatyExpiredNotification(them);
            }
        }
        
        public void UpdateRelationship(Empire us, Empire them)
        {
            if (us.data.Defeated)
            {
                return;
            }

            if (them.data.Defeated)
            {
                if (AtWar)
                {
                    AtWar                 = false;
                    PreparingForWar       = false;
                    ActiveWar.EndStarDate = Empire.Universe.StarDate;
                    WarHistory.Add(ActiveWar);
                    ActiveWar             = null;
                }
            }

            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == them)
                return;

            TurnsAtWar = AtWar ? TurnsAtWar + 1 : 0;
            Risk.UpdateRiskAssessment(us);

            if (us.isFaction)
                return;

            if (us.isPlayer)
            {
                UpdatePlayerRelations(us, them);
                return;
            }

            if (FedQuest != null)
            {
                Empire enemyEmpire = EmpireManager.GetEmpireByName(FedQuest.EnemyName);
                if (FedQuest.type == QuestType.DestroyEnemy && enemyEmpire.data.Defeated)
                {
                    DiplomacyScreen.ShowEndOnly(us, Empire.Universe.PlayerEmpire, "Federation_YouDidIt_KilledEnemy", enemyEmpire);
                    Empire.Universe.PlayerEmpire.AbsorbEmpire(us);
                    FedQuest = null;
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
                        return;
                    }
                }
            }

            if (Posture == Posture.Hostile && Trust > 50f && TotalAnger < 10f)
                Posture = Posture.Neutral;
            if (them.isFaction)
                AtWar = false;

            UpdateIntelligence(us, them);
            if (AtWar && ActiveWar != null) 
            {
                ActiveWar.TurnsAtWar += 1f;
            }

            foreach (TrustEntry te in TrustEntries)
            {
                te.TurnsInExistence += 1;
                if (te.TurnTimer != 0 && te.TurnsInExistence > 250)
                    TrustEntries.QueuePendingRemoval(te);
            }
            TrustEntries.ApplyPendingRemovals();

            foreach (FearEntry te in FearEntries)
            {
                te.TurnsInExistence += 1f;
                if (te.TurnTimer != 0 && !(te.TurnsInExistence <= 250f))
                    FearEntries.QueuePendingRemoval(te);
            }
            FearEntries.ApplyPendingRemovals();

            TurnsAllied = Treaty_Alliance ? TurnsAllied + 1 : 0;

            DTrait dt = us.data.DiplomaticPersonality;
            if (Posture == Posture.Friendly)
            {
                Trust += dt.TrustGainedAtPeace;
                bool allied = us.GetRelations(them).Treaty_Alliance;
                if      (Trust > 100f && !allied) Trust = 100f;
                else if (Trust > 150f &&  allied) Trust = 150f;
            }
            else if (Posture == Posture.Hostile)
            {
                Trust -= dt.TrustGainedAtPeace;
            }

            if (Treaty_NAPact)      Trust += 0.0125f;
            if (Treaty_OpenBorders) Trust += 0.0125f;
            if (Treaty_Trade)
            {
                Trust += 0.0125f;
                Treaty_Trade_TurnsExisted += 1;
            }

            if (Treaty_Peace)
            {
                if (--PeaceTurnsRemaining <= 0)
                {
                    Treaty_Peace = false;
                    us.GetRelations(them).Treaty_Peace = false;
                }
                Anger_DiplomaticConflict    -= 0.1f;
                Anger_FromShipsInOurBorders -= 0.1f;
                Anger_MilitaryConflict      -= 0.1f;
                Anger_TerritorialConflict   -= 0.1f;
            }

            TurnsAbove95 += (Trust <= 95f) ? 0 : 1;
            TrustUsed = 0f;

            foreach (TrustEntry te in TrustEntries) TrustUsed += te.TrustCost;
            foreach (FearEntry  te in FearEntries)  FearUsed  += te.FearCost;

            if (!Treaty_Alliance && !Treaty_OpenBorders)
            {
                float strShipsInBorders = us.GetEmpireAI().ThreatMatrix.StrengthOfAllEmpireShipsInBorders(them);
                if (strShipsInBorders > 0)
                {
                    if (!Treaty_NAPact)
                        Anger_FromShipsInOurBorders += (100f - Trust) / 100f * strShipsInBorders / (us.MilitaryScore);
                    else
                        Anger_FromShipsInOurBorders += (100f - Trust) / 100f * strShipsInBorders / (us.MilitaryScore * 2f);
                }
            }


            float ourMilScore   = 2300f + us.MilitaryScore;
            float theirMilScore = 2300f + them.MilitaryScore;
            Threat = (theirMilScore - ourMilScore) / ourMilScore * 100;
            if (Threat > 100f) Threat = 100f;
            if (us.MilitaryScore < 1000f) Threat = 0f;

            if (Trust > 100f && !us.GetRelations(them).Treaty_Alliance)
                Trust = 100f;
            else if (Trust > 150f && us.GetRelations(them).Treaty_Alliance)
                Trust = 150f;

            InitialStrength += dt.NaturalRelChange;
            if (Anger_TerritorialConflict > 0f) Anger_TerritorialConflict -= dt.AngerDissipation;
            if (Anger_TerritorialConflict < 0f) Anger_TerritorialConflict = 0f;

            if (Anger_FromShipsInOurBorders > 100f) Anger_FromShipsInOurBorders = 100f;
            if (Anger_FromShipsInOurBorders > 0f)   Anger_FromShipsInOurBorders -= dt.AngerDissipation;
            if (Anger_FromShipsInOurBorders < 0f)   Anger_FromShipsInOurBorders = 0f;

            if (Anger_MilitaryConflict > 0f) Anger_MilitaryConflict -= dt.AngerDissipation;
            if (Anger_MilitaryConflict < 0f) Anger_MilitaryConflict = 0f;
            if (Anger_DiplomaticConflict > 0f) Anger_DiplomaticConflict -= dt.AngerDissipation;
            if (Anger_DiplomaticConflict < 0f) Anger_DiplomaticConflict = 0f;

            TotalAnger = Anger_DiplomaticConflict + Anger_FromShipsInOurBorders + Anger_MilitaryConflict + Anger_TerritorialConflict;
            TotalAnger = TotalAnger > 100 ? 100 : TotalAnger;
            TurnsKnown += 1;
            turnsSinceLastContact += 1;
        }

        public bool AttackForBorderViolation(DTrait personality, Empire targetEmpire, Empire attackingEmpire, bool isTrader = true)
        {
            if (Treaty_OpenBorders) return false;
             float borderAnger = Anger_FromShipsInOurBorders * (Anger_MilitaryConflict * .1f) + Anger_TerritorialConflict;
            if (isTrader)
            {
                if (Treaty_Trade) borderAnger *= .2f;
                else if (DoWeShareATradePartner(targetEmpire, attackingEmpire)) borderAnger *= .5f;
            }

            return borderAnger + 10 > (personality?.Territorialism  ?? EmpireManager.Player.data.BorderTolerance);
        }

        public bool AttackForTransgressions(DTrait personality)
        {
            return !Treaty_NAPact && TotalAnger  > (personality?.Territorialism
                ?? EmpireManager.Player.data.BorderTolerance);
        }

        public void LostAShip(Ship ourShip)
        {
            ShipRole.Race killedExpSettings = ShipRole.GetExpSettings(ourShip);

            Anger_MilitaryConflict += killedExpSettings.KillExp;
            ActiveWar?.ShipWeLost(ourShip);

        }
        public void KilledAShip(Ship theirShip) => ActiveWar?.ShipWeKilled(theirShip);

        public void LostAColony(Planet colony, Empire attacker)
        {
            ActiveWar?.PlanetWeLost(attacker, colony);
            Anger_MilitaryConflict += colony.ColonyValue;
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

        public void ResetRelation() // todo move the empire_relationship
        {
            Treaty_Alliance    = false;
            Treaty_NAPact      = false;
            Treaty_OpenBorders = false;
            Treaty_Peace       = false;
            Treaty_Trade       = false;
        }

        public DebugTextBlock DebugWar()
        {
            var debug = new DebugTextBlock
            {
                Header      = $"RelationShip Status: {Name}",
                HeaderColor = EmpireManager.GetEmpireByName(Name).EmpireColor
            };

            debug.AddLine($"Total Anger: {(int)TotalAnger}");
            debug.AddLine($"Anger From Ships in Borders: {(int)Anger_FromShipsInOurBorders}");
            debug.AddLine($"Anger From Military: {(int)Anger_MilitaryConflict}");
            debug.AddLine($"Anger From Territory Violation: {(int)Anger_TerritorialConflict}");
            debug.AddLine($"Anger From Diplomatic Faux pas: {(int)Anger_DiplomaticConflict}");
            debug.AddLine($"Trust: {(int)Trust} TrustUsed: {(int)TrustUsed}");

            ActiveWar?.WarDebugData(ref debug);
            return debug;
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