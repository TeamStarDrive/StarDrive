using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization
{
    public class DataField
    {
        // Zero based field index that is set during serializer type resolve
        public int FieldIdx;
        public string Name;
        readonly PropertyInfo Prop;
        readonly FieldInfo Field;
        public readonly TypeSerializer Serializer;

        public override string ToString() => Prop?.ToString() ?? Field?.ToString() ?? "invalid";

        public DataField(TypeSerializerMap typeMap, StarDataAttribute a, PropertyInfo prop, FieldInfo field)
        {
            Name = a.NameId.NotEmpty() ? a.NameId : (prop?.Name ?? field.Name);
            Prop  = prop;
            Field = field;
            Type type = prop != null ? prop.PropertyType : field.FieldType;
            Serializer = typeMap.Get(type);
        }

        public void Set(object instance, object value)
        {
            if (Field != null) Field.SetValue(instance, value);
            else               Prop.SetValue(instance, value);
        }

        public object Get(object instance)
        {
            if (Field != null) return Field.GetValue(instance);
            else               return Prop.GetValue(instance);
        }

        public bool Serialize(YamlNode parent, object instance)
        {
            object value = Get(instance);
            if (value != null)
            {
                Serializer.Serialize(parent, value);
                return true;
            }
            return false;
        }

        public bool Serialize(BinaryWriter writer, object instance)
        {
            object value = Get(instance);
            if (value != null)
            {
                Serializer.Serialize(writer, value);
                return true;
            }
            return false;
        }

        public void SetConverted(object instance, object valueToConvert)
        {
            object value = Serializer.Convert(valueToConvert);
            Set(instance, value);
        }

        public void SetDeserialized(object instance, YamlNode node)
        {
            object value = Serializer.Deserialize(node);
            Set(instance, value);
        }
    }
}
