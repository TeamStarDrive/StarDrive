using Ship_Game.Gameplay;
using System;

namespace Ship_Game
{
    public sealed class Agent // Refactored by Fat Bastard June 2020
    {
        [Serialize(0)] public string Name;
        [Serialize(1)] public int Level = 1;
        [Serialize(2)] public int Experience;
        [Serialize(3)] public AgentMission Mission;
        [Serialize(4)] public AgentMission PrevisousMission = AgentMission.Training;
        [Serialize(5)] public string PreviousTarget;
        [Serialize(6)] public int TurnsRemaining;
        [Serialize(7)] public string TargetEmpire = "";
        [Serialize(8)] public Guid TargetGUID;
        [Serialize(9)] public int MissionNameIndex = 2183;
        [Serialize(10)] public bool spyMute;
        [Serialize(11)] public string HomePlanet = "";
        [Serialize(12)] public float Age = 30f;
        [Serialize(13)] public float ServiceYears = 0f;
        [Serialize(14)] public short Assassinations;
        [Serialize(15)] public short Training;
        [Serialize(16)] public short Infiltrations;
        [Serialize(17)] public short Sabotages;
        [Serialize(18)] public short TechStolen;
        [Serialize(19)] public short Robberies;
        [Serialize(20)] public short Rebellions;

        public bool IsNovice => Level < 3;

        public void AssignMission(AgentMission mission, Empire owner, string targetEmpire)
        {
            Initialize(mission, owner);
            if (Mission == AgentMission.Undercover)
            {
                foreach (Mole m in owner.data.MoleList)
                {
                    if (m.PlanetGuid != TargetGUID)
                    {
                        continue;
                    }
                    owner.data.MoleList.QueuePendingRemoval(m);
                    break;
                }
            }
            owner.data.MoleList.ApplyPendingRemovals();
            Mission = mission;
            TargetEmpire = targetEmpire;
        }

        bool ReassignedDueToVictimDefeated(Empire us, Empire victim)
        {
            if (victim != null && victim.data.Defeated)
            {
                AssignMission(AgentMission.Defending, us, "");
                return true;
            }

            return false;
        }

        float SpyRoll(Empire us,Empire victim)
        {
            float diceRoll = RandomMath.RollDie(100) + Level*RandomMath.RollDie(3);

            diceRoll += us.data.SpyModifier; // +5 with Xeno Intelligence 
            diceRoll += us.data.OffensiveSpyBonus; // +10 with Duplicitous
            diceRoll -= victim?.GetSpyDefense() ?? 0;

            return diceRoll;
        }


        // Added by gremlin Domission from devek mod. - Refactored by Fat Bastard June 2020
        public void Update(Empire us)
        {
            //Age agents
            Age          += 0.1f;
            ServiceYears += 0.1f;

            if (Mission != AgentMission.Defending)
                TurnsRemaining -= 1;

            if (TurnsRemaining > 0)
                return;

            ExecuteMission(us);
        }

        MissionResolve ResolveTraining(SpyMissionStatus missionStatus, Empire us)
        {
            MissionResolve aftermath = new MissionResolve(us);
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:     aftermath.MessageId = 6025; Training += 1; aftermath.GoodResult = true; break;
                case SpyMissionStatus.Success:          aftermath.MessageId = 6026; Training += 1; aftermath.GoodResult = true; break;
                case SpyMissionStatus.Failed:           aftermath.MessageId = 6029;                                             break;
                case SpyMissionStatus.FailedBadly:      aftermath.MessageId = 6027; aftermath.AgentInjured = true;              break;
                case SpyMissionStatus.FailedCritically: aftermath.MessageId = 6029; aftermath.AgentKilled  = true;              break;
            }

            return aftermath;
        }

        MissionResolve ResolveAssassination(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us);
            if (victim.data.AgentList.Count == 0) // no agent left to assassinate
            {
                aftermath.MessageId   = 6038;
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess: 
                    aftermath.MessageId  = 6039;
                    aftermath.GoodResult = true;
                    Assassinations++; 
                    AssassinateEnemyAgent(us, victim, out string targetNameGreat);
                    aftermath.MessageToVictim = $"{Localizer.Token(6037)} {targetNameGreat}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.MessageId  = 6039;
                    aftermath.GoodResult = true;
                    Assassinations++;
                    AssassinateEnemyAgent(us, victim, out string targetNameGood); // TODo we know who sent - damage relations
                    aftermath.MessageToVictim = $"{Localizer.Token(6037)} {targetNameGood}, {Localizer.Token(6041)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.Failed: // TODo we know who sent - damage relations
                    aftermath.MessageId       = 6047; // Foiled but escaped
                    aftermath.MessageToVictim = $"{Localizer.Token(6043)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6044; // Injured
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6043)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 15;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6046; // Died
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6045)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveInfiltration(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us);
            if (victim == null || victim.GetPlanets().Count == 0)
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                case SpyMissionStatus.Success:
                    aftermath.MessageId  = 6030;
                    aftermath.GoodResult = true;
                    Infiltrations++;
                    InfiltratePlanet(us, victim, out string planetName);
                    AssignMission(AgentMission.Undercover, us, victim.data.Traits.Name);
                    aftermath.CustomMessage = $"{Name}, {Localizer.Token(6030)}, {planetName}, {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageId       = 6036;
                    aftermath.MessageToVictim = $"{Localizer.Token(6033)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage = 10;
                    aftermath.DamageReason   = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6032;
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6033)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6034;
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6035)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveSabotage(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us);
            if (victim == null || victim.GetPlanets().Count == 0)
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            int crippledTurns = 0;
            Planet targetPlanet = victim.FindPlanetToBuildAt(victim.SafeSpacePorts, 0);
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    aftermath.GoodResult = true;
                    Sabotages++;
                    crippledTurns               = 5 + Level*5;
                    targetPlanet.CrippledTurns += crippledTurns;
                    aftermath.MessageToVictim   = $"{Localizer.Token(6048)}  {targetPlanet.Name}";
                    aftermath.CustomMessage     = $"{Name} {Localizer.Token(6084)} {crippledTurns} {Localizer.Token(6085)} " +
                                                  $"{targetPlanet.Name} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    Sabotages++;
                    crippledTurns               = 5 + Level*3;
                    targetPlanet.CrippledTurns += crippledTurns;
                    aftermath.MessageToVictim   = $"{Localizer.Token(6048)}  {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage     = $"{Name} {Localizer.Token(6084)} {crippledTurns} {Localizer.Token(6085)} " +
                                                  $"{targetPlanet.Name} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageToVictim = $"{Localizer.Token(6051)}  {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6055)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6051)}  {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6052)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6054;
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6053)}  {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }




            return aftermath;
        }

        MissionResolve ResolveRobbery(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us);
            if (victim == null || victim.Money <= 0)
            {
                aftermath.CustomMessage = $"Name  {Localizer.Token(6066)} {TargetEmpire} {Localizer.Token(6067)}";
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            float amount       = RandomMath.RandomBetween(1f, victim.GetPlanets().Count * 10f) * Level;
            amount             = amount.UpperBound(victim.Money * 0.5f);
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    victim.AddMoney(-amount);
                    us.AddMoney(amount);
                    Robberies++;
                    aftermath.GoodResult      = true;
                    aftermath.MessageToVictim = $"{amount} {Localizer.Token(6065)}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6068)} {amount} {Localizer.Token(6069)} {TargetEmpire}. " +
                                                $"{Localizer.Token(6031)}";

                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    victim.AddMoney(-amount/2);
                    us.AddMoney(amount/2);
                    Robberies++;
                    aftermath.RelationDamage  = 10;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{amount} {Localizer.Token(6070)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6068)} {amount} {Localizer.Token(6070)} {TargetEmpire}. " +
                                                $"{Localizer.Token(6034)} {us.data.Traits.Name}";

                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageId       = 6075;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(6071)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6071;
                    aftermath.MessageToVictim = $"{Localizer.Token(6073)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 15;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6074;
                    aftermath.MessageToVictim = $"{Localizer.Token(6073)}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveRebellion(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us);
            if (victim == null || victim.GetPlanets().Count == 0)
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            Planet targetPlanet = victim.GetPlanets().RandItem();
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    aftermath.GoodResult = true;
                    Rebellions++;
                    AddRebellion(victim, targetPlanet, (int)(Level * 1.5));
                    aftermath.MessageToVictim = $"{Localizer.Token(6078)} {targetPlanet.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6077)} {targetPlanet.Name}. {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    Rebellions++;
                    AddRebellion(victim, targetPlanet, Level);
                    aftermath.MessageToVictim = $"{Localizer.Token(6078)} {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6077)} {targetPlanet.Name}, {Localizer.Token(6079)}";
                    aftermath.RelationDamage  = 25;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageToVictim = $"{Localizer.Token(6076)} {targetPlanet.Name}, { Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6083)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(6076)} {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    aftermath.MessageToVictim = $"{Localizer.Token(6081)} {targetPlanet.Name}, {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveRecovery(Empire us)
        {
            MissionResolve aftermath = new MissionResolve(us);
            aftermath.MessageId      = 6086;
            // TODO repeat missions here
            /*
            startingMission = PrevisousMission;
            TargetEmpire = PreviousTarget;
            MissionNameIndex = 2183;*/
            return aftermath;
        }

        public void ExecuteMission(Empire us)
        {

            AgentMissionData data        = ResourceManager.AgentMissionData;
            spyMute                      = us.data.SpyMute;
            Empire victim                = EmpireManager.GetEmpireByName(TargetEmpire);
            AgentMission startingMission = Mission;

            if (ReassignedDueToVictimDefeated(us, victim))
                return;

            float diceRoll                 = SpyRoll(us, victim);
            SpyMissionStatus missionStatus = data.SpyRollResult(Mission, diceRoll, out short xpToAdd);

            MissionResolve aftermath = new MissionResolve(us);
            switch (Mission)
            {
                case AgentMission.Training:        aftermath = ResolveTraining(missionStatus, us);              break;
                case AgentMission.Assassinate:     aftermath = ResolveAssassination(missionStatus, us, victim); break;
                case AgentMission.Infiltrate:      aftermath = ResolveInfiltration(missionStatus, us, victim);  break;
                case AgentMission.Sabotage:        aftermath = ResolveSabotage(missionStatus, us, victim);      break;
                #region StealTech hard
                case AgentMission.StealTech:
                    {
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
                        if (victim == null)
                            return;

                        var potentialUIDs = new Array<string>();

                        foreach(var tech in victim.GetEmpireAI().TradableTechs(us)) potentialUIDs.Add(tech.UID);

                        if (potentialUIDs.Count != 0)
                        {
                            string theUID = RandomMath.RandItem(potentialUIDs);
                            if (diceRoll >= data.StealTechRollPerfect)
                            {
                                //Added by McShooterz
                                AddExperience(data.StealTechExpPerfect, us);
                                if (victim == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.
                                        AddAgentResult(false, Localizer.Token(6056), victim);
                                }
                                //Added by McShooterz: new acquire method, unlocks targets bonuses as well
                                us.AcquireTech(theUID, victim, TechUnlockType.Spy);
                                if (!spyMute)
                                {
                                    var stoleTechText = string.Concat(Name, " ", Localizer.Token(6057), " ");
                                    var techStolen    = Localizer.Token(ResourceManager.TechTree[theUID].NameIndex);
                                    var notDetected   = Localizer.Token(6031);
                                    var resultString  = string.Concat(stoleTechText, techStolen, notDetected);

                                    Empire.Universe.NotificationManager.
                                    AddAgentResult(true, resultString, us);

                                }
                                TechStolen++;
                                break;
                            }

                            if (diceRoll > data.StealTechRollGood)
                            {
                                //Added by McShooterz
                                AddExperience(data.StealTechExpGood, us);
                                if (victim == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager
                                        .AddAgentResult(false, string.Concat(Localizer.Token(6058)
                                            , " "
                                            , Localizer.Token(ResourceManager.TechTree[theUID].NameIndex)
                                            , Localizer.Token(6049), " ", us.data.Traits.Name)
                                            , victim);
                                }
                                //Added by McShooterz: new acquire method, unlocks targets bonuses as well
                                //Owner.UnlockTech(theUID);
                                us.AcquireTech(theUID, victim, TechUnlockType.Spy);
                                victim.GetRelations(us).DamageRelationship(victim, us, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(true, string.Concat(Name, " ", Localizer.Token(6057), " ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), Localizer.Token(6042)), us);
                                TechStolen++;
                                break;
                            }

                            if (diceRoll < data.StealTechRollBad)
                            {
                                if (diceRoll >= data.StealTechRollWorst)
                                {
                                    if (victim == EmpireManager.Player)
                                    {
                                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(true, string.Concat(Localizer.Token(6059), Localizer.Token(6049), " ", us.data.Traits.Name), victim);
                                    }
                                    victim.GetRelations(us).DamageRelationship(victim, us, "Caught Spying", 20f, null);
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(false, string.Concat(Name, " ", Localizer.Token(6050)), us);
                                    AssignMission(AgentMission.Recovering, us, "");
                                    PrevisousMission = AgentMission.StealTech;
                                    PreviousTarget = TargetEmpire;
                                    break;
                                }
                                if (victim == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(false, string.Concat(Localizer.Token(6060), Localizer.Token(6049), " ", us.data.Traits.Name), victim);
                                }
                                victim.GetRelations(us).DamageRelationship(victim, us, "Caught Spying Failed", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(false, string.Concat(Name, " ", Localizer.Token(6061)), us);
                                us.data.AgentList.QueuePendingRemoval(this);
                                break;
                            }

                            if (victim == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(true, string.Concat(Localizer.Token(6059), Localizer.Token(6049), " ", us.data.Traits.Name), victim);
                            }
                            victim.GetRelations(us).DamageRelationship(victim, us, "Caught Spying", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(false, string.Concat(Name, " ", Localizer.Token(6062)), us);
                            break;
                        }

                        AddExperience(data.StealTechExp, us);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(false, string.Concat(Name, " ", Localizer.Token(6063), " ", (data.StealTechCost / 2).ToString(), " ", Localizer.Token(6064)), us);
                        Empire owner = us;
                        owner.AddMoney((float)data.StealTechCost / 2);
                        break;
                    }
                #endregion
                case AgentMission.Robbery:         aftermath = ResolveRobbery(missionStatus, us, victim);       break;
                case AgentMission.InciteRebellion: aftermath = ResolveRebellion(missionStatus, us, victim);     break;
                case AgentMission.Recovering:      aftermath = ResolveRecovery(us);                             break;
            }

            aftermath.PerformPostMissionActions(this, xpToAdd);

            #region Mission Repeat
            if (us == EmpireManager.Player
                && Mission == AgentMission.Defending //&& Owner.data.SpyBudget > 500
                && us.data.SpyMissionRepeat
                && (startingMission != AgentMission.Training || startingMission == AgentMission.Training && Level < 10))
            {
                AssignMission(startingMission, us, TargetEmpire);
                return;
            }
            TargetEmpire = "";
            #endregion
        }

        void Initialize(AgentMission mission, Empire owner) // TODO move to AgentMissionData
        {
            float missionCost     = 0;
            AgentMissionData data = ResourceManager.AgentMissionData;
            switch (mission)
            {
                case AgentMission.Undercover:
                    MissionNameIndex = 2201;
                    break;
                case AgentMission.Training:
                    TurnsRemaining   = data.TrainingTurns;
                    missionCost      = data.TrainingCost;
                    MissionNameIndex = 2196;
                    break;
                case AgentMission.Infiltrate:
                    TurnsRemaining   = data.InfiltrateTurns;
                    missionCost      = data.InfiltrateCost;
                    MissionNameIndex = 2188;
                    break;
                case AgentMission.Assassinate:
                    TurnsRemaining   = data.AssassinateTurns;
                    missionCost      = data.AssassinateCost;
                    MissionNameIndex = 2184;
                    break;
                case AgentMission.Sabotage:
                    TurnsRemaining   = data.SabotageTurns;
                    missionCost      = data.SabotageCost;
                    MissionNameIndex = 2190;
                    break;
                case AgentMission.StealTech:
                    TurnsRemaining   = data.StealTechTurns;
                    missionCost      = data.StealTechCost;
                    MissionNameIndex = 2194;
                    break;
                case AgentMission.Robbery:
                    TurnsRemaining   = data.RobberyTurns;
                    missionCost      = data.RobberyCost;
                    MissionNameIndex = 2192;
                    break;
                case AgentMission.InciteRebellion:
                    TurnsRemaining   = data.RebellionTurns;
                    missionCost      = data.RebellionCost;
                    MissionNameIndex = 2186;
                    break;
                case AgentMission.Recovering:
                    TurnsRemaining   = data.RecoveringTurns;
                    MissionNameIndex = 6024;
                    break;
            }

            owner.AddMoney(-missionCost);
            owner.GetEmpireAI().DeductSpyBudget(missionCost);
        }

        void InfiltratePlanet(Empire us, Empire victim, out string planetName)
        {
            Mole m = Mole.PlantMole(us, victim, out planetName);
            TargetGUID = m.PlanetGuid;
        }

        void AssassinateEnemyAgent(Empire us, Empire victim, out string targetName)
        {
            Agent targetAgent = victim.data.AgentList.RandItem(); // TODO - a target specific agent base on threat
            targetName = targetAgent.Name;
            victim.data.AgentList.Remove(targetAgent);
            if (targetAgent.Mission != AgentMission.Undercover)
                return;

            foreach (Mole mole in us.data.MoleList)
            {
                if (mole.PlanetGuid == targetAgent.TargetGUID)
                {
                    us.data.MoleList.QueuePendingRemoval(mole);
                    break;
                }
            }

            us.data.MoleList.ApplyPendingRemovals();
        }

        void AddRebellion(Empire victim, Planet targetPlanet, int numTroops)
        {
            Empire rebels = null;
            if (!victim.data.RebellionLaunched)
                rebels = EmpireManager.CreateRebelsFromEmpireData(victim.data, victim);

            if (rebels == null) 
                rebels = EmpireManager.GetEmpireByName(victim.data.RebelName);

            for (int i = 0; i < numTroops; i++)
            {
                foreach (string troopType in ResourceManager.TroopTypes)
                {
                    if (!victim.WeCanBuildTroop(troopType))
                        continue;

                    Troop t       = ResourceManager.CreateTroop(troopType, rebels);
                    t.Name        = Localizer.Token(rebels.data.TroopNameIndex);
                    t.Description = Localizer.Token(rebels.data.TroopDescriptionIndex);
                    if (targetPlanet.GetFreeTiles(t.Loyalty) == 0 && !targetPlanet.BumpOutTroop(EmpireManager.Corsairs)
                                                            && !t.TryLandTroop(targetPlanet)) // Let's say the rebels are pirates :)
                    {
                        t.Launch(targetPlanet); // launch the rebels
                    }

                    break;
                }
            }
        }

        //Added by McShooterz: add experience to the agent and determine if level up.
        private void AddExperience(int exp, Empire owner) // TODO retire
        {
            Experience += exp;
            while (Experience >= ResourceManager.AgentMissionData.ExpPerLevel * Level)
            {
                Experience -=  ResourceManager.AgentMissionData.ExpPerLevel * Level;
                if (Level < 10)
                {
                    Level++;
                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResult(true, string.Concat(Name, " ", Localizer.Token(6087)), owner);
                }
            }
        }

        struct MissionResolve
        {
            public bool GoodResult;
            public bool ShouldAddXp;
            public int MessageId;
            public string MessageToVictim;
            public string CustomMessage;
            public bool AgentInjured;
            public bool AssignDefense;
            public bool AgentKilled;
            public float RelationDamage;
            public string DamageReason;
            private readonly Empire Us;
            private readonly Empire Victim;

            public MissionResolve(Empire us, Empire victim = null)
            {
                Us              = us;
                Victim          = victim;
                GoodResult      = false;
                ShouldAddXp     = true;
                MessageId       = 0;
                AgentInjured    = false;
                AgentKilled     = false;
                AssignDefense   = true;
                MessageToVictim = "";
                CustomMessage   = "";
                RelationDamage  = 0;
                DamageReason    = "";
            }

            public void PerformPostMissionActions(Agent agent, int xpToAdd)
            {

                // todo repeat missions here

                if (AgentKilled)
                {
                    Us.data.AgentList.QueuePendingRemoval(agent);
                }
                else if (AgentInjured)
                {
                    agent.PrevisousMission = agent.Mission;
                    agent.PreviousTarget   = agent.TargetEmpire;
                    agent.AssignMission(AgentMission.Recovering, Us, "");
                }
                else if (AssignDefense)
                {
                    agent.AssignMission(AgentMission.Defending, Us, "");
                }

                if (ShouldAddXp && !AgentKilled)
                    agent.AddExperience(xpToAdd, Us);

                if (MessageId > 0) // default message
                    Empire.Universe.NotificationManager.AddAgentResult(GoodResult, $"{agent.Name} {Localizer.Token(MessageId)}", Us);

                if (CustomMessage.NotEmpty())
                    Empire.Universe.NotificationManager.AddAgentResult(GoodResult, CustomMessage, Us);

                if (MessageToVictim.NotEmpty())
                    Empire.Universe.NotificationManager.AddAgentResult(!GoodResult, MessageToVictim, Victim);

                if (RelationDamage > 0 && DamageReason.NotEmpty())
                    Victim.GetRelations(Us).DamageRelationship(Victim, Us, DamageReason, RelationDamage, null);
            }
        }
    }
}