using Ship_Game.Gameplay;
using System.IO;

namespace Ship_Game.AI
{
    public sealed partial class EmpireAI
    {
        public float SpyBudget;
        public int EmpireSpyLimit => OwnerEmpire.GetPlanets().Count / 3 + 3;
        public const float SpyCost = 250;


        private void RunAgentManager()
        {
            if (OwnerEmpire.isPlayer)
                return;
            SpyBudget = OwnerEmpire.data.SpyBudget;

            if (SpyBudget > 50 && OwnerEmpire.data.DiplomaticPersonality.Name != null)
            {
                if (CanEmpireAffordSpy())
                {
                    CreateSpy();
                    SpyBudget -= SpyCost;
                }

                CreateMissionsByTrait();
            }
            OwnerEmpire.AddMoney(-(OwnerEmpire.data.SpyBudget - SpyBudget));
            SpyBudget = 0;
        }

        public void CreateMissionsByTrait()
        {
            switch (OwnerEmpire.data.DiplomaticPersonality.TraitName)
            {
                case DTrait.TraitType.Cunning:
                case DTrait.TraitType.Xenophobic:
                    DoCunningAgentManager();
                    break;

                case DTrait.TraitType.Ruthless:
                case DTrait.TraitType.Aggressive:
                    DoAggRuthAgentManager();
                    break;

                case DTrait.TraitType.Honorable:
                case DTrait.TraitType.Pacifist:
                    DoHonPacAgentManager();
                    break;

                default:
                    DoCunningAgentManager();
                    break;
            }
        }

        private void DoAggRuthAgentManager()
        {
            int offense = CalculateSpyUsage(out int defenders);
            float offSpyModifier = (int)CurrentGame.Difficulty * 0.1f;
            int desiredOffense = (int)(OwnerEmpire.data.AgentList.Count * offSpyModifier);
            AssignSpyMissions(offense, desiredOffense, DTrait.TraitType.Aggressive);
        }

        private void DoCunningAgentManager()
        {
            int offense = CalculateSpyUsage(out int defenders);
            float offSpyModifier = (int)CurrentGame.Difficulty * 0.17f;
            int desiredOffense = (int) (OwnerEmpire.data.AgentList.Count * offSpyModifier);
            AssignSpyMissions(offense, desiredOffense, DTrait.TraitType.Cunning);
        }

        private void DoHonPacAgentManager()
        {
            int offense = CalculateSpyUsage(out int defenders);
            float offSpyModifier = (int)CurrentGame.Difficulty * 0.08f;
            int desiredOffense = (int)(OwnerEmpire.data.AgentList.Count * offSpyModifier);
            AssignSpyMissions(offense, desiredOffense, DTrait.TraitType.Honorable);
        }

        private void AssignSpyMissions(int offense, int desiredOffense, DTrait.TraitType traitType)
        {
            Array<Empire> potentialTargets = FindEmpireTargets();
            if (potentialTargets.Count <= 0) return;
            foreach (Agent agent in OwnerEmpire.data.AgentList)
            {
                if (agent.Mission != AgentMission.Defending && agent.Mission != AgentMission.Undercover ||
                    offense >= desiredOffense)
                {
                    continue;
                }

                Empire target = potentialTargets.RandItem();

                Array<AgentMission> potentialMissions;// = new Array<AgentMission>();
                switch (traitType)
                {
                    case DTrait.TraitType.Honorable:
                        potentialMissions = PotentialPeacefulMissions(agent, target);
                        break;
                    case DTrait.TraitType.Cunning:
                        potentialMissions = PotentialCunningSpyMissions(agent, target);
                        break;
                    case DTrait.TraitType.Aggressive:
                        potentialMissions = PotentialAggressiveMissions(agent, target);
                        break;
                    default:
                        return;
                }
                if (potentialMissions.IsEmpty) continue;

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
                        default:
                            break;
                    }
                }

                if (potentialMissions.NotEmpty)
                {
                    AgentMission am = potentialMissions.RandItem();
                    agent.AssignMission(am, OwnerEmpire, target.data.Traits.Name);
                    offense++;
                }
            }
        }

        private Array<AgentMission> PotentialAggressiveMissions(Agent agent, Empire target)
        {
            var potentialMissions = new Array<AgentMission>();
            if (OwnerEmpire.GetRelations(target).AtWar)
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
            if (OwnerEmpire.GetRelations(target).Posture == Posture.Hostile)
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


            if (OwnerEmpire.GetRelations(target).SpiesDetected > 0)
            {
                if (agent.Level >= 4) potentialMissions.Add(AgentMission.Assassinate);
            }
            return potentialMissions;
        }

        private Array<AgentMission> PotentialCunningSpyMissions(Agent agent, Empire target)
        {
            var potentialMissions = new Array<AgentMission>();
            if (OwnerEmpire.GetRelations(target).AtWar)
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

            if (OwnerEmpire.GetRelations(target).Posture == Posture.Hostile)
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

            if (OwnerEmpire.GetRelations(target).Posture == Posture.Neutral ||
                OwnerEmpire.GetRelations(target).Posture == Posture.Friendly)
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

            if (OwnerEmpire.GetRelations(target).SpiesDetected > 0)
            {
                if (agent.Level >= 4) potentialMissions.Add(AgentMission.Assassinate);
            }
            return potentialMissions;
        }

        private Array<AgentMission> PotentialPeacefulMissions(Agent agent, Empire target)
        {
            var potentialMissions = new Array<AgentMission>();

            if (OwnerEmpire.GetRelations(target).AtWar)
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

            if (OwnerEmpire.GetRelations(target).SpiesDetected > 0)
            {
                if (agent.Level >= 4) potentialMissions.Add(AgentMission.Assassinate);
            }

            return potentialMissions;
        }

        private int CalculateSpyUsage(out int defenders)
        {
            defenders = 0;
            int offense = 0;
            foreach (Agent a in OwnerEmpire.data.AgentList)
            {
                if (a.Mission == AgentMission.Defending)
                {
                    defenders++;
                }
                else if (a.Mission != AgentMission.Undercover)
                {
                    offense++;
                }

                if (a.Mission != AgentMission.Defending || a.Level >= 2 || SpyBudget <= 300f)
                {
                    continue;
                }

                a.AssignMission(AgentMission.Training, OwnerEmpire, "");
            }

            return offense;
        }

        private Array<Empire> FindEmpireTargets()
        {
            var PotentialTargets = new Array<Empire>();
            foreach (var relation in OwnerEmpire.AllRelations)
            {
                if (relation.Value.Known && !relation.Key.isFaction && !relation.Key.data.Defeated &&
                    (relation.Value.Posture == Posture.Neutral || relation.Value.Posture == Posture.Hostile))
                    PotentialTargets.Add(relation.Key);
            }

            return PotentialTargets;
        }

        public bool CanEmpireAffordSpy()
        {
            int income = (int)SpyBudget;
            return SpyBudget >= 250f && OwnerEmpire.data.AgentList.Count < EmpireSpyLimit;
        }

        private float CreateSpy()
        {
            string[] spyNames = SpyNames();
            Agent a = new Agent {Name = AgentComponent.GetName(spyNames)};
            OwnerEmpire.data.AgentList.Add(a);
            return 250f;
        }

        private string[] SpyNames()
        {
            string Names;
            Names =
                (!File.Exists(string.Concat("Content/NameGenerators/spynames_", OwnerEmpire.data.Traits.ShipType,
                    ".txt"))
                    ? File.ReadAllText("Content/NameGenerators/spynames_Humans.txt")
                    : File.ReadAllText(string.Concat("Content/NameGenerators/spynames_",
                        OwnerEmpire.data.Traits.ShipType, ".txt")));
            string[] Tokens = Names.Split(',');
            return Tokens;
        }

    }
}