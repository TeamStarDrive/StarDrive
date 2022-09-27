using System;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Utils;

namespace Ship_Game.Data.Binary;

// Serialization Group
public struct SerializationTypeGroup
{
    public TypeSerializer Type;
    public Array<ObjectState> GroupedObjects;
    public override string ToString() => $"SerTypeGroup {Type} GroupedObjs={GroupedObjects.Count}";
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
    FastTypeMap<Map<object, ObjectState>> InstanceMap;

    // maps Type -> Array<ObjectState>
    // grouping objects by their type
    readonly FastTypeMap<Array<ObjectState>> Objects;

    uint NextObjectId;
    public uint RootObjectId;
    public int NumObjects;
    public SerializationTypeGroup[] TypeGroups;

    public ObjectScanner(BinarySerializer rootSer, object rootObject)
    {
        RootSer = rootSer;
        RootObj = rootObject;
        InstanceMap = new(rootSer.TypeMap);
        Objects = new(rootSer.TypeMap);
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
            RootObjectId = ScanObjectState(RootSer, RootObj);
            FinalizeTypes();
            InstanceMap = null; // no longer needed
            LinearRemapObjectIds();

            var groups = new Array<SerializationTypeGroup>();
            foreach (TypeSerializer ser in Types.All)
            {
                if (Objects.ContainsKey(ser))
                {
                    var groupedObjects = Objects.GetValue(ser);
                    if (groupedObjects.NotEmpty)
                        groups.Add(new() { Type = ser, GroupedObjects = groupedObjects });
                }
            }
            TypeGroups = groups.ToArray();
        }
        catch (OutOfMemoryException e)
        {
            var groups = Objects.Values.Sorted(arr => -arr.Count);
            Log.Error(e, $"OOM during object scan! NumObjects={NumObjects} Types={groups.Length}\n"
                         + $"Biggest Group {groups[0].GetType()} Count={groups[0].Count}");
            throw;
        }
    }

    ObjectState NewObjectState(TypeSerializer ser, object instance)
    {
        ++NumObjects;
        uint id = ++NextObjectId;

        if (ser.IsFundamentalType || ser.IsEnumType) return new(instance, id);
        if (ser.IsUserClass) return new UserTypeState(instance, id);
        if (ser.IsCollection) return new CollectionState(instance, id);
        throw new($"Unexpected type: {ser}");
    }

    // Automatically handles abstract/virtual objects
    // @return generated object id
    internal uint ScanObjectState(TypeSerializer ser, object instance)
    {
        // if it's the default value, no need to map it or anything
        if (ser.IsValueType)
        {
            if (instance.Equals(ser.DefaultValue))
                return 0u;
        }
        else if (instance == null) // null is always the default for classes
            return 0u;

        // if this class has any abstract or virtual members,
        // then always double-check the concrete type (albeit this is much slower)
        if (ser is UserTypeSerializer { IsAbstractOrVirtual: true })
        {
            ser = RootSer.TypeMap.Get(instance.GetType());
        }

        ObjectState state;
        if (!InstanceMap.TryGetValue(ser, out Map<object, ObjectState> instMap))
        {
            instMap = new();
            InstanceMap.SetValue(ser, instMap);
            Objects.SetValue(ser, new());
        }
        else if (instMap.TryGetValue(instance, out state))
        {
            return state.Id;
        }

        state = NewObjectState(ser, instance);
        instMap.Add(instance, state);
        Objects.GetValue(ser).Add(state);

        if (!ser.IsFundamentalType)
            state.Scan(this, ser); // scan for child objects
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

        // create the mapping using the properly sorted Types list
        foreach (TypeSerializer ser in Types.All)
        {
            if (Objects.TryGetValue(ser, out Array<ObjectState> groupedObjects))
            {
                foreach (ObjectState objState in groupedObjects)
                {
                    uint newId = ++currentIndex;
                    remap[objState.Id] = newId;
                }
            }
        }

        if (remap[0] != 0)
            throw new("Remap[0] must not happen! This is a bug!");

        // now remap write-commands inside the object states
        foreach (Array<ObjectState> groupedObjects in Objects.GetValues())
        {
            foreach (ObjectState objState in groupedObjects)
            {
                objState.Remap(remap);
            }
        }

        RootObjectId = remap[RootObjectId];
    }

    // for TESTING, too slow for production
    public object GetObject(uint objectId)
    {
        // TODO: if needed, this can use binary search thanks to linear remapping
        foreach (Array<ObjectState> groupedObjects in Objects.GetValues())
            foreach (ObjectState obj in groupedObjects)
                if (obj.Id == objectId)
                    return obj.Obj;
        return null;
    }
}
