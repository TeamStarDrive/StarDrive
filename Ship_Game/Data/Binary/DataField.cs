using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Binary
{
    class DataField
    {
        readonly PropertyInfo Prop;
        readonly FieldInfo Field;
        readonly TypeSerializer Converter;

        public DataField(BinaryConverters converters, PropertyInfo prop, FieldInfo field)
        {
            Prop = prop;
            Field = field;
            Type type = prop != null ? prop.PropertyType : field.FieldType;
            Converter = converters.Get(type);
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
    }
}
