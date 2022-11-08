using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using SDUtils;

namespace Ship_Game.Data.Serialization;

using E = Expression;

public abstract class UserTypeSerializer : TypeSerializer
{
    public override string ToString() => $"UserTypeSerializer {NiceTypeName}:{TypeId}";

    // Shared Type Map for caching type serialization information
    public TypeSerializerMap TypeMap { get; }

    public DataField[] Fields;
    protected Map<string, DataField> Mapping; // field name to DataField mapping
    protected DataField PrimaryKeyName;
    protected DataField PrimaryKeyValue;

    // if true, this UserClass contains abstract or virtual properties
    public bool IsAbstractOrVirtual;

    // if true, this UserClass inherits from IEquatable<T>
    // which requires special equality handling for REFERENCE types
    // since, while two reference types might be "equal", they must be
    // serialized separately
    public bool IsIEquatableT;

    // Method which is called when an object is about to be serialized
    // [StarDataSerialize]
    // StarDataDynamicField[] OnSerialize() { ... }
    public delegate StarDataDynamicField[] OnSerialize(object obj);

    // Method which is called when type has finished serialization
    // [StarDataDeserialized]
    // void OnDeserialized() { .. }
    // or
    // [StarDataDeserialized]
    // void OnDeserialized(RootObject root) { .. }
    public delegate void Deserialized(object obj, object root);

    delegate object New();
    New Ctor;

    protected UserTypeSerializer(Type type, TypeSerializerMap typeMap) : base(type)
    {
        TypeMap = typeMap;
        IsUserClass = true;
        Category = SerializerCategory.UserClass;

        if (Attribute.GetCustomAttribute(type, typeof(StarDataTypeAttribute), inherit:false) is not StarDataTypeAttribute a)
            throw new($"Unsupported type {type} - is the class missing [StarDataType] attribute?");

        if (a.TypeName != null)
            TypeName = a.TypeName;

        IsAbstractOrVirtual = type.IsAbstract;

        // This is an important edge case. If this is a Reference type and inherits
        // from IEquatable, the serializer will accidentally squash objects
        // Better to log an error here than try and look for these weird bugs
        // Business logic should instead implement IEqualityComparer<T>
        if (!IsValueType)
        {
            // TODO: find an actual fix for this
            Type[] interfaces = type.GetInterfaces();
            if (interfaces.Length > 0)
            {
                var equatableType = typeof(IEquatable<>).MakeGenericType(type);
                IsIEquatableT = interfaces.Contains(equatableType);
            }
        }

        // NOTE: We cannot resolve types in the constructor,
        // it would cause a stack overflow due to nested types
    }

    bool CheckedDefaultConstructor;
    // if not null, CreateInstance() should use this constructor instead,
    // substituting parameters with their default values
    ConstructorInfo Constructor;

    public override object CreateInstance()
    {
        try
        {
            if (Ctor == null)
            {
                if (!CheckedDefaultConstructor)
                {
                    CheckedDefaultConstructor = true;
                    Constructor = GetDefaultConstructor();
                }

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

            if (Attribute.IsDefined(ctor, typeof(StarDataConstructor)))
                return ctor;
        }

        throw new($"Missing a default constructor or [StarDataConstructor] attribute on type {Type}");
    }

    bool CheckedOnSerializeEvt;
    OnSerialize OnSerializeEvt;

    public StarDataDynamicField[] InvokeOnSerializeEvt(object obj)
    {
        return GetOnSerializeEvt()?.Invoke(obj);
    }

    OnSerialize GetOnSerializeEvt()
    {
        if (CheckedOnSerializeEvt)
            return OnSerializeEvt;

        CheckedOnSerializeEvt = true;
        var (onSerialize, _) = GetMethodWithAttribute<StarDataSerialize>(Type);
        if (onSerialize == null) return null;

        var obj = E.Parameter(typeof(object), "obj");
        E call = E.Call(E.Convert(obj, Type), onSerialize);
        OnSerializeEvt = E.Lambda<OnSerialize>(call, obj).Compile();
        return OnSerializeEvt;
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

    // the NonPublic methods are only retrieved for current type, not for base type(s)
    const BindingFlags PublicFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    const BindingFlags PrivateFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    static (MethodInfo, A) GetMethodWithAttribute<A>(Type type) where A : Attribute
    {
        Type attrType = typeof(A);
        (MethodInfo info, A attr) = GetMethodsWithAttribute<A>(type, attrType, PublicFlags);
        if (info != null) return (info, attr);

        // if the class has inheritance, check non-public members of the parent
        Type baseType = GetClassBaseType(type);
        return baseType != null ? GetPrivateMethodWithAttributeRecursive<A>(baseType, attrType) : (null, null);
    }

    static (MethodInfo, A) GetPrivateMethodWithAttributeRecursive<A>(Type type, Type attrType) where A : Attribute
    {
        while (true)
        {
            (MethodInfo info, A attr) = GetMethodsWithAttribute<A>(type, attrType, PrivateFlags);
            if (info != null) return (info, attr);

            Type baseType = GetClassBaseType(type);
            if (baseType == null)
                return (null, null);

            type = baseType;
        }
    }

    static (MethodInfo, A) GetMethodsWithAttribute<A>(Type type, Type attrType, BindingFlags flags) where A : Attribute
    {
        var methods = type.GetMethods(flags);
        foreach (MethodInfo mi in methods)
        {
            // checking IsDefined is much faster than directly calling GetCustomAttribute
            if (Attribute.IsDefined(mi, attrType, inherit: false))
                return (mi, (A)Attribute.GetCustomAttribute(mi, attrType, inherit: false));
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
    internal void ResolveTypes()
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
    
    protected void ScanRootType()
    {
        TypeMap.Add(this);
        while (TypeMap.PendingResolve.TryPopLast(out UserTypeSerializer us))
        {
            us.ResolveTypes();
        }
        TypeMap.PendingResolve = null; // disable pending resolver for further dynamic Get(type) calls
    }

    static Type GetClassBaseType(Type type)
    {
        if (type.IsValueType) return null;
        Type baseType = type.BaseType;
        return baseType != null && baseType != typeof(object) ? baseType : null;
    }

    void GetFieldsAndProps(Array<DataField> dataFields, Type type, Type shouldSerializeAttr)
    {
        // depth first search
        Type baseType = GetClassBaseType(type);
        if (baseType != null)
            GetFieldsAndProps(dataFields, baseType, shouldSerializeAttr);

        FieldInfo[] fields = type.GetFields(InstanceFlags);
        PropertyInfo[] props = type.GetProperties(InstanceFlags);

        for (int i = 0; i < fields.Length; ++i)
        {
            FieldInfo f = fields[i];
            if (Attribute.IsDefined(f, shouldSerializeAttr, inherit:false))
            {
                var a = (StarDataAttribute)Attribute.GetCustomAttribute(f, shouldSerializeAttr, inherit:false);
                var field = new DataField(TypeMap, Type, a, null, f) { FieldIdx = dataFields.Count };
                dataFields.Add(field);
                CheckPrimaryKeys(a, field);
            }
        }

        for (int i = 0; i < props.Length; ++i)
        {
            PropertyInfo p = props[i];

            // Always set virtual if there are virtual properties
            // TODO: but what about methods?
            // TODO: maybe this needs to be set for all classes using inheritance?
            if (p.GetGetMethod()?.IsVirtual == true)
                IsAbstractOrVirtual = true;

            if (Attribute.IsDefined(p, shouldSerializeAttr, inherit:false))
            {
                var a = (StarDataAttribute)Attribute.GetCustomAttribute(p, shouldSerializeAttr, inherit:false);
                var field = new DataField(TypeMap, Type, a, p, null) { FieldIdx = dataFields.Count };
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
