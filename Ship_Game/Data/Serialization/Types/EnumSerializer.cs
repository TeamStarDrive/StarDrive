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
        readonly Map<int, object> OrdinalMap = new();
        readonly bool IsFlagsEnum;

        delegate int GetOrdinal(object enumValue);
        readonly GetOrdinal GetOrdinalOf;

        public EnumSerializer(Type toEnum) : base(toEnum)
        {
            Array values = toEnum.GetEnumValues();
            DefaultValue = values.GetValue(0);
            IsFlagsEnum = Attribute.IsDefined(toEnum, typeof(FlagsAttribute), inherit:false);

            GetOrdinalOf = InitGetOrdinalOf(toEnum);

            for (int i = 0; i < values.Length; ++i)
            {
                var enumValue = values.GetValue(i);
                int enumOrdinal = GetOrdinalOf(enumValue);
                OrdinalMap[enumOrdinal] = enumValue;
            }
        }

        GetOrdinal InitGetOrdinalOf(Type toEnum)
        {
            Type underlyingType = toEnum.GetEnumUnderlyingType();

            // duplicating a lot of code here due to EnumSerializer init performance reasons
            if (underlyingType == typeof(int)) return GetOrdinalInt;
            if (underlyingType == typeof(uint)) return GetOrdinalUInt;
            if (underlyingType == typeof(byte)) return GetOrdinalByte;
            if (underlyingType == typeof(short)) return GetOrdinalShort;
            if (underlyingType == typeof(ushort)) return GetOrdinalUShort;
            return GetOrdinalInt;
        }

        static int GetOrdinalInt(object value) => (int)value;
        static int GetOrdinalUInt(object value) => (int)(uint)value;
        static int GetOrdinalByte(object value) => (byte)value;
        static int GetOrdinalShort(object value) => (short)value;
        static int GetOrdinalUShort(object value) => (ushort)value;

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
            int enumIndex = GetOrdinalOf(obj);
            writer.BW.WriteVLi32(enumIndex);
        }
        
        public override object Deserialize(BinarySerializerReader reader)
        {
            int enumIndex = reader.BR.ReadVLi32();
            if (OrdinalMap.TryGetValue(enumIndex, out object enumValue))
                return enumValue;

            // 0 is now equivalent of DefaultValue
            if (enumIndex == 0)
                return DefaultValue;

            if (IsFlagsEnum)
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