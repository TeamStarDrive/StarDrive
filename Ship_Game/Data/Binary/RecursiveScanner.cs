using System;
using System.Collections;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary;

// Serialization Group
public struct SerGroup
{
    public TypeSerializer Ser;
    public Array<ObjectState> Objects;
}

// Reference to another serialized object
public struct SerObjectRef
{
    public TypeSerializer Type;
    public int Id; // starts at [1], 0 means null
    public SerObjectRef(TypeSerializer type, int id)
    {
        Type = type;
        Id = id;
    }
}

// Base state for a serialized object
public class ObjectState
{
    public object Object;
    public int Id; // ID of this object, 0 means null
    public override string ToString() => $"{Object.GetType().Name} {Id} {Object}";

    public ObjectState(object obj, int id) { Object = obj; Id = id; }

    // Scan for child objects
    public virtual void Scan(RecursiveScanner scanner, TypeSerializer ser)
    {
        // Fundamental types don't have anything to scan
    }

    // Serialize this ObjectState into a binary writer
    public virtual void Serialize(BinarySerializerWriter w, TypeSerializer ser)
    {
        ser.Serialize(w, Object);
    }

    protected void WriteChildElement(BinarySerializerWriter w, in SerObjectRef o)
    {
        w.BW.WriteVLu32((uint)o.Type.TypeId);
        w.BW.WriteVLu32((uint)o.Id);
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
    readonly FastTypeMap<Map<object, ObjectState>> InstanceMap;

    // maps Type -> Array<ObjectState>
    // grouping objects by their type
    public readonly FastTypeMap<Array<ObjectState>> Objects;

    int NextObjectId;
    public int RootObjectId;
    public int NumObjects;
    public SerGroup[] TypeGroups;

    public RecursiveScanner(BinarySerializer rootSer, object rootObject)
    {
        RootSer = rootSer;
        RootObj = rootObject;
        InstanceMap = new(rootSer.TypeMap);
        Objects = new(rootSer.TypeMap);
        PrepareTypes(rootSer);
    }

    void PrepareTypes(BinarySerializer rootSer)
    {
        AllTypes = rootSer.TypeMap.AllTypes;

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
        CollectionTypes = AllTypes.FilterSelect(s => s.IsCollection,
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

            var groups = new Array<SerGroup>();
            foreach (TypeSerializer ser in AllTypes)
            {
                if (Objects[ser] != null)
                {
                    groups.Add(new SerGroup { Ser = ser, Objects = Objects[ser] });
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

        if (ser.IsFundamentalType) return new(instance, id);
        if (ser.IsUserClass) return new UserTypeState(instance, id);
        if (ser.IsCollection) return new CollectionState(instance, id);
        throw new($"Unexpected type: {ser}");
    }

    // @return generated object id
    internal int ScanObjectState(TypeSerializer ser, object instance)
    {
        ObjectState state;
        var instMap = InstanceMap[ser];
        if (instMap == null)
        {
            InstanceMap[ser] = instMap = new();
            Objects[ser] = new();
        }
        else if (instMap.TryGetValue(instance, out state))
        {
            return state.Id;
        }

        state = NewState(ser, instance);
        instMap.Add(instance, state);
        Objects[ser].Add(state);

        if (!ser.IsFundamentalType)
            state.Scan(this, ser); // scan for child objects
        return state.Id;
    }

    // @return TRUE if `type` is dependent on `on`
    //         example: type=Map<string,Array<Ship>> on=Ship returns true
    //         example: type=Ship on=Array<Ship> returns false
    static bool TypeDependsOn(TypeSerializer type, TypeSerializer on)
    {
        // Array<Ship> or Map<string, Array<Ship>> or Ship[]
        if (TypeDependsOn(type.Type, on.Type))
            return true;
        if (type is UserTypeSerializer us)
            foreach (DataField field in us.Fields)
                if (TypeDependsOn(field.Serializer, on))
                    return true;
        return false;
    }

    static bool TypeDependsOn(Type type, Type on)
    {
        if (type.IsGenericType)
        {
            foreach (Type arg in type.GetGenericArguments())
                if (arg == on || TypeDependsOn(arg, on))
                    return true;
        }
        else if (type.HasElementType && type.GetElementType() == on)
        {
            return true;
        }
        return false;
    }

    internal class UserTypeState : ObjectState
    {
        public SerObjectRef[] Fields;

        public UserTypeState(object obj, int id) : base(obj, id)
        {
        }

        public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
        {
            for (int i = 0; i < Fields.Length; ++i)
            {
                WriteChildElement(w, Fields[i]);
            }
        }

        public override void Scan(RecursiveScanner scanner, TypeSerializer ser)
        {
            var user = (UserTypeSerializer)ser;
            Fields = user.Fields.Length > 0 ? new SerObjectRef[user.Fields.Length] : Empty<SerObjectRef>.Array;

            for (int i = 0; i < user.Fields.Length; ++i)
            {
                DataField field = user.Fields[i];
                // HOTSPOT, some PROPERTIES can also perform computations here
                object obj = field.Get(Object);
                int fieldObjectId = scanner.ScanObjectState(field.Serializer, obj);
                Fields[i] = new(field.Serializer, fieldObjectId);
            }
        }
    }

    // T[], Array<T>, Map<K,V> or HashSet<T>
    internal class CollectionState : ObjectState
    {
        public SerObjectRef[] Items;

        public CollectionState(object obj, int id) : base(obj, id)
        {
        }

        public override void Serialize(BinarySerializerWriter w, TypeSerializer ser)
        {
            for (int i = 0; i < Items.Length; ++i)
            {
                WriteChildElement(w, Items[i]);
            }
        }

        public override void Scan(RecursiveScanner scanner, TypeSerializer ser)
        {
            var coll = (CollectionSerializer)ser;
            int count = coll.Count(Object);
            if (count == 0)
                return;

            var typeMap = scanner.RootSer.TypeMap;
            bool valCanBeNull = coll.ElemSerializer.IsPointerType;

            void SetValue(int index, object instance)
            {
                if (valCanBeNull && instance == null)
                {
                    Items[index] = new(coll.ElemSerializer, 0);
                }
                else
                {
                    // NOTE: VALUES CAN USE ABSTRACT TYPES, SO TYPE CHECK IS REQUIRED FOR EACH ELEMENT
                    TypeSerializer item = typeMap.Get(instance!.GetType());
                    int valId = scanner.ScanObjectState(item, instance);
                    Items[index] = new(item, valId);
                }
            }

            if (coll is MapSerializer maps)
            {
                Items = new SerObjectRef[count * 2];
                var e = ((IDictionary)Object).GetEnumerator();
                for (int i = 0; i < count && e.MoveNext(); ++i)
                {
                    int keyId = scanner.ScanObjectState(maps.KeySerializer, e.Key);
                    Items[i*2+0] = new SerObjectRef(maps.KeySerializer, keyId);
                    SetValue(i*2+1, e.Value);
                }
            }
            else if (coll is HashSetSerializer)
            {
                Items = new SerObjectRef[count];
                var e = ((IEnumerable)Object).GetEnumerator();
                for (int i = 0; i < count && e.MoveNext(); ++i)
                {
                    SetValue(i, e.Current);
                }
            }
            else
            {
                Items = new SerObjectRef[count];
                for (int i = 0; i < count; ++i)
                {
                    object element = coll.GetElementAt(Object, i);
                    SetValue(i, element);
                }
            }
        }
    }
}
