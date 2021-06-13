namespace Ship_Game
{
    public enum WarType
    {
        //purge enemies from shared systems.
        BorderConflict,
        //attack all enemy systems 
        ImperialistWar,
        GenocidalWar,
        // Attack border systems. 
        DefensiveWar,
        //do border conflict then attack all border systems. then imperialist war. 
        SkirmishWar,
        EmpireDefense
    }
}