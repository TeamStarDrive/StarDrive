using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization
{
    public class TypeSerializer
    {
        // Id which is valid in a single serialization context
        internal ushort Id;

        public virtual object Convert(object value)
        {
            Log.Error($"Direct Convert not supported for {ToString()}. Value: {value}");
            return null;
        }

        public virtual object Deserialize(YamlNode node)
        {
            object value = node.Value;
            if (value == null)
                return null;
            return Convert(value);
        }

        public virtual void Serialize(MemoryStream ms, object obj)
        {
            
        }

        public virtual object Deserialize(MemoryStream ms)
        {
            return null;
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
    }

}
