﻿using System;
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
        public class TextSerializerContext
        {
            // StringWriter(StringBuilder) or StreamWriter(File)
            public TextWriter Writer;
            public int Depth;
            public StringBuilder Buffer = new StringBuilder();

            // If True, next Serialized value will omit prefix spaces
            public bool IgnoreSpacePrefixOnce;

            public int TabSize = 2; // default tab size for Depth increase
        }
        
        // Id which is valid in a single serialization context
        internal ushort Id;
        internal Type Type;

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
        /// TEXT Serialize this object
        /// </summary>
        public abstract void Serialize(TextSerializerContext context, object obj);

        /// <summary>
        /// TEXT Serialize this object
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

        public static void WriteFieldId(BinaryWriter writer, int fieldId)
        {
            if (fieldId > 255)
                throw new IndexOutOfRangeException($"TypeSerializer could not handle so many fields: {fieldId} > 255");
            writer.Write((byte)fieldId);
        }

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
