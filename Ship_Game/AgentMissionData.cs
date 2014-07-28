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
        public float RandomDefenceLevelBonus = 3f;
        public float MinRollPerLevel = 3f;
        public float MaxRoll = 90f;

        //Training
        public short TrainingCost = 50;
        public short TrainingTurns = 25;
        public short TrainingPerfect = 95;
        public short TrainingGood = 25;
        public short TrainingBad = 10;
        public short TrainingWorst = 5;

        //Infiltrate
        public short InfiltrateCost = 75;
        public short InfiltrateTurns = 30;
        public short InfiltrateGood = 50;
        public short InfiltrateBad = 25;
        public short InfiltrateWorst = 15;

        //Assassinate
        public short AssassinateCost = 75;
        public short AssassinateTurns = 50;
        public short AssassinatePerfect = 85;
        public short AssassinateGood = 70;
        public short AssassinateBad = 25;
        public short AssassinateWorst = 15;

        //Sabotage
        public short SabotageCost = 75;
        public short SabotageTurns = 30;
        public short SabotagePerfect = 80;
        public short SabotageGood = 50;
        public short SabotageBad = 25;
        public short SabotageWorst = 15;

        //StealTech
        public short StealTechCost = 250;
        public short StealTechTurns = 50;
        public short StealTechPerfect = 85;
        public short StealTechGood = 75;
        public short StealTechBad = 20;
        public short StealTechWorst = 10;

        //Robbery
        public short RobberyCost = 50;
        public short RobberyTurns = 30;
        public short RobberyPerfect = 85;
        public short RobberyGood = 60;
        public short RobberyBad = 20;
        public short RobberyWorst = 10;

        //InciteRebellion
        public short RebellionCost = 250;
        public short RebellionTurns = 100;
        public short RebellionPerfect = 85;
        public short RebellionGood = 70;
        public short RebellionBad = 40;
        public short RebellionWorst = 30;

        //Recovering
        public short RecoveringTurns = 20;
    }
}
