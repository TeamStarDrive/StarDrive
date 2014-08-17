using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ship_Game
{
    //Added by McShooterz: store modifiable data for agent missions
    public class AgentMissionData
    {
        public short AgentCost = 200;
        public short ExpPerLevel = 2;
        public float RandomLevelBonus = 5f;
        public float DefenceLevelBonus = 3f;
        public float MinRollPerLevel = 3f;
        public float MaxRoll = 90f;

        //Training
        public short TrainingCost = 50;
        public short TrainingTurns = 25;
        public short TrainingRollPerfect = 95;
        public short TrainingRollGood = 25;
        public short TrainingRollBad = 10;
        public short TrainingRollWorst = 5;
        public short TrainingExpPerfect = 2;
        public short TrainingExpGood = 1;

        //Infiltrate
        public short InfiltrateCost = 75;
        public short InfiltrateTurns = 30;
        public short InfiltrateRollGood = 50;
        public short InfiltrateRollBad = 25;
        public short InfiltrateRollWorst = 15;
        public short InfiltrateExpGood = 3;
        public short InfiltrateExp = 2;

        //Assassinate
        public short AssassinateCost = 75;
        public short AssassinateTurns = 50;
        public short AssassinateRollPerfect = 85;
        public short AssassinateRollGood = 70;
        public short AssassinateRollBad = 25;
        public short AssassinateRollWorst = 15;
        public short AssassinateExpPerfect = 6;
        public short AssassinateExpGood = 5;
        public short AssassinateExp = 3;

        //Sabotage
        public short SabotageCost = 75;
        public short SabotageTurns = 30;
        public short SabotageRollPerfect = 80;
        public short SabotageRollGood = 50;
        public short SabotageRollBad = 25;
        public short SabotageRollWorst = 15;
        public short SabotageExpPerfect = 4;
        public short SabotageExpGood = 3;
        public short SabotageExp = 2;

        //StealTech
        public short StealTechCost = 250;
        public short StealTechTurns = 50;
        public short StealTechRollPerfect = 85;
        public short StealTechRollGood = 75;
        public short StealTechRollBad = 20;
        public short StealTechRollWorst = 10;
        public short StealTechExpPerfect = 6;
        public short StealTechExpGood = 5;
        public short StealTechExp = 3;

        //Robbery
        public short RobberyCost = 50;
        public short RobberyTurns = 30;
        public short RobberyRollPerfect = 85;
        public short RobberyRollGood = 60;
        public short RobberyRollBad = 20;
        public short RobberyRollWorst = 10;
        public short RobberyExpPerfect = 4;
        public short RobberyExpGood = 3;
        public short RobberyExp = 2;

        //InciteRebellion
        public short RebellionCost = 250;
        public short RebellionTurns = 100;
        public short RebellionRollPerfect = 85;
        public short RebellionRollGood = 70;
        public short RebellionRollBad = 40;
        public short RebellionRollWorst = 30;
        public short RebellionExpPerfect = 7;
        public short RebellionExpGood = 5;
        public short RebellionExp = 3;

        //Recovering
        public short RecoveringTurns = 20;
    }
}
