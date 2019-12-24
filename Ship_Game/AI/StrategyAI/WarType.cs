namespace Ship_Game
{
	public enum WarType
	{
		//purge enemies from shared systems. Attack border systems. 
		BorderConflict,
		//attack all enemy systems 
		ImperialistWar,
		GenocidalWar,
		//same as borderConflict
		DefensiveWar,
		//do border conflict then attack all border systems. then imperialist war. 
        SkirmishWar
	}
}