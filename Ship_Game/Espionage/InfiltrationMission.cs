using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;

namespace Ship_Game
{
    [StarDataType]
    public abstract class InfiltrationMission
    {
        [StarData] public readonly int Cost;
        [StarData] public readonly byte Level;
        [StarData] public readonly InfiltrationMissionType Type;
        [StarData] public float Progress { get; private set; }

        public InfiltrationMission(int cost, byte level, InfiltrationMissionType type)
        {
            Cost = cost;
            Level = level;
            Type = type;
        }

        public void SetProgress(float value)
        {
            Progress = value;
        }

        public abstract void CompleteMission();


        public void Update(float progressToUdpate)
        {
            Progress += progressToUdpate;
            if (Progress >= Cost)
            {
                Progress = 0;
                CompleteMission();
            }
        }

        protected InfiltrationMissionResult RollMissionResult(Empire owner, 
            Empire them, int targetNumber, byte missionLevel)
        {

            int baseModifier = (owner.GetEspionage(them).Level - missionLevel).LowerBound(0);
            int baseResult   = owner.Random.RollDie(100) + baseModifier;

            if (baseResult >= 99) return InfiltrationMissionResult.GreatSuccess;
            if (baseResult <= 2)  return InfiltrationMissionResult.Disaster;

            int defense = (int)(them.EspionageDefenseRatio * 20);
            int modifiedResult = (int)(baseResult - defense + owner.data.OffensiveSpyBonus + owner.data.SpyModifier);

            if (modifiedResult < targetNumber/10) return InfiltrationMissionResult.CriticalFail;
            if (modifiedResult < targetNumber/5)  return InfiltrationMissionResult.MiserableFail;
            if (modifiedResult < targetNumber)    return InfiltrationMissionResult.Fail;
            else                                  return InfiltrationMissionResult.Success;
        }

        protected float CalcRelationDamage(float baseDamage, Espionage espionage, bool withLevelMultiplier = false)
        {
            if (espionage.Them.isPlayer)
                return baseDamage;

            float levelMultiplier = withLevelMultiplier ? (float)(espionage.Level * 0.5f) : 1;
            return baseDamage * espionage.Them.PersonalityModifiers.SpyDamageRelationsMultiplier * levelMultiplier;
        }

        protected struct InfiltrationMissionResolve
        {
            public bool GoodResult;
            public LocalizedText Message;
            public string MessageToVictim;
            public string CustomMessage;
            public float RelationDamage;
            public string DamageReason;
            private readonly Empire Us;
            private readonly Empire Victim;

            public InfiltrationMissionResolve(Empire us, Empire victim)
            {
                Us              = us;
                Victim          = victim;
                GoodResult      = false;
                Message         = LocalizedText.None;
                RelationDamage  = 0;
                MessageToVictim = "";
                CustomMessage   = "";
                DamageReason    = "";
            }

            public void SendNotifications(UniverseState u)
            {
                if (Message.IsEmpty) // default message
                    u.Notifications.AddAgentResult(GoodResult, $"{Message.Text}", Us);

                if (CustomMessage.NotEmpty())
                    u.Notifications.AddAgentResult(GoodResult, CustomMessage, Us);

                if (MessageToVictim.NotEmpty())
                    u.Notifications.AddAgentResult(!GoodResult, MessageToVictim, Victim);

                if (RelationDamage > 0 && DamageReason.NotEmpty())
                    Victim.GetRelations(Us).DamageRelationship(Victim, Us, DamageReason, RelationDamage, null);
            }
        }
    }

    public enum InfiltrationMissionType
    {
        PlantMole
    }

    public enum InfiltrationMissionResult
    {
        GreatSuccess,  // Natural 99 or 100
        Success,       // target reached
        Fail,          // below target
        MiserableFail, // 0.2 of target
        CriticalFail,  // 0.1 of target
        Disaster       // natural 1 or 2
    }
}
