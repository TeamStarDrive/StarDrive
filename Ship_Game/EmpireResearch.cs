using System;
using System.Collections.Generic;
using SDGraphics;
using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    [StarDataType]
    public class EmpireResearch
    {
        [StarData] readonly Empire Empire;
        [StarData] public bool NoResearchLeft { get; private set; }

        // The FIRST item (0) is always the Current research topic
        Array<string> Queue => Empire.data.ResearchQueue;

        // Enumerates items in the back of the queue
        // This does not include the Current research topic
        public IEnumerable<string> QueuedItems
        {
            get
            {
                for (int i = 1; i < Queue.Count; ++i)
                    yield return Queue[i];
            }
        }

        // NET research this turn
        public float NetResearch { get; private set; }
        public float MaxResearchPotential { get; private set; }
        public float ResearchStationResearchPerturn { get; private set; }
        float LeftoverResearch;

        public bool NoTopic => Queue.IsEmpty;
        public bool HasTopic => Queue.NotEmpty;

        // The CURRENT research topic is simply the first item in our ResearchQueue
        public string Topic => Queue.NotEmpty ? Queue.First : "";
        public TechEntry Current => Queue.NotEmpty ? Empire.GetTechEntry(Queue.First) : TechEntry.None;

        public LocalizedText TopicLocText => ResourceManager.TryGetTech(Topic, out Technology tech)
                                   ? new LocalizedText(tech.NameIndex) : GameText.None;

        [StarData]
        public EconomicResearchStrategy Strategy { get; private set; }

        [StarDataConstructor]
        public EmpireResearch(Empire empire)
        {
            // @warning EmpireResearch is constructed while Empire is still uninitialized
            //          Do NOT perform any sensitive initialization here
            Empire = empire;
        }

        public void Reset()
        {
            NetResearch = 0;
            MaxResearchPotential = 0;
            LeftoverResearch = 0;
            Queue.Clear();
        }

        public void Initialize()
        {
            if (Strategy == null)
            {
                if (Empire.data.EconomicPersonality == null)
                    Empire.data.EconomicPersonality = new ETrait { Name = "Generalists" };
                Strategy = ResourceManager.GetEconomicStrategy(Empire.data.EconomicPersonality.Name);
            }
        }

        public void Update()
        {
            UpdateNetResearch();
            ApplyResearchPoints();
            ResearchStationResearchPerturn = 0;
        }
        
        public void UpdateNetResearch()
        {
            Initialize();
            NetResearch = ResearchStationResearchPerturn;
            MaxResearchPotential = ResearchStationResearchPerturn;
            foreach (Planet planet in Empire.GetPlanets())
            {
                NetResearch          += planet.Res.NetIncome;
                MaxResearchPotential += planet.Res.GrossMaxPotential;
            }

            float ResearchFromAlliances = 0;
            foreach (Empire ally in Empire.Universe.GetAllies(Empire))
            {
                if (Empire.isPlayer && ally.DifficultyModifiers.ResearchMod.NotZero())
                {
                    float grossResearch = ally.Research.NetResearch / ally.DifficultyModifiers.ResearchMod;
                    float netMultiplier = ally.data.Traits.ResearchMod / ally.DifficultyModifiers.ResearchMod;
                    ResearchFromAlliances += grossResearch * netMultiplier;
                }
                else
                {
                    ResearchFromAlliances += ally.Research.NetResearch;
                }
            }

            ResearchFromAlliances *= GlobalStats.Defaults.ResearchBenefitFromAlliance + Empire.data.Traits.ResearchBenefitFromAlliance;
            NetResearch += ResearchFromAlliances;
            MaxResearchPotential += ResearchFromAlliances;
        }

        public void AddResearchStationResearchPerTurn(float value)
        {
            ResearchStationResearchPerturn += value;
        }

        void ApplyResearchPoints()
        {
            if (NoTopic)
                return;

            TechEntry tech = Current;
            if (tech.UID.IsEmpty())
            {
                Log.Error($"ApplyResearchPoints: Tech UID was empty!: {tech}");
                return;
            }

            float researchThisTurn = NetResearch + LeftoverResearch;
            LeftoverResearch = tech.AddToProgress(researchThisTurn, Empire, out bool unLocked);

            if (unLocked)
            {
                Empire.UnlockTech(tech, TechUnlockType.Normal, null);
                if (Empire.isPlayer)
                    Empire.Universe?.Notifications.AddResearchComplete(tech.UID, Empire);
            }
        }

        /// <summary>
        /// If true, it will prevent UI warning for player if there is no research
        /// </summary>
        public void SetNoResearchLeft(bool value)
        {
            NoResearchLeft = value;
        }

        // Inserts to the front of ResearchQueue, OR moves existing tech to the front
        public void SetTopic(string techUID)
        {
            if (techUID.IsEmpty())
                return; // ignore empty topics

            if (techUID.NotEmpty() && !ResourceManager.TryGetTech(techUID, out _))
            {
                Log.Error($"SetResearchTopic: Unrecognized tech: {techUID}");
                return;
            }

            Queue.Remove(techUID);
            Queue.Insert(0, techUID); // this makes it the current ResearchTopic
        }

        // @return TRUE if tech was added to the queue and wasn't already present
        public bool AddToQueue(string techUID)
        {
            if (!Empire.TryGetTechEntry(techUID, out _))
            {
                Log.Error($"AddToResearchQueue: Unrecognized tech: {techUID}");
                return false;
            }
            return Queue.AddUnique(techUID);
        }

        public void RemoveFromQueue(string techUID) => Queue.Remove(techUID);
        public int IndexInQueue(string techUID) => Queue.IndexOf(techUID);
        public bool IsQueued(string tech) => Queue.Contains(tech);

        // reorders tech in the queue
        // [0] is the currently researched tech
        public void ReorderTech(int oldIndex, int newIndex)
        {
            Queue.Reorder(oldIndex, newIndex);
        }
    }
}
