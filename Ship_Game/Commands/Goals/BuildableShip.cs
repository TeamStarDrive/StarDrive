﻿using Ship_Game.AI;
using Ship_Game.Data.Serialization;
using Ship_Game.Ships;

namespace Ship_Game.Commands.Goals
{
    [StarDataType]
    public class BuildableShip
    {
        // this is the template of the ship to be built
        [StarData] public IShipDesign Template;
        [StarData] public bool Rush;
        
        [StarDataConstructor] protected BuildableShip() {}
        public BuildableShip(string shipUid)
        {
            if (!ResourceManager.Ships.GetDesign(shipUid, out Template))
                throw new($"BuildableShip: no template to build with UID={shipUid ?? "null"}");
        }

        public BuildableShip(IShipDesign design)
        {
            Template = design;
            if (Template == null)
                throw new("BuildableShip: ShipDesign cannot be null");
        }

        protected static string ErrMsg(Empire owner, string category)
        {
            return $"{category} failed for {owner.Name}. This is a FATAL bug in data files, where Empire is unable to build this required ship!";
        }

        public static IShipDesign GetConstructor(Empire owner)
        {
            Ship constructor = ShipBuilder.PickConstructor(owner);
            if (constructor == null)
                throw new(ErrMsg(owner, "PickConstructor"));
            return constructor.ShipData;
        }

        public static IShipDesign GetFreighter(Empire owner)
        {
            Ship constructor = ShipBuilder.PickFreighter(owner, owner.FastVsBigFreighterRatio);
            if (constructor == null)
                throw new(ErrMsg(owner, "PickConstructor"));
            return constructor.ShipData;
        }
    }
}
