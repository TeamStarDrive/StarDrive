using System;
using System.Collections;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.Utils;

namespace Ship_Game.Data.Binary;

// Serialization Group
public struct SerializationTypeGroup
{
    public TypeSerializer Type;
    public Array<ObjectState> GroupedObjects;
    public override string ToString() => $"SerTypeGroup {Type} GroupedObjs={GroupedObjects.Count}";
}

// Base state for a serialized object
public class ObjectState
{
    public object Obj;
    public int Id; // ID of this object, 0 means null
    public override string ToString() => $"ObjState {Obj.GetType().Name} Id={Id} Obj={Obj}";

    public ObjectState(object obj, int id) { Obj = obj; Id = id; }

    // Scan for child objects
    public virtual void Scan(RecursiveScanner scanner, TypeSerializer ser)
    {
        // Fundamental types don't have anything to scan
    }

    // Remaps object id-s
    public virtual void Remap(int[] map)
    {
        Remap(map, null);
    }

    protected void Remap(int[] map, int[] fields)
    {
        Id = map[Id];
        if (fields != null)
        {
            for (int i = 0; i < fields.Length; ++i)
            {
                int oldId = fields[i];
                fields[i] = map[oldId];
            }
        }
    }

    // Serialize this ObjectState into a binary writer
    public virtual void Serialize(BinarySerializerWriter w, TypeSerializer ser)
    {
        ser.Serialize(w, Obj);
    }
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

public class RecursiveScanner
{
    readonly BinarySerializer RootSer;
    readonly object RootObj;
    
    public ScannedTypes Types;

    // maps Type -> object* -> ObjectState
    // allowing for much faster object lookup if you already know the object Type
    FastTypeMap<Map<object, ObjectState>> InstanceMap;

    // maps Type -> Array<ObjectState>
    // grouping objects by their type
    readonly FastTypeMap<Array<ObjectState>> Objects;

    int NextObjectId;
    public int RootObjectId;
    public int NumObjects;
    public SerializationTypeGroup[] TypeGroups;

    public RecursiveScanner(BinarySerializer rootSer, object rootObject)
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

    ObjectState NewState(TypeSerializer ser, object instance)
    {
        ++NumObjects;
        int id = ++NextObjectId;

        if (ser.IsFundamentalType || ser.IsEnumType) return new(instance, id);
        if (ser.IsUserClass) return new UserTypeState(instance, id);
        if (ser.IsCollection) return new CollectionState(instance, id);
        throw new($"Unexpected type: {ser}");
    }

    // Automatically handles abstract/virtual objects
    // @return generated object id
    internal int ScanObjectState(TypeSerializer ser, object instance)
    {
        if (instance == null) // typically when Array<T> contains nulls
            return 0;

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

        state = NewState(ser, instance);
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
            var explored = new Utils.BitArray(1024);
            return DependsOn(classType, on, ref explored);
        }
        return false;
    }

    static bool DependsOn(UserTypeSerializer classType, TypeSerializer on, ref Utils.BitArray explored)
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
        var remap = new int[NumObjects + 1];
        int currentIndex = 0;

        // create the mapping using the properly sorted Types list
        foreach (TypeSerializer ser in Types.All)
        {
            if (Objects.TryGetValue(ser, out Array<ObjectState> groupedObjects))
            {
                foreach (ObjectState objState in groupedObjects)
                {
                    int newId = ++currentIndex;
                    remap[objState.Id] = newId;
                }
            }
        }

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
    public object GetObject(int objectId)
    {
        // TODO: if needed, this can use binary search thanks to linear remapping
        foreach (Array<ObjectState> groupedObjects in Objects.GetValues())
            foreach (ObjectState obj in groupedObjects)
                if (obj.Id == objectId)
                    return obj.Obj;
        return null;
    }

    public class UserTypeState : ObjectState
    {
        public int[] Fields;

        public UserTypeState(object obj, int id) : base(obj, id)
        {
        }

        public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
        {
            for (int i = 0; i < Fields.Length; ++i)
                w.BW.WriteVLu32((uint)Fields[i]);
        }

        public override void Remap(int[] map)
        {
            Remap(map, Fields);
        }

        public override void Scan(RecursiveScanner scanner, TypeSerializer ser)
        {
            var user = (UserTypeSerializer)ser;

            // get dynamic fields
            StarDataDynamicField[] dynamicF = user.InvokeOnSerializeEvt(Obj);

            // the # of fields remains constant because we rely on predefined object layout
            Fields = user.Fields.Length > 0 ? new int[user.Fields.Length] : Empty<int>.Array;

            for (int i = 0; i < user.Fields.Length; ++i)
            {
                DataField field = user.Fields[i];
                // HOTSPOT, some PROPERTIES can also perform computations here
                object obj = field.Get(Obj);
                int fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
                Fields[i] = fieldObjectId;
            }

            // dynamic fields override existing fields
            // TODO: instead of override, prevent original value from being scanned and written
            if (dynamicF != null)
            {
                for (int i = 0; i < dynamicF.Length; ++i)
                {
                    StarDataDynamicField dynF = dynamicF[i];
                    int fieldIdx = user.Fields.IndexOf(f => f.Name == dynF.Name);
                    if (fieldIdx == -1)
                        throw new($"StarDataDynamicField: Could not find a [StarData] field with Name=`{dynF.Name}`");

                    // and now replace the current object
                    DataField field = user.Fields[fieldIdx];
                    int fieldObjectId = scanner.ScanObjectState(field.Serializer, dynF.Value);
                    Fields[fieldIdx] = fieldObjectId;
                }
            }
        }
    }

    // T[], Array<T>, Map<K,V> or HashSet<T>
    internal class CollectionState : ObjectState
    {
        public int[] Items;

        public CollectionState(object obj, int id) : base(obj, id)
        {
        }

        public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
        {
            if (Items == null)
            {
                w.BW.WriteVLu32((uint)0); // collection length
            }
            else
            {
                w.BW.WriteVLu32((uint)Items.Length); // collection length
                for (int i = 0; i < Items.Length; ++i)
                    w.BW.WriteVLu32((uint)Items[i]);
            }
        }

        public override void Remap(int[] map)
        {
            Remap(map, Items);
        }

        public override void Scan(RecursiveScanner scanner, TypeSerializer ser)
        {
            var coll = (CollectionSerializer)ser;
            int count = coll.Count(Obj);
            if (count == 0)
                return;

            var typeMap = scanner.RootSer.TypeMap;
            bool valCanBeNull = coll.ElemSerializer.IsPointerType;

            if (coll is MapSerializer maps)
            {
                Items = new int[count * 2];
                var e = ((IDictionary)Obj).GetEnumerator();
                for (int i = 0; i < count && e.MoveNext(); ++i)
                {
                    int keyId = scanner.ScanObjectState(maps.KeySerializer, e.Key);
                    Items[i*2+0] = keyId;
                    SetValue(scanner, i*2+1, e.Value, valCanBeNull, typeMap);
                }
            }
            else if (coll is HashSetSerializer)
            {
                Items = new int[count];
                var e = ((IEnumerable)Obj).GetEnumerator();
                for (int i = 0; i < count && e.MoveNext(); ++i)
                {
                    SetValue(scanner, i, e.Current, valCanBeNull, typeMap);
                }
            }
            else
            {
                Items = new int[count];
                for (int i = 0; i < count; ++i)
                {
                    object element = coll.GetElementAt(Obj, i);
                    SetValue(scanner, i, element, valCanBeNull, typeMap);
                }
            }
        }

        void SetValue(RecursiveScanner scanner, int index, object instance, bool valCanBeNull, TypeSerializerMap typeMap)
        {
            if (valCanBeNull && instance == null)
            {
                Items[index] = 0;
            }
            else
            {
                // NOTE: VALUES CAN USE ABSTRACT TYPES, SO TYPE CHECK IS REQUIRED FOR EACH ELEMENT
                TypeSerializer item = typeMap.Get(instance!.GetType());
                int valId = scanner.ScanObjectState(item, instance);
                Items[index] = valId;
            }
        }
    }
}
