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
        public override string ToString() => $"UserTypeSerializer {TheType.GetTypeName()}";

        // Shared Type Map for caching type serialization information
        public TypeSerializerMap TypeMap { get; }

        protected Map<string, DataField> Mapping; // field name to DataField mapping
        protected Array<DataField> Index;
        protected DataField PrimaryKeyName;
        protected DataField PrimaryKeyValue;
        protected readonly Type TheType;

        public IReadOnlyList<DataField> Fields => Index;

        protected UserTypeSerializer(Type type, TypeSerializerMap typeMap)
        {
            TheType = type;
            TypeMap = typeMap;
            if (type.GetCustomAttribute<StarDataTypeAttribute>() == null)
                throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");

            ResolveTypes();
        }

        public DataField GetField(int fieldId)
        {
            if (fieldId >= Index.Count)
                throw new IndexOutOfRangeException($"{this} invalid fieldId={fieldId}");
            return Index[fieldId];
        }

        public DataField GetFieldOrNull(string fieldName)
        {
            return Mapping.TryGetValue(fieldName, out DataField f) ? f : null;
        }

        void ResolveTypes()
        {
            Mapping = new Map<string, DataField>();
            Index = new Array<DataField>();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = TheType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = TheType.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    AddMapping(a, null, f);
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
                    AddMapping(a, p, null);
                }
            }
        }

        void AddMapping(StarDataAttribute a, PropertyInfo p, FieldInfo f)
        {
            int fieldIdx = Index.Count;
            var field = new DataField(TypeMap, fieldIdx, a, p, f);

            Mapping.Add(field.Name, field);
            Index.Add(field);

            if (a.IsPrimaryKeyName)
            {
                if (PrimaryKeyName != null)
                    throw new InvalidDataException($"StarDataSerializer cannot have more than 1 [StarDataKeyName] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyName = field;
            }
            else if (a.IsPrimaryKeyValue)
            {
                if (PrimaryKeyValue != null)
                    throw new InvalidDataException($"StarDataSerializer cannot have more than 1 [StarDataKeyValue] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyValue = field;
            }
        }
    }
}
