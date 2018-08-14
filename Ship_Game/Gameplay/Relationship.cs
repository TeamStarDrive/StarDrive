using Ship_Game;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json;
using Ship_Game.AI;

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

        public float RiskAssesment (Empire us, Empire them, float riskLimit = 1)
        {
            if (!Known) return 0;
            float risk = float.MaxValue;
            float strength = Math.Max(100, us.currentMilitaryStrength);
            if (!them.isFaction && !AtWar && !PreparingForWar &&
                !(TotalAnger > us.data.DiplomaticPersonality.Territorialism)) return 0;
            if (!them.isFaction)            
                return (risk = us.GetGSAI().ThreatMatrix.StrengthOfEmpire(them) / strength) > riskLimit ? 0 :risk;
            var s = new HashSet<SolarSystem>();
            foreach (var task in us.GetGSAI().TaskList)
            {
                if (task.type != AI.Tasks.MilitaryTask.TaskType.DefendClaim) continue;
                var p = task.TargetPlanet;
                var ss = p.ParentSystem;
                if (!s.Add(ss)) continue;
                float test;
                if ((test = us.GetGSAI().ThreatMatrix.StrengthOfEmpireInSystem(them, ss)) > 0 && test <  risk)
                    risk = test;
            }            
            risk /= strength;
            return risk > riskLimit ? 0 : risk;
        }

        public float BorderRiskAssesment(Empire us, Empire them, float riskLimit = .5f)
        {
            if (!Known) return 0;
            float strength = 0;
            foreach (var ss in us.GetBorderSystems(them))
            {
                strength += us.GetGSAI().ThreatMatrix.StrengthOfEmpireInSystem(them, ss);
            }
            strength /= Math.Max(us.currentMilitaryStrength, 100);
            return strength > riskLimit ? 0 : strength;            
        }

        public float ExpansionRiskAssement(Empire us, Empire them, float riskLimit = .5f)
        {
            if (!Known || them.NumPlanets ==0) return 0;
            float themStrength = 0;
            float usStrength = 0;
            
            foreach (Planet p in them.GetPlanets())
            {
                if (!p.IsExploredBy(us)) continue;
                themStrength += p.DevelopmentLevel;
            }
            
            foreach (Planet p in us.GetPlanets())
            {
                usStrength += p.DevelopmentLevel;
            }
            float strength = (themStrength / usStrength) *.25f;
            return strength > riskLimit ? 0 : strength;
        }

        public void DamageRelationship(Empire Us, Empire Them, string why, float Amount, Planet p)
        {
            if (Us.data.DiplomaticPersonality == null)
            {
                return;
            }

            
            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == Them)
                return;
            float angerMod = 1+ ((int)Empire.Universe.GameDifficulty+1) * .2f;
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
                    if (this.Treaty_Alliance)
                    {
                        Relationship timesSpiedOnAlly = this;
                        timesSpiedOnAlly.TimesSpiedOnAlly = timesSpiedOnAlly.TimesSpiedOnAlly + 1;
                        if (this.TimesSpiedOnAlly == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_Ally_1", true));
                                return;
                            }
                        }
                        else if (this.TimesSpiedOnAlly > 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_Ally_2", true));
                            }
                            this.Treaty_Alliance = false;
                            this.Treaty_NAPact = false;
                            this.Treaty_OpenBorders = false;
                            this.Treaty_Trade = false;
                            this.Posture = Ship_Game.Gameplay.Posture.Hostile;
                            return;
                        }
                    }
                    else if (this.SpiesDetected == 1 && !this.AtWar && Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                    {
                        if (this.SpiesDetected == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_1", true));
                                return;
                            }
                        }
                        else if (this.SpiesDetected == 2)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_2", true));
                                return;
                            }
                        }
                        else if (this.SpiesDetected >= 3)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_3", true));
                            }
                            this.Treaty_Alliance = false;
                            this.Treaty_NAPact = false;
                            this.Treaty_OpenBorders = false;
                            this.Treaty_Trade = false;
                            this.Posture = Ship_Game.Gameplay.Posture.Hostile;
                            return;
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
                    if (this.Treaty_Alliance)
                    {
                        Relationship timesSpiedOnAlly1 = this;
                        timesSpiedOnAlly1.TimesSpiedOnAlly = timesSpiedOnAlly1.TimesSpiedOnAlly + 1;
                        if (this.TimesSpiedOnAlly == 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_Ally_1", true));
                                return;
                            }
                        }
                        else if (this.TimesSpiedOnAlly > 1)
                        {
                            if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Caught_Spying_Ally_2", true));
                            }
                            this.Treaty_Alliance = false;
                            this.Treaty_NAPact = false;
                            this.Treaty_OpenBorders = false;
                            this.Treaty_Trade = false;
                            this.Posture = Ship_Game.Gameplay.Posture.Hostile;
                            return;
                        }
                    }
                    else if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                    {
                        Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Killed_Spy_1", true));
                        return;
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
                        return;
                    }
                }
                else if (str1 == "Colonized Owned System")
                {
                    Array<Planet> OurTargetPlanets = new Array<Planet>();
                    Array<Planet> TheirTargetPlanets = new Array<Planet>();
                    foreach (Goal g in Us.GetGSAI().Goals)
                    {
                        if (g.type != GoalType.Colonize)
                        {
                            continue;
                        }
                        OurTargetPlanets.Add(g.GetMarkedPlanet());
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
                    Relationship angerTerritorialConflict = this;
                    angerTerritorialConflict.Anger_TerritorialConflict = angerTerritorialConflict.Anger_TerritorialConflict + Amount *1+expansion;
                    Relationship relationship4 = this;
                    relationship4.Trust = relationship4.Trust - Amount;
                    

                    if (this.Anger_TerritorialConflict < (float)Us.data.DiplomaticPersonality.Territorialism && !this.AtWar)
                    {
                        if (this.AtWar)
                        {
                            return;
                        }
                        if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                        {
                            if (!this.WarnedAboutShips)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Colonized Warning", p));
                            }
                            else if (!this.AtWar)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Warning Ships then Colonized", p));
                            }
                            this.turnsSinceLastContact = 0;
                            this.WarnedAboutColonizing = true;
                            this.contestedSystem = p.ParentSystem;
                            this.contestedSystemGuid = p.ParentSystem.guid;
                            return;
                        }
                    }
                }
                else if(str1=="Expansion")
                {

                }
                else
                {
                    if (str1 != "Destroyed Ship")
                    {
                        return;
                    }
                    if (this.Anger_MilitaryConflict == 0f && !this.AtWar)
                    {
                        Relationship angerMilitaryConflict = this;
                        angerMilitaryConflict.Anger_MilitaryConflict = angerMilitaryConflict.Anger_MilitaryConflict + Amount;
                        Relationship trust5 = this;
                        trust5.Trust = trust5.Trust - Amount;
                        if (Empire.Universe.PlayerEmpire == Them && !Us.isFaction)
                        {
                            if (this.Anger_MilitaryConflict < 2f)
                            {
                                Empire.Universe.ScreenManager.AddScreen(new DiplomacyScreen(Empire.Universe, Us, Them, "Aggression Warning"));
                            }
                            Relationship relationship5 = this;
                            relationship5.Trust = relationship5.Trust - Amount;
                        }
                    }
                    Relationship angerMilitaryConflict1 = this;
                    angerMilitaryConflict1.Anger_MilitaryConflict = angerMilitaryConflict1.Anger_MilitaryConflict + Amount;
                }
            }
        }

        public SolarSystem GetContestedSystem()
        {
            return Empire.Universe.SolarSystemDict[this.contestedSystemGuid];
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
            us.Money -= IntelligenceBudget;
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

            if (!Treaty_Peace || --PeaceTurnsRemaining > 0)
                return;
            Treaty_Peace = false;
            us.GetRelations(them).Treaty_Peace = false;
            Empire.Universe.NotificationManager.AddPeaceTreatyExpiredNotification(them);
        }

        public void UpdateRelationship(Empire us, Empire them)
        {
            if (us.data.Defeated)
                return;

            if (GlobalStats.RestrictAIPlayerInteraction && Empire.Universe.PlayerEmpire == them)
                return;

            Risk.UpdateRiskAssessment(us);

            if(us.isPlayer)
            {
                UpdatePlayerRelations(us, them);
                return;
            }
            if (FedQuest != null)
            {
                var enemyEmpire = EmpireManager.GetEmpireByName(FedQuest.EnemyName);
                if (FedQuest.type == QuestType.DestroyEnemy && enemyEmpire.data.Defeated)
                {
                    var ds = new DiplomacyScreen(Empire.Universe, us, Empire.Universe.PlayerEmpire, "Federation_YouDidIt_KilledEnemy", true)
                    { empToDiscuss = enemyEmpire };
                    Empire.Universe.ScreenManager.AddScreen(ds);
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
                        var ds = new DiplomacyScreen(Empire.Universe, us, Empire.Universe.PlayerEmpire, "Federation_YouDidIt_AllyFriend", true)
                        {
                            empToDiscuss = EmpireManager.GetEmpireByName(FedQuest.EnemyName)
                        };
                        Empire.Universe.ScreenManager.AddScreen(ds);
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
                if (te.TurnTimer == 0 || te.TurnsInExistence <= 250)
                    continue;
                TrustEntries.QueuePendingRemoval(te);
            }
            TrustEntries.ApplyPendingRemovals();
            foreach (FearEntry te in FearEntries)
            {
                te.TurnsInExistence += 1f;
                if (te.TurnTimer == 0 || te.TurnsInExistence <= 250f)
                    continue;
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
                float strengthofshipsinborders = us.GetGSAI().ThreatMatrix.StrengthOfAllEmpireShipsInBorders(them);
                if (strengthofshipsinborders > 0)
                {
                    if (!Treaty_NAPact)
                        Anger_FromShipsInOurBorders += (100f - Trust) / 100f * strengthofshipsinborders / (us.MilitaryScore);
                    else 
                        Anger_FromShipsInOurBorders += (100f - Trust) / 100f * strengthofshipsinborders / (us.MilitaryScore * 2f);
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

        public bool AttackForBorderViolation(DTrait personality)
        {
            if (Treaty_OpenBorders) return false;
             float borderAnger = Anger_FromShipsInOurBorders * (Anger_MilitaryConflict * .1f) + Anger_TerritorialConflict;
            if (Treaty_Trade) borderAnger *= .2f;
                    
            return borderAnger + 10 > (personality?.Territorialism  ?? EmpireManager.Player.data.BorderTolerance);
        }
        
        public bool AttackForTransgressions(DTrait personality)
        {            
            return !Treaty_NAPact && TotalAnger  > (personality?.Territorialism 
                ?? EmpireManager.Player.data.BorderTolerance);
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
        
    }
}