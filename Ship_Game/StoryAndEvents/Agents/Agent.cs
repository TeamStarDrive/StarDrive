using System;
using System.Collections.Generic;

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

        //added by gremlin Domission from devek mod.
        public void DoMission(Empire us)
        {
            spyMute = us.data.SpyMute;
            Planet target;
            Empire Target = EmpireManager.GetEmpireByName(TargetEmpire);
            AgentMission startingmission = Mission;
            #region EmpireDefeated
            if (Target != null && Target.data.Defeated)
            {
                Mission = AgentMission.Defending;
                MissionNameIndex = 2183;
                return;
            }
            #endregion
            #region New DiceRoll
            float DiceRoll = RandomMath.RandomBetween(Level * ResourceManager.AgentMissionData.MinRollPerLevel, ResourceManager.AgentMissionData.MaxRoll);
            float DefensiveRoll = 0f;
            DiceRoll += Level * RandomMath.RandomBetween(1f, ResourceManager.AgentMissionData.RandomLevelBonus);
            DiceRoll += us.data.SpyModifier;
            DiceRoll += us.data.OffensiveSpyBonus;
            if (Target != null)
            {
                for (int i = 0; i < Target.data.AgentList.Count; i++)
                {
                    if (Target.data.AgentList[i].Mission == AgentMission.Defending)
                    {
                        DefensiveRoll += Target.data.AgentList[i].Level * ResourceManager.AgentMissionData.DefenceLevelBonus;
                    }
                }
                DefensiveRoll /= us.GetPlanets().Count / 3 + 1;
                DefensiveRoll += Target.data.SpyModifier;
                DefensiveRoll += Target.data.DefensiveSpyBonus;

                DiceRoll -= DefensiveRoll;
            }
            #endregion
            switch (Mission)
            {
                #region Training
                case AgentMission.Training:
                {
                    Mission = AgentMission.Defending;
                    MissionNameIndex = 2183;
                    if (DiceRoll >= ResourceManager.AgentMissionData.TrainingRollPerfect)
                    {
                        //Added by McShooterz
                        AddExperience(ResourceManager.AgentMissionData.TrainingExpPerfect, us);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6025)), us);
                        Training++;
                        break;
                    }

                    if (DiceRoll > ResourceManager.AgentMissionData.TrainingRollGood)
                    {
                        //Added by McShooterz
                        AddExperience(ResourceManager.AgentMissionData.TrainingExpGood, us);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6026)), us);
                        Training++;
                        break;
                    }

                    if (DiceRoll < ResourceManager.AgentMissionData.TrainingRollBad)
                    {
                        if (DiceRoll >= ResourceManager.AgentMissionData.TrainingRollWorst)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6027)), us);
                            AssignMission(AgentMission.Recovering, us, "");
                            PrevisousMission = AgentMission.Training;
                            break;
                        }
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6028)), us);
                        us.data.AgentList.QueuePendingRemoval(this);
                        break;
                    }

                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6029)), us);
                    break;
                }
                #endregion
                #region Infiltrate easy
                case AgentMission.Infiltrate:
                    {
                        if (Target == null || Target.GetPlanets().Count == 0)
                        {
                            Mission = AgentMission.Defending;
                            MissionNameIndex = 2183;
                            return;
                        }
                        if (DiceRoll >= ResourceManager.AgentMissionData.InfiltrateRollGood)
                        {
                            Mission = AgentMission.Undercover;
                            MissionNameIndex = 2201;
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.InfiltrateExpGood, us);
                            Mole m = Mole.PlantMole(us, Target);
                            TargetGUID = m.PlanetGuid;
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6030), " ", Empire.Universe.GetPlanet(m.PlanetGuid).Name, Localizer.Token(6031)), us);
                            Infiltrations++;
                            break;
                        }

                        if (DiceRoll < ResourceManager.AgentMissionData.InfiltrateRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.InfiltrateRollWorst)
                            {
                                Mission = AgentMission.Defending;
                                MissionNameIndex = 2183;
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6032)), us);
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6033), " ", us.data.Traits.Name), Target);
                                }
                                AssignMission(AgentMission.Recovering, us, "");
                                PrevisousMission = AgentMission.Infiltrate;
                                PreviousTarget = TargetEmpire;
                                break;
                            }
                            Mission = AgentMission.Defending;
                            MissionNameIndex = 2183;
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6034)), us);
                            us.data.AgentList.QueuePendingRemoval(this);
                            if (Target != EmpireManager.Player)
                            {
                                break;
                            }

                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6035), " ", us.data.Traits.Name), Target);
                            break;
                        }

                        //Added by McShooterz
                        AddExperience(ResourceManager.AgentMissionData.InfiltrateExp, us);
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6036)), us);
                        if (Target != EmpireManager.Player)
                        {
                            break;
                        }
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6033), " ", us.data.Traits.Name), Target);
                        break;
                    }
                #endregion
                #region Assassinate hard
                case AgentMission.Assassinate:
                    {
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
                        if (Target == null || Target.data.AgentList.Count == 0)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6038)), us);
                            return;
                        }
                        if (DiceRoll >= ResourceManager.AgentMissionData.AssassinateRollPerfect)
                        {
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.AssassinateExpPerfect, us);
                            Agent m = Target.data.AgentList[RandomMath.InRange(Target.data.AgentList.Count)];
                            Target.data.AgentList.Remove(m);
                            if (m.Mission == AgentMission.Undercover)
                            {
                                foreach (Mole mole in us.data.MoleList)
                                {
                                    if (mole.PlanetGuid != m.TargetGUID)
                                    {
                                        continue;
                                    }
                                    us.data.MoleList.QueuePendingRemoval(mole);
                                    break;
                                }
                            }
                            us.data.MoleList.ApplyPendingRemovals();
                            if (Target == EmpireManager.Player)
                            {
                                //if (!GremlinAgentComponent.AutoTrain) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat("One of our Agents was mysteriously assassinated: ", m.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6039), " ", m.Name, Localizer.Token(6040)), us);
                            Assassinations++;
                            break;
                        }

                        if (DiceRoll >= ResourceManager.AgentMissionData.AssassinateRollGood)
                        {
                            Agent m = Target.data.AgentList[RandomMath.InRange(Target.data.AgentList.Count)];
                            Target.data.AgentList.Remove(m);
                            if (m.Mission == AgentMission.Undercover)
                            {
                                foreach (Mole mole in us.data.MoleList)
                                {
                                    if (mole.PlanetGuid != m.TargetGUID)
                                    {
                                        continue;
                                    }
                                    us.data.MoleList.QueuePendingRemoval(mole);
                                    break;
                                }
                            }
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.AssassinateExpGood, us);
                            us.data.MoleList.ApplyPendingRemovals();
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6037), " ", m.Name, Localizer.Token(6041), " ", us.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6039), " ", m.Name, Localizer.Token(6042)), us);
                            Assassinations++;
                            break;
                        }

                        if (DiceRoll < ResourceManager.AgentMissionData.AssassinateRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.AssassinateRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6043), " ", us.data.Traits.Name), Target);
                                }
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6044)), us);
                                AssignMission(AgentMission.Recovering, us, "");
                                PrevisousMission = AgentMission.Assassinate;
                                PreviousTarget = TargetEmpire;
                                break;
                            }
                            Mission = AgentMission.Defending;
                            MissionNameIndex = 2183;
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying Failed", 20f, null);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6045), " ", us.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6046)), us);
                            us.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }

                        //Added by McShooterz
                        AddExperience(ResourceManager.AgentMissionData.AssassinateExp, us);
                        if (Target == EmpireManager.Player)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6043), " ", us.data.Traits.Name), Target);
                        }
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6047)), us);
                        break;
                    }
                #endregion
                #region Sabotage easy
                case AgentMission.Sabotage:
                    {
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
                        if (Target == null || Target.NumPlanets == 0)
                        {
                            return;
                        }
                        Empire targetEmpire = EmpireManager.GetEmpireByName(TargetEmpire);
                        target = targetEmpire.GetPlanets()[RandomMath.InRange(targetEmpire.NumPlanets)];
                        TargetGUID = target.guid;
                        if (DiceRoll >= ResourceManager.AgentMissionData.SabotageRollPerfect)
                        {
                            Planet crippledTurns = target;
                            crippledTurns.CrippledTurns = crippledTurns.CrippledTurns + 5 + Level * 5;
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6048), " ", target.Name), Target);
                            }
                            NotificationManager notificationManager = Empire.Universe.NotificationManager;
                            string[] name = { Name, " " + Localizer.Token(6084) + " ", null, null, null, null };
                            int num = 5 + Level * 5;
                            name[2] = num.ToString();
                            name[3] = " " + Localizer.Token(6085) + " ";
                            name[4] = target.Name;
                            name[5] = Localizer.Token(6031);
                            if (!spyMute) notificationManager.AddAgentResultNotification(true, string.Concat(name), us);
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.SabotageExpPerfect, us);
                            Sabotages++;
                            break;
                        }

                        if (DiceRoll > ResourceManager.AgentMissionData.SabotageRollGood)
                        {
                            Planet planet = target;
                            planet.CrippledTurns = planet.CrippledTurns + 5 + Level * 3;
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6048), " ", target.Name, Localizer.Token(6049),  " ", us.data.Traits.Name), Target);
                            }
                            NotificationManager notificationManager1 = Empire.Universe.NotificationManager;
                            string[] str = { Name, " " + Localizer.Token(6084) + " ", null, null, null, null };
                            int num1 = 5 + Level * 3;
                            str[2] = num1.ToString();
                            str[3] = " " + Localizer.Token(6085) + " ";
                            str[4] = target.Name;
                            str[5] = Localizer.Token(6031);
                            if (!spyMute) notificationManager1.AddAgentResultNotification(true, string.Concat(str), us);
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.SabotageExpGood, us);
                            Sabotages++;
                            break;
                        }

                        if (DiceRoll < ResourceManager.AgentMissionData.SabotageRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.SabotageRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6051), " ", target.Name, Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                                }
                                Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6052), " ", target.Name), us);
                                AssignMission(AgentMission.Recovering, us, "");
                                PrevisousMission = AgentMission.Sabotage;
                                PreviousTarget = TargetEmpire;
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!us.data.SpyMissionRepeat) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6053), " ", target.Name, Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                            }
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6054)), us);
                            us.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }

                        //Added by McShooterz
                        AddExperience(ResourceManager.AgentMissionData.SabotageExp, us);
                        if (Target == EmpireManager.Player)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6051)," ", target.Name, Localizer.Token(6049)," ", us.data.Traits.Name), Target);
                        }
                        Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6055), " ", target.Name), us);
                        break;
                    }
                #endregion
                #region StealTech hard
                case AgentMission.StealTech:
                    {
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
                        if (Target == null)
                            return;

                        var potentialUIDs = new Array<string>();

                        foreach(var tech in Target.GetEmpireAI().TradableTechs(us)) potentialUIDs.Add(tech.UID);

                        if (potentialUIDs.Count != 0)
                        {
                            string theUID = RandomMath.RandItem(potentialUIDs);
                            if (DiceRoll >= ResourceManager.AgentMissionData.StealTechRollPerfect)
                            {
                                //Added by McShooterz
                                AddExperience(ResourceManager.AgentMissionData.StealTechExpPerfect, us);
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.
                                        AddAgentResultNotification(false, Localizer.Token(6056), Target);
                                }
                                //Added by McShooterz: new acquire method, unlocks targets bonuses as well
                                us.AcquireTech(theUID, Target, TechUnlockType.Spy);
                                if (!spyMute)
                                {
                                    var stoleTechText = string.Concat(Name, " ", Localizer.Token(6057), " ");
                                    var techStolen    = Localizer.Token(ResourceManager.TechTree[theUID].NameIndex);
                                    var notDetected   = Localizer.Token(6031);
                                    var resultString  = string.Concat(stoleTechText, techStolen, notDetected);

                                    Empire.Universe.NotificationManager.
                                    AddAgentResultNotification(true, resultString, us);

                                }
                                TechStolen++;
                                break;
                            }

                            if (DiceRoll > ResourceManager.AgentMissionData.StealTechRollGood)
                            {
                                //Added by McShooterz
                                AddExperience(ResourceManager.AgentMissionData.StealTechExpGood, us);
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager
                                        .AddAgentResultNotification(false, string.Concat(Localizer.Token(6058)
                                            , " "
                                            , Localizer.Token(ResourceManager.TechTree[theUID].NameIndex)
                                            , Localizer.Token(6049), " ", us.data.Traits.Name)
                                            , Target);
                                }
                                //Added by McShooterz: new acquire method, unlocks targets bonuses as well
                                //Owner.UnlockTech(theUID);
                                us.AcquireTech(theUID, Target, TechUnlockType.Spy);
                                Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6057), " ", Localizer.Token(ResourceManager.TechTree[theUID].NameIndex), Localizer.Token(6042)), us);
                                TechStolen++;
                                break;
                            }

                            if (DiceRoll < ResourceManager.AgentMissionData.StealTechRollBad)
                            {
                                if (DiceRoll >= ResourceManager.AgentMissionData.StealTechRollWorst)
                                {
                                    if (Target == EmpireManager.Player)
                                    {
                                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6059), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                                    }
                                    Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6050)), us);
                                    AssignMission(AgentMission.Recovering, us, "");
                                    PrevisousMission = AgentMission.StealTech;
                                    PreviousTarget = TargetEmpire;
                                    break;
                                }
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6060), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                                }
                                Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying Failed", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6061)), us);
                                us.data.AgentList.QueuePendingRemoval(this);
                                break;
                            }

                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6059), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                            }
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6062)), us);
                            break;
                        }

                        AddExperience(ResourceManager.AgentMissionData.StealTechExp, us);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6063), " ", (ResourceManager.AgentMissionData.StealTechCost / 2).ToString(), " ", Localizer.Token(6064)), us);
                        Empire owner = us;
                        owner.AddMoney((float)ResourceManager.AgentMissionData.StealTechCost / 2);
                        break;
                    }
                #endregion
                #region Robbery
                case AgentMission.Robbery:
                    {
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
                        if (Target == null)
                            return;
                        int amount = (int)(RandomMath.RandomBetween(1f, Target.GetPlanets().Count * 10f) * Level);
                        if (amount > Target.Money && Target.Money > 0f)
                        {
                            amount = (int)Target.Money;
                        }
                        else if (Target.Money <= 0f)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6066), " ", TargetEmpire, Localizer.Token(6067)), us);
                            return;
                        }
                        if (DiceRoll >= ResourceManager.AgentMissionData.RobberyRollPerfect)
                        {
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.RobberyExpPerfect, us);
                            Empire money = Target;
                            money.AddMoney(-amount);
                            Empire empire = us;
                            empire.AddMoney(amount);
                            NotificationManager notificationManager2 = Empire.Universe.NotificationManager;
                            object[] objArray = { Name, " ", Localizer.Token(6068), " ", amount, " ", Localizer.Token(6069), " ", TargetEmpire, Localizer.Token(6031) };
                            if (!spyMute) notificationManager2.AddAgentResultNotification(true, string.Concat(objArray), us);
                            if (Target != EmpireManager.Player)
                            {
                                break;
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " ", Localizer.Token(6065)), Target);
                            Robberies++;
                            break;
                        }

                        if (DiceRoll > ResourceManager.AgentMissionData.RobberyRollGood)
                        {
                            //Added by McShooterz
                            AddExperience(ResourceManager.AgentMissionData.RobberyExpGood, us);
                            Empire money1 = Target;
                            money1.AddMoney(-amount);
                            Empire owner1 = us;
                            owner1.AddMoney(amount);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(amount, " ", Localizer.Token(6070), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                            }
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                            NotificationManager notificationManager3 = Empire.Universe.NotificationManager;
                            object[] name1 = { Name, " ", Localizer.Token(6068), " ", amount, " ", Localizer.Token(6069), " ", TargetEmpire, Localizer.Token(6042) };
                            if (!spyMute) notificationManager3.AddAgentResultNotification(true, string.Concat(name1), us);
                            Robberies++;
                            break;
                        }

                        if (DiceRoll < ResourceManager.AgentMissionData.RobberyRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.RobberyRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!us.data.SpyMissionRepeat && !spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6071), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                                }
                                Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                                if (!spyMute) if (!us.data.SpyMissionRepeat) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6072)), us);
                                AssignMission(AgentMission.Recovering, us, "");
                                PrevisousMission = AgentMission.Robbery;
                                PreviousTarget = TargetEmpire;
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6073), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                            }
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6074), " ", TargetEmpire), us);
                            us.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }

                        AddExperience(ResourceManager.AgentMissionData.RobberyExp, us);
                        if (Target == EmpireManager.Player)
                        {
                            if (!us.data.SpyMissionRepeat && !spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6071), Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                        }
                        Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                        if (!spyMute) if (!us.data.SpyMissionRepeat) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6075)), us);
                        break;
                    }
                #endregion
                #region Rebellion
                case AgentMission.InciteRebellion:
                    {
                        Mission = AgentMission.Defending;
                        MissionNameIndex = 2183;
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
                            AddExperience(ResourceManager.AgentMissionData.RebellionExpPerfect, us);
                            if (!targetEmpire.data.RebellionLaunched)
                            {
                                Empire rebels = EmpireManager.CreateRebelsFromEmpireData(targetEmpire.data, targetEmpire);
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
                            Empire daRebels = EmpireManager.GetEmpireByName(targetEmpire.data.RebelName);
                            for (int i = 0; i < 4; i++)
                            {
                                foreach (string troopType in ResourceManager.TroopTypes)
                                {
                                    if (!targetEmpire.WeCanBuildTroop(troopType))
                                        continue;
                                    Troop t = ResourceManager.CreateTroop(troopType, daRebels);
                                    t.Name = Localizer.Token(daRebels.data.TroopNameIndex);
                                    t.Description = Localizer.Token(daRebels.data.TroopDescriptionIndex);
                                    if (target.FreeTiles == 0 && !target.BumpOutTroop(EmpireManager.Corsairs)
                                                              && !t.TryLandTroop(target)) // Let's say the rebels are pirates :)
                                    {
                                        t.Launch(target); // launch the rebels
                                    }

                                    break;
                                }
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6078), " ", target.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6077), " ", target.Name, Localizer.Token(6031)), us);
                            Rebellions++;
                            break;
                        }

                        if (DiceRoll > ResourceManager.AgentMissionData.RebellionRollGood)
                        {
                            AddExperience(ResourceManager.AgentMissionData.RebellionExpGood, us);
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Localizer.Token(6078), " ", target.Name, Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                            }
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6077), " ", target.Name, Localizer.Token(6079)), us);
                            Rebellions++;
                            break;
                        }

                        if (DiceRoll < ResourceManager.AgentMissionData.RebellionRollBad)
                        {
                            if (DiceRoll >= ResourceManager.AgentMissionData.RebellionRollWorst)
                            {
                                if (Target == EmpireManager.Player)
                                {
                                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6076), " ", target.Name, Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                                }
                                Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6080), " ", target.Name), us);
                                AssignMission(AgentMission.Recovering, us, "");
                                PrevisousMission = AgentMission.InciteRebellion;
                                PreviousTarget = TargetEmpire;
                                break;
                            }
                            if (Target == EmpireManager.Player)
                            {
                                if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6081), " ", target.Name, Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                            }
                            Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying Failed", 20f, null);
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6082), " ", target.Name), us);
                            us.data.AgentList.QueuePendingRemoval(this);
                            break;
                        }

                        AddExperience(ResourceManager.AgentMissionData.RebellionExp, us);
                        if (Target == EmpireManager.Player)
                        {
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Localizer.Token(6076), " ", target.Name, Localizer.Token(6049), " ", us.data.Traits.Name), Target);
                        }
                        Target.GetRelations(us).DamageRelationship(Target, us, "Caught Spying", 20f, null);
                        if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(false, string.Concat(Name, " ", Localizer.Token(6083), " ", target.Name), us);
                        break;
                    }
                #endregion
                #region Recovery
                case AgentMission.Recovering :
                        {
                            Mission = AgentMission.Defending;
                            startingmission = PrevisousMission;
                            TargetEmpire = PreviousTarget;
                            MissionNameIndex = 2183;
                            if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6086)), us);
                            break;
                        }
                #endregion
            }
            #region Mission Repeat
            if (us == EmpireManager.Player
                && Mission == AgentMission.Defending //&& Owner.data.SpyBudget > 500
                && us.data.SpyMissionRepeat
                && (startingmission != AgentMission.Training || startingmission == AgentMission.Training && Level < 10))
            {
                AssignMission(startingmission, us, TargetEmpire);
                return;
            }
            TargetEmpire = "";
            #endregion
        }

        public bool Initialize(AgentMission TheMission, Empire Owner)
        {
            float spyBudget =  Owner.GetEmpireAI().SpyBudget;
            if (Owner.isPlayer)
                spyBudget = Owner.Money;
            bool returnvalue = false;
            switch (TheMission)
            {
                case AgentMission.Training:
                {
                    if (spyBudget >= ResourceManager.AgentMissionData.TrainingCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.TrainingTurns;
                        spyBudget -= ResourceManager.AgentMissionData.TrainingCost;
                        MissionNameIndex = 2196;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.Infiltrate:
                {
                    if (spyBudget >= ResourceManager.AgentMissionData.InfiltrateCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.InfiltrateTurns;
                        spyBudget -= ResourceManager.AgentMissionData.InfiltrateCost;
                        MissionNameIndex = 2188;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.Assassinate:
                {
                    if (spyBudget >= ResourceManager.AgentMissionData.AssassinateCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.AssassinateTurns;
                        spyBudget -= ResourceManager.AgentMissionData.AssassinateCost;
                        MissionNameIndex = 2184;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.Sabotage:
                {
                    if (spyBudget > ResourceManager.AgentMissionData.SabotageCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.SabotageTurns;
                        spyBudget -= ResourceManager.AgentMissionData.SabotageCost;
                        MissionNameIndex = 2190;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.StealTech:
                {
                    if (spyBudget >= ResourceManager.AgentMissionData.StealTechCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.StealTechTurns;
                        spyBudget -= ResourceManager.AgentMissionData.StealTechCost;
                        MissionNameIndex = 2194;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.Robbery:
                {
                    if (spyBudget >= ResourceManager.AgentMissionData.RobberyCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.RobberyTurns;

                        spyBudget -= ResourceManager.AgentMissionData.RobberyCost;
                        MissionNameIndex = 2192;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.InciteRebellion:
                {
                    if (spyBudget >= ResourceManager.AgentMissionData.RebellionCost)
                    {
                        TurnsRemaining = ResourceManager.AgentMissionData.RebellionTurns;

                        spyBudget -= ResourceManager.AgentMissionData.RebellionCost;
                        MissionNameIndex = 2186;
                        returnvalue = true;
                    }
                    break;
                }
                case AgentMission.Recovering:
                {
                    TurnsRemaining = ResourceManager.AgentMissionData.RecoveringTurns;
                    MissionNameIndex = 6024;
                    return true;
                }
                default:
                {
                    return false;
                }

            }


            if (Owner.isPlayer)
                //Owner.Money = spyBudget;
                Owner.AddMoney(-Owner.Money + spyBudget); // Fatbastard - refactor all this crappy copy paste function
            else
            {
                Owner.GetEmpireAI().SpyBudget = spyBudget;
            }
            return returnvalue;
        }

        //Added by McShooterz: add experience to the agent and determine if level up.
        private void AddExperience(int exp, Empire Owner)
        {
            Experience += exp;
            while (Experience >= ResourceManager.AgentMissionData.ExpPerLevel * Level)
            {
                Experience -=  ResourceManager.AgentMissionData.ExpPerLevel * Level;
                if (Level < 10)
                {
                    Level++;
                    if (!spyMute) Empire.Universe.NotificationManager.AddAgentResultNotification(true, string.Concat(Name, " ", Localizer.Token(6087)), Owner);
                }
            }
        }
    }
}