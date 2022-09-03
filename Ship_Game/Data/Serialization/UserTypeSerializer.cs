using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using SDUtils;

namespace Ship_Game.Data.Serialization
{
    using E = Expression;

    public abstract class UserTypeSerializer : TypeSerializer
    {
        public override string ToString() => $"UserTypeSerializer {NiceTypeName}:{TypeId}";

        // Shared Type Map for caching type serialization information
        public TypeSerializerMap TypeMap { get; }

        protected Map<string, DataField> Mapping; // field name to DataField mapping
        public DataField[] Fields;
        protected DataField PrimaryKeyName;
        protected DataField PrimaryKeyValue;

        // if true, this UserClass contains abstract or virtual properties
        public bool IsAbstractOrVirtual;

        // Method which is called when type has finished serialization
        // [StarDataDeserialized]
        // void OnDeserialized() { .. }
        // or
        // [StarDataDeserialized]
        // void OnDeserialized(RootObject root) { .. }
        public delegate void Deserialized(object obj, object root);

        // if not null, CreateInstance() should use this constructor instead,
        // substituting parameters with their default values
        readonly ConstructorInfo Constructor;

        delegate object New();
        New Ctor;

        protected UserTypeSerializer(Type type, TypeSerializerMap typeMap) : base(type)
        {
            TypeMap = typeMap;
            IsUserClass = true;
            Category = SerializerCategory.UserClass;

            var a = type.GetCustomAttribute<StarDataTypeAttribute>();
            if (a == null)
                throw new($"Unsupported type {type} - is the class missing [StarDataType] attribute?");
            if (a.TypeName != null)
                TypeName = a.TypeName;

            IsAbstractOrVirtual = type.IsAbstract;
            Constructor = GetDefaultConstructor();

            // NOTE: We cannot resolve types in the constructor, it would cause a stack overflow due to nested types
        }

        public override object CreateInstance()
        {
            try
            {
                if (Ctor == null)
                {
                    // for value types or parameterless constructors
                    if (Constructor == null)
                    {
                        // This is 30% faster than Activator.CreateInstance (no params)
                        E newE = E.New(Type);
                        if (Type.IsValueType) // box the struct
                            newE = E.Convert(newE, typeof(object));
                        Ctor = E.Lambda<New>(newE).Compile();
                    }
                    else
                    {
                        // this is 25x faster than Activator.CreateInstance (with params)
                        // and 5x faster than Constructor.Invoke
                        var p = Constructor.GetParameters();
                        var paramExpr = new E[p.Length];
                        for (int i = 0; i < p.Length; i++)
                            paramExpr[i] = E.Default(p[i].ParameterType);
                        Ctor = E.Lambda<New>(E.New(Constructor, paramExpr)).Compile();
                    }
                }
                return Ctor();
            }
            catch (Exception ex)
            {
                throw new($"UserType CreateInstance failed: {Type}", ex);
            }
        }

        ConstructorInfo GetDefaultConstructor()
        {
            if (Type.IsValueType)
                return null; // structs always have the default constructor

            var c = Type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (ConstructorInfo ctor in c)
            {
                if (ctor.GetParameters().Length == 0)
                    return null; // we're good, there's a default ctor

                if (ctor.GetCustomAttribute<StarDataConstructor>() != null)
                    return ctor;
            }

            throw new($"Missing a default constructor or [StarDataConstructor] attribute on type {Type}");
        }

        public (Deserialized,StarDataDeserialized) GetOnDeserializedEvt()
        {
            var (onDeserialized, a) = GetMethodWithAttribute<StarDataDeserialized>(Type);
            if (onDeserialized == null) return (null, null);

            var obj = E.Parameter(typeof(object), "obj");
            var root = E.Parameter(typeof(object), "root");
            var instance = E.Convert(obj, Type);
            E call;
            var p = onDeserialized.GetParameters();
            if (p.Length == 0)
                call = E.Call(instance, onDeserialized);
            else if (p.Length == 1)
                call = E.Call(instance, onDeserialized, E.Convert(root, p[0].ParameterType));
            else
                throw new InvalidOperationException($"{Type}.OnDeserialized event can only have 0 or 1 arguments");

            var evt = E.Lambda<Deserialized>(call, obj, root).Compile();
            return (evt, a);
        }

        static (MethodInfo, A) GetMethodWithAttribute<A>(Type type) where A : Attribute
        {
            Type attrType = typeof(A);
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                var a = (A)mi.GetCustomAttribute(attrType);
                if (a != null)
                    return (mi, a);
            }
            return (null, null);
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
            var index = new Array<DataField>();

            Type shouldSerialize = typeof(StarDataAttribute);
            PropertyInfo[] props = Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[]   fields = Type.GetFields(    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var dataFields = new Array<DataField>();

            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, Type, a, null, f);
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }

            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetGetMethod()?.IsVirtual == true)
                    IsAbstractOrVirtual = true;

                if (p.GetCustomAttribute(shouldSerialize) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, Type, a, p, null);
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }

            if (dataFields.IsEmpty)
            {
                // for abstract/virtual types, the base class is allowed to have no [StarData] fields
                Fields = Empty<DataField>.Array;
                if (Type.IsAbstract)
                    return;
                // give a warning for other types
                Log.Warning($"[StarDataType] {NiceTypeName} has no [StarData] fields, consider not serializing it!");
                return;
            }

            foreach (DataField field in dataFields)
            {
                field.FieldIdx = index.Count;
                Mapping.Add(field.Name, field);
                index.Add(field);
            }

            Fields = index.ToArr();
        }

        void CheckPrimaryKeys(StarDataAttribute a, DataField field)
        {
            if (a.IsPrimaryKeyName)
            {
                if (PrimaryKeyName != null)
                    throw new InvalidDataException($"[StarDataType] {NiceTypeName} cannot have more than 1 [StarDataKeyName] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyName = field;
            }
            else if (a.IsPrimaryKeyValue)
            {
                if (PrimaryKeyValue != null)
                    throw new InvalidDataException($"[StarDataType] {NiceTypeName} cannot have more than 1 [StarDataKeyValue] attributes! Original {PrimaryKeyValue}, New {field}");
                PrimaryKeyValue = field;
            }
        }
    }
}
