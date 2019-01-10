using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class EmpireAI
    {
        private int DesiredAgentsPerHostile = 2;
        private int DesiredAgentsPerNeutral = 1;
        private int DesiredAgentCount;
        private int BaseAgents;
        public float spyBudget;

        private void DoAggRuthAgentManager()
        {
            string Names;

            float income = spyBudget;


            DesiredAgentsPerHostile = (int) (income * .08f) + 1;
            DesiredAgentsPerNeutral = (int) (income * .03f) + 1;

            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 2;
            BaseAgents = OwnerEmpire.GetPlanets().Count / 2;
            DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    EmpireAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount +
                                                          DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                EmpireAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + DesiredAgentsPerNeutral;
            }
            EmpireAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + BaseAgents;

            int empirePlanetSpys =
                OwnerEmpire.GetPlanets().Count() / 3 + 3; // (int)(this.spyBudget / (this.empire.GrossTaxes * 3));
            int currentSpies = OwnerEmpire.data.AgentList.Count;
            if (spyBudget >= 250f && currentSpies < empirePlanetSpys)
            {
                Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", OwnerEmpire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        OwnerEmpire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(',');
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                OwnerEmpire.data.AgentList.Add(a);
                spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in OwnerEmpire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || spyBudget <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, OwnerEmpire, "");
            }
            float offSpyModifier = (int)CurrentGame.Difficulty * 0.1f;
            int DesiredOffense = (int) (OwnerEmpire.data.AgentList.Count * offSpyModifier);
            //int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .33f); // (int)(0.33f * (float)this.empire.data.AgentList.Count);
            //int DesiredOffense = this.empire.data.AgentList.Count / 2;
            foreach (Agent agent in OwnerEmpire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    Offense >= DesiredOffense)
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Relationship> Relation in OwnerEmpire.AllRelations)
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated ||
                        Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                HashSet<AgentMission> PotentialMissions = new HashSet<AgentMission>();
                Empire Target = PotentialTargets[RandomMath.InRange(PotentialTargets.Count)];
                if (OwnerEmpire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);

                        PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                    }
                }
                if (OwnerEmpire.GetRelations(Target).Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                }


                if (OwnerEmpire.GetRelations(Target).SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                HashSet<AgentMission> remove = new HashSet<AgentMission>();
                foreach (AgentMission mission in PotentialMissions)
                {
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;
                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Recovering:
                            break;
                        default:
                            break;
                    }
                }
                foreach (AgentMission removeMission in remove)
                {
                    PotentialMissions.Remove(removeMission);
                }
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions.Skip(RandomMath.InRange(PotentialMissions.Count)).FirstOrDefault();
                agent.AssignMission(am, OwnerEmpire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void DoCunningAgentManager()
        {
            int income = (int) spyBudget;
            string Names;
            BaseAgents = OwnerEmpire.GetPlanets().Count / 2;
            DesiredAgentsPerHostile = (int) (income * .010f); // +1;
            DesiredAgentsPerNeutral = (int) (income * .05f); // +1;

            DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    EmpireAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount +
                                                          DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                EmpireAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + DesiredAgentsPerNeutral;
            }
            EmpireAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + BaseAgents;
            int empireSpyLimit =
                OwnerEmpire.GetPlanets().Count() / 3 + 3; // (int)(this.spyBudget / this.empire.GrossTaxes);
            int currentSpies = OwnerEmpire.data.AgentList.Count;
            if (spyBudget >= 250f && currentSpies < empireSpyLimit)
            {
                Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", OwnerEmpire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        OwnerEmpire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(',');
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                OwnerEmpire.data.AgentList.Add(a);
                spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in OwnerEmpire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }

                if (a.Mission != AgentMission.Defending || a.Level >= 2 || spyBudget <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, OwnerEmpire, "");
            }
            float offSpyModifier = (int)CurrentGame.Difficulty * 0.17f;

            int DesiredOffense = (int) (OwnerEmpire.data.AgentList.Count * offSpyModifier);
            foreach (Agent agent in OwnerEmpire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    Offense >= DesiredOffense)
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Relationship> Relation in OwnerEmpire.AllRelations)
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated ||
                        Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                Array<AgentMission> PotentialMissions = new Array<AgentMission>();
                Empire Target = PotentialTargets[RandomMath.InRange(PotentialTargets.Count)];
                if (OwnerEmpire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count)
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                    }
                }
                if (OwnerEmpire.GetRelations(Target).Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level >= 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level < 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                }
                if (OwnerEmpire.GetRelations(Target).Posture == Posture.Neutral ||
                    OwnerEmpire.GetRelations(Target).Posture == Posture.Friendly)
                {
                    if (agent.Level >= 8)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)

                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                }
                if (OwnerEmpire.GetRelations(Target).SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                HashSet<AgentMission> remove = new HashSet<AgentMission>();
                foreach (AgentMission mission in PotentialMissions)
                {
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;
                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Recovering:
                            break;
                        default:
                            break;
                    }
                }
                foreach (AgentMission removeMission in remove)
                {
                    PotentialMissions.Remove(removeMission);
                }
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[RandomMath.InRange(PotentialMissions.Count)];
                agent.AssignMission(am, OwnerEmpire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void DoHonPacAgentManager()
        {
            string Names;

            int income = (int) spyBudget;


            DesiredAgentsPerHostile = (int) (income * .05f) + 1;
            DesiredAgentsPerNeutral = (int) (income * .02f) + 1;


            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 1;
            DesiredAgentCount = 0;
            BaseAgents = OwnerEmpire.GetPlanets().Count / 2 + (int) (spyBudget / (OwnerEmpire.GrossPlanetIncomes * 2));
            foreach (KeyValuePair<Empire, Relationship> Relationship in OwnerEmpire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    EmpireAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount +
                                                          DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                EmpireAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + DesiredAgentsPerNeutral;
            }
            EmpireAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + BaseAgents;
            //int empirePlanetSpys = empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            int empirePlanetSpys = OwnerEmpire.GetPlanets().Count() / 3 + 3;

            if (spyBudget >= 250f && OwnerEmpire.data.AgentList.Count < empirePlanetSpys)
            {
                Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", OwnerEmpire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        OwnerEmpire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(',');
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                OwnerEmpire.data.AgentList.Add(a);
                spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in OwnerEmpire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || spyBudget <= 200f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, OwnerEmpire, "");
            }

            float offSpyModifier = (int)CurrentGame.Difficulty * 0.08f;
            int DesiredOffense =
                (int) (OwnerEmpire.data.AgentList.Count *
                       offSpyModifier); // /(int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .4f);
            foreach (Agent agent in OwnerEmpire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    Offense >= DesiredOffense)
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Relationship> Relation in OwnerEmpire.AllRelations)
                {
                    if (!Relation.Value.Known || Relation.Key.isFaction || Relation.Key.data.Defeated ||
                        Relation.Value.Posture != Posture.Neutral && Relation.Value.Posture != Posture.Hostile)
                    {
                        continue;
                    }
                    PotentialTargets.Add(Relation.Key);
                }
                if (PotentialTargets.Count <= 0)
                {
                    continue;
                }
                Array<AgentMission> PotentialMissions = new Array<AgentMission>();
                Empire Target = PotentialTargets[RandomMath.InRange(PotentialTargets.Count)];
                if (OwnerEmpire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                    }
                }
                if (OwnerEmpire.GetRelations(Target).SpiesDetected > 0)
                {
                    if (agent.Level >= 4) PotentialMissions.Add(AgentMission.Assassinate);
                }
                HashSet<AgentMission> remove = new HashSet<AgentMission>();
                foreach (AgentMission mission in PotentialMissions)
                {
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;
                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Recovering:
                            break;
                        default:
                            break;
                    }
                }
                foreach (AgentMission removeMission in remove)
                {
                    PotentialMissions.Remove(removeMission);
                }
                if (PotentialMissions.Count <= 0)
                {
                    continue;
                }
                AgentMission am = PotentialMissions[RandomMath.InRange(PotentialMissions.Count)];
                agent.AssignMission(am, OwnerEmpire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void RunAgentManager()
        {
            if (OwnerEmpire.isPlayer)
                return;
            spyBudget = OwnerEmpire.data.SpyBudget;
            string name = OwnerEmpire.data.DiplomaticPersonality.Name;
            if (spyBudget > 50 && name != null)
            {
                switch (name)
                {
                    case "Cunning":
                        DoCunningAgentManager();
                        break;
                    case "Ruthless":
                        DoAggRuthAgentManager();
                        break;
                    case "Aggressive":
                        DoAggRuthAgentManager();
                        break;
                    case "Honorable":
                        DoHonPacAgentManager();
                        break;
                    case "Xenophobic":
                        DoCunningAgentManager();
                        break;
                    case "Pacifist":
                        DoHonPacAgentManager();
                        break;
                    default:
                        DoCunningAgentManager();
                        break;
                }
            }
            OwnerEmpire.Money -= OwnerEmpire.data.SpyBudget - spyBudget;
            spyBudget = 0;
        }
    }
}