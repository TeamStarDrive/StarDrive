using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary;

// Binary Object counts grouped by their type (includes strings)
// TypeInfo Type is mandatory
public record struct TypeGroup(TypeInfo Type, TypeSerializer Ser, int Count, int BaseId);

/// <summary>
/// Core logic of the binary reader
/// </summary>
public class BinarySerializerReader
{
    public readonly Reader BR;
    public readonly TypeSerializerMap TypeMap;
    readonly BinarySerializerHeader Header;
    TypeGroup[] TypeGroups;
    readonly TypeInfo[] StreamTypes;

    // flat list of deserialized objects
    public object[] ObjectsList;

    public bool Verbose;

    public BinarySerializerReader(Reader reader, TypeSerializerMap typeMap, in BinarySerializerHeader header)
    {
        BR = reader;
        TypeMap = typeMap;
        Header = header;
        StreamTypes = new TypeInfo[header.MaxTypeId + 1];
        SetFundamentalTypes(typeMap);
    }

    void SetFundamentalTypes(TypeSerializerMap typeMap)
    {
        for (uint typeId = 1; typeId < TypeSerializer.MaxFundamentalTypes; ++typeId)
        {
            if (!typeMap.TryGet(typeId, out TypeSerializer s))
                break;
            AddTypeInfo(typeId, s.Type.Name, s, null, isStruct:false, SerializerCategory.Fundamental);
        }
    }

    void AddTypeInfo(uint streamTypeId, string name, TypeSerializer s, FieldInfo[] fields, bool isStruct, SerializerCategory c)
    {
        var info = new TypeInfo(streamTypeId, name, s, fields, isStruct, c);
        StreamTypes[streamTypeId] = info;
    }

    void AddDeletedTypeInfo(uint streamTypeId, string name, FieldInfo[] fields, bool isStruct, SerializerCategory c)
    {
        var info = new TypeInfo(streamTypeId, name, null, fields, isStruct, c);
        StreamTypes[streamTypeId] = info;
    }

    TypeInfo GetType(uint streamTypeId)
    {
        return StreamTypes[streamTypeId];
    }

    TypeInfo GetTypeOrNull(uint streamTypeId)
    {
        return streamTypeId < StreamTypes.Length ? StreamTypes[streamTypeId] : null;
    }

    static string[] ReadStringArray(Reader br)
    {
        string[] items = new string[br.ReadVLu32()];
        for (int i = 0; i < items.Length; ++i)
            items[i] = br.ReadString();
        return items;
    }

    public void ReadTypesList()
    {
        // [assemblies]
        // [namespaces]
        // [typeNames]
        // [fieldNames]
        // [types]
        string[] assemblies = ReadStringArray(BR);
        string[] namespaces = ReadStringArray(BR);
        string[] typeNames  = ReadStringArray(BR);
        string[] fieldNames = ReadStringArray(BR);

        if (Verbose) Log.Info($"Reading {Header.NumUsedTypes} Types");
        for (uint i = 0; i < Header.NumUsedTypes; ++i)
        {
            // [type ID]
            // [assembly ID]
            // [namespace ID]
            // [typename ID]
            // [type flags]
            // [fields info]
            uint typeId = BR.ReadVLu32();
            uint asmId  = BR.ReadVLu32();
            uint nsId   = BR.ReadVLu32();
            uint nameId = BR.ReadVLu32();
            uint flags  = BR.ReadVLu32();
            uint numFields = BR.ReadVLu32();
            bool isStruct      = (flags & 0b0000_0001) != 0;
            bool isEnumType    = (flags & 0b0000_0010) != 0;
            bool isNestedType  = (flags & 0b0000_0100) != 0;

            var fields = new FieldInfo[numFields];
            for (uint fieldIdx = 0; fieldIdx < fields.Length; ++fieldIdx)
            {
                // [field type ID]
                // [field name index]
                uint fieldTypeId = BR.ReadVLu32();
                uint nameIdx = BR.ReadVLu32();
                fields[fieldIdx] = new FieldInfo
                {
                    StreamTypeId = (ushort)fieldTypeId,
                    Name = nameIdx < fieldNames.Length ? fieldNames[nameIdx] : null,
                };
            }

            var c = isEnumType ? SerializerCategory.Enums : SerializerCategory.UserClass;
            string name = typeNames[nameId];
            Type type = GetTypeFrom(assemblies[asmId], namespaces[nsId], typeNames[nameId], isNestedType);
            if (type != null)
            {
                if (!TypeMap.TryGet(type, out TypeSerializer s))
                    s = TypeMap.Get(type);

                if (Verbose) Log.Info($"Read {c} {typeId}:{name}");
                AddTypeInfo(typeId, name, s, fields, isStruct, c);
            }
            else
            {
                if (Verbose) Log.Warning($"Read DELETED {c} {typeId}:{name}");
                AddDeletedTypeInfo(typeId, name, fields, isStruct, c);
            }
        }

        if (Verbose) Log.Info($"Reading {Header.NumCollectionTypes} Collections");
        ReadCollectionTypes();
    }

    void ReadCollectionTypes()
    {
        for (uint i = 0; i < Header.NumCollectionTypes; ++i)
        {
            // [streamTypeId]  Type ID from the stream
            // [cTypeId]    Collection Type;  1:T[] 2:ArrayT 3:MapKV 4:HashSetT
            // [valTypeId]  Element Type Id
            // [keyTypeId]  Key Type Id (only for Maps)
            uint streamTypeId = BR.ReadVLu32();
            uint cTypeId = BR.ReadVLu32();
            uint valTypeId = BR.ReadVLu32();
            uint keyTypeId = cTypeId == 3 ? BR.ReadVLu32() : 0;
            AddCollectionTypeInfo(streamTypeId, cTypeId, valTypeId, keyTypeId);
        }
    }

    void AddCollectionTypeInfo(uint streamType, uint cTypeId, uint valType, uint keyType)
    {
        TypeInfo keyTypeInfo = keyType != 0 ? StreamTypes[keyType] : null;
        TypeInfo valTypeInfo = StreamTypes[valType];

        if (valTypeInfo == null) // element type does not exist anywhere
        {
            // stream type is not allowed to be null !! this means the stream is invalid
            throw new($"Missing StreamType={streamType} valType={valType} (the stream is corrupted)");
        }
        if (cTypeId == 3 && keyTypeInfo == null) // map key does not exist anywhere
        {
            // stream type is not allowed to be null !! this means the stream is invalid
            throw new($"Missing StreamType={streamType} keyType={keyType} (the stream is corrupted)");
        }

        Type cType = null;
        if (cTypeId == 1)
            cType = valTypeInfo.Type.MakeArrayType();
        else if (cTypeId == 2)
            cType = typeof(Array<>).MakeGenericType(valTypeInfo.Type);
        else if (cTypeId == 3)
            cType = typeof(Map<,>).MakeGenericType(keyTypeInfo!.Type, valTypeInfo.Type);
        else if (cTypeId == 4)
            cType = typeof(HashSet<>).MakeGenericType(valTypeInfo.Type);
        else
            Log.Error($"Unrecognized cTypeId={cTypeId} for Collection<{valTypeInfo}>");

        if (cType != null)
        {
            TypeSerializer cTypeSer = TypeMap.Get(cType);
            SerializerCategory c = cTypeId == 1 ? SerializerCategory.RawArray : SerializerCategory.Collection;

            if (Verbose) Log.Info($"Read {c} {streamType}:{cType.GetTypeName()}");
            AddTypeInfo(streamType, cType.GetTypeName(), cTypeSer, null, isStruct:false, c);
        }
    }

    Map<Assembly, Map<string, Type>> TypeNameCache;

    Type GetTypeFrom(string assemblyName, string nameSpace, string typeName, bool isNested)
    {
        // nested types are prefixed by '+', global types by '.'
        // for nested types nameSpace is the full name of the containing type
        string fullName =  $"{nameSpace}{(isNested?'+':'.')}{typeName},{assemblyName}";
        Type type = Type.GetType(fullName, throwOnError: false);
        if (type != null)
            return type; // perfect match

        // type has been moved, deleted or renamed
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Assembly a = assemblies.Find(asm => asm.GetName().Name == assemblyName);
        if (a == null) // not in loaded assembly? then give up. we don't want to load new assemblies
            return null;

        TypeNameCache ??= new();

        if (!TypeNameCache.TryGetValue(a, out Map<string, Type> typeNamesMap))
        {
            TypeNameCache.Add(a, (typeNamesMap = new()));

            // find type by name from all module types (including nested types)
            Module module = a.Modules.First();
            Type[] moduleTypes = module.GetTypes();
            foreach (Type t in moduleTypes)
            {
                var attr = t.GetCustomAttribute<StarDataTypeAttribute>();
                if (attr != null)
                {
                    try
                    {
                        typeNamesMap.Add(attr.TypeName ?? t.Name, t);
                    }
                    catch (Exception)
                    {
                        // TODO: we don't support duplicate class names
                        Type existing = typeNamesMap[attr.TypeName ?? t.Name];
                        throw new($"There was a duplicate TypeName in one of the submodules. Rename your class. First={existing} Second={t}");
                    }
                }
            }
        }

        typeNamesMap.TryGetValue(typeName, out type);
        return type;
    }

    // reads the type groups
    public void ReadTypeGroups()
    {
        if (Verbose) Log.Info($"Reading {Header.NumTypeGroups} TypeGroups");
        TypeGroups = new TypeGroup[Header.NumTypeGroups];

        int totalCount = 0;
        for (int i = 0; i < TypeGroups.Length; ++i)
        {
            uint streamTypeId = BR.ReadVLu32();
            int count = (int)BR.ReadVLu32();
            int baseObjectId = (int)BR.ReadVLu32();

            TypeInfo type = GetType(streamTypeId);
            if (type == null)
            {
                // TypeInfo is mandatory. If it's missing, then the stream is invalid
                // and it's not possible to read rest of the stream beyond this type
                throw new ($"Failed to read StreamTypeId={streamTypeId}");
            }

            if (count == 0) // count must not be 0
                throw new InvalidDataException($"ReadGroup Type={streamTypeId}:{type.Name} Count was 0");

            if (baseObjectId == 0) // object ID-s are in range [1..N]
                throw new InvalidDataException($"ReadGroup Type={streamTypeId}:{type.Name} BaseObjectId was 0");

            if (Verbose) Log.Info($"ReadGroup {type.Category} {streamTypeId}:{type.Name}  N={count}");
            TypeGroups[i] = new(type, type.Ser, count, BaseId:baseObjectId);
            totalCount += count;
        }

        ObjectsList = new object[totalCount + 1];
    }

    // populate all object instances by reading the object fields
    public object ReadObjectsList()
    {
        if (Verbose) Log.Info($"ReadObjectsList {ObjectsList.Length}");

        PreInstantiate();

        // read types in the order they were Serialized
        // if there is a sequencing/dependency problem, the issue must be
        // solved in the Type sorting stage during Serialization
        if (Verbose) Log.Info("Read Values and Fields");

        var lastArray = TypeGroups.LastOrDefault(g => g.Ser is { IsFundamentalType: false } and RawArraySerializer);

        for (int i = 0; i < TypeGroups.Length; ++i)
        {
            TypeGroup g = TypeGroups[i];
            ReadObjects(g.Type, g.Ser, g.Count, g.BaseId);

            if (g.Type == lastArray.Type)
            {
                // update any structs with RawArrays that we just finished reading
                SetDeferredFields();
            }
        }

        return AfterDeserialization();
    }

    void PreInstantiate()
    {
        // pre-instantiate UserClass instances
        if (Verbose) Log.Info("PreInstantiate objects");
        foreach (var g in GetTypeGroups(t => t.Category is (SerializerCategory.UserClass or SerializerCategory.Collection)))
        {
            for (int i = 0; i < g.Count; ++i)
                ObjectsList[g.BaseId + i] = g.Ser.CreateInstance();
        }
    }

    object AfterDeserialization()
    {
        object root = ObjectsList[Header.RootObjectId]; // ID-s are from [1...N]

        if (Verbose) Log.Info("Invoke UserClass events");
        var onDeserialized = new EventContextOnDeserialized(root, ObjectsList, Verbose);
        onDeserialized.InvokeEvents(TypeGroups);

        return root;
    }

    IEnumerable<TypeGroup> GetTypeGroups(Func<TypeInfo, bool> condition)
    {
        foreach (TypeGroup g in TypeGroups)
            if (g.Type?.Ser != null && condition(g.Type))
                yield return g;
    }

    void ReadObjects(TypeInfo type, TypeSerializer ser, int count, int baseId)
    {
        uint typeId = BR.ReadVLu32();
        if (Verbose) Log.Info($"ReadObjects N={count} {type}");

        // we are trying to read objects but the group type does not match
        if (typeId != type.StreamTypeId)
        {
            TypeInfo actual = GetTypeOrNull(typeId);
            throw new InvalidDataException($"Invalid TypeGroup StreamId={typeId} Expected={type} Encountered={actual}");
        }

        // the count does not match, there's an invalid offset in the deserializer and we're reading bad data
        uint actualCount = BR.ReadVLu32();
        if (actualCount != count)
            throw new InvalidDataException($"Invalid TypeGroup Count={actualCount} Expected={count} for Type={type}");

        if (type.Category is SerializerCategory.UserClass)
        {
            for (int i = 0; i < count; ++i)
            {
                object instance = ObjectsList[baseId + i];
                ReadUserClass(type, instance);
            }
        }
        else if (type.Category is SerializerCategory.Collection)
        {
            var cs = (CollectionSerializer)ser;
            for (int i = 0; i < count; ++i)
            {
                object instance = ObjectsList[baseId + i];
                cs.Deserialize(this, instance);
            }
        }
        // fundamental types such as int, Vector2, String, Byte[], Point. @see TypeSerializerMap.cs
        // also RawArrays like Ship[]
        else
        {
            for (int i = 0; i < count; ++i)
            {
                ObjectsList[baseId + i] = ser.Deserialize(this);
            }
        }
    }

    record struct DeferredSet(object Instance, FieldInfo FI, uint Pointer);
    readonly Array<DeferredSet> DeferredSets = new();

    void SetDeferredFields()
    {
        if (DeferredSets.IsEmpty) return;
        if (Verbose) Log.Info($"Set deferred fields: {DeferredSets.Count}");

        foreach ((object instance, FieldInfo fi, uint pointer) in DeferredSets)
        {
            object fieldValue = ObjectsList[pointer];
            try
            {
                fi.Field.Set(instance, fieldValue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to set FieldValue={fieldValue} into Field={fi}");
            }
        }

        DeferredSets.Clear();
    }

    void ReadUserClass(TypeInfo instanceType, object instance)
    {
        bool isStruct = instanceType.IsStruct;

        // using partial layout or full layout?
        uint numPartialFields = BR.ReadVLu32();
        if (numPartialFields > 0u) // partial
        {
            for (uint i = 0; i < numPartialFields; ++i)
            {
                // the value has to be read, even if Field or Type is deleted
                uint streamFieldIdx = BR.ReadVLu32();
                uint pointer = BR.ReadVLu32();

                FieldInfo fi = instanceType.Fields[streamFieldIdx];
                ReadUserClassField(instance, fi, pointer, isStruct);
            }
        }
        else // full layout
        {
            for (uint streamFieldIdx = 0; streamFieldIdx < instanceType.Fields.Length; ++streamFieldIdx)
            {
                // the value has to be read, even if Field or Type is deleted
                uint pointer = BR.ReadVLu32();

                FieldInfo fi = instanceType.Fields[streamFieldIdx];
                ReadUserClassField(instance, fi, pointer, isStruct);
            }
        }
    }

    void ReadUserClassField(object instance, FieldInfo fi, uint pointer, bool isStruct)
    {
        // if Type has been deleted, we skip and read the next field
        if (instance == null) return;

        object fieldValue = pointer != 0 ? ObjectsList[pointer] : fi.Ser?.DefaultValue;

        // if field has been deleted, then Field==null and Set() is ignored
        if (fi.Field == null) return;

        // if this is a Struct referencing a RawArray, we need to defer the write
        if (isStruct && pointer != 0 && fieldValue == null && 
            fi.Field.Serializer is RawArraySerializer)
        {
            DeferredSets.Add(new(instance, fi, pointer));
            return;
        }

        try
        {
            fi.Field.Set(instance, fieldValue);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to set FieldValue={fieldValue} (Pointer={pointer}) into Field={fi}");
        }
    }

    // Reads a pointer from the stream and looks it up from the ObjectsList
    // This is used by Array/Collection deserializers
    public object ReadCollectionElement(TypeSerializer elemType)
    {
        uint pointer = BR.ReadVLu32();
        if (pointer == 0)
            return elemType.DefaultValue;
        return ObjectsList[pointer];
    }
}
