using Ship_Game.Gameplay;
using System.IO;
using SDGraphics;
using SDUtils;
using Ship_Game.GameScreens.Espionage;
using Ship_Game.Data.Serialization;


namespace Ship_Game.AI
{
    [StarDataType]
    public sealed class EspionageManager
    {
        const int EspionageDefaultTimer = 10;
        [StarData] int EspionageUpdateTimer;
        [StarData] readonly Empire Owner;

        public EspionageManager(Empire e)
        {
            Owner = e;
        }
        public void RunEspionagePlanner(bool forceRun = false)
        {
            DetermineBudget();
            SetupDefenseWeight();
            SetupWeights(forceRun);
        }

        [StarDataConstructor]
        EspionageManager() { }

        public void InitEspionageManager(int id)
        {
            EspionageUpdateTimer = EspionageDefaultTimer + id; // for loadbalancing the updates per empire
        }

        void DetermineBudget()
        {
            float espionageCreditRating = (Owner.AI.CreditRating - 0.6f).LowerBound(0); // will get 0-0.4
            float allowedBudget = Owner.AI.SpyBudget * espionageCreditRating / 0.4f;
            Owner.SetAiEspionageBudgetMultiplier(allowedBudget);
        }

        void SetupDefenseWeight()
        {

        }

        void SetupWeights(bool ignoreTimer)
        {
            if (!ignoreTimer && --EspionageUpdateTimer > 0)
                return;

            if (!ignoreTimer)
                EspionageUpdateTimer = EspionageDefaultTimer;

            foreach (Empire empire in Owner.Universe.ActiveMajorEmpires.Filter(e => e != Owner)) 
            {
                Relationship relations = Owner.GetRelations(empire);
                Espionage espionage = relations.Espionage;
                SetEspionageLimitLevel(relations, espionage);
                EnableDisableEspionageOperations(relations, espionage);
            }
        }

        void SetEspionageLimitLevel(Relationship relations, Espionage espionage)    
        {

        }

        void EnableDisableEspionageOperations(Relationship relations, Espionage espionage)
        {

        }
    }
}