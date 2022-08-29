using System;
using System.Collections;
using System.Collections.Generic;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

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

public class RecursiveScanner
{
    readonly BinarySerializer RootSer;
    readonly object RootObj;
    public TypeSerializer[] AllTypes;
    public TypeSerializer[] ValueTypes;
    public TypeSerializer[] ClassTypes;
    public TypeSerializer[] UsedTypes;
    public CollectionSerializer[] CollectionTypes;

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
        AllTypes = RootSer.TypeMap.AllTypes;

        // this determines the types ordering
        AllTypes.Sort((a, b) =>
        {
            // don't move fundamentals, because they don't have dependencies
            int r;
            if (a.IsFundamentalType && b.IsFundamentalType)
                return Compare(a.TypeId, b.TypeId); // fundamentals are equal

            if ((r = Compare(a.IsFundamentalType, b.IsFundamentalType)) != 0) return r;
            if ((r = Compare(a.IsEnumType, b.IsEnumType)) != 0) return r;
            if ((r = Compare(a.IsValueType, b.IsValueType)) != 0) return r;
            if ((r = Compare(a.Type.IsArray, b.Type.IsArray)) != 0) return r;
            if ((r = Compare(a.IsCollection, b.IsCollection)) != 0) return r;

            // finally user classes, sort by dependency
            if (TypeDependsOn(b, a)) return -1; // b depends on a, A must come first
            if (TypeDependsOn(a, b)) return +1; // a depends on b, B must come first
            return string.CompareOrdinal(a.Type.Name, b.Type.Name);
        });

        UsedTypes = AllTypes.Filter(s => !s.IsFundamentalType && !s.IsCollection);
        ValueTypes = UsedTypes.Filter(s => s.IsValueType);
        ClassTypes = UsedTypes.Filter(s => s.IsPointerType);
        CollectionTypes = AllTypes.FilterSelect(s => !s.IsFundamentalType && s.IsCollection,
                                                s => (CollectionSerializer)s);

        Log.Info($"ValueTypes={ValueTypes.Length} UserTypes={ClassTypes.Length} CollectionTypes={CollectionTypes.Length}");
    }

    static int Compare(int a, int b) => (a < b) ? -1 : (a > b) ? 1 : 0;
    static int Compare(bool a, bool b) => (a && !b) ? -1 : (!a && b) ? 1 : 0;

    public void CreateWriteCommands()
    {
        try
        {
            RootObjectId = ScanObjectState(RootSer, RootObj);
            FinalizeTypes();
            InstanceMap = null; // no longer needed
            LinearRemapObjectIds();

            var groups = new Array<SerializationTypeGroup>();
            foreach (TypeSerializer ser in AllTypes)
            {
                if (Objects.ContainsKey(ser))
                {
                    groups.Add(new() { Type = ser, GroupedObjects = Objects.GetValue(ser) });
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
    //         example: type=Map<string,Array<Ship>> on=Ship returns true
    //         example: type=Ship on=Array<Ship> returns false
    static bool TypeDependsOn(TypeSerializer type, TypeSerializer on)
    {
        var explored = new HashSet<Type>();
        return TypeDependsOn(type, on, explored);
    }

    static bool TypeDependsOn(TypeSerializer type, TypeSerializer on, HashSet<Type> explored)
    {
        // Array<Ship> or Map<string, Array<Ship>> or Ship[]
        if (TypeDependsOn(type.Type, on.Type, explored))
            return true;
        if (type is UserTypeSerializer us)
        {
            foreach (DataField field in us.Fields)
            {
                TypeSerializer fieldSer = field.Serializer;
                if (!fieldSer.IsFundamentalType && !explored.Contains(fieldSer.Type) && TypeDependsOn(fieldSer, on, explored))
                    return true;
            }
        }
        return false;
    }

    static bool TypeDependsOn(Type type, Type on, HashSet<Type> explored)
    {
        if (explored.Contains(type))
            return false;
        explored.Add(type);

        if (type.IsGenericType)
        {
            foreach (Type arg in type.GetGenericArguments())
                if (arg == on || TypeDependsOn(arg, on, explored))
                    return true;
        }
        else if (type.HasElementType && type.GetElementType() == on)
        {
            return true;
        }
        return false;
    }

    // remaps scattered object-ids to align with type-group ordering
    void LinearRemapObjectIds()
    {
        var remap = new int[NumObjects + 1];
        int currentIndex = 0;

        // create the mapping using the properly sorted Types list
        foreach (TypeSerializer ser in AllTypes)
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
            Fields = user.Fields.Length > 0 ? new int[user.Fields.Length] : Empty<int>.Array;

            for (int i = 0; i < user.Fields.Length; ++i)
            {
                DataField field = user.Fields[i];
                // HOTSPOT, some PROPERTIES can also perform computations here
                object obj = field.Get(Obj);
                int fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
                Fields[i] = fieldObjectId;
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
