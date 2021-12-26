using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization
{
    public abstract class TypeSerializer
    {
        public const int MaxFundamentalTypes = 32;

        // Id which is valid in a single serialization context
        internal ushort Id;
        internal Type Type;

        /// <summary>
        /// If TRUE, this serializer is a primitive fundamental type
        /// </summary>
        public bool IsFundamentalType => (Id <= MaxFundamentalTypes);

        /// <summary>
        /// If TRUE, this serializer is a collection serializer for Arrays or Maps
        /// </summary>
        public bool IsCollection { get; protected set; }

        /// <summary>
        /// If TRUE, this serializer is made for a custom user class type
        /// </summary>
        public bool IsUserClass { get; protected set; }

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
        public abstract void Serialize(BinaryWriter writer, object obj);
        
        /// <summary>
        /// BINARY Deserialize this object
        /// </summary>
        public abstract object Deserialize(BinaryReader reader);

        public static void WriteSerializerId(BinaryWriter writer, int serializerId)
        {
            if (serializerId > ushort.MaxValue)
                throw new IndexOutOfRangeException($"TypeSerializer could not handle so many serializers: {serializerId} > 65535");
            writer.Write((ushort)serializerId);
        }

        public static void Error(object value, string couldNotConvertToWhat)
        {
            string e = $"TypeSerializer could not convert '{value}' ({value?.GetType()}) to {couldNotConvertToWhat}";
            Log.Error(e);
        }

        public static float Float(object value)
        {
            if (value is float f)  return f;
            if (value is int i)    return i;
            if (value is string s) return StringView.ToFloat(s);
            Error(value, "Float -- expected int or float or string");
            return 0f;
        }

        public static float Float(string value)
        {
            return StringView.ToFloat(value);
        }

        public static byte Byte(object value)
        {
            if (value is int i)   return (byte)i;
            if (value is float f) return (byte)(int)f;
            Error(value, "Byte -- expected int or float");
            return 0;
        }

        public static int Int(object value)
        {
            if (value is int i) return i;
            if (value is float f) return (int)f;
            if (value is string s) return StringView.ToInt(s);
            Error(value, "Int -- expected int or float or string");
            return 0;
        }
    }

}
