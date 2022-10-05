using SDUtils;
using Ship_Game.Data.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Ship_Game.Data.Binary;

/// <summary>
/// Maps object instances to ObjectStates
/// </summary>
public class ObjectStateMap
{
    public readonly TypeSerializer Ser;
    IInstanceMap Instances;
    public IReadOnlyCollection<ObjectState> Objects => Instances.Values;
    public int NumObjects => Instances.Values.Count;

    public ObjectStateMap(TypeSerializer ser)
    {
        Ser = ser;
        if (ser is UserTypeSerializer us && us.IsIEquatableT && !us.IsValueType)
        {
            var mapType = typeof(EquatableInstanceMap<>).MakeGenericType(ser.Type);
            Instances = (IInstanceMap)Activator.CreateInstance(mapType);
        }
        else
        {
            var mapType = typeof(InstanceMap<>).MakeGenericType(ser.Type);
            Instances = (IInstanceMap)Activator.CreateInstance(mapType);
        }
    }

    interface IInstanceMap
    {
        IReadOnlyCollection<ObjectState> Values { get; }
        bool Get(object instance, out ObjectState state);
        void Add(object instance, ObjectState state);
    }

    class EquatableInstanceMap<T> : IInstanceMap where T : class
    {
        class Comparer : IEqualityComparer<T> // only ever use ReferenceEquals() for IEquatable instances
        {
            public bool Equals(T x, T y) => ReferenceEquals(x, y);
            public int GetHashCode(T obj) => obj.GetHashCode();
        }
        readonly Map<T, ObjectState> Mapping = new(new Comparer());
        public IReadOnlyCollection<ObjectState> Values => Mapping.Values;
        public bool Get(object instance, out ObjectState state) => Mapping.TryGetValue((T)instance, out state);
        public void Add(object instance, ObjectState state)     => Mapping.Add((T)instance, state);
    }

    class InstanceMap<T> : IInstanceMap
    {
        readonly Map<T, ObjectState> Mapping = new();
        public IReadOnlyCollection<ObjectState> Values => Mapping.Values;
        public bool Get(object instance, out ObjectState state) => Mapping.TryGetValue((T)instance, out state);
        public void Add(object instance, ObjectState state)     => Mapping.Add((T)instance, state);
    }

    public bool Get(object instance, out ObjectState state)
    {
        return Instances.Get(instance, out state);
    }

    static ObjectState NewObjectState(TypeSerializer ser, object instance, uint id)
    {
        if (ser.IsFundamentalType || ser.IsEnumType) return new(instance, id);
        if (ser.IsUserClass) return new UserTypeState(instance, id);
        if (ser.IsCollection) return new CollectionState(instance, id);
        throw new($"Unexpected type: {ser}");
    }

    public ObjectState AddNew(object instance, uint id)
    {
        ObjectState state = NewObjectState(Ser, instance, id);
        Instances.Add(instance, state);
        return state;
    }
}
