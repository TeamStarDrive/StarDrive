using Ship_Game.Data.Serialization;
using Ship_Game.Utils;

namespace Ship_Game.Universe
{
    [StarDataType]
    public class ExplorableGameObject : GameObject
    {
        // this is a tiny bitset where Empire.Id is the bit index for Explored:true|false
        [StarData] SmallBitSet ExploredBy;

        public ExplorableGameObject(int id, GameObjectType type) : base(id, type)
        {
        }

        public void SetExploredBy(Empire empire) => ExploredBy.Set(empire.Id);
        public bool IsExploredBy(Empire empire)  => ExploredBy.IsSet(empire.Id);
    }
}
