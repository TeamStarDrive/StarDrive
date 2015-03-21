using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class DesignAction
	{
		public SlotStruct clickedSS;

		public List<SlotStruct> AlteredSlots = new List<SlotStruct>();

		public DesignAction()
		{
		}
	}
}