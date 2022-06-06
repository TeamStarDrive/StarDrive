using Ship_Game.Data.Yaml;
using System;
using System.IO;
using Ship_Game.Data.Binary;
using SDUtils;

namespace Ship_Game.Data.Serialization
{
    public abstract class TypeSerializer
    {
        public const int MaxFundamentalTypes = 32;

        // TypeId which is valid in a single serialization context
        internal ushort TypeId;
        public readonly Type Type;

        /// <summary>
        /// If TRUE, this serializer is a primitive fundamental type
        /// </summary>
        public bool IsFundamentalType => (TypeId < MaxFundamentalTypes);

        /// <summary>
        /// If TRUE, this serializer is a collection serializer for Arrays or Maps
        /// </summary>
        public bool IsCollection { get; protected set; }

        /// <summary>
        /// If TRUE, this serializer is made for a custom user class type
        /// marked with [StarDataType] attribute
        /// </summary>
        public bool IsUserClass { get; protected set; }

        /// <summary>
        /// If TRUE, instances of this type should be represented by pointers,
        /// all Classes fall in this category.
        /// If FALSE, instances are value types such as primitives or structs,
        /// and can't be represented by pointers.
        /// </summary>
        public bool IsPointerType { get; protected set; }

        /// <summary>
        /// Non-Pointer types are value types
        /// </summary>
        public bool IsValueType { get; protected set; }

        /// <summary>
        /// Enums get some special treatment as ValueTypes
        /// </summary>
        public bool IsEnumType { get; protected set; }

        /// <summary>
        /// Serializer category for easier classification during Deserialization
        /// </summary>
        public SerializerCategory Category { get; protected set; }

        /// <summary>
        /// Overriden TypeName of this TypeSerializer
        /// Defaults to Type.Name
        ///
        /// This is used during Type lookup while deserializing binary streams
        /// </summary>
        public string TypeName { get; protected set; }

        /// <summary>
        /// Nice human-readable typename
        /// </summary>
        public string NiceTypeName => Type.GetTypeName();

        protected TypeSerializer(Type type)
        {
            Type = type;
            IsPointerType = !type.IsValueType;
            IsValueType = !IsPointerType;
            IsEnumType = type.IsEnum;
            TypeName = type.Name;
        }

        /// <summary>
        /// Convert from a generic Deserialized object into the underlying Type
        /// </summary>
        public virtual object Convert(object value)
        {
            Log.Error($"Direct Convert not supported for {ToString()}. Value: {value}");
            return null;
        }

        /// <summary>
        /// Deserialize FROM YamlNode (TEXT)
        /// </summary>
        public virtual object Deserialize(YamlNode node)
        {
            object value = node.Value;
            if (value == null)
                return null;
            return Convert(value);
        }

        /// <summary>
        /// TEXT Serialize this object into YamlNode
        /// </summary>
        public abstract void Serialize(YamlNode parent, object obj);

        /// <summary>
        /// TEXT Serialize this object (default is YAML)
        /// </summary>
        public virtual void Serialize(TextWriter writer, object obj)
        {
            throw new NotImplementedException("This is not a top-level serializer like YamlSerializer");
        }

        /// <summary>
        /// BINARY Serialize this object
        /// </summary>
        public abstract void Serialize(BinarySerializerWriter writer, object obj);
        
        /// <summary>
        /// BINARY Deserialize this object
        /// </summary>
        public abstract object Deserialize(BinarySerializerReader reader);

        /// <summary>
        /// Attempts to create an instance of `type` or returns null
        /// </summary>
        public object CreateInstanceOf(Type type)
        {
            try
            {
                return Activator.CreateInstance(type, nonPublic:true);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"No parameterless constructor for {type.GetTypeName()}");
                return null;
            }
        }

        protected static void Error(object value, string couldNotConvertToWhat)
        {
            string e = $"TypeSerializer could not convert '{value}' ({value?.GetType()}) to {couldNotConvertToWhat}";
            Log.Error(e);
        }

        protected static float Float(object value)
        {
            if (value is float f)  return f;
            if (value is double d) return (float)d;
            if (value is int i)    return i;
            if (value is string s) return StringView.ToFloat(s);
            Error(value, "Float -- expected int or float or double or string");
            return 0.0f;
        }

        protected static double Double(object value)
        {
            if (value is double d) return d;
            if (value is float f) return f;
            if (value is int i) return i;
            if (value is string s) return StringView.ToFloat(s);
            Error(value, "Double -- expected int or float or doubl or string");
            return 0.0;
        }

        protected static float Float(string value)
        {
            return StringView.ToFloat(value);
        }

        protected static double Double(string value)
        {
            return StringView.ToDouble(value);
        }

        protected static byte Byte(object value)
        {
            if (value is int i)   return (byte)i;
            if (value is float f) return (byte)(int)f;
            Error(value, "Byte -- expected int or float");
            return 0;
        }

        protected static int Int(object value)
        {
            if (value is int i) return i;
            if (value is float f) return (int)f;
            if (value is string s) return StringView.ToInt(s);
            Error(value, "Int -- expected int or float or string");
            return 0;
        }
    }

}
