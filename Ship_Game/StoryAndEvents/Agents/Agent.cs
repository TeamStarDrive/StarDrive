using NAudio.Wave;
using Newtonsoft.Json;
using Ship_Game.Gameplay;
using System;
using System.Xml.Serialization;

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

        [XmlIgnore][JsonIgnore]
        public bool IsNovice => Level < 3;
        
        [XmlIgnore][JsonIgnore]
        public LocalizedText MissionName => ResourceManager.AgentMissionData.GetMissionName(Mission);

        public void AssignMission(AgentMission mission, Empire owner, string targetEmpire)
        {
            (int turns, int cost) = ResourceManager.AgentMissionData.GetTurnsAndCost(mission);
            if (cost > 0 && cost > owner.Money)
                return; // Do not go into negative money, cost > 0 check is for 0 mission cost which can be done in negative

            if (mission == AgentMission.Undercover)
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
            owner.AddMoney(-cost);
            owner.GetEmpireAI().DeductSpyBudget(cost);

            Mission = mission;
            TargetEmpire = targetEmpire;
            TurnsRemaining = turns;
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

            diceRoll += us.data.OffensiveSpyBonus;  // +10 with Duplicitous 
            if (Mission != AgentMission.Training)
                diceRoll += us.data.SpyModifier; // +5 with Xeno Intelligence 

            if (victim != null && victim != us)
                diceRoll -= victim.GetSpyDefense();

            return diceRoll;
        }


        // Added by gremlin Domission from devek mod. - Refactored by Fat Bastard June 2020
        public void Update(Empire us)
        {
            //Age agents
            Age          += 0.1f;
            ServiceYears += 0.1f;

            if (Mission == AgentMission.Defending)
                return;

            TurnsRemaining -= 1;
            if (TurnsRemaining > 0)
                return;

            ExecuteMission(us);
        }

        MissionResolve ResolveTraining(SpyMissionStatus missionStatus, Empire us)
        {
            MissionResolve aftermath = new MissionResolve(us, null);
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:     aftermath.Message = GameText.HasSuccessfullyCompleteTrainingntheAgents; break;
                case SpyMissionStatus.Success:          aftermath.Message = GameText.HasSuccessfullyCompletedTrainingnandHas; break;
                case SpyMissionStatus.Failed:           aftermath.Message = GameText.HasCompletedTrainingButFailed; break;
                case SpyMissionStatus.FailedBadly:      aftermath.Message = GameText.WasInjuredInATraining; break;
                case SpyMissionStatus.FailedCritically: aftermath.Message = GameText.HasCompletedTrainingButFailed; break;
            }
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:     Training += 1; aftermath.GoodResult = true; break;
                case SpyMissionStatus.Success:          Training += 1; aftermath.GoodResult = true; break;
                case SpyMissionStatus.Failed:           break;
                case SpyMissionStatus.FailedBadly:      aftermath.AgentInjured = true; break;
                case SpyMissionStatus.FailedCritically: aftermath.AgentKilled  = true; break;
            }
            return aftermath;
        }

        MissionResolve ResolveAssassination(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us, victim);
            if (victim.data.AgentList.Count == 0) // no agent left to assassinate
            {
                aftermath.Message = GameText.CouldNotAssassinateAnEnemy;
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess: 
                    aftermath.Message = GameText.AssassinatedAnEnemyAgent;
                    aftermath.GoodResult = true;
                    Assassinations++; 
                    AssassinateEnemyAgent(us, victim, out string targetNameGreat);
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.OneOfOurAgentsWas)} {targetNameGreat}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.Message = GameText.AssassinatedAnEnemyAgent;
                    aftermath.GoodResult = true;
                    Assassinations++;
                    AssassinateEnemyAgent(us, victim, out string targetNameGood);
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.OneOfOurAgentsWas)} {targetNameGood}, {Localizer.Token(GameText.NtheAssassinWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.Message = GameText.WasFoiledTryingToAssassinate; // Foiled but escaped
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeManagedToDetectAn)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.Message = GameText.WasWoundedTryingToAssassinate; // Injured
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeManagedToDetectAn)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 15;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.Message = GameText.WasKilledTryingToAssassinate; // Died
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasKilled2)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveInfiltration(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us, victim);
            if (victim == null || victim.GetPlanets().Count == 0)
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                case SpyMissionStatus.Success:
                    aftermath.Message = GameText.SuccessfullyInfiltratedAColony;
                    aftermath.GoodResult = true;
                    Infiltrations++;
                    InfiltratePlanet(us, victim, out string planetName);
                    AssignMission(AgentMission.Undercover, us, victim.data.Traits.Name);
                    aftermath.CustomMessage = $"{Name}, {Localizer.Token(GameText.SuccessfullyInfiltratedAColony)} {planetName} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.Message= GameText.WasUnableToInfiltrateA2;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasFoiled)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage = 10;
                    aftermath.DamageReason   = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.Message = GameText.WasUnableToInfiltrateA;
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasFoiled)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.Message = GameText.WasKilledTryingToInfiltrate;
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasKilled)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveSabotage(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us, victim);
            if (victim == null || victim.GetPlanets().Count == 0)
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            int crippledTurns;
            Planet targetPlanet = victim.FindPlanetToBuildAt(victim.SpacePorts, 0) 
                                  ?? victim.FindPlanetToBuildAt(victim.GetPlanets(), 0);

            if (targetPlanet == null) // no planet was found, abort mission
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    aftermath.GoodResult = true;
                    Sabotages++;
                    crippledTurns               = 5 + Level*5;
                    targetPlanet.CrippledTurns += crippledTurns;
                    aftermath.MessageToVictim   = $"{Localizer.Token(GameText.AnEnemyAgentHasSabotaged)}  {targetPlanet.Name}";
                    aftermath.CustomMessage     = $"{Name} {Localizer.Token(GameText.SabotagedProductionFor)} {crippledTurns} {Localizer.Token(GameText.Turns3)} " +
                                                  $"{targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    Sabotages++;
                    crippledTurns               = 5 + Level*3;
                    targetPlanet.CrippledTurns += crippledTurns;
                    aftermath.MessageToVictim   = $"{Localizer.Token(GameText.AnEnemyAgentHasSabotaged)}  {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage     = $"{Name} {Localizer.Token(GameText.SabotagedProductionFor)} {crippledTurns} {Localizer.Token(GameText.Turns3)} " +
                                                  $"{targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyAgent)}  {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.EscapedAfterBeingDetectedWhile)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyAgent)}  {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.WasWoundedWhileTryingTo)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.Message = GameText.WasKilledTryingToSabotage;
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeKilledAnEnemyAgent)}  {targetPlanet.Name}, {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveRobbery(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us, victim);
            if (victim == null || victim.Money <= 0)
            {
                aftermath.CustomMessage = $"Name  {Localizer.Token(GameText.CouldNotRob)} {TargetEmpire} {Localizer.Token(GameText.NbecauseTheyHaveNoMoney)}";
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            int amount = RandomMath.IntBetween(1, victim.GetPlanets().Count * 50) * Level;
            amount     = amount.UpperBound((int)(victim.Money * 0.5));
            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    victim.AddMoney(-amount);
                    us.AddMoney(amount);
                    Robberies++;
                    aftermath.GoodResult      = true;
                    aftermath.MessageToVictim = $"{amount} {Localizer.Token(GameText.CreditsWereMysteriouslyStolenFrom)}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.Stole)} {amount} {Localizer.Token(GameText.CreditsFrom)} {TargetEmpire}. " +
                                                $"{Localizer.Token(GameText.NtheAgentWasNotDetected)}";

                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    victim.AddMoney(-amount/2);
                    us.AddMoney(amount/2);
                    Robberies++;
                    aftermath.RelationDamage  = 10;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{amount} {Localizer.Token(GameText.CreditsWereStolenFromOur)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.Stole)} {amount} {Localizer.Token(GameText.CreditsFrom)} {TargetEmpire}";

                    break;
                case SpyMissionStatus.Failed:
                    aftermath.Message = GameText.WasUnableToStealAny2;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyPlot2)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.Message = GameText.WeFoiledAnEnemyPlot2;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeKilledAnEnemyAgent2)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 15;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeKilledAnEnemyAgent2)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.WasKilledTryingToSteal2)} {TargetEmpire}";
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveRebellion(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us, victim);
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
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentHasIncited)} {targetPlanet.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.IncitedASeriousRebellionOn)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    Rebellions++;
                    AddRebellion(victim, targetPlanet, Level);
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentHasIncited)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.IncitedASeriousRebellionOn)} {targetPlanet.Name} {Localizer.Token(GameText.NhoweverTheyKnowWeAre)}";
                    aftermath.RelationDamage  = 25;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyPlot3)} {targetPlanet.Name} { Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.EscapedAfterBeingDetectedWhile2)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyPlot3)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeKilledAnEnemyAgent3)} {targetPlanet.Name} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveStealTech(SpyMissionStatus missionStatus, Empire us, Empire victim)
        {
            MissionResolve aftermath = new MissionResolve(us, victim);

            if (victim == null)
            {
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            if (!victim.GetEmpireAI().TradableTechs(us, out Array<TechEntry> potentialTechs))
            {
                aftermath.Message = GameText.AbortedTheStealTechnologyMission;
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            string stolenTech     = potentialTechs.RandItem().UID;
            string stolenTechName = ResourceManager.TechTree[stolenTech].Name.Text;

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    us.AcquireTech(stolenTech, victim, TechUnlockType.Spy);
                    TechStolen++;
                    aftermath.GoodResult      = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemySpyStoleSome)}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.StoleTechnologyDataChipsFor)} {stolenTechName} {Localizer.Token(GameText.NtheAgentWasNotDetected)}";
                    break;
                case SpyMissionStatus.Success:
                    us.AcquireTech(stolenTech, victim, TechUnlockType.Spy);
                    TechStolen++;
                    aftermath.GoodResult = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentStoleA)} {stolenTechName} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(GameText.StoleTechnologyDataChipsFor)} {stolenTechName} {Localizer.Token(GameText.NourAgentWasDetectedBut)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.Message = GameText.WasDetectedWhileAttemptingTo2;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyPlot)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.Message = GameText.WasDetectedWhileAttemptingTo;
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.WeFoiledAnEnemyPlot)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.Message = GameText.WasKilledTryingToSteal;
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    aftermath.MessageToVictim = $"{Localizer.Token(GameText.AnEnemyAgentWasKilled3)} {Localizer.Token(GameText.NtheAgentWasSentBy)} {us.data.Traits.Name}";
                    break;
            }

            return aftermath;
        }

        MissionResolve ResolveRecovery(Empire us)
        {
            MissionResolve aftermath = new MissionResolve(us, null) { Message = GameText.HasRecoveredFromTheirInjuries};
            Mission = PrevisousMission;
            TargetEmpire = PreviousTarget;
            return aftermath;
        }

        public void ExecuteMission(Empire us)
        {
            AgentMissionData data = ResourceManager.AgentMissionData;
            spyMute = us.data.SpyMute;
            Empire victim = EmpireManager.GetEmpireByName(TargetEmpire);

            if (ReassignedDueToVictimDefeated(us, victim))
                return;

            float diceRoll                 = SpyRoll(us, victim);
            SpyMissionStatus missionStatus = data.SpyRollResult(Mission, diceRoll, out short xpToAdd);

            MissionResolve aftermath = new MissionResolve(us, victim);
            switch (Mission)
            {
                case AgentMission.Training:        aftermath = ResolveTraining(missionStatus, us);              break;
                case AgentMission.Assassinate:     aftermath = ResolveAssassination(missionStatus, us, victim); break;
                case AgentMission.Infiltrate:      aftermath = ResolveInfiltration(missionStatus, us, victim);  break;
                case AgentMission.Sabotage:        aftermath = ResolveSabotage(missionStatus, us, victim);      break;
                case AgentMission.StealTech:       aftermath = ResolveStealTech(missionStatus, us, victim);     break;
                case AgentMission.Robbery:         aftermath = ResolveRobbery(missionStatus, us, victim);       break;
                case AgentMission.InciteRebellion: aftermath = ResolveRebellion(missionStatus, us, victim);     break;
                case AgentMission.Recovering:      aftermath = ResolveRecovery(us);                             break;
            }

            aftermath.PerformPostMissionActions(this, xpToAdd, missionStatus);
            RepeatMission(us);
        }

        void RepeatMission(Empire us)
        {
            if (Mission == AgentMission.Undercover)
                return;  // do not repeat mission for undercover agents, they are moles now.

            if (us.isPlayer && us.data.SpyMissionRepeat)
            {
                if (Mission != AgentMission.Training || Mission == AgentMission.Training && IsNovice)
                {
                    AssignMission(Mission, us, TargetEmpire);
                    return;
                }
            }

            AssignMission(AgentMission.Defending, us, "");
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

                    Troop t = ResourceManager.CreateTroop(troopType, rebels);
                    t.Name        = rebels.data.TroopName.Text;
                    t.Description = rebels.data.TroopDescription.Text;
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
        private void AddExperience(int exp, Empire owner) 
        {
            Experience += exp;
            while (Experience >= ResourceManager.AgentMissionData.ExpPerLevel * Level)
            {
                Experience -=  ResourceManager.AgentMissionData.ExpPerLevel * Level;
                if (Level < 10)
                {
                    Level++;
                    if (!spyMute)
                    {
                        string message = $"{Name} {Localizer.Token(GameText.HasBeenPromotedAndGains)}";
                        if (Mission == AgentMission.Training && Level == 3 && owner.data.SpyMissionRepeat)
                            message += "\nTraining is stopped since the agent has reached Level 3";

                        Empire.Universe.NotificationManager.AddAgentResult(true, message, owner);
                    }
                }
                else
                {
                    RetireAgent(owner); // Reaching above level 10, the agent will retire
                }
            }
        }

        void RetireAgent(Empire owner)
        {
            string message = $"{Name} has decided to retire.\n" +
                             $"All agents below Level 6 gain 1 Level\n" +
                             "due to this agent's tutoring and vast experience";

            Empire.Universe.NotificationManager.AddAgentResult(true, message, owner);
            owner.data.AgentList.QueuePendingRemoval(this);
            for (int i = 0; i < owner.data.AgentList.Count; i++)
            {
                Agent agent = owner.data.AgentList[i];
                if (agent.Level < 6 && agent != this)
                    agent.Level++;
            }

        }

        struct MissionResolve
        {
            public bool GoodResult;
            public bool ShouldAddXp;
            public LocalizedText Message;
            public string MessageToVictim;
            public string CustomMessage;
            public bool AgentInjured;
            public bool AgentKilled;
            public float RelationDamage;
            public string DamageReason;
            private readonly Empire Us;
            private readonly Empire Victim;

            public MissionResolve(Empire us, Empire victim)
            {
                Us              = us;
                Victim          = victim;
                GoodResult      = false;
                ShouldAddXp     = true;
                Message         = LocalizedText.None;
                AgentInjured    = false;
                AgentKilled     = false;
                MessageToVictim = "";
                CustomMessage   = "";
                RelationDamage  = 0;
                DamageReason    = "";
            }

            public void PerformPostMissionActions(Agent agent, int xpToAdd, SpyMissionStatus missionStatus)
            {
                AgentRelatedActions(agent, xpToAdd, missionStatus);
                SendNotifications(agent);
            }

            void AgentRelatedActions(Agent agent, int xpToAdd, SpyMissionStatus missionStatus)
            {
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

                if (ShouldAddXp && !AgentKilled)
                    agent.AddExperience(xpToAdd, Us);

                // One of the victim's defending agent will be get XP for a very successful defense
                if (missionStatus <= SpyMissionStatus.FailedBadly 
                    && Victim != null 
                    && Victim != Us
                    && Victim.data.AgentList.Count > 0)
                {
                    var defendingAgents = Victim.data.AgentList.Filter(a => a.Mission == AgentMission.Defending);
                    if (defendingAgents.Length > 0)
                        defendingAgents.RandItem().AddExperience(1, Victim);
                }
            }

            void SendNotifications(Agent agent)
            {
                if (Message.NotEmpty) // default message
                    Empire.Universe.NotificationManager.AddAgentResult(GoodResult, $"{agent.Name} {Message.Text}", Us);

                if (CustomMessage.NotEmpty())
                    Empire.Universe.NotificationManager.AddAgentResult(GoodResult, CustomMessage, Us);

                if (MessageToVictim.NotEmpty())
                    Empire.Universe.NotificationManager.AddAgentResult(!GoodResult, MessageToVictim, Victim);

                if (RelationDamage > 0 && DamageReason.NotEmpty())
                    Victim.GetRelations(Us).DamageRelationship(Victim, Us, DamageReason, RelationDamage, null);
            }
        }
    }

    public enum AgentMission
    {
        Defending,
        Training,
        Infiltrate,
        Assassinate,
        Sabotage,
        StealTech,
        Robbery,
        InciteRebellion,
        Undercover,
        Recovering
    }
}
