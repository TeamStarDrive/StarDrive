using System;
using System.Collections.Generic;

namespace Ship_Game
{
    public sealed class DesignAction
    {
        public SlotStruct clickedSS;

        public Array<SlotStruct> AlteredSlots = new Array<SlotStruct>();

        public DesignAction()
        {
        }

        public DesignAction(SlotStruct slotStructToCopy)
        {
            clickedSS = new SlotStruct(slotStructToCopy);
        }
    }
}