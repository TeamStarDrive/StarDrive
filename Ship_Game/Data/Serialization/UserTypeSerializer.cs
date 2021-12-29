using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Ship_Game.Data.Serialization
{
    public abstract class UserTypeSerializer : TypeSerializer
    {
        public override string ToString() => $"UserTypeSerializer {Type.GetTypeName()}";

        // Shared Type Map for caching type serialization information
        public TypeSerializerMap TypeMap { get; }

        protected Map<string, DataField> Mapping; // field name to DataField mapping
        protected Array<DataField> Index;
        protected DataField PrimaryKeyName;
        protected DataField PrimaryKeyValue;

        public IReadOnlyList<DataField> Fields => Index;

        protected UserTypeSerializer(Type type, TypeSerializerMap typeMap) : base(type)
        {
            TypeMap = typeMap;
            IsUserClass = true;

            if (type.GetCustomAttribute<StarDataTypeAttribute>() == null)
                throw new InvalidDataException($"Unsupported type {type} - is the class missing [StarDataType] attribute?");

            // NOTE: We cannot resolve types in the constructor, it would cause a stack overflow due to nested types
        }

        public DataField GetFieldOrNull(uint fieldIdx)
        {
            if (fieldIdx >= Index.Count)
                return null;
            return Index[(int)fieldIdx];
        }

        public DataField GetFieldOrNull(string fieldName)
        {
            return Mapping.TryGetValue(fieldName, out DataField f) ? f : null;
        }

        // This is somewhat slow, which is why it should be done only once,
        // and all fields should be immutable
        public void ResolveTypes()
        {
            if (Mapping != null)
                return;

            Mapping = new Map<string, DataField>();
            Index = new Array<DataField>();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = Type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var dataFields = new Array<DataField>();

            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, a, null, f);
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }
            
            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    MethodInfo setter = p.GetSetMethod(nonPublic: true);
                    if (setter == null)
                        throw new Exception($"[StarDataType] {Type.GetTypeName()} Property {p.Name} has no setter!");

                    var field = new DataField(TypeMap, a, p, null);
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }

            if (dataFields.IsEmpty)
            {
                Log.Warning($"[StarDataType] {Type.GetTypeName()} has no [StarData] fields, consider not serializing it!");
                return;
            }

            // sorting by name will give fields easy stability even if they are shuffled around
            dataFields.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (DataField field in dataFields)
            {
                field.FieldIdx = Index.Count;
                Mapping.Add(field.Name, field);
                Index.Add(field);
            }
        }

        void CheckPrimaryKeys(StarDataAttribute a, DataField field)
        {
            if (a.IsPrimaryKeyName)
            {
                if (PrimaryKeyName != null)
                    throw new InvalidDataException($"[StarDataType] {Type.GetTypeName()} cannot have more than 1 [StarDataKeyName] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyName = field;
            }
            else if (a.IsPrimaryKeyValue)
            {
                if (PrimaryKeyValue != null)
                    throw new InvalidDataException($"[StarDataType] {Type.GetTypeName()} cannot have more than 1 [StarDataKeyValue] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyValue = field;
            }
        }
    }
}
