using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Empires.DataPackets;
using Ship_Game.Ships;

namespace Ship_Game.Empires
{
    /// <summary>
    /// Do not add or remove ships from the empire if a loyalty change will do what is needed.
    /// In most cases Ship.LoyaltyChange methods exist to change the ships loyalty. 
    /// inactive and removed from universe ships will be automatically removed from these lists. 
    /// Otherwise add and removal from the empire will be automatic and does not need extra code to work.
    /// </summary>
    public interface IEmpireShipLists
    {
        void AddNewShipAtEndOfTurn(Ship s);
        void RemoveShipAtEndOfTurn(Ship s);
    }
}
