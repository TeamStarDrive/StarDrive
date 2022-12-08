using System;

namespace Ship_Game.Data.Serialization;

/// <summary>
/// Note: This MUST be applied to classes that are serialized with StarDataSerializer
///
/// [StarDataType]
/// class ShipData
/// {
/// }
///
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, Inherited = false)]
public sealed class StarDataTypeAttribute : Attribute
{
    /// <summary>
    /// Override the type name during serialization.
    /// This is then used during Deserialization when doing Type lookup by name
    ///
    /// Useful if you want to rename your class, but keep backwards compatibility.
    /// </summary>
    public string TypeName;

    public StarDataTypeAttribute()
    {
    }
    public StarDataTypeAttribute(string typeName)
    {
        TypeName = typeName;
    }
}

/// <summary>
/// Note: StarDataParser is opt-in, so properties/fields must be marked with [StarData]
///       The name of the FIELD is used for the mapping.
/// 
/// [StarData] public string Style;
///
/// Ship:
///   Style: Kulrathi
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class StarDataAttribute : Attribute
{
    public string NameId;
    public bool IsPrimaryKeyName;
    public bool IsPrimaryKeyValue;

    // If true, then this ValueType Field or Property has no default value
    // and should always be serialized. This has no effect on ReferenceTypes.
    public bool NoDefaults;

    // Overrides the default value for this Field or Property
    public object DefaultValue;

    public StarDataAttribute()
    {
    }
    public StarDataAttribute(string nameId, bool keyName = false, bool keyValue = false)
    {
        NameId = nameId;
        IsPrimaryKeyName = keyName;
        IsPrimaryKeyValue = keyValue;
    }
}

/// <summary>
/// Note: This can be used to capture object Key Name attributes.
///
/// [StarDataKeyName] public string KeyName;
///
/// Ship: my_ship_name  # KeyName="Ship"
///   Style: xxx        # KeyName="Style"
/// 
/// </summary>
public sealed class StarDataKeyNameAttribute : StarDataAttribute
{
    public StarDataKeyNameAttribute() : base(null, keyName:true)
    {
    }
    public StarDataKeyNameAttribute(string nameId) : base(nameId, keyName:true)
    {
    }
}

/// <summary>
/// An instance method decorated with this attribute will be called
/// when binary serializer is about to scan this object for Serialization
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class StarDataSerialize : Attribute
{
}

/// <summary>
/// An instance method decorated with this attribute will be called
/// when binary serializer has deserialized all fields and this object is now valid
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class StarDataDeserialized : Attribute
{
    // types that must call OnDeserialized() before us
    public Type[] Required;

    // we must call OnDeserialized() before these types
    public Type[] DeserializeBefore;

    public StarDataDeserialized() {}
    public StarDataDeserialized(params Type[] requires)
    {
        Required = requires;
    }
}

/// <summary>
/// A [StarDataType] class constructor decorated with this attribute
/// will be used as the default constructor for creating an instance
/// during DESERIALIZATION.
/// Arguments will be replaced with their default values (eg 0 or null).
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
public class StarDataConstructor : Attribute
{
}

