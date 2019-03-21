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
        public int Id;
        readonly PropertyInfo Prop;
        readonly FieldInfo Field;
        readonly TypeSerializer Serializer;
        
        public override string ToString() => Prop?.ToString() ?? Field?.ToString() ?? "invalid";

        public DataField(int id, TypeSerializerMap typeMap, PropertyInfo prop, FieldInfo field)
        {
            Id = id;
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

        public void Serialize(BinaryWriter writer, object instance)
        {
            object value = Get(instance);
            Serializer.Serialize(writer, Id, value);
        }

        public void Deserialize(BinaryReader reader, object instance)
        {
            object value = Serializer.Deserialize(reader);
            Set(instance, value);
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
