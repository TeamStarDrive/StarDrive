using System;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary;

using IndexMap = Map<string, int>;

public class BinarySerializerWriter
{
    public Writer BW;

    // total number of objects
    public int NumObjects;

    // index of the root object which is being serialized/deserialized
    public uint RootObjectId;

    // all scanned types, organized into categories, in sorted order
    public ScannedTypes Types;

    // Object lists grouped by their type
    // includes UserClasses, Array<T>, Map<K,V>, strings
    SerializationTypeGroup[] TypeGroups;

    // gets the biggest TypeId
    public int MaxTypeId => Types.All.Max(s => s.TypeId);

    // how many TypeGroups are going to be written to the binary stream?
    public int NumTypeGroups => TypeGroups.Length;

    public bool Verbose;

    public BinarySerializerWriter(Writer writer)
    {
        BW = writer;
    }

    public void ScanObjects(BinarySerializer rootSer, object rootObject)
    {
        var rs = new ObjectScanner(rootSer, rootObject);
        rs.CreateWriteCommands();

        NumObjects = rs.NumObjects;
        TypeGroups = rs.TypeGroups;
        Types = rs.Types;

        Log.Info($"Serializer NumObjects={NumObjects} NumTypes={TypeGroups.Length}");

        // find the root object ID from the neatly sorted type groups
        RootObjectId = rs.RootObjectId;
    }

    string[] MapSingle(Func<TypeSerializer, string> selector)
    {
        var names = new HashSet<string>();
        foreach (TypeSerializer s in Types.ValuesAndClasses)
            names.Add(selector(s));
        return names.ToArr();
    }

    string[] MapFields()
    {
        var names = new HashSet<string>();
        foreach (TypeSerializer s in Types.ValuesAndClasses)
            if (s is UserTypeSerializer us)
                foreach (DataField field in us.Fields)
                    names.Add(field.Name);
        return names.ToArr();
    }

    static IndexMap CreateIndexMap(string[] names)
    {
        Array.Sort(names);
        var indexMap = new IndexMap();
        for (int i = 0; i < names.Length; ++i)
            indexMap.Add(names[i], i);
        return indexMap;
    }

    (string[] Names, IndexMap Index) GetMappedNames(Func<TypeSerializer, string> selector)
    {
        string[] names = MapSingle(selector);
        return (names, CreateIndexMap(names));
    }

    (string[] Names, IndexMap Index) GetMappedFields()
    {
        string[] names = MapFields();
        return (names, CreateIndexMap(names));
    }

    static string GetAssembly(TypeSerializer s) => s.Type.Assembly.GetName().Name;
    static string GetTypeName(TypeSerializer s) => s.TypeName;
    static string GetNamespace(TypeSerializer s)
    {
        // for nested types nameSpace is the full name of the containing type
        return s.Type.IsNested ? s.Type.DeclaringType!.FullName : s.Type.Namespace;
    }

    void WriteArray(string[] strings)
    {
        BW.WriteVLu32((uint)strings.Length);
        foreach (string s in strings)
            BW.Write(s);
    }

    public void WriteTypesList()
    {
        // [assemblies]
        // [namespaces]
        // [typenames]
        // [fieldnames]
        // [used types]
        // [collection types]
        (string[] assemblies, IndexMap assemblyMap)  = GetMappedNames(GetAssembly);
        (string[] namespaces, IndexMap namespaceMap) = GetMappedNames(GetNamespace);
        (string[] typeNames,  IndexMap typenameMap)  = GetMappedNames(GetTypeName);
        (string[] fieldNames, IndexMap fieldNameMap) = GetMappedFields();
        WriteArray(assemblies);
        WriteArray(namespaces);
        WriteArray(typeNames);
        WriteArray(fieldNames);

        foreach (TypeSerializer s in Types.ValuesAndClasses)
            WriteTypeInfo(s, assemblyMap, namespaceMap, typenameMap, fieldNameMap);

        foreach (CollectionSerializer s in Types.Collections)
            WriteCollectionTypeInfo(s);
    }

    void WriteTypeInfo(TypeSerializer s, IndexMap assemblyMap, IndexMap namespaceMap,
                       IndexMap typenameMap, IndexMap fieldNameMap)
    {
        if (Verbose) Log.Info($"Write {s.Category} {s.TypeId}:{s.NiceTypeName}");
        // [type ID]
        // [assembly ID]
        // [namespace ID]
        // [typename ID]
        // [type flags]
        // [fields info]
        int assemblyId  = assemblyMap[GetAssembly(s)];
        int namespaceId = namespaceMap[GetNamespace(s)];
        int typenameId  = typenameMap[GetTypeName(s)];
        int flags = 0;
        if (s.IsStruct)      flags |= 0b0000_0001; // struct
        if (s.IsEnumType)    flags |= 0b0000_0010; // enum
        if (s.Type.IsNested) flags |= 0b0000_0100; // requires nested namespace resolution
        BW.WriteVLu32((uint)s.TypeId);
        BW.WriteVLu32((uint)assemblyId);
        BW.WriteVLu32((uint)namespaceId);
        BW.WriteVLu32((uint)typenameId);
        BW.WriteVLu32((uint)flags);

        if (s is UserTypeSerializer us)
        {
            BW.WriteVLu32((uint)us.Fields.Length);
            foreach (DataField field in us.Fields)
            {
                // [field type ID]
                // [field name index]
                BW.WriteVLu32((uint)field.Serializer.TypeId);
                BW.WriteVLu32((uint)fieldNameMap[field.Name]);
            }
        }
        else
        {
            BW.WriteVLu32(0); // count = 0
        }
    }

    void WriteCollectionTypeInfo(CollectionSerializer s)
    {
        if (Verbose) Log.Info($"Write {s.Category} {s.TypeId}:{s.NiceTypeName}");
        // [type ID]
        // [collection type]   1:T[] 2:Array<T> 3:Map<K,V> 4:HashSet<T>
        // [element type ID]
        // [key type ID] (only for Map<K,V>)
        uint type = 0;
        if      (s is RawArraySerializer)  type = 1;
        else if (s is ArrayListSerializer) type = 2;
        else if (s is MapSerializer)       type = 3;
        else if (s is HashSetSerializer)   type = 4;
        BW.WriteVLu32((uint)s.TypeId);
        BW.WriteVLu32(type);
        BW.WriteVLu32((uint)s.ElemSerializer.TypeId);
        if (s is MapSerializer ms)
            BW.WriteVLu32((uint)ms.KeySerializer.TypeId);
    }

    public void WriteObjects()
    {
        if (Verbose) Log.Info($"WriteObjects {NumObjects}");

        // The pre-instance and Object data is ordered 
        // [fundamental/enums/structs]
        // [raw arrays]
        // [classes]
        // [collections]
        var valTypes    = FilterGroups(t => t.IsFundamentalType || t.IsValueType);
        var rawArrays   = FilterGroups(t => !t.IsFundamentalType && t is RawArraySerializer);
        var classes     = FilterGroups(t => t.IsUserClass && !t.IsValueType);
        var collections = FilterGroups(t => t.IsCollection && t is not RawArraySerializer);

        // Type PreInstance Info
        foreach (var t in valTypes)    WritePreInstance(t);
        foreach (var t in rawArrays)   WritePreInstance(t);
        foreach (var t in classes)     WritePreInstance(t);
        foreach (var t in collections) WritePreInstance(t);

        // Object Fields and Elements
        foreach (var t in valTypes)    WriteGroup(t);
        foreach (var t in rawArrays)   WriteGroup(t);
        foreach (var t in classes)     WriteGroup(t);
        foreach (var t in collections) WriteGroup(t);
    }

    SerializationTypeGroup[] FilterGroups(Func<TypeSerializer, bool> filter)
    {
        return TypeGroups.Filter(t => filter(t.Type));
    }

    void WritePreInstance(in SerializationTypeGroup g)
    {
        int count = g.GroupedObjects.Length;
        uint baseObjectId = g.GroupedObjects[0].Id;
        var t = g.Type;
        if (Verbose)
            Log.Info($"WritePreInstance {t.Category} {t.TypeId}:{t.NiceTypeName}  N={count} baseId={baseObjectId}");

        BW.WriteVLu32((uint)t.TypeId);
        BW.WriteVLu32((uint)count);
        // baseObjectId is required because TypeGroup order doesn't match object id-s
        BW.WriteVLu32(baseObjectId);
    }

    void WriteGroup(in SerializationTypeGroup g)
    {
        var objects = g.GroupedObjects;
        var t = g.Type;
        if (Verbose)
            Log.Info($"WriteObjects {t.Category} {t.TypeId}:{t.NiceTypeName}  N={objects.Length}");

        BW.WriteVLu32((uint)t.TypeId);
        BW.WriteVLu32((uint)objects.Length);
        for (int i = 0; i < objects.Length; ++i)
        {
            ObjectState state = objects[i];
            state.Serialize(this, t);
        }
    }
}
