using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;
using Ship_Game.Universe;
using System;

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

        public InfiltrationOperation(int cost, InfiltrationOpsType type, int baseRampUpTurns, Empire owner)
        {
            Cost = cost;
            Type = type;
            Level = Espionage.GetOpsLevel(type);
            RampUpTimer = RampUpTurns = (int)(baseRampUpTurns * owner.Universe.SettingsResearchModifier * owner.Universe.ProductionPace);
        }

        public void SetProgress(float value)
        {
            Progress = value;
        }

        static public int BaseRemainingTurns(InfiltrationOpsType type, int levelCost, float progressPerTurn, UniverseState us)
        {
            if (progressPerTurn == 0)
                return -1;

            (int baseRampUpTurns, float percentOfLevelCost) = GetRampUpAndPercentLevel(type);
            int rampupTurns = (int)(baseRampUpTurns * us.SettingsResearchModifier * us.ProductionPace);
            int opsCost = (int)(levelCost * percentOfLevelCost);
            return rampupTurns + (int)(opsCost / progressPerTurn);
        }

        static (int baseRampupTurns, float percentOfLevelCost) GetRampUpAndPercentLevel(InfiltrationOpsType type)
        {
            switch (type) 
            {
                case InfiltrationOpsType.PlantMole:         return (InfiltrationOpsPlantMole.BaseRampUpTurns, InfiltrationOpsPlantMole.PercentOfLevelCost);
                case InfiltrationOpsType.CounterEspionage:  return (InfiltrationOpsCounterEspionage.BaseRampUpTurns, InfiltrationOpsCounterEspionage.PercentOfLevelCost);
                case InfiltrationOpsType.DisruptProjection: return (InfiltrationOpsDisruptProjection.BaseRampUpTurns, InfiltrationOpsDisruptProjection.PercentOfLevelCost);
                case InfiltrationOpsType.SlowResearch:      return (InfiltrationOpsDisruptResearch.BaseRampUpTurns, InfiltrationOpsDisruptResearch.PercentOfLevelCost);
                case InfiltrationOpsType.Rebellion:         return (InfiltrationOpsRebellion.BaseRampUpTurns, InfiltrationOpsRebellion.PercentOfLevelCost);
                case InfiltrationOpsType.Sabotage:          return (InfiltrationOpsSabotage.BaseRampUpTurns, InfiltrationOpsSabotage.PercentOfLevelCost);
                case InfiltrationOpsType.Uprise:            return (InfiltrationOpsUprise.BaseRampUpTurns, InfiltrationOpsUprise.PercentOfLevelCost);
                default:                                    throw new ArgumentOutOfRangeException("InfiltrationOpsType", $"InfiltrationOpsType {type} case not defined");
            }
        }

        public abstract void CompleteOperation();

        public int TurnsToComplete(float progressPerTurn) => progressPerTurn > 0 
            ? (int)(RampUpTimer + (Cost - Progress).LowerBound(0)/progressPerTurn) 
            : -1;

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
            Empire them, int targetNumber)
        {

            int baseModifier = (owner.GetEspionage(them).Level - Level).LowerBound(0);
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

            float levelMultiplier = withLevelMultiplier ? (float)(espionage.EffectiveLevel * 0.5f) : 1;
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
            public bool breakTreatiesIfAllied = true;
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
                    Victim.GetRelations(Us).DamageRelationship(Victim, Us, DamageReason, RelationDamage, null, breakTreatiesIfAllied);
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
        DisruptProjection
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
