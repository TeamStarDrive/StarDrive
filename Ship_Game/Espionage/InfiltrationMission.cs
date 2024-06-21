using SDGraphics;
using Ship_Game.Data.Serialization;
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
        [StarData] float Progress;
        [StarData] public readonly InfiltrationMissionType Type;

        public InfiltrationMission(int cost, byte level, InfiltrationMissionType type)
        {
            Cost = cost;
            Level = level;
            Type = type;
        }

        public abstract void CompleteMission();


        public void Update(float progressToUdpate)
        {
            Progress += progressToUdpate;
            if (Progress >= Cost)
            {
                CompleteMission();
                Progress = 0;
            }
        }

        public static InfiltrationMissionResult RollMissionResult(RandomBase random, Empire owner, 
            Empire them, int targetNumber, byte missionLevel)
        {

            int baseModifier = (owner.GetEspionage(them).Level - missionLevel).LowerBound(0);
            int baseResult   = random.RollDie(100) + baseModifier;

            if (baseResult >= 99) return InfiltrationMissionResult.CriticalSuccess;
            if (baseResult <= 2)  return InfiltrationMissionResult.Disaster;

            int defense = (int)(them.EspionageDefenseRatio * 20);
            int modifiedResult = (int)(baseResult - defense + owner.data.OffensiveSpyBonus + owner.data.SpyModifier);

            if (modifiedResult < targetNumber/2 ) return InfiltrationMissionResult.CriticalFail;
            if (modifiedResult < targetNumber)    return InfiltrationMissionResult.Fail;
            else                                  return InfiltrationMissionResult.Success;
        }
    }

    public enum InfiltrationMissionType
    {
        PlantMole
    }

    public enum InfiltrationMissionResult
    {
        CriticalSuccess, // Natural 99 or 100
        Success,         // target reached
        Fail,            // below target
        CriticalFail,    // 0.5 of target
        Disaster         // natural 1 or 2
    }


    [StarDataType]
    public class InfiltrationMissionPlantMole : InfiltrationMission
    {
        [StarData] readonly Empire Owner;
        [StarData] readonly Empire Them;
        const float PercentOfLevelCost = 0.2f;

        public InfiltrationMissionPlantMole(Empire owner, Empire them, int levelCost, byte level) : 
            base((int)(levelCost * PercentOfLevelCost), level, InfiltrationMissionType.PlantMole)
        { 
            Owner = owner;
            Them  = them;
        }

        public override void CompleteMission() 
        { 
        }

    }
}
