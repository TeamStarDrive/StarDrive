using Newtonsoft.Json;
using Ship_Game.AI;
using Ship_Game.Ships;
using System;
using Ship_Game.Debug;

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
        [Serialize(1)] public Posture Posture = Posture.Neutral;
        [Serialize(2)] public string Name;
        [Serialize(3)] public bool Known;
        [Serialize(4)] public float IntelligenceBudget;
        [Serialize(5)] public float IntelligencePenetration;
        [Serialize(6)] public int turnsSinceLastContact;
        [Serialize(7)] public bool WarnedAboutShips;
        [Serialize(8)] public bool WarnedAboutColonizing;
        [Serialize(9)] public int EncounterStep;

        [Serialize(10)] public float Anger_FromShipsInOurBorders;
        [Serialize(11)] public float Anger_TerritorialConflict;
        [Serialize(12)] public float Anger_MilitaryConflict;
        [Serialize(13)] public float Anger_DiplomaticConflict;

        [Serialize(14)] public int SpiesDetected;
        [Serialize(15)] public int TimesSpiedOnAlly;
        [Serialize(16)] public int SpiesKilled;
        [Serialize(17)] public float TotalAnger;
        [Serialize(18)] public bool Treaty_OpenBorders;
        [Serialize(19)] public bool Treaty_NAPact;
        [Serialize(20)] public bool Treaty_Trade;
        [Serialize(21)] public int Treaty_Trade_TurnsExisted;
        [Serialize(22)] public bool Treaty_Alliance;
        [Serialize(23)] public bool Treaty_Peace;

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
        [Serialize(52)] public int TurnsAbove95;
        [Serialize(53)] public int TurnsAllied;

        [Serialize(54)] public BatchRemovalCollection<TrustEntry> TrustEntries = new BatchRemovalCollection<TrustEntry>();
        [Serialize(55)] public BatchRemovalCollection<FearEntry> FearEntries = new BatchRemovalCollection<FearEntry>();
        [Serialize(56)] public float TrustUsed;
        [Serialize(57)] public float FearUsed;
        [Serialize(58)] public float TheyOweUs;
        [Serialize(59)] public float WeOweThem;
        [JsonIgnore] public EmpireRiskAssessment Risk;

        public bool HaveRejectedDemandTech
        {
            get { return haveRejectedDemandTech; }
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

        public float TradeIncome() => (0.25f * Treaty_Trade_TurnsExisted - 3f).Clamped(-3f, 3f);

        public bool WarnedSystemListContains(Planet claimedPlanet) => WarnedSystemsList.Any(guid => guid == claimedPlanet.ParentSystem.guid);

        public void StoleOurColonyClaim(Empire onwer, Planet claimedPlanet)
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

        public void DamageRelationship(Empire Us, Empire Them, string why, float Amount, Planet p)
        {
            if (Us.data.DiplomaticPersonality == null)
            {
                return;
            }


            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == Them)
                return;
            float angerMod = 1 + ((int)CurrentGame.Difficulty + 1) * 0.2f;
            Amount *= angerMod;
            string str = why;
            string str1 = str;
            if (str != null)
            {
                if (str1 == "Caught Spying")
                {
                    Relationship angerDiplomaticConflict = this;
                    angerDiplomaticConflict.Anger_DiplomaticConflict = angerDiplomaticConflict.Anger_DiplomaticConflict + Amount;
                    Relationship totalAnger = this;
                    totalAnger.TotalAnger = totalAnger.TotalAnger + Amount;
                    Relationship trust = this;
                    trust.Trust = trust.Trust - Amount;
                    Relationship spiesDetected = this;
                    spiesDetected.SpiesDetected = spiesDetected.SpiesDetected + 1;
                    if (Us.data.DiplomaticPersonality.Name == "Honorable" || Us.data.DiplomaticPersonality.Name == "Xenophobic")
                    {
                        Relationship relationship = this;
                        relationship.Anger_DiplomaticConflict = relationship.Anger_DiplomaticConflict + Amount;
                        Relationship totalAnger1 = this;
                        totalAnger1.TotalAnger = totalAnger1.TotalAnger + Amount;
                        Relationship trust1 = this;
                        trust1.Trust = trust1.Trust - Amount;
                    }
                    if (Treaty_Alliance)
                    {
                        Relationship timesSpiedOnAlly = this;
                        timesSpiedOnAlly.TimesSpiedOnAlly = timesSpiedOnAlly.TimesSpiedOnAlly + 1;
                        if (TimesSpiedOnAlly == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_Ally_1");
                            }
                        }
                        else if (TimesSpiedOnAlly > 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_Ally_2");
                            }
                            Treaty_Alliance = false;
                            Treaty_NAPact = false;
                            Treaty_OpenBorders = false;
                            Treaty_Trade = false;
                            Posture = Posture.Hostile;
                        }
                    }
                    else if (SpiesDetected == 1 && !AtWar && Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                    {
                        if (SpiesDetected == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_1");
                            }
                        }
                        else if (SpiesDetected == 2)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_2");
                            }
                        }
                        else if (SpiesDetected >= 3)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_3");
                            }
                            Treaty_Alliance = false;
                            Treaty_NAPact = false;
                            Treaty_OpenBorders = false;
                            Treaty_Trade = false;
                            Posture = Posture.Hostile;
                        }
                    }
                }
                else if (str1 == "Caught Spying Failed")
                {
                    Relationship angerDiplomaticConflict1 = this;
                    angerDiplomaticConflict1.Anger_DiplomaticConflict = angerDiplomaticConflict1.Anger_DiplomaticConflict + Amount;
                    Relationship relationship1 = this;
                    relationship1.TotalAnger = relationship1.TotalAnger + Amount;
                    Relationship trust2 = this;
                    trust2.Trust = trust2.Trust - Amount;
                    if (Us.data.DiplomaticPersonality.Name == "Honorable" || Us.data.DiplomaticPersonality.Name == "Xenophobic")
                    {
                        Relationship angerDiplomaticConflict2 = this;
                        angerDiplomaticConflict2.Anger_DiplomaticConflict = angerDiplomaticConflict2.Anger_DiplomaticConflict + Amount;
                        Relationship totalAnger2 = this;
                        totalAnger2.TotalAnger = totalAnger2.TotalAnger + Amount;
                        Relationship relationship2 = this;
                        relationship2.Trust = relationship2.Trust - Amount;
                    }
                    Relationship spiesKilled = this;
                    spiesKilled.SpiesKilled = spiesKilled.SpiesKilled + 1;
                    if (Treaty_Alliance)
                    {
                        Relationship timesSpiedOnAlly1 = this;
                        timesSpiedOnAlly1.TimesSpiedOnAlly = timesSpiedOnAlly1.TimesSpiedOnAlly + 1;
                        if (TimesSpiedOnAlly == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_Ally_1");
                            }
                        }
                        else if (TimesSpiedOnAlly > 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                DiplomacyScreen.ShowEndOnly(Us, Them, "Caught_Spying_Ally_2");
                            }
                            Treaty_Alliance = false;
                            Treaty_NAPact = false;
                            Treaty_OpenBorders = false;
                            Treaty_Trade = false;
                            Posture = Posture.Hostile;
                        }
                    }
                    else if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                    {
                        DiplomacyScreen.ShowEndOnly(Us, Them, "Killed_Spy_1");
                    }
                }
                else if (str1 == "Insulted")
                {
                    Relationship angerDiplomaticConflict3 = this;
                    angerDiplomaticConflict3.Anger_DiplomaticConflict = angerDiplomaticConflict3.Anger_DiplomaticConflict + Amount;
                    Relationship totalAnger3 = this;
                    totalAnger3.TotalAnger = totalAnger3.TotalAnger + Amount;
                    Relationship trust3 = this;
                    trust3.Trust = trust3.Trust - Amount;
                    if (Us.data.DiplomaticPersonality.Name == "Honorable" || Us.data.DiplomaticPersonality.Name == "Xenophobic")
                    {
                        Relationship relationship3 = this;
                        relationship3.Anger_DiplomaticConflict = relationship3.Anger_DiplomaticConflict + Amount;
                        Relationship totalAnger4 = this;
                        totalAnger4.TotalAnger = totalAnger4.TotalAnger + Amount;
                        Relationship trust4 = this;
                        trust4.Trust = trust4.Trust - Amount;
                    }
                }
                else if (str1 == "Colonized Owned System")
                {
                    Array<Planet> OurTargetPlanets = new Array<Planet>();
                    Array<Planet> TheirTargetPlanets = new Array<Planet>();
                    foreach (Goal g in Us.GetEmpireAI().Goals)
                    {
                        if (g.type != GoalType.Colonize)
                        {
                            continue;
                        }
                        OurTargetPlanets.Add(g.ColonizationTarget);
                    }
                    foreach (Planet theirp in Them.GetPlanets())
                    {
                        TheirTargetPlanets.Add(theirp);
                    }
                    bool MatchFound = false;
                    SolarSystem sharedSystem = null;
                    foreach (Planet planet in OurTargetPlanets)
                    {
                        foreach (Planet other in TheirTargetPlanets)
                        {
                            if (p == null || other == null || p.ParentSystem != other.ParentSystem)
                            {
                                continue;
                            }
                            sharedSystem = p.ParentSystem;
                            MatchFound = true;
                            break;
                        }
                        if (!MatchFound || !Us.GetRelations(Them).WarnedSystemsList.Contains(sharedSystem.guid))
                        {
                            continue;
                        }
                        return;
                    }

                    float expansion = UniverseScreen.SolarSystemList.Count / Us.GetOwnedSystems().Count + Them.GetOwnedSystems().Count;
                    Anger_TerritorialConflict += Amount + expansion;
                    Trust -= Amount;

                    if (Anger_TerritorialConflict < Us.data.DiplomaticPersonality.Territorialism && !AtWar)
                    {
                        if (AtWar)
                            return;

                        if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                        {
                            if (!WarnedAboutShips)
                            {
                                DiplomacyScreen.Show(Us, Them, "Colonized Warning", p);
                            }
                            else if (!AtWar)
                            {
                                DiplomacyScreen.Show(Us, Them, "Warning Ships then Colonized", p);
                            }
                            turnsSinceLastContact = 0;
                            WarnedAboutColonizing = true;
                            contestedSystem = p.ParentSystem;
                            contestedSystemGuid = p.ParentSystem.guid;
                        }
                    }
                }
                else if(str1=="Expansion")
                {

                }
                else if (str1 == "Destroyed Ship")
                {
                    if (Anger_MilitaryConflict == 0f && !AtWar)
                    {
                        Anger_MilitaryConflict += Amount;
                        Trust -= Amount;
                        if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                        {
                            if (Anger_MilitaryConflict < 2f)
                            {
                                DiplomacyScreen.Show(Us, Them, "Aggression Warning");
                            }

                            Trust -= Amount;
                        }
                    }

                    Anger_MilitaryConflict += Amount;
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
            {
                Treaty_Trade_TurnsExisted++;
            }

            if (Treaty_Peace && --PeaceTurnsRemaining <= 0)
            {
                Treaty_Peace = false;
                us.GetRelations(them).Treaty_Peace = false;
                Empire.Universe.NotificationManager.AddPeaceTreatyExpiredNotification(them);
            }
        }

        public void UpdateRelationship(Empire us, Empire them)
        {
            if (us.data.Defeated)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == them)
                return;

            Risk.UpdateRiskAssessment(us);

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

            if (!Treaty_Alliance)
            {
                TurnsAllied = 0;
            }
            else
            {
                TurnsAllied += 1;
            }

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
            ActiveWar?.RestoreFromSave();
            foreach (var war in WarHistory)
                war.RestoreFromSave();
        }

        public void ResetRelation()
        {
            Treaty_Alliance    = false;
            Treaty_NAPact      = false;
            Treaty_OpenBorders = false;
            Treaty_Peace       = false;
            Treaty_Trade       = false;
        }

        public DebugTextBlock DebugWar()
        {
            var debug = new DebugTextBlock();
            debug.Header = $"RelationShip Status: {Name}";
            debug.HeaderColor = EmpireManager.GetEmpireByName(Name).EmpireColor;
            debug.AddLine($"Total Anger: {(int)TotalAnger}");
            debug.AddLine($"Anger From Ships in Borders: {(int)Anger_FromShipsInOurBorders}");
            debug.AddLine($"Anger From Military: {(int)Anger_MilitaryConflict}");
            debug.AddLine($"Anger From Territory Violation: {(int)Anger_TerritorialConflict}");
            debug.AddLine($"Anger From Diplomatic Faux pas: {(int)Anger_DiplomaticConflict}");
            debug.AddLine($"Trust: {(int)Trust} TrustUsed: {(int)TrustUsed}");

            ActiveWar?.WarDebugData(debug);
            return debug;
        }
    }
}