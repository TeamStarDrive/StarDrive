using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data.Serialization
{
    public abstract class UserTypeSerializer : TypeSerializer
    {
        public override string ToString() => $"UserTypeSerializer {TheType.GenericName()}";

        protected Map<string, DataField> Mapping;
        protected DataField Primary;
        protected readonly Type TheType;

        protected UserTypeSerializer(Type type)
        {
            TheType = type;
            if (type.GetCustomAttribute<StarDataTypeAttribute>() == null)
                throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
        }

        protected abstract TypeSerializerMap CreateTypeMap();

        protected void ResolveTypes()
        {
            Mapping = new Map<string, DataField>();
            TypeSerializerMap typeMap = CreateTypeMap();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = TheType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = TheType.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    string id = a.Id.NotEmpty() ? a.Id : f.Name;
                    AddMapping(id, a, new DataField(typeMap, null, f));
                }
            }
            
            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    MethodInfo setter = p.GetSetMethod(nonPublic: true);
                    if (setter == null)
                        throw new Exception($"StarDataSerializer Class {TheType.Name} Property {p.Name} has no setter!");

                    string id = a.Id.NotEmpty() ? a.Id : p.Name;
                    AddMapping(id, a, new DataField(typeMap, p, null));
                }
            }
        }

        void AddMapping(string name, StarDataAttribute a, DataField info)
        {
            Mapping.Add(name, info);
            if (a.IsPrimaryKey)
            {
                if (Primary != null)
                    throw new InvalidDataException($"StarDataSerializer cannot have more than 1 [StarDataKey] attributes! Original {Primary}, New {info}");
                Primary = info;
            }
        }
    }
}
