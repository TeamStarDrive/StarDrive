using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data
{
    
    // Note: This MUST be applied to classes that are serialized with StarDataSerializer
    //
    // [StarDataType]
    // class ShipData
    // {
    // }
    //
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class StarDataTypeAttribute : Attribute
    {
        public StarDataTypeAttribute()
        {
        }
    }


    // Note: StarDataParser is opt-in, so properties/fields must be marked with [StarData]
    //       The name of the FIELD is used for the mapping.
    // 
    // [StarData] public string Style;
    //
    // Ship:
    //   Style: Kulrathi
    //
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class StarDataAttribute : Attribute
    {
        public string Id;
        public bool IsPrimaryKey;
        public StarDataAttribute()
        {
        }
        public StarDataAttribute(string id, bool key = false)
        {
            Id = id;
            IsPrimaryKey = key;
        }
    }

    // Note: This can be used for Key attributes. The name of the field
    //       is IRRELEVANT. The mapping is resolved by this attribute.
    //
    // [StarDataKey] public string Name;
    //
    // Ship: my_ship_name
    //   Style: xxx
    public sealed class StarDataKeyAttribute : StarDataAttribute
    {
        public StarDataKeyAttribute() : base(null, true)
        {
        }
        public StarDataKeyAttribute(string id) : base(id, true)
        {
        }
    }

}
