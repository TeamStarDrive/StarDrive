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
        public short TrainingRollPerfect = 95;
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
        public short RebellionRollPerfect = 95;
        public short RebellionRollGood    = 80;
        public short RebellionRollBad     = 50;
        public short RebellionRollWorst   = 30;
        public short RebellionExpPerfect  = 7;
        public short RebellionExpGood     = 5;
        public short RebellionExp         = 3;

        //Recovering
        public short RecoveringTurns = 20;
    }
}
