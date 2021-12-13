using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class ObjectGroupInfo
    {
        #pragma warning disable 649
        [StarData] public string Type = "fighter";
        [StarData] public int Count = 1;
        #pragma warning restore 649
    }
}
