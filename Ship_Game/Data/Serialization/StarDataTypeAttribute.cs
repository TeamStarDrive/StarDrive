using System;

namespace Ship_Game.Data.Serialization
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
        public int Id;
        public string NameId;
        public bool IsPrimaryKey;
        public StarDataAttribute()
        {
        }
        public StarDataAttribute(int id, bool key = false)
        {
            Id = id;
            IsPrimaryKey = key;
        }
        public StarDataAttribute(string nameId, bool key = false)
        {
            NameId = nameId;
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
        public StarDataKeyAttribute(int id) : base(id, true)
        {
        }
        public StarDataKeyAttribute(string nameId) : base(nameId, true)
        {
        }
    }

}
