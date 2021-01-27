using NAudio.Wave;
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
            ResourceManager.AgentMissionData.Initialize(mission, out int index, out int turns, out int cost);
            if (cost > 0 && cost > owner.Money)
                return; // Do not go into negative money, cost > 0 check is for 0 mission cost which can be done in negative

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
            owner.AddMoney(-cost);
            owner.GetEmpireAI().DeductSpyBudget(cost);

            Mission          = mission;
            TargetEmpire     = targetEmpire;
            MissionNameIndex = index;
            TurnsRemaining   = turns;
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
            MissionResolve aftermath = new MissionResolve(us, victim);
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
                    AssassinateEnemyAgent(us, victim, out string targetNameGood);
                    aftermath.MessageToVictim = $"{Localizer.Token(6037)} {targetNameGood}, {Localizer.Token(6041)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageId       = 6047; // Foiled but escaped
                    aftermath.MessageToVictim = $"{Localizer.Token(6043)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6044; // Injured
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6043)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 15;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6046; // Died
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6045)} {Localizer.Token(6049)} {us.data.Traits.Name}";
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
                    aftermath.MessageId  = 6030;
                    aftermath.GoodResult = true;
                    Infiltrations++;
                    InfiltratePlanet(us, victim, out string planetName);
                    AssignMission(AgentMission.Undercover, us, victim.data.Traits.Name);
                    aftermath.CustomMessage = $"{Name}, {Localizer.Token(6030)} {planetName} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageId       = 6036;
                    aftermath.MessageToVictim = $"{Localizer.Token(6033)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage = 10;
                    aftermath.DamageReason   = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6032;
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6033)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6034;
                    aftermath.AgentKilled     = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6035)} {Localizer.Token(6049)} {us.data.Traits.Name}";
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
                    aftermath.MessageToVictim   = $"{Localizer.Token(6048)}  {targetPlanet.Name}";
                    aftermath.CustomMessage     = $"{Name} {Localizer.Token(6084)} {crippledTurns} {Localizer.Token(6085)} " +
                                                  $"{targetPlanet.Name} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    Sabotages++;
                    crippledTurns               = 5 + Level*3;
                    targetPlanet.CrippledTurns += crippledTurns;
                    aftermath.MessageToVictim   = $"{Localizer.Token(6048)}  {targetPlanet.Name} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage     = $"{Name} {Localizer.Token(6084)} {crippledTurns} {Localizer.Token(6085)} " +
                                                  $"{targetPlanet.Name} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageToVictim = $"{Localizer.Token(6051)}  {targetPlanet.Name} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6055)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.AgentInjured    = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6051)}  {targetPlanet.Name} {Localizer.Token(6049)} {us.data.Traits.Name}";
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
            MissionResolve aftermath = new MissionResolve(us, victim);
            if (victim == null || victim.Money <= 0)
            {
                aftermath.CustomMessage = $"Name  {Localizer.Token(6066)} {TargetEmpire} {Localizer.Token(6067)}";
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
                    aftermath.MessageToVictim = $"{amount} {Localizer.Token(6070)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6068)} {amount} {Localizer.Token(6069)} {TargetEmpire}";

                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageId       = 6075;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(6071)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6071;
                    aftermath.MessageToVictim = $"{Localizer.Token(6073)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 15;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageToVictim = $"{Localizer.Token(6073)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6074)} {TargetEmpire}";
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
                    aftermath.MessageToVictim = $"{Localizer.Token(6078)} {targetPlanet.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6077)} {targetPlanet.Name} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Success:
                    aftermath.GoodResult = true;
                    Rebellions++;
                    AddRebellion(victim, targetPlanet, Level);
                    aftermath.MessageToVictim = $"{Localizer.Token(6078)} {targetPlanet.Name} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6077)} {targetPlanet.Name} {Localizer.Token(6079)}";
                    aftermath.RelationDamage  = 25;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageToVictim = $"{Localizer.Token(6076)} {targetPlanet.Name} { Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6083)} {targetPlanet.Name}";
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(6076)} {targetPlanet.Name} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    aftermath.MessageToVictim = $"{Localizer.Token(6081)} {targetPlanet.Name} {Localizer.Token(6049)} {us.data.Traits.Name}";
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
                aftermath.MessageId = 6063;
                aftermath.ShouldAddXp = false;
                return aftermath;
            }

            string stolenTech     = potentialTechs.RandItem().UID;
            string stolenTechName = Localizer.Token(ResourceManager.TechTree[stolenTech].NameIndex);

            switch (missionStatus)
            {
                case SpyMissionStatus.GreatSuccess:
                    us.AcquireTech(stolenTech, victim, TechUnlockType.Spy);
                    TechStolen++;
                    aftermath.GoodResult      = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6056)}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6057)} {stolenTechName} {Localizer.Token(6031)}";
                    break;
                case SpyMissionStatus.Success:
                    us.AcquireTech(stolenTech, victim, TechUnlockType.Spy);
                    TechStolen++;
                    aftermath.GoodResult = true;
                    aftermath.MessageToVictim = $"{Localizer.Token(6058)} {stolenTechName} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    aftermath.CustomMessage   = $"{Name} {Localizer.Token(6057)} {stolenTechName} {Localizer.Token(6042)}";
                    break;
                case SpyMissionStatus.Failed:
                    aftermath.MessageId       = 6062;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(6059)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedBadly:
                    aftermath.MessageId       = 6050;
                    aftermath.AgentInjured    = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying";
                    aftermath.MessageToVictim = $"{Localizer.Token(6059)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
                case SpyMissionStatus.FailedCritically:
                    aftermath.MessageId       = 6061;
                    aftermath.AgentKilled     = true;
                    aftermath.RelationDamage  = 20;
                    aftermath.DamageReason    = "Caught Spying Failed";
                    aftermath.MessageToVictim = $"{Localizer.Token(6060)} {Localizer.Token(6049)} {us.data.Traits.Name}";
                    break;
            }

            return aftermath;
        }

            MissionResolve ResolveRecovery(Empire us)
        {
            MissionResolve aftermath = new MissionResolve(us, null) {MessageId = 6086};

            Mission         = PrevisousMission;
            TargetEmpire    = PreviousTarget;
            return aftermath;
        }

        public void ExecuteMission(Empire us)
        {
            AgentMissionData data        = ResourceManager.AgentMissionData;
            spyMute                      = us.data.SpyMute;
            Empire victim                = EmpireManager.GetEmpireByName(TargetEmpire);

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
                        string message = $"{Name} {Localizer.Token(6087)}";
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
            public int MessageId;
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
                MessageId       = 0;
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