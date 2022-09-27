using System;
using System.Linq.Expressions;
using System.Reflection;
using SDUtils;
using Ship_Game.Data.Binary;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization.Types
{
    using E = Expression;

    internal class EnumSerializer : TypeSerializer
    {
        public override string ToString() => $"{TypeId}:EnumSerializer {NiceTypeName}";
        readonly Map<int, object> Mapping = new();
        readonly bool IsFlagsEnum;

        delegate int GetIntValue(object enumValue);
        readonly GetIntValue GetValueOf;

        public EnumSerializer(Type toEnum) : base(toEnum)
        {
            Array values = toEnum.GetEnumValues();
            DefaultValue = values.GetValue(0);
            IsFlagsEnum = toEnum.GetCustomAttribute<FlagsAttribute>() != null;

            // precompile enum to integer conversion, otherwise it's too slow
            var enumVal = E.Parameter(typeof(object), "enumValue");
            
            Type underlyingType = toEnum.GetEnumUnderlyingType();

            // (object enumValue) => (int)enumValue;
            // (object enumValue) => (int)(T)enumValue;
            E toInteger = underlyingType == typeof(int)
                ? E.Convert(enumVal, typeof(int))
                : E.Convert(E.Convert(enumVal, underlyingType), typeof(int));

            GetValueOf = E.Lambda<GetIntValue>(toInteger, enumVal).Compile();

            for (int i = 0; i < values.Length; ++i)
            {
                var enumValue = values.GetValue(i);
                int enumIndex = GetValueOf(enumValue);
                Mapping[enumIndex] = enumValue;
            }
        }

        public override object Convert(object value)
        {
            try
            {
                if (value is string enumLiteral)
                    return Enum.Parse(Type, enumLiteral, ignoreCase:true);
                if (value is int enumIndex)
                    return Enum.ToObject(Type, enumIndex);
                Error(value, $"Enum '{Type.Name}' -- expected a string or int");
            }
            catch (Exception e)
            {
                Error(value, $"Enum '{Type.Name}' -- {e.Message}");
            }
            return DefaultValue;
        }

        public override void Serialize(YamlNode parent, object obj)
        {
            var e = (Enum)obj;
            parent.Value = e.ToString();
        }

        public override void Serialize(BinarySerializerWriter writer, object obj)
        {
            int enumIndex = GetValueOf(obj);
            writer.BW.WriteVLi32(enumIndex);
        }
        
        public override object Deserialize(BinarySerializerReader reader)
        {
            int enumIndex = reader.BR.ReadVLi32();
            if (Mapping.TryGetValue(enumIndex, out object enumValue))
                return enumValue;

            if (IsFlagsEnum && enumIndex != 0)
            {
                try
                {
                    return Enum.ToObject(Type, enumIndex);
                }
                catch
                {
                }
            }

            Error(enumIndex, $"Enum '{Type.Name}' -- using Default value '{DefaultValue}' instead");
            return DefaultValue;
        }
    }
}