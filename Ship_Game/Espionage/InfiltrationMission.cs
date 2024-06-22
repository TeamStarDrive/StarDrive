using SDGraphics;
using Ship_Game.Data.Serialization;
using Ship_Game.Universe;
using Ship_Game.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public InfiltrationMissionResult RollMissionResult(Empire owner, 
            Empire them, int targetNumber, byte missionLevel)
        {

            int baseModifier = (owner.GetEspionage(them).Level - missionLevel).LowerBound(0);
            int baseResult   = owner.Random.RollDie(100) + baseModifier;

            if (baseResult >= 99) return InfiltrationMissionResult.CriticalSuccess;
            if (baseResult <= 2)  return InfiltrationMissionResult.Disaster;

            int defense = (int)(them.EspionageDefenseRatio * 20);
            int modifiedResult = (int)(baseResult - defense + owner.data.OffensiveSpyBonus + owner.data.SpyModifier);

            if (modifiedResult < targetNumber/10) return InfiltrationMissionResult.CriticalFailDetected;
            if (modifiedResult < targetNumber/5)  return InfiltrationMissionResult.CriticalFail;
            if (modifiedResult < targetNumber)    return InfiltrationMissionResult.Fail;
            else                                  return InfiltrationMissionResult.Success;
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
                if (Message != LocalizedText.None) // default message
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
        CriticalSuccess,      // Natural 99 or 100
        Success,              // target reached
        Fail,                 // below target
        CriticalFail,         // 0.2 of target
        CriticalFailDetected, // 0.1 of target
        Disaster              // natural 1 or 2
    }


    [StarDataType]
    public class InfiltrationMissionPlantMole : InfiltrationMission
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;
        const int SuccessTargetNumber = 20; // need to get 20 and above in a roll of d100)

        public InfiltrationMissionPlantMole(Empire owner, Empire them, int levelCost, byte level) : 
            base((int)(levelCost * PercentOfLevelCost), level, InfiltrationMissionType.PlantMole)
        { 
            Owner = owner;
            Them  = them;
        }

        public override void CompleteMission() 
        {
            InfiltrationMissionResolve afterMath = new InfiltrationMissionResolve(Owner, Them);
            var result = RollMissionResult(Owner, Them, SuccessTargetNumber, Level);
            switch (result) 
            {
                case InfiltrationMissionResult.CriticalSuccess:
                    afterMath.GoodResult = true;
                    SetProgress(Cost * 0.5f);
                    Mole.PlantMole(Owner, Them, out string planetName);
                    break;
                case InfiltrationMissionResult.Success:
                    afterMath.GoodResult = true;
                    break;
                case InfiltrationMissionResult.Fail:
                case InfiltrationMissionResult.CriticalFail:
                case InfiltrationMissionResult.CriticalFailDetected:
                case InfiltrationMissionResult.Disaster: break;
            }

            afterMath.SendNotifications(Owner.Universe);
        }

    }
}
