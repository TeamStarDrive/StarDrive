using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class Message
	{
		public int Step;

		public string text;

		public int Index;

		public int SetEncounterStep;

		public bool SetWar;

		public bool EndWar;

		public List<Response> ResponseOptions = new List<Response>();

		public int MoneyDemanded;

		public bool EndTransmission;

		public Message()
		{
		}
	}
}