using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;

namespace Ship_Game
{
    [StarDataType]
    public abstract class InfiltrationOperation
    {
        [StarData] public readonly int Cost;
        [StarData] public readonly byte Level;
        [StarData] public readonly InfiltrationOpsType Type;
        [StarData] public float Progress { get; private set; }
        [StarData] readonly int RampUpTurns;
        [StarData] int RampUpTimer;

        public InfiltrationOperation(int cost, byte level, InfiltrationOpsType type, int rampUpTurns, Empire owner)
        {
            Cost = cost;
            Level = level;
            Type = type;
            RampUpTimer = RampUpTurns = (int)(rampUpTurns * owner.Universe.SettingsResearchModifier * owner.Universe.ProductionPace);
        }

        public void SetProgress(float value)
        {
            Progress = value;
        }

        public abstract void CompleteOperation();

        public virtual void Update(float progressToUdpate)
        {
            if (IsRampingUp)
            {
                RampUpTimer = (RampUpTimer - 1).LowerBound(0);
            }
            else
            {
                Progress += progressToUdpate;
                if (Progress >= Cost)
                {
                    Progress = 0;
                    CompleteOperation();
                    RampUpTimer = RampUpTurns;
                }
            }
        }

        public bool IsRampingUp => RampUpTimer > 0;

        public bool Active => RampUpTimer == 0;

        protected InfiltrationOpsResult RollMissionResult(Empire owner, 
            Empire them, int targetNumber, byte missionLevel)
        {

            int baseModifier = (owner.GetEspionage(them).Level - missionLevel).LowerBound(0);
            int baseResult   = owner.Random.RollDie(100) + baseModifier;

            if (baseResult >= 99) return InfiltrationOpsResult.Phenomenal;
            if (baseResult <= 2)  return InfiltrationOpsResult.Disaster;

            int defense = (int)(them.EspionageDefenseRatio * 20);
            int modifiedResult = (int)(baseResult - defense + owner.data.OffensiveSpyBonus + owner.data.SpyModifier);

            if (modifiedResult >= targetNumber*2) return InfiltrationOpsResult.GreatSuccess;
            if (modifiedResult < targetNumber/10) return InfiltrationOpsResult.CriticalFail;
            if (modifiedResult < targetNumber/5)  return InfiltrationOpsResult.MiserableFail;
            if (modifiedResult < targetNumber)    return InfiltrationOpsResult.Fail;
            else                                  return InfiltrationOpsResult.Success;
        }

        protected float CalcRelationDamage(float baseDamage, Espionage espionage, bool withLevelMultiplier = false)
        {
            if (espionage.Them.isPlayer)
                return baseDamage;

            float levelMultiplier = withLevelMultiplier ? (float)(espionage.Level * 0.5f) : 1;
            return baseDamage * espionage.Them.PersonalityModifiers.SpyDamageRelationsMultiplier * levelMultiplier;
        }

        protected struct InfiltrationOpsResolve
        {
            public bool GoodResult;
            public LocalizedText Message;
            public string MessageToVictim;
            public string CustomMessage;
            public float RelationDamage;
            public string DamageReason;
            public bool MessageUseTheirName = true;
            public Planet Planet;
            readonly Empire Us;
            readonly Empire Victim;
            

            public InfiltrationOpsResolve(Empire us, Empire victim)
            {
                Us              = us;
                Victim          = victim;
                Message         = LocalizedText.None;
                RelationDamage  = 0;
                MessageToVictim = "";
                CustomMessage   = "";
                DamageReason    = "";
            }

            public void SendNotifications(UniverseState u)
            {
                if (Message.NotEmpty) // default message
                {
                    string message = MessageUseTheirName ? $"{Victim.data.Traits.Name}: {Message.Text}" : Message.Text;
                    u.Notifications.AddAgentResult(GoodResult, message, Us, Planet);
                }

                if (CustomMessage.NotEmpty())
                    u.Notifications.AddAgentResult(GoodResult, CustomMessage, Us, Planet);

                if (MessageToVictim.NotEmpty())
                    u.Notifications.AddAgentResult(!GoodResult, MessageToVictim, Victim, Planet);

                if (RelationDamage > 0 && DamageReason.NotEmpty())
                    Victim.GetRelations(Us).DamageRelationship(Victim, Us, DamageReason, RelationDamage, null);
            }
        }
    }

    public enum InfiltrationOpsType
    {
        PlantMole,
        Uprise,
        CounterEspionage,
        Sabotage,
        SlowResearch,
        Rebellion,
        StealTech
    }

    public enum InfiltrationOpsResult
    {
        Phenomenal,    // Natural 99 or 100
        GreatSuccess,  // target * 2
        Success,       // target reached
        Fail,          // below target
        MiserableFail, // 0.2 of target
        CriticalFail,  // 0.1 of target
        Disaster       // natural 1 or 2
    }
}
