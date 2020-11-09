using Ship_Game.Gameplay;
using System.IO;
using Ship_Game.GameScreens.Espionage;


namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public float SpyBudget { get; private set; }

        public float SpyCost      => ResourceManager.AgentMissionData.AgentCost + ResourceManager.AgentMissionData.TrainingCost;
        public int EmpireSpyLimit => (OwnerEmpire.GetPlanets().Count / 3).LowerBound(3);

        void RunAgentManager()
        {
            if (OwnerEmpire.isPlayer)
                return;

            SpyBudget = OwnerEmpire.data.SpyBudget;

            if (OwnerEmpire.data.DiplomaticPersonality.Name != null && CanEmpireAffordSpy())
                    CreateAgent();

            TrainAgents();
            CreateMissionsByTrait();
        }

        void TrainAgents()
        {
            short trainingCost = ResourceManager.AgentMissionData.TrainingCost;
            for (int i = 0; i < OwnerEmpire.data.AgentList.Count; i++)
            {
                Agent agent = OwnerEmpire.data.AgentList[i];
                if (trainingCost <= SpyBudget && agent.Mission == AgentMission.Defending && agent.IsNovice)
                    agent.AssignMission(AgentMission.Training, OwnerEmpire, "");
            }
        }

        public void DeductSpyBudget(float value)
        {
            SpyBudget -= value;
        }

        public void CreateMissionsByTrait()
        {
            int freeAgents      = CalculateSpyUsage(out int defenders);
            int desiredMissions = (int)(OwnerEmpire.data.AgentList.Count * GetSpyModifier());
            AssignSpyMissions(freeAgents, desiredMissions);
        }

        float GetSpyModifier()
        {
            float modifier;
            switch (OwnerEmpire.Personality)
            {
                default:
                case PersonalityType.Cunning:
                case PersonalityType.Xenophobic: modifier = 0.13f;  break;
                case PersonalityType.Ruthless:
                case PersonalityType.Aggressive: modifier = 0.115f; break;
                case PersonalityType.Honorable:
                case PersonalityType.Pacifist:   modifier = 0.1f;   break;
            }

            return (1 + (int)CurrentGame.Difficulty) * modifier;
        }

        void AssignSpyMissions(int currentMissions, int wantedMissions)
        {
            if (!TryFindEmpireTargets(out Array<Empire> potentialTargets))
                return;

            foreach (Agent agent in OwnerEmpire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending  && agent.Mission != AgentMission.Undercover 
                    || currentMissions >= wantedMissions)

                {
                    continue;
                }

                Empire target = potentialTargets.RandItem();

                Array<AgentMission> potentialMissions;
                switch (OwnerEmpire.Personality)
                {
                    case PersonalityType.Pacifist:
                    case PersonalityType.Honorable:  potentialMissions = PotentialPeacefulMissions(agent, target);   break;
                    case PersonalityType.Cunning:
                    case PersonalityType.Xenophobic: potentialMissions = PotentialCunningSpyMissions(agent, target); break;
                    case PersonalityType.Ruthless:
                    case PersonalityType.Aggressive: potentialMissions = PotentialAggressiveMissions(agent, target); break;
                    default: return;

                }

                if (potentialMissions.IsEmpty) 
                    continue;

                for (int x = potentialMissions.Count - 1; x >= 0; x--)
                {
                    AgentMission mission = potentialMissions[x];
                    switch (mission)
                    {
                        case AgentMission.Defending:
                        case AgentMission.Training:
                            break;

                        case AgentMission.Infiltrate:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.Assassinate:
                            if (ResourceManager.AgentMissionData.AssassinateCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.Sabotage:
                            if (ResourceManager.AgentMissionData.SabotageCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.StealTech:
                            if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.Robbery:
                            if (ResourceManager.AgentMissionData.RobberyCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.InciteRebellion:
                            if (ResourceManager.AgentMissionData.RebellionCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.Undercover:
                            if (ResourceManager.AgentMissionData.InfiltrateCost > SpyBudget)
                                potentialMissions.RemoveAtSwapLast(x);
                            break;

                        case AgentMission.Recovering:
                            break;
                    }
                }

                if (potentialMissions.NotEmpty)
                {
                    AgentMission am = potentialMissions.RandItem();
                    agent.AssignMission(am, OwnerEmpire, target.data.Traits.Name);
                    currentMissions++;
                }
            }
        }

        private Array<AgentMission> PotentialAggressiveMissions(Agent agent, Empire target)
        {
            var potentialMissions = new Array<AgentMission>();
            if (!OwnerEmpire.GetRelations(target, out Relationship relations))
                return potentialMissions;

            if (relations.AtWar)
            {
                if (agent.Level >= 8)
                {
                    potentialMissions.Add(AgentMission.InciteRebellion);
                    potentialMissions.Add(AgentMission.Assassinate);
                    potentialMissions.Add(AgentMission.StealTech);
                }
                if (agent.Level >= 4)
                {
                    potentialMissions.Add(AgentMission.StealTech);
                    potentialMissions.Add(AgentMission.Robbery);
                    potentialMissions.Add(AgentMission.Sabotage);
                }
                if (agent.Level < 4)
                {
                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.StealTech);
                    potentialMissions.Add(AgentMission.Robbery);
                }
            }
            if (relations.Posture == Posture.Hostile)
            {
                if (agent.Level >= 8)
                {
                    potentialMissions.Add(AgentMission.StealTech);
                    potentialMissions.Add(AgentMission.Assassinate);
                }
                if (agent.Level >= 4)
                {
                    potentialMissions.Add(AgentMission.Robbery);
                    potentialMissions.Add(AgentMission.Sabotage);
                }
                if (agent.Level < 4)
                {
                    potentialMissions.Add(AgentMission.Sabotage);
                }
            }


            if (relations.SpiesDetected > 0)
            {
                if (agent.Level >= 4) potentialMissions.Add(AgentMission.Assassinate);
            }
            return potentialMissions;
        }

        private Array<AgentMission> PotentialCunningSpyMissions(Agent agent, Empire target)
        {
            var potentialMissions = new Array<AgentMission>();
            if (!OwnerEmpire.GetRelations(target, out Relationship relations))
                return potentialMissions;

            if (relations.AtWar)
            {
                if (agent.Level >= 8)
                {
                    potentialMissions.Add(AgentMission.InciteRebellion);
                    potentialMissions.Add(AgentMission.Assassinate);
                    potentialMissions.Add(AgentMission.Robbery);
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);
                }

                if (agent.Level >= 4)
                {
                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.Robbery);
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Assassinate);
                }

                if (agent.Level < 4)
                {
                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.Robbery);
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);
                }
            }

            if (relations.Posture == Posture.Hostile)
            {
                if (agent.Level >= 8)
                {
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Assassinate);
                    potentialMissions.Add(AgentMission.Robbery);
                }

                if (agent.Level >= 4)
                {
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.Robbery);
                }

                if (agent.Level < 4)
                {
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.Robbery);
                }
            }

            if (relations.Posture == Posture.Neutral || relations.Posture == Posture.Friendly)
            {
                if (agent.Level >= 8)
                {
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Assassinate);
                    potentialMissions.Add(AgentMission.Robbery);
                    potentialMissions.Add(AgentMission.Sabotage);
                }

                if (agent.Level >= 4)
                {
                    potentialMissions.Add(AgentMission.Robbery);
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Sabotage);
                }

                if (agent.Level < 4)
                {
                    if (ResourceManager.AgentMissionData.StealTechCost > SpyBudget)
                        potentialMissions.Add(AgentMission.StealTech);

                    potentialMissions.Add(AgentMission.Robbery);
                }
            }

            if (relations.SpiesDetected > 0)
            {
                if (agent.Level >= 4) potentialMissions.Add(AgentMission.Assassinate);
            }
            return potentialMissions;
        }

        private Array<AgentMission> PotentialPeacefulMissions(Agent agent, Empire target)
        {
            var potentialMissions = new Array<AgentMission>();
            if (!OwnerEmpire.GetRelations(target, out Relationship relations))
                return potentialMissions;

            if (relations.AtWar)
            {
                if (agent.Level >= 8)
                {
                    potentialMissions.Add(AgentMission.InciteRebellion);
                    potentialMissions.Add(AgentMission.Assassinate);
                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.Robbery);
                }

                if (agent.Level >= 4)
                {
                    potentialMissions.Add(AgentMission.Robbery);
                    potentialMissions.Add(AgentMission.Sabotage);
                }

                if (agent.Level < 4)
                {
                    potentialMissions.Add(AgentMission.Sabotage);
                    potentialMissions.Add(AgentMission.Robbery);
                }
            }

            if (relations.SpiesDetected > 0)
            {
                if (agent.Level >= 4) potentialMissions.Add(AgentMission.Assassinate);
            }

            return potentialMissions;
        }

        private int CalculateSpyUsage(out int defenders)
        {
            defenders   = 0;
            int offense = 0;
            foreach (Agent a in OwnerEmpire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                    defenders++;
                else if (a.Mission != AgentMission.Undercover && a.Mission != AgentMission.Recovering) 
                    offense++;
            }

            return offense;
        }

        bool TryFindEmpireTargets(out Array<Empire> targets)
        {
            targets = new Array<Empire>();
            foreach ((Empire them, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (rel.Known
                    && !them.isFaction
                    && !them.data.Defeated
                    && (rel.Posture == Posture.Hostile 
                        || !OwnerEmpire.IsHonorable && !OwnerEmpire.IsPacifist && rel.Posture == Posture.Neutral))
                {
                    targets.Add(them);
                }
            }

            return targets.Count > 0;
        }

        public bool CanEmpireAffordSpy() // TODO - do we need agents?
        {
            return SpyBudget >= SpyCost && OwnerEmpire.data.AgentList.Count < EmpireSpyLimit;
        }

        void CreateAgent()
        {
            string[] spyNames = SpyNames();
            var agent = new Agent { Name = AgentComponent.GetName(spyNames) };
            OwnerEmpire.data.AgentList.Add(agent);
            OwnerEmpire.AddMoney(-ResourceManager.AgentMissionData.AgentCost);
            DeductSpyBudget(ResourceManager.AgentMissionData.AgentCost);
            agent.AssignMission(AgentMission.Training, OwnerEmpire, OwnerEmpire.Name);
        }

        private string[] SpyNames()
        {
            string names;
            if (!File.Exists(string.Concat("Content/NameGenerators/spynames_"
                , OwnerEmpire.data.Traits.ShipType,".txt")))
                names = File.ReadAllText("Content/NameGenerators/spynames_Humans.txt");
            else
                names = File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                    OwnerEmpire.data.Traits.ShipType, ".txt"));
            string[] tokens = names.Split(',');
            return tokens;
        }

    }
}