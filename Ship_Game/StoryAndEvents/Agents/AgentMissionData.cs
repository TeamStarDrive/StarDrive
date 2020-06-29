﻿using System.Security.Permissions;

namespace Ship_Game
{
    //Added by McShooterz: store modifiable data for agent missions
    public sealed class AgentMissionData
    {
        public short AgentCost         = 200;
        public short ExpPerLevel       = 2;
        public float DefenseLevelBonus = 3f;

        //Training
        public short TrainingCost        = 50;
        public short TrainingTurns       = 25;
        public short TrainingRollPerfect = 85;
        public short TrainingRollGood    = 25;
        public short TrainingRollBad     = 10;
        public short TrainingRollWorst   = 5;
        public short TrainingExpPerfect  = 2;
        public short TrainingExpGood     = 1;

        //Infiltrate
        public short InfiltrateCost      = 75;
        public short InfiltrateTurns     = 30;
        public short InfiltrateRollGood  = 55;
        public short InfiltrateRollBad   = 30;
        public short InfiltrateRollWorst = 15;
        public short InfiltrateExpGood   = 3;
        public short InfiltrateExp       = 2;

        //Assassinate
        public short AssassinateCost        = 75;
        public short AssassinateTurns       = 50;
        public short AssassinateRollPerfect = 90;
        public short AssassinateRollGood    = 75;
        public short AssassinateRollBad     = 35;
        public short AssassinateRollWorst   = 20;
        public short AssassinateExpPerfect  = 6;
        public short AssassinateExpGood     = 5;
        public short AssassinateExp         = 3;

        //Sabotage
        public short SabotageCost           = 75;
        public short SabotageTurns          = 30;
        public short SabotageRollPerfect    = 90;
        public short SabotageRollGood       = 60;
        public short SabotageRollBad        = 30;
        public short SabotageRollWorst      = 20;
        public short SabotageExpPerfect     = 4;
        public short SabotageExpGood        = 3;
        public short SabotageExp            = 2;

        //StealTech
        public short StealTechCost          = 250;
        public short StealTechTurns         = 50;
        public short StealTechRollPerfect   = 95;
        public short StealTechRollGood      = 85;
        public short StealTechRollBad       = 35;
        public short StealTechRollWorst     = 20;
        public short StealTechExpPerfect    = 6;
        public short StealTechExpGood       = 5;
        public short StealTechExp           = 3;

        //Robbery
        public short RobberyCost        = 50;
        public short RobberyTurns       = 30;
        public short RobberyRollPerfect = 95;
        public short RobberyRollGood    = 70;
        public short RobberyRollBad     = 40;
        public short RobberyRollWorst   = 20;
        public short RobberyExpPerfect  = 4;
        public short RobberyExpGood     = 3;
        public short RobberyExp         = 2;

        //InciteRebellion
        public short RebellionCost        = 200;
        public short RebellionTurns       = 100;
        public short RebellionRollPerfect = 85;
        public short RebellionRollGood    = 60;
        public short RebellionRollBad     = 40;
        public short RebellionRollWorst   = 20;
        public short RebellionExpPerfect  = 7;
        public short RebellionExpGood     = 5;
        public short RebellionExp         = 3;

        //Recovering
        public short RecoveringTurns = 20;

        public SpyMissionStatus SpyRollResult(AgentMission mission, float roll, out short xpToAdd)
        {
            xpToAdd = 0;
            switch (mission)
            {
                case AgentMission.Training:
                    if (roll >= TrainingRollPerfect) { xpToAdd = TrainingExpPerfect; return SpyMissionStatus.GreatSuccess; }
                    if (roll >= TrainingRollGood)    { xpToAdd = TrainingExpGood;    return SpyMissionStatus.Success; }
                    if (roll > TrainingRollBad)                                      return SpyMissionStatus.Failed; 
                    if (roll > TrainingRollWorst)                                    return SpyMissionStatus.FailedBadly;

                    break;
                case AgentMission.Infiltrate:
                    if (roll >= InfiltrateRollGood)  { xpToAdd = InfiltrateExpGood; return SpyMissionStatus.Success; }
                    if (roll > InfiltrateRollBad)    { xpToAdd = InfiltrateExp;     return SpyMissionStatus.Success; }
                    if (roll > InfiltrateRollWorst)                                 return SpyMissionStatus.FailedBadly;

                    break;
                case AgentMission.Assassinate:
                    if (roll >= AssassinateRollPerfect) { xpToAdd = AssassinateExpPerfect; return SpyMissionStatus.GreatSuccess; }
                    if (roll >= AssassinateRollGood)    { xpToAdd = AssassinateExpGood;    return SpyMissionStatus.Success; }
                    if (roll >  AssassinateRollBad)     { xpToAdd = AssassinateExp;        return SpyMissionStatus.Failed; }
                    if (roll >  AssassinateRollWorst)                                      return SpyMissionStatus.FailedBadly;

                    break;

                case AgentMission.Sabotage:
                    if (roll >= SabotageRollPerfect) { xpToAdd = SabotageExpPerfect; return SpyMissionStatus.GreatSuccess; }
                    if (roll >= SabotageRollGood)    { xpToAdd = SabotageExpGood;    return SpyMissionStatus.Success; }
                    if (roll >  SabotageRollBad)     { xpToAdd = SabotageExp;        return SpyMissionStatus.Failed; }
                    if (roll >  SabotageRollWorst)                                   return SpyMissionStatus.FailedBadly;

                    break;
                case AgentMission.StealTech:
                    if (roll >= StealTechRollPerfect) { xpToAdd = StealTechExpPerfect; return SpyMissionStatus.GreatSuccess; }
                    if (roll >= StealTechRollGood)    { xpToAdd = StealTechExpGood;    return SpyMissionStatus.Success; }
                    if (roll >  StealTechRollBad)     { xpToAdd = StealTechExp;        return SpyMissionStatus.Failed; }
                    if (roll >  StealTechRollWorst)                                    return SpyMissionStatus.FailedBadly;

                    break;
                case AgentMission.Robbery:
                    if (roll >= RobberyRollPerfect) { xpToAdd = RobberyExpPerfect; return SpyMissionStatus.GreatSuccess; }
                    if (roll >= RobberyRollGood)    { xpToAdd = RobberyExpGood;    return SpyMissionStatus.Success; }
                    if (roll >  RobberyRollBad)     { xpToAdd = RobberyExp;        return SpyMissionStatus.Failed; }
                    if (roll >  RobberyRollWorst)                                  return SpyMissionStatus.FailedBadly;

                    break;
                case AgentMission.InciteRebellion:
                    if (roll >= RebellionRollPerfect) { xpToAdd = RebellionExpPerfect; return SpyMissionStatus.GreatSuccess; }
                    if (roll >= RebellionRollGood)    { xpToAdd = RebellionExpGood;    return SpyMissionStatus.Success; }
                    if (roll >  RebellionRollBad)     { xpToAdd = RebellionExp;        return SpyMissionStatus.Failed; }
                    if (roll >  RebellionRollWorst)                                    return SpyMissionStatus.FailedBadly;

                    break;
            }

            return SpyMissionStatus.FailedCritically;
        }

        public void Initialize(AgentMission mission, out int index, out int turns, out int cost)
        {
            cost  = 0;
            index = 0;
            turns = 0;

            switch (mission)
            {
                case AgentMission.Training:        index = 2196; turns = TrainingTurns;    cost  = TrainingCost;    break;
                case AgentMission.Infiltrate:      index = 2188; turns = InfiltrateTurns;  cost  = InfiltrateCost;  break;
                case AgentMission.Assassinate:     index = 2184; turns = AssassinateTurns; cost  = AssassinateCost; break;
                case AgentMission.Sabotage:        index = 2190; turns = SabotageTurns;    cost  = SabotageCost;    break;
                case AgentMission.StealTech:       index = 2194; turns = StealTechTurns;   cost  = StealTechCost;   break;
                case AgentMission.Robbery:         index = 2192; turns = RobberyTurns;     cost  = RobberyCost;     break;
                case AgentMission.InciteRebellion: index = 2186; turns = RebellionTurns;   cost  = RebellionCost;   break;
                case AgentMission.Recovering:      index = 6024; turns = RecoveringTurns;                           break;
                case AgentMission.Undercover:      index = 2201;                                                    break;
                case AgentMission.Defending:       index = 2183;                                                    break;
            }
        }
    }

    public enum SpyMissionStatus
    {
        FailedCritically,
        FailedBadly,
        Failed,
        Success,
        GreatSuccess
    }
}
