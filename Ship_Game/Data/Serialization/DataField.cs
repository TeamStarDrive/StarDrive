using System;
using System.Linq.Expressions;
using System.Reflection;
using Ship_Game.Data.Yaml;

namespace Ship_Game.Data.Serialization
{
    using E = Expression;

    public class DataField
    {
        // Zero based field index that is set during serializer type resolve
        public int FieldIdx;
        public readonly string Name;
        public readonly TypeSerializer Serializer;

        public delegate object Getter(object instance);
        public delegate void Setter(object instance, object value);
        public readonly Getter Get;
        public readonly Setter Set;

        public override string ToString() => $"{Serializer?.NiceTypeName ?? "invalid"} {Name}:{FieldIdx}";

        public DataField(TypeSerializerMap typeMap, Type instanceType, StarDataAttribute a,
                         PropertyInfo prop, FieldInfo field)
        {
            if (prop != null)
            {
                // if the property is defined in base class, we need to fetch the property
                // through declaring class type in order to access the Setter
                var p = (prop.DeclaringType != instanceType) ? prop.DeclaringType?.GetProperty(prop.Name) : prop;
                if (p?.SetMethod == null)
                    throw new Exception($"[StarData] {instanceType.FullName}.{prop.Name} has no setter! Add a private setter.");
                prop = p;
            }

            Name = a.NameId.NotEmpty() ? a.NameId : (prop?.Name ?? field.Name);
            Type type = prop != null ? prop.PropertyType : field.FieldType;
            Serializer = typeMap.Get(type);
            
            MemberInfo m = prop ?? field as MemberInfo;
            try
            {
                // precompile the getter
                var obj = E.Parameter(typeof(object), "instance");
                var castToClassType = E.Convert(obj, m.ReflectedType);
                var member = field != null
                    ? E.Field(castToClassType, field)
                    : E.Property(castToClassType, prop);

                Get = E.Lambda<Getter>(E.Convert(member, typeof(object)), obj).Compile();

                // it's slow, but the only thing that really works for all cases
                if (field != null)
                    Set = field.SetValue;
                else
                    Set = prop.SetValue;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to generate Get/Set for: {type} {m.DeclaringType}.{m.Name}");
                throw;
            }
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
