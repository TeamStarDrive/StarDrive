using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ship_Game.Gameplay;

namespace Ship_Game.AI {
    public sealed partial class GSAI
    {
        private int DesiredAgentsPerHostile = 2;
        private int DesiredAgentsPerNeutral = 1;
        private int DesiredAgentCount;
        private int BaseAgents;
        public float spyBudget = 0;

        private void DoAggRuthAgentManager()
        {
            string Names;

            float income = this.spyBudget;


            this.DesiredAgentsPerHostile = (int) (income * .08f) + 1;
            this.DesiredAgentsPerNeutral = (int) (income * .03f) + 1;

            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 2;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            this.DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount +
                                                          this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;

            int empirePlanetSpys =
                this.empire.GetPlanets().Count() / 3 + 3; // (int)(this.spyBudget / (this.empire.GrossTaxes * 3));
            int currentSpies = this.empire.data.AgentList.Count;
            if (this.spyBudget >= 250f && currentSpies < empirePlanetSpys)
            {
                Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] {','});
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                this.spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.spyBudget <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            float offSpyModifier = (int) Empire.Universe.GameDifficulty * .1f;
            int DesiredOffense = (int) (this.empire.data.AgentList.Count * offSpyModifier);
            //int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .33f); // (int)(0.33f * (float)this.empire.data.AgentList.Count);
            //int DesiredOffense = this.empire.data.AgentList.Count / 2;
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    Offense >= DesiredOffense)
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.AllRelations)
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
                if (this.empire.GetRelations(Target).AtWar)
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
                if (this.empire.GetRelations(Target).Posture == Posture.Hostile)
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


                if (this.empire.GetRelations(Target).SpiesDetected > 0)
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
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
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
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void DoCunningAgentManager()
        {
            int income = (int) this.spyBudget;
            string Names;
            this.BaseAgents = empire.GetPlanets().Count / 2;
            this.DesiredAgentsPerHostile = (int) (income * .010f); // +1;
            this.DesiredAgentsPerNeutral = (int) (income * .05f); // +1;

            this.DesiredAgentCount = 0;
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount +
                                                          this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            //int empirePlanetSpys = this.empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            //if (this.empire.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;
            int empireSpyLimit =
                this.empire.GetPlanets().Count() / 3 + 3; // (int)(this.spyBudget / this.empire.GrossTaxes);
            int currentSpies = this.empire.data.AgentList.Count;
            if (this.spyBudget >= 250f && currentSpies < empireSpyLimit)
            {
                Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] {','});
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                this.spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }

                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.spyBudget <= 50f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            // int DesiredOffense = (int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .2);// (int)(0.20f * (float)this.empire.data.AgentList.Count);
            float offSpyModifier = (int) Empire.Universe.GameDifficulty * .17f;

            int DesiredOffense = (int) (this.empire.data.AgentList.Count * offSpyModifier);
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    Offense >= DesiredOffense)
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.AllRelations)
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
                if (this.empire.GetRelations(Target).AtWar)
                {
                    if (agent.Level >= 8)
                    {
                        PotentialMissions.Add(AgentMission.InciteRebellion);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                    }
                    if (agent.Level < 4)
                    {
                        PotentialMissions.Add(AgentMission.Sabotage);
                        //PotentialMissions.Add(AgentMission.Infiltrate);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count)
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                    }
                }
                if (this.empire.GetRelations(Target).Posture == Posture.Hostile)
                {
                    if (agent.Level >= 8)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level >= 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                    }
                    if (agent.Level < 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                }
                if (this.empire.GetRelations(Target).Posture == Posture.Neutral ||
                    this.empire.GetRelations(Target).Posture == Posture.Friendly)
                {
                    if (agent.Level >= 8)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)

                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Assassinate);
                        PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level >= 4)
                    {
                        PotentialMissions.Add(AgentMission.Robbery);
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        PotentialMissions.Add(AgentMission.Sabotage);
                    }
                    if (agent.Level < 4)
                    {
                        if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            PotentialMissions.Add(AgentMission.StealTech);
                        //if (this.empire.Money < 50 * this.empire.GetPlanets().Count) PotentialMissions.Add(AgentMission.Robbery);
                        PotentialMissions.Add(AgentMission.Robbery);
                    }
                }
                if (this.empire.GetRelations(Target).SpiesDetected > 0)
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
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
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
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void DoHonPacAgentManager()
        {
            string Names;

            int income = (int) this.spyBudget;


            this.DesiredAgentsPerHostile = (int) (income * .05f) + 1;
            this.DesiredAgentsPerNeutral = (int) (income * .02f) + 1;


            //this.DesiredAgentsPerHostile = 5;
            //this.DesiredAgentsPerNeutral = 1;
            this.DesiredAgentCount = 0;
            this.BaseAgents = empire.GetPlanets().Count / 2 + (int) (this.spyBudget / (this.empire.GrossTaxes * 2));
            foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relationship in this.empire.AllRelations)
            {
                if (!Relationship.Value.Known || Relationship.Key.isFaction || Relationship.Key.data.Defeated)
                {
                    continue;
                }
                if (Relationship.Value.Posture == Posture.Hostile)
                {
                    GSAI desiredAgentCount = this;
                    desiredAgentCount.DesiredAgentCount = desiredAgentCount.DesiredAgentCount +
                                                          this.DesiredAgentsPerHostile;
                }
                if (Relationship.Value.Posture != Posture.Neutral)
                {
                    continue;
                }
                GSAI gSAI = this;
                gSAI.DesiredAgentCount = gSAI.DesiredAgentCount + this.DesiredAgentsPerNeutral;
            }
            GSAI desiredAgentCount1 = this;
            desiredAgentCount1.DesiredAgentCount = desiredAgentCount1.DesiredAgentCount + this.BaseAgents;
            //int empirePlanetSpys = empire.GetPlanets().Where(canBuildTroops => canBuildTroops.CanBuildInfantry() == true).Count();
            int empirePlanetSpys = empire.GetPlanets().Count() / 3 + 3;
            //if (empire.GetPlanets().Where(canBuildTroops => canBuildTroops.BuildingList.Where(building => building.Name == "Capital City") != null).Count() > 0) empirePlanetSpys = empirePlanetSpys + 2;

            if (this.spyBudget >= 250f && this.empire.data.AgentList.Count < empirePlanetSpys)
            {
                Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", this.empire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        this.empire.data.Traits.ShipType, ".txt")));
                string[] Tokens = Names.Split(new char[] {','});
                Agent a = new Agent();
                a.Name = AgentComponent.GetName(Tokens);
                this.empire.data.AgentList.Add(a);
                this.spyBudget -= 250f;
            }
            int Defenders = 0;
            int Offense = 0;
            foreach (Agent a in this.empire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    Defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    Offense++;
                }
                if (a.Mission != AgentMission.Defending || a.Level >= 2 || this.spyBudget <= 200f)
                {
                    continue;
                }
                a.AssignMission(AgentMission.Training, this.empire, "");
            }
            float offSpyModifier = (int) Empire.Universe.GameDifficulty * .08f;
            int DesiredOffense =
                (int) (this.empire.data.AgentList.Count *
                       offSpyModifier); // /(int)(this.empire.data.AgentList.Count - empire.GetPlanets().Count * .4f);
            foreach (Agent agent in this.empire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    Offense >= DesiredOffense)
                {
                    continue;
                }
                Array<Empire> PotentialTargets = new Array<Empire>();
                foreach (KeyValuePair<Empire, Ship_Game.Gameplay.Relationship> Relation in this.empire.AllRelations)
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
                if (this.empire.GetRelations(Target).AtWar)
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
                if (this.empire.GetRelations(Target).SpiesDetected > 0)
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
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > this.spyBudget)
                            {
                                remove.Add(mission);
                            }
                            break;
                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > this.spyBudget)
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
                agent.AssignMission(am, this.empire, Target.data.Traits.Name);
                Offense++;
            }
        }

        private void RunAgentManager()
        {
            float spyincomemodifer = .01f; // ((int)Empire.Universe.GameDifficulty + 1) * .0033f;
            int income = (int) ((this.empire.Money * spyincomemodifer) *
                                (1 - this.empire.data.TaxRate)); //-this.empire.GrossTaxes*5 );//* .2f);
            if (income < 0 || this.empire.data.SpyBudget > this.empire.Money * .75f)
                income = 0;

            this.spyBudget = income + this.empire.data.SpyBudget;
            this.empire.Money -= income;

            string name = empire.data.DiplomaticPersonality.Name;
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
            this.empire.data.SpyBudget = this.spyBudget;
            this.spyBudget = 0;
        }
    }
}