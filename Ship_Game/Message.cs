namespace Ship_Game
{
	public sealed class Message
	{
		public int Step;

		public string text;

		public int Index;

		public int SetEncounterStep;

		public bool SetWar;

		public bool EndWar;

		public Array<Response> ResponseOptions = new Array<Response>();

		public int MoneyDemanded;

		public bool EndTransmission;
	}
}