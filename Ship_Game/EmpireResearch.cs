﻿using System.Collections.Generic;
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

        public float TaxedResearch { get; private set; }
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
            TaxedResearch = 0;
            IReadOnlyList<Planet> planets = Empire.GetPlanets();
            for (int i = 0; i < planets.Count; i++)
            {
                Planet planet = planets[i];
                NetResearch          += planet.Res.NetIncome;
                TaxedResearch        += planet.Res.GrossIncome - planet.Res.NetIncome;
                MaxResearchPotential += planet.Res.GrossMaxPotential;
            }

            UpdateNetResearchDistuption();
            float researchFromAlliances = GetResearchFromAllies();
            NetResearch          += researchFromAlliances;
            MaxResearchPotential += researchFromAlliances;
        }

        float GetResearchFromAllies()
        {
            float researchFromAlliances = 0;
            foreach (Empire ally in Empire.Universe.GetAllies(Empire))
            {
                if (Empire.isPlayer && ally.DifficultyModifiers.ResearchMod.NotZero())
                {
                    float grossResearch = ally.Research.NetResearch / ally.DifficultyModifiers.ResearchMod;
                    float netMultiplier = ally.data.Traits.ResearchMod / ally.DifficultyModifiers.ResearchMod;
                    researchFromAlliances += grossResearch * netMultiplier;
                }
                else
                {
                    researchFromAlliances += ally.Research.NetResearch;
                }
            }

            researchFromAlliances *= GlobalStats.Defaults.ResearchBenefitFromAlliance + Empire.data.Traits.ResearchBenefitFromAlliance;
            return researchFromAlliances;
        }



        void UpdateNetResearchDistuption()
        {
            if (Empire.Universe.P.UseLegacyEspionage)
                return;

            Empire[] empires = Empire.Universe.ActiveMajorEmpires.Filter(e => e != Empire);
            for (int i = 0; i < empires.Length; i++)
            {
                Empire empire = empires[i];
                Espionage espionage = empire.GetEspionage(Empire);
                if (espionage.CanSlowResearch && empire.Random.RollDice(espionage.SlowResearchChance))
                    NetResearch *= 1 - Espionage.SlowResearchBy;
            }
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

                LeechTechByEspionage(tech.UID);
            }
        }

        void LeechTechByEspionage(string techName)
        {
            if (Empire.Universe.P.UseLegacyEspionage)
                return;

            foreach (Empire leecher in Empire.Universe.ActiveMajorEmpires.Filter(e => e != Empire))
            {
                if (leecher.GetEspionage(Empire).CanLeechTech)
                {
                    var tech = leecher.GetTechEntry(techName);
                    if (tech.Locked && !tech.ContentRestrictedTo(Empire))
                    {
                        tech.AddToProgress(tech.TechCost * 0.1f, leecher, out bool unlocked);
                        if (unlocked)
                        {
                            leecher.UnlockTech(tech, TechUnlockType.Normal, null);
                            if (leecher.isPlayer)
                                leecher.Universe?.Notifications.AddResearchComplete(tech.UID, leecher);
                        }

                        ResourceManager.TryGetTech(techName, out Technology technology);
                        string message = $"{Empire.data.Traits.Name} - {Localizer.Token(GameText.NotifyLeechedTech)} {technology?.Name.Text ?? techName}";
                        leecher.Universe.Notifications.AddAgentResult(true, message, leecher);
                    }
                }
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
        
        /// <summary>
        /// Removes the given tech from the queue, and all techs that depend on it.
        /// </summary>
        public void RemoveTechFromQueue(string techUid)
        {
            void RemoveLeadsToRecursive(string tech)
            {
                Queue.Remove(tech);
                foreach (Technology.LeadsToTech dependent in ResourceManager.Tech(tech).LeadsTo)
                    RemoveLeadsToRecursive(dependent.UID);
            }

            RemoveLeadsToRecursive(techUid);
        }
        
        /// <summary>
        /// Adds the given tech to the queue, and all of its PreReqs.
        /// It checks if the tech (and PreReqs) have been discovered and not finished yet and not already enqueued.
        /// </summary>
        public void AddTechToQueue(string techUid)
        {
            var technology = ResourceManager.Tech(techUid);
            var predecessorTechs = technology.PredecessorTechs();
            foreach (Technology predecessor in predecessorTechs)
            {
                var techEntry = Empire.GetTechEntry(predecessor.UID);
                if (techEntry.Discovered && !techEntry.Unlocked && !IsQueued(predecessor.UID))
                    AddToQueue(predecessor.UID);
            }
            AddToQueue(techUid);
        }
        
        string ResearchUidAt(int index) => Queue[index];
        Technology ResearchTechnologyAt(int index) => ResourceManager.Tech(ResearchUidAt(index));
        public bool AboveIsPreReq(int currentIndex)
        {
            var currentTech = ResearchTechnologyAt(currentIndex);
            foreach (Technology.LeadsToTech dependent in ResourceManager.Tech(ResearchUidAt(currentIndex - 1)).LeadsTo)
                if (dependent.UID == currentTech.UID)
                    return true;
            return false;
        }
        
        public bool IsPreReqOfBellow(int currentIndex)
        {
            Technology current = ResearchTechnologyAt(currentIndex);
            string next = ResearchUidAt(currentIndex + 1);
            foreach (Technology.LeadsToTech dependent in current.LeadsTo)
                if (dependent.UID == next)
                    return true;
            return false;
        }
        
        private void SwapQueueItems(int first, int second)
        {
            (Queue[first], Queue[second]) = (Queue[second], Queue[first]);
        }
        
        public void MoveUp(int index)
        {
            if (CanMoveUp(index))
                SwapQueueItems(index - 1, index);
        }
        
        public void MoveDown(int index)
        {
            if (CanMoveDown(index))
                SwapQueueItems(index + 1, index);
        }
        
        public bool CanMoveUp(int index)
        {
            return index != -1 && index >= 1 && !AboveIsPreReq(index);
        }
        
        public bool CanMoveDown(int index)
        {
            return index != -1 && index != Queue.Count - 1 && !IsPreReqOfBellow(index);
        }
        
        /// <summary>
        /// This will move the item at the given index to the top of the queue, or until it hits a PreReq.
        /// </summary>
        /// <returns>The amount of research that was skipped over.</returns>
        public int MoveToTopOrPreReq(int index)
        {
            int skipped = 0;
            while (CanMoveUp(index))
            {
                MoveUp(index);
                index--;
                skipped++;
            }
            return skipped;
        }
        
        /// <summary>
        /// If the item at the given index has any enqueued PreReqs, they will be moved to the top of the queue.
        /// </summary>
        /// <returns>The amount of research that has moved in the queue.</returns>
        public int MovePreReqsToTop(int index)
        {
            int preReqsThatMovedUp = 0;
            
            Technology current = ResearchTechnologyAt(index);
            
            foreach (string researchUid in Queue)
            { 
                int indexOfResearch = IndexInQueue(researchUid);
                
                Array<Technology> descendantTechs = ResourceManager.Tech(researchUid).DescendantTechs();
                
                foreach (Technology descendant in descendantTechs)
                {
                    if (descendant.UID == current.UID)
                    {
                        int movedPositions = MoveToTopOrPreReq(indexOfResearch);
                        if (movedPositions > 0)
                            preReqsThatMovedUp++;
                    }
                }
            }
            
            return preReqsThatMovedUp;
        }
        
        /// <summary>
        /// Moves the item at the given index to the top of the queue, and all of its PreReqs as well.
        /// </summary>
        /// <returns>How many items in total have moved in the queue</returns>
        public int MoveToTopWithPreReqs(int index)
        {
            int movedResearchItems = 0;
            
            int movedPreReqs = MovePreReqsToTop(index);
            movedResearchItems = movedPreReqs;
            
            int movedPositions = MoveToTopOrPreReq(index);
            if (movedPositions > 0)
                movedResearchItems++;
            
            return movedResearchItems;
        }
    }
}
