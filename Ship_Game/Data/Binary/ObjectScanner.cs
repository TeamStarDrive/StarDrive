using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Utils;

namespace Ship_Game.Data.Binary;

// Serialization Group
public struct SerializationTypeGroup
{
    public TypeSerializer Type;
    public ObjectState[] GroupedObjects;
    public override string ToString() => $"SerTypeGroup {Type} GroupedObjs={GroupedObjects.Length}";
}

// Scanned object types, separated by category
public record struct ScannedTypes(
    TypeSerializer[] Fundamental, // fundamental types
    TypeSerializer[] Values, // enums and UserClass structs
    TypeSerializer[] Classes, // UserClass classes
    TypeSerializer[] ValuesAndClasses, // Values+Classes
    CollectionSerializer[] Collections, // Arrays, Maps, Sets, RawArrays
    TypeSerializer[] All  // Fundamental+Values+Classes+Collections in this order
);

public class ObjectScanner
{
    internal readonly BinarySerializer RootSer;
    readonly object RootObj;
    
    public ScannedTypes Types;

    // maps Type -> object* -> ObjectState
    // allowing for much faster object lookup if you already know the object Type
    FastTypeMap InstanceMap;

    uint NextObjectId;
    public uint RootObjectId;
    public int NumObjects;
    public SerializationTypeGroup[] TypeGroups;

    public ObjectScanner(BinarySerializer rootSer, object rootObject)
    {
        RootSer = rootSer;
        RootObj = rootObject;
        InstanceMap = new(rootSer.TypeMap);
    }

    public void FinalizeTypes()
    {
        var types = RootSer.TypeMap.AllTypes;

        Types = new();
        Types.Fundamental = types.Filter(s => s.IsFundamentalType);
        Types.Values = types.Filter(s => !s.IsFundamentalType && s.IsValueType);
        Types.Classes = types.Filter(s => !s.IsFundamentalType && !s.IsValueType && !s.IsCollection);
        var collectionTypes = types.Filter(s => !s.IsFundamentalType && s.IsCollection);

        DependencySorter<TypeSerializer>.Sort(Types.Values, GetDependencies);
        DependencySorter<TypeSerializer>.Sort(Types.Classes, GetDependencies);
        DependencySorter<TypeSerializer>.Sort(collectionTypes, GetDependencies);

        Types.ValuesAndClasses = Types.Values.Concat(Types.Classes);
        Types.Collections = collectionTypes.FastCast<TypeSerializer, CollectionSerializer>();
        Types.All = Types.Fundamental.Concat(Types.ValuesAndClasses, collectionTypes);
    }

    public void CreateWriteCommands()
    {
        try
        {
            RootObjectId = ScanObjectState(RootSer, RootObj, null);
            FinalizeTypes();
            LinearRemapObjectIds();
        }
        catch (OutOfMemoryException e)
        {
            var groups = InstanceMap.Values.Sorted(stateMap => -stateMap.NumObjects);
            Log.Error(e, $"OOM during object scan! NumObjects={NumObjects} Types={groups.Length}\n"
                         + $"Biggest Group {groups[0].GetType()} Count={groups[0].NumObjects}");
            throw;
        }
    }

    internal static bool IsDefaultValue(TypeSerializer ser, object instance, DataField owner)
    {
        if (ser.IsValueType)
        {
            if (owner?.A.NoDefaults == true) // should not check for default values?
                return false;
            return instance.Equals(owner?.A.DefaultValue ?? ser.DefaultValue);
        }
        // if A.DefaultValue is null, then classes use default value `null`
        return instance == owner?.A.DefaultValue;
    }

    // Scans this object for serializable child-object
    // @param ser Base serializer for this object.
    //            For abstract/virtual objects the actual serializer is deduced automatically.
    // @param instance The object to be scanned. Can be null.
    // @param owner DataField information for debugging purposes. Can be null.
    // @return generated object id
    internal uint ScanObjectState(TypeSerializer ser, object instance, DataField owner)
    {
        // if it's the default value, no need to map it or anything
        if (IsDefaultValue(ser, instance, owner))
            return 0u;

        // if this class has any abstract or virtual members,
        // then always double-check the concrete type (albeit this is much slower)
        if (ser is UserTypeSerializer { IsAbstractOrVirtual: true })
        {
            ser = RootSer.TypeMap.Get(instance.GetType());
        }

        (ObjectStateMap instMap, bool existing) = InstanceMap.GetOrAddNew(ser);
        if (existing && instMap.Get(instance, out ObjectState state))
            return state.Id;

        ++NumObjects;
        uint id = ++NextObjectId;
        state = instMap.AddNew(instance, id);

        // TODO: convert recursion into a loop instead
        if (!ser.IsFundamentalType)
            state.Scan(this, ser, owner); // scan for child objects
        return state.Id;
    }

    // @return TRUE if `type` is dependent on `on`
    //         example: type=Map<string,Array<Ship>> on=Ship returns true, because Ship is required for the Collection
    //         example: type=Ship on=Array<Ship> returns false, because Class can never depend on a Collection
    public static bool TypeDependsOn(TypeSerializer type, TypeSerializer on)
    {
        // type should never depend on itself
        // UserClass can never depend on a Collection type, it is always the other way around
        if (type == on || (!type.IsCollection && on.IsCollection))
            return false;
        // If Type is a Collection, figure out if it contains the other type
        // eg Array<Array<Ship>> depends on Array<Ship> returns true;
        if (type.IsCollection)
            return CollectionDependsOn(type.Type, on.Type);
        if (type is UserTypeSerializer classType)
        {
            var explored = new BitArray(1024);
            return DependsOn(classType, on, ref explored);
        }
        return false;
    }

    static bool DependsOn(UserTypeSerializer classType, TypeSerializer on, ref BitArray explored)
    {
        explored.Set(classType.TypeId);
        foreach (DataField field in classType.Fields)
        {
            TypeSerializer f = field.Serializer;
            if (f == on) return true;
            if (f is UserTypeSerializer fus && !explored.IsSet(f.TypeId) && 
                DependsOn(fus, on, ref explored))
                return true;
        }
        return false;
    }

    static bool CollectionDependsOn(Type type, Type on)
    {
        if (type.IsGenericType)
        {
            foreach (Type arg in type.GetGenericArguments())
                if (arg == on || CollectionDependsOn(arg, on))
                    return true;
        }
        else if (type.HasElementType && type.GetElementType() == on)
        {
            return true;
        }
        return false;
    }

    TypeSerializer[] GetDependencies(TypeSerializer s)
    {
        if (s is UserTypeSerializer us)
        {
            return us.Fields.FilterSelect(f => f.Serializer != s, f => f.Serializer);
        }
        if (s.Type.IsGenericType)
        {
            return s.Type.GetGenericArguments().Select(RootSer.TypeMap.Get);
        }
        if (s.Type.HasElementType)
        {
            return new[]{ RootSer.TypeMap.Get(s.Type.GetElementType()) };
        }
        return null;
    }

    // remaps scattered object-ids to align with type-group ordering
    void LinearRemapObjectIds()
    {
        var remap = new uint[NumObjects + 1];
        uint currentIndex = 0;

        // first create TypeGroups using the properly sorted Types list
        var groups = new Array<SerializationTypeGroup>();
        foreach (ObjectStateMap stateMap in InstanceMap.GetValues(Types.All))
        {
            var groupedObjects = stateMap.Objects;
            if (groupedObjects.Count != 0)
                groups.Add(new() { Type = stateMap.Ser, GroupedObjects = groupedObjects.ToArr() });
        }
        TypeGroups = groups.ToArray();
        InstanceMap = null; // no longer needed

        // create the mapping using the sorted TypeGroups
        for (int i = 0; i < TypeGroups.Length; ++i)
        {
            var objects = TypeGroups[i].GroupedObjects;
            for (int j = 0; j < objects.Length; ++j) // using C-style for loops, because they are the fastest
            {
                uint newId = ++currentIndex;
                remap[objects[j].Id] = newId;
            }
        }

        if (remap[0] != 0)
            throw new("Remap[0] must not happen! This is a bug!");

        // now remap write-commands inside the object states
        for (int i = 0; i < TypeGroups.Length; ++i)
        {
            var objects = TypeGroups[i].GroupedObjects;
            for (int j = 0; j < objects.Length; ++j) // using C-style for loops, because they are the fastest
                objects[j].Remap(remap);
        }

        RootObjectId = remap[RootObjectId];
    }

    // for TESTING, too slow for production
    public object GetObject(uint objectId)
    {
        // TODO: if needed, this can use binary search thanks to linear remapping
        for (int i = 0; i < TypeGroups.Length; ++i)
        {
            var objects = TypeGroups[i].GroupedObjects;
            for (int j = 0; j < objects.Length; ++j) // using C-style for loops, because they are the fastest
            {
                ObjectState objState = objects[j];
                if (objState.Id == objectId)
                    return objState.Obj;
            }
        }
        return null;
    }
}
