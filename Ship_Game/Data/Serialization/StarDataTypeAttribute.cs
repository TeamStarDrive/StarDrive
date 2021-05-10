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
        public bool IsPrimaryKeyName;
        public bool IsPrimaryKeyValue;
        public StarDataAttribute()
        {
        }
        public StarDataAttribute(int id, bool keyName = false, bool keyValue = false)
        {
            Id = id;
            IsPrimaryKeyName = keyName;
            IsPrimaryKeyValue = keyValue;
        }
        public StarDataAttribute(string nameId, bool keyName = false, bool keyValue = false)
        {
            NameId = nameId;
            IsPrimaryKeyName = keyName;
            IsPrimaryKeyValue = keyValue;
        }
    }

    // Note: This can be used to capture object Key Name attributes.
    //
    // [StarDataKeyName] public string KeyName;
    //
    // Ship: my_ship_name  # KeyName="Ship"
    //   Style: xxx        # KeyName="Style"
    // 
    public sealed class StarDataKeyNameAttribute : StarDataAttribute
    {
        public StarDataKeyNameAttribute() : base(null, keyName:true)
        {
        }
        public StarDataKeyNameAttribute(int id) : base(id, keyName:true)
        {
        }
        public StarDataKeyNameAttribute(string nameId) : base(nameId, keyName:true)
        {
        }
    }

    // Note: This can be used for Key attributes. The name of the field
    //       is IRRELEVANT. The mapping is resolved by this attribute.
    // Warning: THIS IS NOT COMPATIBLE WITH YAML STANDARD
    //
    // [StarDataKeyValue] public string Name;
    //
    // Ship: my_ship_name  # KeyValue="my_ship_name"
    //   Style: xxx
    public sealed class StarDataKeyValueAttribute : StarDataAttribute
    {
        public StarDataKeyValueAttribute() : base(null, keyValue:true)
        {
        }
        public StarDataKeyValueAttribute(int id) : base(id, keyValue:true)
        {
        }
        public StarDataKeyValueAttribute(string nameId) : base(nameId, keyValue:true)
        {
        }
    }

}
