using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Gameplay
{
    public class DialogOptionListItem : ScrollList<DialogOptionListItem>.Entry
    {
        DialogOption Option;
        public DialogOptionListItem(DialogOption option)
        {
            Option = option;
        }
    }
}
