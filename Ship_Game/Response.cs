using System;

namespace Ship_Game
{
	public class Response
	{
		public string Text;

		public int MoneyToThem;

		public bool EndsTransmission;

		public bool FailIfNotAlluring;

		public string RequiredTech;

		public int DefaultIndex = -1;

		public int FailIndex;

		public int SuccessIndex;

		public string SetResearchTo;

		public Response()
		{
		}
	}
}