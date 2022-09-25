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

        // Method which is called when an object is about to be serialized
        // [StarDataSerialize]
        // StarDataDynamicField[] OnSerialize() { ... }
        public delegate StarDataDynamicField[] OnSerialize(object obj);
        readonly OnSerialize OnSerializeEvt;

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
            OnSerializeEvt = GetOnSerializeEvt();

            // This is an important edge case. If this is a Reference type and inherits from
            // IEquatable, the serializer will accidentally squash objects
            // Better to log an error here than try and look for these weird bugs
            // Business logic should instead implement IEqualityComparer<T>
            if (!IsValueType)
            {
                Type[] interfaces = type.GetInterfaces();
                if (interfaces.Length > 0)
                {
                    var equatableType = typeof(IEquatable<>).MakeGenericType(type);
                    if (interfaces.Contains(equatableType))
                        throw new($"Reference Type {type} implements IEquatable<> which will squash reference objects during serialization");
                }
            }

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
            // structs always have the default ctor, interfaces don't have ctors
            if (Type.IsValueType || Type.IsInterface)
                return null;

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

        public StarDataDynamicField[] InvokeOnSerializeEvt(object obj)
        {
            return OnSerializeEvt?.Invoke(obj);
        }

        OnSerialize GetOnSerializeEvt()
        {
            var (onSerialize, _) = GetMethodWithAttribute<StarDataSerialize>(Type);
            if (onSerialize == null) return null;

            var obj = E.Parameter(typeof(object), "obj");
            E call = E.Call(E.Convert(obj, Type), onSerialize);
            return E.Lambda<OnSerialize>(call, obj).Compile();
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

        // Flags for only getting fields/props from the current Type, ignoring base type
        const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        // This is somewhat slow, which is why it should be done only once,
        // and all fields should be immutable
        public void ResolveTypes()
        {
            if (Mapping != null)
                return;

            Mapping = new();

            var dataFields = new Array<DataField>();
            GetFieldsAndProps(dataFields, Type, typeof(StarDataAttribute));
            Fields = dataFields.ToArr();

            if (Fields.Length == 0)
            {
                // for abstract/virtual types, the base class is allowed to have no [StarData] fields
                if (Type.IsAbstract)
                    return;
                // give a warning for other types
                Log.Warning($"[StarDataType] {NiceTypeName} has no [StarData] fields, consider not serializing it!");
                return;
            }

            foreach (DataField field in Fields)
                Mapping.Add(field.Name, field);
        }

        void GetFieldsAndProps(Array<DataField> dataFields, Type type, Type shouldSerializeAttr)
        {
            // depth first search
            Type baseType = type.BaseType;
            if (baseType != null)
            {
                var systemBaseType = type.IsValueType ? typeof(ValueType) : typeof(object);
                if (baseType != systemBaseType)
                    GetFieldsAndProps(dataFields, baseType, shouldSerializeAttr);
            }

            FieldInfo[] fields = type.GetFields(InstanceFlags);
            PropertyInfo[] props = type.GetProperties(InstanceFlags);

            for (int i = 0; i < fields.Length; ++i)
            {
                FieldInfo f = fields[i];
                if (f.GetCustomAttribute(shouldSerializeAttr) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, Type, a, null, f)
                    { FieldIdx = dataFields.Count };
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }

            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo p = props[i];
                if (p.GetGetMethod()?.IsVirtual == true)
                    IsAbstractOrVirtual = true;

                if (p.GetCustomAttribute(shouldSerializeAttr) is StarDataAttribute a)
                {
                    var field = new DataField(TypeMap, Type, a, p, null)
                    { FieldIdx = dataFields.Count };
                    dataFields.Add(field);
                    CheckPrimaryKeys(a, field);
                }
            }
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
