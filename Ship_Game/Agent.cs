using Ship_Game.Gameplay;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;


namespace Ship_Game
{
	public sealed class Agent
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

		public Agent()
		{
		}
       
		public void AssignMission(AgentMission mission, Empire Owner, string empname)
		{
			this.Initialize(mission, Owner);
			if (this.Mission == AgentMission.Undercover)
			{
				foreach (Mole m in Owner.data.MoleList)
				{
					if (m.PlanetGuid != this.TargetGUID)
					{
						continue;
					}
					Owner.data.MoleList.QueuePendingRemoval(m);
					break;
				}
			}
			Owner.data.MoleList.ApplyPendingRemovals();
			this.Mission = mission;
			this.TargetEmpire = empname;
		}

        //added by gremlin Domission from devek mod.
        public void DoMission(Empire Owner)
        {
            spyMute = Owner.data.SpyMute;
            Planet target;
            Empire Target = EmpireManager.GetEmpireByName(this.TargetEmpire);
            AgentMission startingmission = this.Mission;
            #region EmpireDefeated
            if (Target != null && Target.data.Defeated)
            {
                this.Mission = AgentMission.Defending;
                this.MissionNameIndex = 2183;
                return;
            }
            #endregion
            #region New DiceRoll
            float DiceRoll = RandomMath.RandomBetween(this.Level * ResourceManager.AgentMissionData.MinRollPerLevel, ResourceManager.AgentMissionData.MaxRoll);
            float DefensiveRoll = 0f;
            DiceRoll += (float)this.Level * RandomMath.RandomBetween(1f, ResourceManager.AgentMissionData.RandomLevelBonus);
            DiceRoll += Owner.data.SpyModifier;
            DiceRoll += Owner.data.OffensiveSpyBonus;
            if (Target != null)
            {
                for (int i = 0; i < Target.data.AgentList.Count; i++)
                {
                    if (Target.data.AgentList[i].Mission == AgentMission.Defending)
                    {
                        DefensiveRoll += (float)Target.data.AgentList[i].Level * ResourceManager.AgentMissionData.DefenceLevelBonus;
                    }
                }
                DefensiveRoll /= Owner.GetPlanets().Count / 3 + 1;
                DefensiveRoll += Target.data.SpyModifier;
                DefensiveRoll += Target.data.DefensiveSpyBonus;

                DiceRoll -= DefensiveRoll;
            }
            #endregion
            switch (this.Mission)
            {
                #region Training
                case AgentMission.Training:
                {
                    this.Mission = AgentMission.Defending;
                    this.MissionNameIndex = 2183;
                    if (DiceRoll >= ResourceManager.AgentMissionData.TrainingRollPerfect)
                    {
                        //Added by McShooterz
                        this.AddExperience(ResourceManager.AgentMissionData.TrainingExpPerfect, Owner);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6025)), Owner);
                        this.Training++;
                        break;
                    }
                    else if (DiceRoll > ResourceManager.AgentMissionData.TrainingRollGood)
                    {
                        //Added by McShooterz
                        this.AddExperience(ResourceManager.AgentMissionData.TrainingExpGood, Owner);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6026)), Owner);
                        this.Training++;
                        break;
                    }
                    else if (DiceRoll < ResourceManager.AgentMissionData.TrainingRollBad)
                    {
                        if (DiceRoll >= ResourceManager.AgentMissionData.TrainingRollWorst)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6027)), Owner);
                            this.AssignMission(AgentMission.Recovering, Owner, "");
                            this.PrevisousMission = AgentMission.Training;
                            break;
                        }
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6028)), Owner);
                        Owner.data.AgentList.QueuePendingRemoval(this);
                        break;
                    }
                    else
                    {
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6029)), Owner);
                        break;
                    }
                }
                #endregion
                #region Infiltrate easy
                case AgentMission.Infiltrate:
                    {
                        if (Target == null || Target.GetPlanets().Count == 0)
                        {
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            return;
                        }
                        if (DiceRoll >= ResourceManager.AgentMissionData.InfiltrateRollGood)
                        {
                            this.Mission = AgentMission.Undercover;
                            this.MissionNameIndex = 2201;
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.InfiltrateExpGood, Owner);
                            Mole m = Mole.PlantMole(Owner, Target);
                            this.TargetGUID = m.PlanetGuid;
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6030), " ", Empire.Universe.PlanetsDict[m.PlanetGuid].Name, Localizer.Token(6031)), Owner);
                            this.Infiltrations++;
                            break;
                        }
                        else if (DiceRoll < ResourceManager.AgentMissionData.InfiltrateRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.InfiltrateRollWorst)
                            {
                                this.Mission = AgentMission.Defending;
                                this.MissionNameIndex = 2183;
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6032)), Owner);
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6033), " ", Owner.data.Traits.Name), Target);
                                }
                                this.AssignMission(AgentMission.Recovering, Owner, "");
                                this.PrevisousMission = AgentMission.Infiltrate;
                                this.PreviousTarget = this.TargetEmpire;
                                break;
                            }
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6034)), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            if (Target != EmpireManager.Player)
                            {
                                break;
                            }

                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6035), " ", Owner.data.Traits.Name), Target);
                            break;
                        }
                        else
                        {
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.InfiltrateExp, Owner);
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6036)), Owner);
                            if (Target != EmpireManager.Player)
                            {
                                break;
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6033), " ", Owner.data.Traits.Name), Target);
                            break;
                        }
                    }
                #endregion
                #region Assassinate hard
                case AgentMission.Assassinate:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target == null || Target.data.AgentList.Count == 0)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6038)), Owner);
                            return;
                        }
                        if (DiceRoll >= ResourceManager.AgentMissionData.AssassinateRollPerfect)
                        {
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.AssassinateExpPerfect, Owner);
                            Agent m = Target.data.AgentList[RandomMath.InRange(Target.data.AgentList.Count)];
                            Target.data.AgentList.Remove(m);
                            if (m.Mission == AgentMission.Undercover)
                            {
                                foreach (Mole mole in Owner.data.MoleList)
                                {
                                    if (mole.PlanetGuid != m.TargetGUID)
                                    {
                                        continue;
                                    }
                                    Owner.data.MoleList.QueuePendingRemoval(mole);
                                    break;
                                }
                            }
                            Owner.data.MoleList.ApplyPendingRemovals();
                            if (Target == EmpireManager.Player)
                            {
                                //if (!GremlinAgentComponent.AutoTrain) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was mysteriously assassinated: ", m.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6039), " ", m.Name, Localizer.Token(6040)), Owner);
                            this.Assassinations++;
                            break;
                        }
                        else if (DiceRoll >= ResourceManager.AgentMissionData.AssassinateRollGood)
                        {
                            Agent m = Target.data.AgentList[RandomMath.InRange(Target.data.AgentList.Count)];
                            Target.data.AgentList.Remove(m);
                            if (m.Mission == AgentMission.Undercover)
                            {
                                foreach (Mole mole in Owner.data.MoleList)
                                {
                                    if (mole.PlanetGuid != m.TargetGUID)
                                    {
                                        continue;
                                    }
                                    Owner.data.MoleList.QueuePendingRemoval(mole);
                                    break;
                                }
                            }
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.AssassinateExpGood, Owner);
                            Owner.data.MoleList.ApplyPendingRemovals();
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6037), " ", m.Name, Localizer.Token(6041), " ", Owner.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6039), " ", m.Name, Localizer.Token(6042)), Owner);
                            this.Assassinations++;
                            break;
                        }
                        else if (DiceRoll < ResourceManager.AgentMissionData.AssassinateRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.AssassinateRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6043), " ", Owner.data.Traits.Name), Target);
                                }
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6044)), Owner);
                                this.AssignMission(AgentMission.Recovering, Owner, "");
                                this.PrevisousMission = AgentMission.Assassinate;
                                this.PreviousTarget = this.TargetEmpire;
                                break;
                            }
                            this.Mission = AgentMission.Defending;
                            this.MissionNameIndex = 2183;
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6045), " ", Owner.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6046)), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.AssassinateExp, Owner);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6043), " ", Owner.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6047)), Owner);
                            break;
                        }
                    }
                #endregion
                #region Sabotage easy
                case AgentMission.Sabotage:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target == null || Target.NumPlanets == 0)
                        {
                            return;
                        }
                        Empire targetEmpire = EmpireManager.GetEmpireByName(TargetEmpire);
                        target = targetEmpire.GetPlanets()[RandomMath.InRange(targetEmpire.NumPlanets)];
                        this.TargetGUID = target.guid;
                        if (DiceRoll >= ResourceManager.AgentMissionData.SabotageRollPerfect)
                        {
                            Planet crippledTurns = target;
                            crippledTurns.Crippled_Turns = crippledTurns.Crippled_Turns + 5 + this.Level * 5;
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6048), " ", target.Name), Target);
                            }
                            NotificationManager notificationManager = Empire.Universe.NotificationManager;
                            string[] name = new string[] { this.Name, " " + Localizer.Token(6084) + " ", null, null, null, null };
                            int num = 5 + this.Level * 5;
                            name[2] = num.ToString();
                            name[3] = " " + Localizer.Token(6085) + " ";
                            name[4] = target.Name;
                            name[5] = Localizer.Token(6031);
                            if (!spyMute) notificationManager.AddAgentResultNotification(true, string.Concat(name), Owner);
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.SabotageExpPerfect, Owner);
                            this.Sabotages++;
                            break;
                        }
                        else if (DiceRoll > ResourceManager.AgentMissionData.SabotageRollGood)
                        {
                            Planet planet = target;
                            planet.Crippled_Turns = planet.Crippled_Turns + 5 + this.Level * 3;
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6048), " ", target.Name, Localizer.Token(6049),  " ", Owner.data.Traits.Name), Target);
                            }
                            NotificationManager notificationManager1 = Empire.Universe.NotificationManager;
                            string[] str = new string[] { this.Name, " " + Localizer.Token(6084) + " ", null, null, null, null };
                            int num1 = 5 + this.Level * 3;
                            str[2] = num1.ToString();
                            str[3] = " " + Localizer.Token(6085) + " ";
                            str[4] = target.Name;
                            str[5] = Localizer.Token(6031);
                            if (!spyMute) notificationManager1.AddAgentResultNotification(true, string.Concat(str), Owner);
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.SabotageExpGood, Owner);
                            this.Sabotages++;
                            break;
                        }
                        else if (DiceRoll < ResourceManager.AgentMissionData.SabotageRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.SabotageRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6051), " ", target.Name, Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6052), " ", target.Name), Owner);
                                this.AssignMission(AgentMission.Recovering, Owner, "");
                                this.PrevisousMission = AgentMission.Sabotage;
                                this.PreviousTarget = this.TargetEmpire;
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!Owner.data.SpyMissionRepeat) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6053), " ", target.Name, Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6054)), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.SabotageExp, Owner);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6051)," ", target.Name, Localizer.Token(6049)," ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6055), " ", target.Name), Owner);
                            break;
                        }
                    }
                #endregion
                #region StealTech hard
                case AgentMission.StealTech:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target == null)
                            return;
                        Array<string> PotentialUIDs = new Array<string>();
                        foreach (KeyValuePair<string, TechEntry> entry in Target.GetTDict())
                        {
                            //Added by McShooterz: Root nodes cannot be stolen
                            if (!entry.Value.Unlocked || !Owner.HavePreReq(entry.Value.UID) || Owner.GetTDict()[entry.Value.UID].Unlocked || entry.Value.Tech.RootNode == 1)
                            {
                                continue;
                            }
                            PotentialUIDs.Add(entry.Value.UID);
                        }
                        string theUID = "";
                        if (PotentialUIDs.Count != 0)
                        {
                            theUID = PotentialUIDs[RandomMath.InRange(PotentialUIDs.Count)];
                            if (DiceRoll >= ResourceManager.AgentMissionData.StealTechRollPerfect)
                            {
                                //Added by McShooterz
                                this.AddExperience(ResourceManager.AgentMissionData.StealTechExpPerfect, Owner);
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, Localizer.Token(6056), Target);
                                }
                                //Added by McShooterz: new acquire method, unlocks targets bonuses as well
                                //Owner.UnlockTech(theUID);
                                Owner.AcquireTech(theUID, Target);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6057), " ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), Localizer.Token(6031)), Owner);
                                this.TechStolen++;
                                break;
                            }
                            else if (DiceRoll > ResourceManager.AgentMissionData.StealTechRollGood)
                            {
                                //Added by McShooterz
                                this.AddExperience(ResourceManager.AgentMissionData.StealTechExpGood, Owner);
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6058), " ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                }
                                //Added by McShooterz: new acquire method, unlocks targets bonuses as well
                                //Owner.UnlockTech(theUID);
                                Owner.AcquireTech(theUID, Target);
                                Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6057), " ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), Localizer.Token(6042)), Owner);
                                this.TechStolen++;
                                break;
                            }
                            else if (DiceRoll < ResourceManager.AgentMissionData.StealTechRollBad)
                            {
                                if (DiceRoll >= ResourceManager.AgentMissionData.StealTechRollWorst)
                                {
                                    if (Target == EmpireManager.Player)
                                    {
                                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6059), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                    }
                                    Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6050)), Owner);
                                    this.AssignMission(AgentMission.Recovering, Owner, "");
                                    this.PrevisousMission = AgentMission.StealTech;
                                    this.PreviousTarget = this.TargetEmpire;
                                    break;
                                }
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6060), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6061)), Owner);
                                Owner.data.AgentList.QueuePendingRemoval(this);
                                break;
                            }
                            else
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6059), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6062)), Owner);
                                break;
                            }
                        }
                        else
                        {
                            this.AddExperience(ResourceManager.AgentMissionData.StealTechExp, Owner);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6063), " ", (ResourceManager.AgentMissionData.StealTechCost / 2).ToString(), " ", Localizer.Token(6064)), Owner);
                            Empire owner = Owner;
                            owner.Money += ResourceManager.AgentMissionData.StealTechCost / 2;
                            break;
                        }
                    }
                #endregion
                #region Robbery
                case AgentMission.Robbery:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target == null)
                            return;
                        int amount = (int)(RandomMath.RandomBetween(1f, (float)Target.GetPlanets().Count * 10f) * (float)this.Level);
                        if ((float)amount > Target.Money && Target.Money > 0f)
                        {
                            amount = (int)Target.Money;
                        }
                        else if (Target.Money <= 0f)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6066), " ", this.TargetEmpire, Localizer.Token(6067)), Owner);
                            return;
                        }
                        if (DiceRoll >= ResourceManager.AgentMissionData.RobberyRollPerfect)
                        {
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.RobberyExpPerfect, Owner);
                            Empire money = Target;
                            money.Money = money.Money - (float)amount;
                            Empire empire = Owner;
                            empire.Money = empire.Money + (float)amount;
                            NotificationManager notificationManager2 = Empire.Universe.NotificationManager;
                            object[] objArray = new object[] { this.Name, " ", Localizer.Token(6068), " ", amount, " ", Localizer.Token(6069), " ", this.TargetEmpire, Localizer.Token(6031) };
                            if (!spyMute) notificationManager2.AddAgentResultNotification(true, string.Concat(objArray), Owner);
                            if (Target != EmpireManager.Player)
                            {
                                break;
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " ", Localizer.Token(6065)), Target);
                            this.Robberies++;
                            break;
                        }
                        else if (DiceRoll > ResourceManager.AgentMissionData.RobberyRollGood)
                        {
                            //Added by McShooterz
                            this.AddExperience(ResourceManager.AgentMissionData.RobberyExpGood, Owner);
                            Empire money1 = Target;
                            money1.Money = money1.Money - (float)amount;
                            Empire owner1 = Owner;
                            owner1.Money = owner1.Money + (float)amount;
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " ", Localizer.Token(6070), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            NotificationManager notificationManager3 = Empire.Universe.NotificationManager;
                            object[] name1 = new object[] { this.Name, " ", Localizer.Token(6068), " ", amount, " ", Localizer.Token(6069), " ", this.TargetEmpire, Localizer.Token(6042) };
                            if (!spyMute) notificationManager3.AddAgentResultNotification(true, string.Concat(name1), Owner);
                            this.Robberies++;
                            break;
                        }
                        else if (DiceRoll < ResourceManager.AgentMissionData.RobberyRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.RobberyRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!Owner.data.SpyMissionRepeat && !spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6071), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                if (!spyMute) if (!Owner.data.SpyMissionRepeat) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6072)), Owner);
                                this.AssignMission(AgentMission.Recovering, Owner, "");
                                this.PrevisousMission = AgentMission.Robbery;
                                this.PreviousTarget = this.TargetEmpire;
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6073), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6074), " ", this.TargetEmpire), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            this.AddExperience(ResourceManager.AgentMissionData.RobberyExp, Owner);
                            if (Target == EmpireManager.Player)
                            {
                                if (!Owner.data.SpyMissionRepeat && !spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6071), Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) if (!Owner.data.SpyMissionRepeat) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6075)), Owner);
                            break;
                        }
                    }
                #endregion
                #region Rebellion
                case AgentMission.InciteRebellion:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target == null)
                            return;
                        if (Target.GetPlanets().Count == 0)
                        {
                            return;
                        }
                        Empire targetEmpire = EmpireManager.GetEmpireByName(TargetEmpire);
                        target = targetEmpire.GetPlanets()[RandomMath.InRange(targetEmpire.GetPlanets().Count)];
                        if (DiceRoll >= ResourceManager.AgentMissionData.RebellionRollPerfect)
                        {
                            AddExperience(ResourceManager.AgentMissionData.RebellionExpPerfect, Owner);
                            if (!targetEmpire.data.RebellionLaunched)
                            {
                                Empire rebels = CreatingNewGameScreen.CreateRebelsFromEmpireData(targetEmpire.data, targetEmpire);
                                rebels.data.IsRebelFaction  = true;
                                rebels.data.Traits.Name     = targetEmpire.data.RebelName;
                                rebels.data.Traits.Singular = targetEmpire.data.RebelSing;
                                rebels.data.Traits.Plural   = targetEmpire.data.RebelPlur;
                                rebels.isFaction = true;
                                foreach (Empire e in EmpireManager.Empires)
                                {
                                    e.AddRelation(rebels);
                                    rebels.AddRelation(e);
                                }
                                EmpireManager.Add(rebels);
                                targetEmpire.data.RebellionLaunched = true;
                            }
                            Empire darebels = EmpireManager.GetEmpireByName(targetEmpire.data.RebelName);
                            for (int i = 0; i < 4; i++)
                            {
                                foreach (var troop in ResourceManager.TroopsDict)
                                {
                                    if (!targetEmpire.WeCanBuildTroop(troop.Key))
                                    {
                                        continue;
                                    }
                                    Troop t = ResourceManager.CreateTroop(troop.Value, darebels);
                                    t.Name = Localizer.Token(darebels.data.TroopNameIndex);
                                    t.Description = Localizer.Token(darebels.data.TroopDescriptionIndex);
                                    target.AssignTroopToTile(t);
                                    break;
                                }
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6078), " ", target.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6077), " ", target.Name, Localizer.Token(6031)), Owner);
                            this.Rebellions++;
                            break;
                        }
                        else if (DiceRoll > ResourceManager.AgentMissionData.RebellionRollGood)
                        {
                            this.AddExperience(ResourceManager.AgentMissionData.RebellionExpGood, Owner);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6078), " ", target.Name, Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6077), " ", target.Name, Localizer.Token(6079)), Owner);
                            this.Rebellions++;
                            break;
                        }
                        else if (DiceRoll < ResourceManager.AgentMissionData.RebellionRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.RebellionRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6076), " ", target.Name, Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                                }
                                Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6080), " ", target.Name), Owner);
                                this.AssignMission(AgentMission.Recovering, Owner, "");
                                this.PrevisousMission = AgentMission.InciteRebellion;
                                this.PreviousTarget = this.TargetEmpire;
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6081), " ", target.Name, Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6082), " ", target.Name), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            this.AddExperience(ResourceManager.AgentMissionData.RebellionExp, Owner);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6076), " ", target.Name, Localizer.Token(6049), " ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " ", Localizer.Token(6083), " ", target.Name), Owner);
                            break;
                        }
                    }
                #endregion
                #region Recovery
                case AgentMission.Recovering :
                        {
                            this.Mission = AgentMission.Defending;
                            startingmission = this.PrevisousMission;
                            this.TargetEmpire = this.PreviousTarget;
                            this.MissionNameIndex = 2183;
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6086)), Owner);
                            break;
                        }
                #endregion
            }
            #region Mission Repeat
            if (Owner == EmpireManager.Player 
                && Mission == AgentMission.Defending && Owner.Money > 500
                && Owner.data.SpyMissionRepeat == true 
                && (startingmission != AgentMission.Training || (startingmission == AgentMission.Training && this.Level < 10)))
            {
                this.AssignMission(startingmission, Owner, this.TargetEmpire);
                return;
            }
            this.TargetEmpire = "";
            #endregion
        }

		public bool Initialize(AgentMission TheMission, Empire Owner)
		{
            float spyBudget =0;// = Owner.GetGSAI().spyBudget;
            if (Owner.isPlayer)
                Owner.GetGSAI().spyBudget = Owner.Money;
            bool returnvalue = false;            
            switch (TheMission)
			{
				case AgentMission.Training:
				{
                    if (Owner.GetGSAI().spyBudget >= ResourceManager.AgentMissionData.TrainingCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.TrainingTurns;
                        spyBudget -= ResourceManager.AgentMissionData.TrainingCost;
                        this.MissionNameIndex = 2196;
                        returnvalue = true;
                    }
					break;
				}
				case AgentMission.Infiltrate:
				{
                    if (Owner.GetGSAI().spyBudget >= ResourceManager.AgentMissionData.InfiltrateCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.InfiltrateTurns;
                        spyBudget -= ResourceManager.AgentMissionData.InfiltrateCost;
                        this.MissionNameIndex = 2188;
                        returnvalue = true;
                    }
                    break;
				}
				case AgentMission.Assassinate:
				{
                    if (Owner.GetGSAI().spyBudget >= ResourceManager.AgentMissionData.AssassinateCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.AssassinateTurns;
                        spyBudget -= ResourceManager.AgentMissionData.AssassinateCost;
                        this.MissionNameIndex = 2184;
                        returnvalue = true;
                    }
                    break;
				}
				case AgentMission.Sabotage:
				{
                    if (Owner.GetGSAI().spyBudget > ResourceManager.AgentMissionData.SabotageCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.SabotageTurns;
                        spyBudget -= ResourceManager.AgentMissionData.SabotageCost;
                        this.MissionNameIndex = 2190;
                        returnvalue = true;
                    }
                    break;
				}
				case AgentMission.StealTech:
				{
                    if (Owner.GetGSAI().spyBudget >= ResourceManager.AgentMissionData.StealTechCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.StealTechTurns;
                        spyBudget -= ResourceManager.AgentMissionData.StealTechCost;
                        this.MissionNameIndex = 2194;
                        returnvalue = true;
                    }
                    break;
				}
				case AgentMission.Robbery:
				{
                    if (Owner.GetGSAI().spyBudget >= ResourceManager.AgentMissionData.RobberyCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.RobberyTurns;

                        spyBudget -= ResourceManager.AgentMissionData.RobberyCost;
                        this.MissionNameIndex = 2192;
                        returnvalue = true;
                    }
                    break;
				}
				case AgentMission.InciteRebellion:
				{
                    if (Owner.GetGSAI().spyBudget >= ResourceManager.AgentMissionData.RebellionCost)
                    {
                        this.TurnsRemaining = ResourceManager.AgentMissionData.RebellionTurns;

                        spyBudget -= ResourceManager.AgentMissionData.RebellionCost;
                        this.MissionNameIndex = 2186;
                        returnvalue = true;
                    }
                    break;
				}
                case AgentMission.Recovering:
                {
                    this.TurnsRemaining = ResourceManager.AgentMissionData.RecoveringTurns;
                    this.MissionNameIndex = 6024;
                    return true;
                }
				default:
				{
					return false;
				}
                 
			}
            
            
            if (Owner.isPlayer)
                Owner.Money+=spyBudget;
            else
            {
                Owner.GetGSAI().spyBudget += spyBudget;
            }
            return returnvalue;
		}

        //Added by McShooterz: add experience to the agent and determine if level up.
        private void AddExperience(int exp, Empire Owner)
        {
            this.Experience += exp;
            while (this.Experience >= ResourceManager.AgentMissionData.ExpPerLevel * this.Level)
            {
                this.Experience -=  ResourceManager.AgentMissionData.ExpPerLevel * this.Level;
                if (this.Level < 10)
                {
                    this.Level++;
                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " ", Localizer.Token(6087)), Owner);
                }
            }
        }
	}
}

#region Unused
/*
public void DoMissionorig(Empire Owner)
		{
			Planet target;
			Empire Target = EmpireManager.GetEmpireByName(this.TargetEmpire);
			if (Target != null && Target.data.Defeated)
			{
				this.Mission = AgentMission.Defending;
				this.MissionNameIndex = 2183;
				return;
			}
			float DiceRoll = RandomMath.RandomBetween(0f, 100f);
			if (DiceRoll > 97.5f)
			{
				DiceRoll = DiceRoll + 100f;
			}
			DiceRoll = DiceRoll + Owner.data.SpyModifier;
			DiceRoll = DiceRoll + (float)this.Level * RandomMath.RandomBetween(1f, 5f);
			DiceRoll = DiceRoll + Owner.data.OffensiveSpyBonus;
			float DefensiveRoll = 0f;
			if (Target != null)
			{
				for (int i = 0; i < Target.data.AgentList.Count; i++)
				{
					if (Target.data.AgentList[i].Mission == AgentMission.Defending)
					{
						float Roll = 1f + (float)Target.data.AgentList[i].Level * RandomMath.RandomBetween(0f, 3f);
						if (Roll > DefensiveRoll)
						{
							DefensiveRoll = Roll;
						}
					}
				}
				DefensiveRoll = DefensiveRoll + Target.data.SpyModifier;
				DefensiveRoll = DefensiveRoll + Target.data.DefensiveSpyBonus;
			}
			switch (this.Mission)
			{
				case AgentMission.Training:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (DiceRoll >= 95f)
					{
						Agent level = this;
						level.Level = level.Level + 2;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has successfully complete training\nThe Agent's brilliance results in a gain of +2 levels."), Owner);
						break;
					}
					else if (DiceRoll > 25f)
					{
						Agent agent = this;
						agent.Level = agent.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " has successfully completed training\nand has gained +1 level."), Owner);
						break;
					}
					else if (DiceRoll < 10f)
					{
						if (DiceRoll >= 10f)
						{
							break;
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed in a training accident."), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " has completed training, but failed to learn anything useful."), Owner);
						break;
					}
				}
				case AgentMission.Infiltrate:
				{
					if (Target.GetPlanets().Count == 0)
					{
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						return;
					}
					if (DiceRoll >= 50f)
					{
						this.Mission = AgentMission.Undercover;
						this.MissionNameIndex = 2201;
						Agent level1 = this;
						level1.Level = level1.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Mole m = Mole.PlantMole(Owner, Target);
						this.TargetGUID = m.PlanetGuid;
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " successfully infiltrated a colony: ", Empire.Universe.PlanetsDict[m.PlanetGuid].Name, "\nThe Agent was not detected and gains +1 level"), Owner);
						break;
					}
					else if (DiceRoll < 25f)
					{
						if (DiceRoll >= 25f)
						{
							break;
						}
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to infiltrate a colony"), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						if (Target != EmpireManager.Player)
						{
							break;
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was killed trying to infiltrate one of our colonies\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						break;
					}
					else
					{
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " was unable to infiltrate a colony\nand had to abort the mission to avoid capture"), Owner);
						if (Target != EmpireManager.Player)
						{
							break;
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was foiled trying to infiltrate a colony\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						break;
					}
				}
				case AgentMission.Assassinate:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (Target.data.AgentList.Count == 0)
					{
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " could not assassinate an enemy Agent \nbecause target empire has no Agents"), Owner);
						return;
					}
					if (DiceRoll >= 85f)
					{
						Agent agent1 = this;
						agent1.Level = agent1.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Agent m = Target.data.AgentList[RandomMath.InRange(Target.data.AgentList.Count)];
						Target.data.AgentList.Remove(m);
						if (m.Mission == AgentMission.Undercover)
						{
							foreach (Mole mole in Owner.data.MoleList)
							{
								if (mole.PlanetGuid != m.TargetGUID)
								{
									continue;
								}
								Owner.data.MoleList.QueuePendingRemoval(mole);
								break;
							}
						}
						Owner.data.MoleList.ApplyPendingRemovals();
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was mysteriously assassinated: ", m.Name), Target);
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " assassinated an enemy Agent: ", m.Name, "\nOur agent escaped unharmed and undetected, gaining + 1 level"), Owner);
						break;
					}
					else if (DiceRoll >= 70f)
					{
						Agent m = Target.data.AgentList[RandomMath.InRange(Target.data.AgentList.Count)];
						Target.data.AgentList.Remove(m);
						if (m.Mission == AgentMission.Undercover)
						{
							foreach (Mole mole in Owner.data.MoleList)
							{
								if (mole.PlanetGuid != m.TargetGUID)
								{
									continue;
								}
								Owner.data.MoleList.QueuePendingRemoval(mole);
								break;
							}
						}
						Owner.data.MoleList.ApplyPendingRemovals();
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was assassinated: ", m.Name, "\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " assassinated an enemy Agent: ", m.Name, "\nOur agent was detected but escaped, gaining + 1 level"), Owner);
						break;
					}
					else if (DiceRoll < 25f)
					{
						if (DiceRoll >= 25f)
						{
							break;
						}
						this.Mission = AgentMission.Defending;
						this.MissionNameIndex = 2183;
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("An enemy Agent was killed during an assassination attempt\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to assassinate an enemy agent"), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We managed to detect an enemy Assassin before it could strike\nThe Assassin was sent by ", Owner.data.Traits.Name), Target);
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " was foiled trying to assassinate an enemy agent, but managed to escape"), Owner);
						break;
					}
				}
				case AgentMission.Sabotage:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (Target.GetPlanets().Count == 0)
					{
						return;
					}
					target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[RandomMath.InRange(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
					this.TargetGUID = target.guid;
					if (DiceRoll >= 80f)
					{
						Planet crippledTurns = target;
						crippledTurns.Crippled_Turns = crippledTurns.Crippled_Turns + 5 + this.Level * 5;
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has sabotaged production on ", target.Name), Target);
						}
						NotificationManager notificationManager = Empire.Universe.NotificationManager;
						string[] name = new string[] { this.Name, " sabotaged production for ", null, null, null, null };
						int num = 5 + this.Level * 5;
						name[2] = num.ToString();
						name[3] = " turns: ";
						name[4] = target.Name;
						name[5] = "\nThe Agent was not detected and gains +1 level";
						notificationManager.AddAgentResultNotification(true, string.Concat(name), Owner);
						Agent level2 = this;
						level2.Level = level2.Level + 1;
						if (this.Level <= 10)
						{
							break;
						}
						this.Level = 10;
						break;
					}
					else if (DiceRoll > 50f)
					{
						Planet planet = target;
						planet.Crippled_Turns = planet.Crippled_Turns + 5 + this.Level * 3;
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has sabotaged production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						NotificationManager notificationManager1 = Empire.Universe.NotificationManager;
						string[] str = new string[] { this.Name, " sabotaged production for ", null, null, null, null };
						int num1 = 5 + this.Level * 3;
						str[2] = num1.ToString();
						str[3] = " turns: ";
						str[4] = target.Name;
						str[5] = "\nThe Agent was not detected and gains +1 level";
						notificationManager1.AddAgentResultNotification(true, string.Concat(str), Owner);
						Agent agent2 = this;
						agent2.Level = agent2.Level + 1;
						if (this.Level <= 10)
						{
							break;
						}
						this.Level = 10;
						break;
					}
					else if (DiceRoll < 15f)
					{
						if (DiceRoll >= 15f)
						{
							break;
						}
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("We killed an enemy Agent trying to sabotage production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to sabotage an enemy colony"), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy Agent trying to sabotage production on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to sabotage ", target.Name), Owner);
						break;
					}
				}
				case AgentMission.StealTech:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					Array<string> PotentialUIDs = new Array<string>();
					foreach (KeyValuePair<string, TechEntry> entry in Target.GetTDict())
					{
                        //Added by McShooterz: Racial tech cannot be stolen
                        if (!entry.Value.Unlocked || !Owner.HavePreReq(entry.Value.UID) || Owner.GetTDict()[entry.Value.UID].Unlocked || entry.Value.GetTech().RaceRestrictions.Count != 0)
						{
							continue;
						}
						PotentialUIDs.Add(entry.Value.UID);
					}
					string theUID = "";
					if (PotentialUIDs.Count != 0)
					{
						theUID = PotentialUIDs[RandomMath.InRange(PotentialUIDs.Count)];
						if (DiceRoll >= 85f)
						{
							Agent level3 = this;
							level3.Level = level3.Level + 1;
							if (this.Level > 10)
							{
								this.Level = 10;
							}
							if (Target == EmpireManager.Player)
							{
								Empire.Universe.NotificationManager.AddAgentResultNotification(false, "An enemy spy stole some technology from us \nbut we don't know who they were working for", Target);
							}
							Owner.UnlockTech(theUID);
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " stole a technology: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nThe Agent was not detected and gains +1 level"), Owner);
							break;
						}
						else if (DiceRoll > 75f)
						{
							Agent agent3 = this;
							agent3.Level = agent3.Level + 1;
							if (this.Level > 10)
							{
								this.Level = 10;
							}
							if (Target == EmpireManager.Player)
							{
								Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent stole a technology from us: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
							}
							Owner.UnlockTech(theUID);
							Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " stole a technology: ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), "\nHowever, the Agent was detected but escaped. + 1 level"), Owner);
							break;
						}
						else if (DiceRoll < 20f)
						{
							if (DiceRoll >= 20f)
							{
								break;
							}
							if (Target == EmpireManager.Player)
							{
								Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent was killed trying to steal our technology\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
							}
							Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to steal technology"), Owner);
							Owner.data.AgentList.QueuePendingRemoval(this);
							break;
						}
						else
						{
							if (Target == EmpireManager.Player)
							{
								Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to steal our technology\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
							}
							Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was detected while attempting to steal a technology\nbut managed to escape unharmed"), Owner);
							break;
						}
					}
					else
					{
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " aborted the Steal Technology mission because\nthere is nothing to steal; 125 Credits are therefore refunded"), Owner);
						Empire owner = Owner;
						owner.Money = owner.Money + 125f;
						break;
					}
				}
				case AgentMission.Robbery:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					int amount = (int)(RandomMath.RandomBetween(1f, 50f) * (float)this.Level);
					if ((float)amount > Target.Money && Target.Money > 0f)
					{
						amount = (int)Target.Money;
					}
					else if (Target.Money <= 0f)
					{
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " could not rob ", this.TargetEmpire, "\nbecause they have no money"), Owner);
						return;
					}
					if (DiceRoll >= 85f)
					{
						Agent level4 = this;
						level4.Level = level4.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Empire money = Target;
						money.Money = money.Money - (float)amount;
						Empire empire = Owner;
						empire.Money = empire.Money + (float)amount;
						NotificationManager notificationManager2 = Empire.Universe.NotificationManager;
						object[] objArray = new object[] { this.Name, " stole ", amount, " credits from ", this.TargetEmpire, "\nThe Agent was not detected and gains +1 level" };
						notificationManager2.AddAgentResultNotification(true, string.Concat(objArray), Owner);
						if (Target != EmpireManager.Player)
						{
							break;
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " credits were mysteriously stolen from our treasury.\nWe have no suspects in the theft"), Target);
						break;
					}
					else if (DiceRoll > 60f)
					{
						Agent agent4 = this;
						agent4.Level = agent4.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						Empire money1 = Target;
						money1.Money = money1.Money - (float)amount;
						Empire owner1 = Owner;
						owner1.Money = owner1.Money + (float)amount;
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " credits were stolen from our treasury by an enemy Agent.\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						NotificationManager notificationManager3 = Empire.Universe.NotificationManager;
						object[] name1 = new object[] { this.Name, " stole ", amount, " credits from ", this.TargetEmpire, "\nHowever, the Agent was detected but escaped. + 1 level" };
						notificationManager3.AddAgentResultNotification(true, string.Concat(name1), Owner);
						break;
					}
					else if (DiceRoll < 20f)
					{
						if (DiceRoll >= 20f)
						{
							break;
						}
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy Agent during an attempted robbery\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to steal credits from ", this.TargetEmpire), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to rob our treasury\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was unable to steal any credits\nbut managed to escape unharmed"), Owner);
						break;
					}
				}
				case AgentMission.InciteRebellion:
				{
					this.Mission = AgentMission.Defending;
					this.MissionNameIndex = 2183;
					if (Target.GetPlanets().Count == 0)
					{
						return;
					}
					target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[RandomMath.InRange(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
					if (DiceRoll >= 85f)
					{
						Agent level5 = this;
						level5.Level = level5.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						if (!EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched)
						{
							Empire rebels = CreatingNewGameScreen.CreateRebelsFromEmpireData(EmpireManager.GetEmpireByName(this.TargetEmpire).data, EmpireManager.GetEmpireByName(this.TargetEmpire));
							rebels.data.IsRebelFaction = true;
							rebels.data.Traits.Name = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName;
							rebels.data.Traits.Singular = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelSing;
							rebels.data.Traits.Plural = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelPlur;
							rebels.isFaction = true;
							foreach (Empire e in EmpireManager.Empires)
							{
								e.GetRelations().Add(rebels, new Relationship(rebels.data.Traits.Name));
								rebels.GetRelations().Add(e, new Relationship(e.data.Traits.Name));
							}
							EmpireManager.Empires.Add(rebels);
							EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched = true;
						}
						Empire darebels = EmpireManager.GetEmpireByName(EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName);
						for (int i = 0; i < 4; i++)
						{
							foreach (KeyValuePair<string, Troop> troop in ResourceManager.TroopsDict)
							{
								if (!EmpireManager.GetEmpireByName(this.TargetEmpire).WeCanBuildTroop(troop.Key))
								{
									continue;
								}
								Troop t = ResourceManager.CreateTroop(troop.Value, darebels);
								t.Name = Localizer.Token(darebels.data.TroopNameIndex);
								t.Description = Localizer.Token(darebels.data.TroopDescriptionIndex);
								target.AssignTroopToTile(t);
								break;
							}
						}
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name), Target);
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nThe Agent was not detected and gains +1 level"), Owner);
						break;
					}
					else if (DiceRoll > 70f)
					{
						Agent agent5 = this;
						agent5.Level = agent5.Level + 1;
						if (this.Level > 10)
						{
							this.Level = 10;
						}
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nHowever, they know we are behind it. Agent gains +1 level"), Owner);
						break;
					}
					else if (DiceRoll < 40f)
					{
						if (DiceRoll >= 40f)
						{
							break;
						}
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy agent trying to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to incite rebellion on ", target.Name), Owner);
						Owner.data.AgentList.QueuePendingRemoval(this);
						break;
					}
					else
					{
						if (Target == EmpireManager.Player)
						{
							Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
						}
						Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
						Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to incite rebellion on ", target.Name), Owner);
						break;
					}
				}
			}
			this.TargetEmpire = "";
		}
        //added by gremlin custom damage spy routine.
        public bool DamageSpy(Agent agent, Empire owner, int damage, string deathMessage, Empire target)
        {
            agent.Level = agent.Level - damage;
            if (agent.Level <= 0)
            {
                agent.Level = 0;
                if (deathMessage == "") deathMessage = string.Concat(this.Name, "was horribly wounded and is in hospital.\nDr. Gremlin thinks amputation is the only recourse");
                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, deathMessage, owner);


                return true;
            }
            Array<string> messageList = new Array<string>();
            messageList.Add(string.Concat(this.Name, " saw unspeakable things... \nThier mind has suffered...\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " Took the blue pill...\nand the red one\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " feels their pay is too little...\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " had an epic battle\nthat is too expensive to show here\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " demands more assitance next time\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " Had parts of their memory erased\n-", damage, "level"));
            messageList.Add(string.Concat(this.Name, " Spent a day thier buddy in the null prison\n-", damage, "level"));
            if (deathMessage == "") deathMessage = messageList[RandomMath.InRange(messageList.Count)];

            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, deathMessage, owner);
            return false;
        }
 * 
 * #region DefensiveRoll
            /*
            float DefensiveRoll = 0f;
            int spysOnDefense = 0;
            int spysOnDefenseLevel = 0;
            int targetPlanets = 0;
            int effectiveLevel = this.Level;
            int defensiveCoverage = 0;
            Array<Agent> defendingAgents = new Array<Agent>();

            if (Target != null)
            {
                targetPlanets = Target.GetPlanets().Count;
                IEnumerable<Agent> defendingAgentList = Target.data.AgentList.OrderByDescending(level => level.Level).Where(defending => defending.Mission == AgentMission.Defending);
                foreach (Agent defendingAgent in defendingAgentList)
                {
                    if (defendingAgent.Mission == AgentMission.Defending && spysOnDefense < targetPlanets)
                    {
                        spysOnDefense++;
                        spysOnDefenseLevel = spysOnDefenseLevel + defendingAgent.Level;
                        defendingAgents.Add(defendingAgent);
                    }
                }




                if (spysOnDefenseLevel > 0)
                {
                    for (int i = 0; i < spysOnDefenseLevel; i++)
                    {
                        DefensiveRoll = DefensiveRoll + RandomMath.RandomBetween(0f, .75f) + RandomMath.RandomBetween(0f, .75f) + RandomMath.RandomBetween(0f, .75f);
                    }

                    if (Target.data.SpyModifier > 0) DefensiveRoll = DefensiveRoll + DefensiveRoll / (100 / Target.data.SpyModifier);
                    if (Target.data.DefensiveSpyBonus > 0) DefensiveRoll = DefensiveRoll + DefensiveRoll / (100 / Target.data.DefensiveSpyBonus);
                    if (spysOnDefense > 0) defensiveCoverage = (int)DefensiveRoll / targetPlanets;

                    //if (effectiveLevel < 1) effectiveLevel = 1;
                    if (defensiveCoverage > 0)
                    {
                        effectiveLevel -= defensiveCoverage;
                        int spiesNeededfordefense = effectiveLevel;
                        if (spiesNeededfordefense < 1) spiesNeededfordefense = 1;
                        if (spiesNeededfordefense > spysOnDefense) spiesNeededfordefense = spysOnDefense;
                        for (int i = 0; i < spiesNeededfordefense; i++)//((this.Level + Target.GetPlanets().Count) / spysOnDefense) + 1; i++)
                        {
                            Agent defender = defendingAgents.Where(notTraining => notTraining.Mission == AgentMission.Defending).FirstOrDefault();

                            //Agent recovering = defendingAgents.Where(notTraining => notTraining.Mission == AgentMission.Defending).OrderByDescending(agentLevel => agentLevel.Level).FirstOrDefault();
                            //if (recovering != null) recovering.AssignMission(AgentMission.Training,Target,"");
                            if (defender == null)
                            {
                                break;
                            }
                            if (defender != null)
                            {
                                int spyBattle = (int)RandomMath.RandomBetween((float)-1 * defender.Level, (float)effectiveLevel);
                                if (spyBattle <= -5)
                                {
                                    effectiveLevel--;
                                    defender.AddExperience(2, Target);
                                    //this.Level--;
                                    //if (defender.Level < 8) defender.Level++;
                                    //defender.AssignMission(AgentMission.Training, Target, "");
                                }
                                else
                                {
                                    if (spyBattle <= -1)
                                    {
                                        //if (this.Level > defender.Level && defender.Level < 8) defender.Level++;
                                        effectiveLevel--;
                                        defender.AddExperience(1, Target);
                                        //this.Level--;
                                        //defender.AssignMission(AgentMission.Training, Target, "");
                                    }
                                }
                                if (spyBattle == 0)
                                {
                                    effectiveLevel--;
                                    //this.Level--;
                                    //if (defender.Level > 1) defender.Level--;
                                    //defender.AssignMission(AgentMission.Training, Target, "");
                                }
                                if (spyBattle >= 5)
                                {
                                    //if (defender.Level > 1) defender.Level--;
                                    this.AddExperience(1, Owner);
                                    //defender.AssignMission(AgentMission.Training, Target, "");
                                }
                                else
                                {
                                    if (spyBattle >= 1)
                                    {
                                        //if (defender.Level > 1) defender.Level--;
                                        //defender.AssignMission(AgentMission.Training, Target, "");
                                    }
                                }
                            }


                        }
                    }


                }



            }


            #endregion
 * 
 * /*
            IEnumerable<Agent> moleList = Owner.data.AgentList.Where(moles => moles.Mission == AgentMission.Undercover);


            //effectiveLevel = this.Level - defensiveCoverage;
            if (effectiveLevel <= -5) effectiveLevel = -5;
            string moleEmpire = null;
            string moleSpies = null;
            string missionReport = null;


            float DiceRoll = (RandomMath.RandomBetween(0f, 10f) + RandomMath.RandomBetween(0f, 20f) + RandomMath.RandomBetween(0f, 20f) + RandomMath.RandomBetween(0f, 20f) + RandomMath.RandomBetween(0f, 20f));
            if (DiceRoll > 77.5f)
            {
                DiceRoll = DiceRoll + 10 * this.Level;
            }
            DiceRoll = DiceRoll + Owner.data.SpyModifier;
            for (float i = 0; i < effectiveLevel; i++)
            {
                DiceRoll = DiceRoll + RandomMath.RandomBetween(1f, 5f);
            }
            if (effectiveLevel < 0)
            {
                for (float i = effectiveLevel; i < 0; i++)
                {
                    DiceRoll = DiceRoll - RandomMath.RandomBetween(1f, 5f);
                }
            }
            DiceRoll = DiceRoll + Owner.data.OffensiveSpyBonus;*/

/* case AgentMission.InciteRebellion:
                    {
                        this.Mission = AgentMission.Defending;
                        this.MissionNameIndex = 2183;
                        if (Target.GetPlanets().Count == 0)
                        {
                            return;
                        }
                        target = EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets()[RandomMath.InRange(EmpireManager.GetEmpireByName(this.TargetEmpire).GetPlanets().Count)];
                        if (DiceRoll >= 70f)
                        {
                            //Added by McShooterz
                            this.AddExperience(8, Owner);
                            if (!EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched)
                            {
                                Empire rebels = CreatingNewGameScreen.CreateRebelsFromEmpireData(EmpireManager.GetEmpireByName(this.TargetEmpire).data, EmpireManager.GetEmpireByName(this.TargetEmpire));
                                rebels.data.IsRebelFaction = true;
                                rebels.data.Traits.Name = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName;
                                rebels.data.Traits.Singular = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelSing;
                                rebels.data.Traits.Plural = EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelPlur;
                                rebels.isFaction = true;
                                foreach (Empire e in EmpireManager.Empires)
                                {
                                    e.GetRelations().Add(rebels, new Relationship(rebels.data.Traits.Name));
                                    rebels.GetRelations().Add(e, new Relationship(e.data.Traits.Name));
                                }
                                EmpireManager.Empires.Add(rebels);
                                EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebellionLaunched = true;
                            }
                            Empire darebels = EmpireManager.GetEmpireByName(EmpireManager.GetEmpireByName(this.TargetEmpire).data.RebelName);
                            for (int i = 0; i < effectiveLevel * ((DiceRoll - 65) * .01); i++)
                            {
                                foreach (KeyValuePair<string, Troop> troop in ResourceManager.TroopsDict)
                                {
                                    if (!EmpireManager.GetEmpireByName(this.TargetEmpire).WeCanBuildTroop(troop.Key))
                                    {
                                        continue;
                                    }
                                    Troop t = ResourceManager.CreateTroop(troop.Value, darebels);
                                    t.Name = Localizer.Token(darebels.data.TroopNameIndex);
                                    t.Description = Localizer.Token(darebels.data.TroopDescriptionIndex);
                                    target.AssignTroopToTile(t);
                                    break;
                                }
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (effectiveLevel * ((DiceRoll - 65) * .01) > 4)
                                {
                                    Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has has released the Kraken on ", target.Name), Target);
                                    ///Wyvern
                                    target.AssignTroopToTile(ResourceManager.TroopsDict["Wyvern"]);
                                }
                                else
                                {
                                    Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name), Target);
                                }
                            }
                            if (effectiveLevel * ((DiceRoll - 65) * .01) > 4)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " Released the Kraken on ", target.Name), Target);
                                ///Wyvern
                                target.AssignTroopToTile(ResourceManager.TroopsDict["Wyvern"]);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nThe Agent was not detected"), Owner);
                            break;
                        }
                        //else if (DiceRoll > 70f)
                        //{
                        //    Agent agent5 = this;
                        //    agent5.Level = agent5.Level + 1;
                        //    if (this.Level > 10)
                        //    {
                        //        this.Level = 10;
                        //    }

                        //    if (Target == EmpireManager.Player)
                        //    {
                        //        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("An enemy Agent has incited rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                        //    }
                        //    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " incited a serious rebellion on ", target.Name, "\nHowever, they know we are behind it. Agent gains +1 level"), Owner);
                        //    break;
                        //}
                        else if (DiceRoll < 40f)
                        {
                            if (DiceRoll >= 25f)
                            {
                                DamageSpy(this, Owner, 2, "", Target);
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We killed an enemy agent trying to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " was killed trying to incite rebellion on ", target.Name), Owner);
                            Owner.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }
                        else
                        {
                            this.AddExperience(4, Owner);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat("We foiled an enemy plot to incite rebellion on ", target.Name, "\nThe Agent was sent by ", Owner.data.Traits.Name), Target);
                            }
                            Target.GetRelations(Owner).DamageRelationship(Target, Owner, "Caught Spying", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(this.Name, " escaped after being detected while trying to incite rebellion on ", target.Name), Owner);
                            break;
                        }
                    }
 *  /*
            if (Owner == EmpireManager.Player && Mission == AgentMission.Defending && Owner.Money > 500 && AgentComponent.AutoTrain == true)
            {
                //if (startingmission == AgentMission.Training && this.Level >= 10)
                //{
                //    Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(this.Name, " is a master spy."), Owner);
                //    this.TargetEmpire = "";
                //    return;
                //}
                //agent.AssignMission(AgentMission.Training, Owner, universeScreen.PlayerLoyalty);
                if (this.Level > 1)
                {
                    this.AssignMission(startingmission, Owner, this.TargetEmpire);
                }
                else
                {
                    this.AssignMission(AgentMission.Training, Owner, this.TargetEmpire);
                }
                return;
            }
             */
#endregion