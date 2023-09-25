using SDUtils;
using Ship_Game.Data.Serialization;

namespace Ship_Game
{
    public enum Goods
    {
        None,
        Production,
        Food,
        Colonists
    }

    [StarDataType]
    public class Good
    {
        [StarData] public readonly float RefiningRatio; // How much is processed per 1 food in the refining module
        [StarData] public readonly byte MaxRichness; // Speed of mining per turn (1-10 are logical numbers)
        [StarData] public readonly int NameIndex;
        [StarData] public readonly int DescriptionIndex;
        [StarData] public readonly int Weight;
        [StarData] public readonly string UID;
        [StarData] public readonly bool IsGasGiantMineable;
    }
}
