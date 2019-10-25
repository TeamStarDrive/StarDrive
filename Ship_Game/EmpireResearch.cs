using System;

namespace Ship_Game
{
    public class EmpireResearch
    {
        readonly Empire Empire;

        // This is our research queue
        // The FIRST item (0) is always the Current research topic
        public Array<string> Queue => Empire.data.ResearchQueue;

        // NET research this turn
        public float NetResearch { get; private set; }
        public float MaxResearchPotential { get; private set; }
        float LeftoverResearch;

        public bool NoTopic  => Queue.IsEmpty;
        public bool HasTopic => Queue.NotEmpty;

        // The CURRENT research topic is simply the first item in our ResearchQueue
        public string    Topic   => Queue.NotEmpty ? Queue.First : "";
        public TechEntry Current => Queue.NotEmpty ? Empire.TechnologyDict[Queue.First] : TechEntry.None;

        public string TopicLocText => Localizer.Token(ResourceManager.TryGetTech(Topic, out Technology tech) ? tech.NameIndex : 341);

        public EconomicResearchStrategy Strategy { get; private set; }

        public EmpireResearch(Empire empire)
        {
            // @warning EmpireResearch is constructed while Empire is still uninitialized
            //          Do NOT perform any sensitive initialization here and use
            Empire = empire;
        }

        public void Reset()
        {
            NetResearch = 0;
            MaxResearchPotential = 0;
            LeftoverResearch = 0;
            Queue.Clear();
        }

        public void Update()
        {
            if (Strategy == null)
            {
                if (Empire.data.EconomicPersonality == null)
                    Empire.data.EconomicPersonality = new ETrait { Name = "Generalists" };
                Strategy = ResourceManager.GetEconomicStrategy(Empire.data.EconomicPersonality.Name);
            }

            UpdateNetResearch();
            ApplyResearchPoints();
        }
        
        void UpdateNetResearch()
        {
            NetResearch = 0;
            MaxResearchPotential = 0;
            foreach (Planet planet in Empire.GetPlanets())
            {
                NetResearch          += planet.Res.NetIncome;
                MaxResearchPotential += planet.Res.GrossMaxPotential;
            }
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
                Empire.UnlockTech(Topic, TechUnlockType.Normal);
                if (Empire.isPlayer)
                    Empire.Universe.NotificationManager.AddResearchComplete(Topic, Empire);
            }
        }

        // Inserts to the front of ResearchQueue, OR moves existing tech to the front
        public void SetTopic(string techUID)
        {
            if (techUID.IsEmpty())
                return; // ignore empty topics

            if (techUID.NotEmpty() && !ResourceManager.TechTree.ContainsKey(techUID))
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
            if (!Empire.TechnologyDict.ContainsKey(techUID))
            {
                Log.Error($"AddToResearchQueue: Unrecognized tech: {techUID}");
                return false;
            }
            return Queue.AddUnique(techUID);
        }

        public void RemoveFromQueue(string techUID) => Queue.Remove(techUID);
        public int IndexInQueue(string techUID) => Queue.IndexOf(techUID);
        public bool IsQueued(string tech) => Queue.Contains(tech);
    }
}
