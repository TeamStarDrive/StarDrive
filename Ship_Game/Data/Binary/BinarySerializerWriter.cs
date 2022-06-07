using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SDUtils;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializerWriter
    {
        public BinaryWriter BW;

        // total number of objects
        public int NumObjects;
 
        // index of the root object which is being serialized/deserialized
        public uint RootObjectIndex;

        // which types were used, includes: classes, Enums and Structs
        public TypeSerializer[] UsedTypes;

        // different generic collection types such as: T[], Array<T>, Map<K,V>
        public CollectionSerializer[] CollectionTypes;

        public struct TypeGroup { public TypeSerializer Ser; public object[] Objects; }

        // Object lists grouped by their type
        // includes UserClasses, Array<T>, Map<K,V>, strings
        public TypeGroup[] TypeGroups;

        // how many TypeGroups are going to be written to the binary stream?
        public int NumUsedGroups => TypeGroups.Count(g => g.Objects.Length > 0);

        Map<object, uint> Pointers;

        public bool Verbose;

        public BinarySerializerWriter(BinaryWriter writer)
        {
            BW = writer;
        }

        class RecursiveScanner
        {
            (TypeSerializer Ser, HashSet<object> Objects)[] PointerTypeGroups = {};
            TypeSerializer[] StructTypes = {};

            // outputs:
            public int NumObjects;
            public TypeGroup[] TypeGroups;
            public TypeSerializer[] UsedTypes;
            public CollectionSerializer[] CollectionTypes;

            public RecursiveScanner(TypeSerializer rootSer, object rootObject)
            {
                try
                {
                    Scan(rootSer, rootObject);
                    FinalizeDependencies();
                    SortTypesByDependency();
                }
                catch (OutOfMemoryException e)
                {
                    var groups = PointerTypeGroups.Filter(kv => kv.Ser != null).Sorted(kv => -kv.Objects.Count);
                    Log.Error(e, $"OOM during object scan! NumObjects={NumObjects} Types={groups.Length}\n"
                                +$"Biggest Group {groups[0].Ser} Count={groups[0].Objects.Count}");
                    throw;
                }
            }

            HashSet<object> RecordPointerType(TypeSerializer ser)
            {
                int id = ser.TypeId;
                var groups = PointerTypeGroups;
                if (id < groups.Length && groups[id].Ser != null)
                    return groups[id].Objects;

                if (id >= groups.Length)
                {
                    Array.Resize(ref PointerTypeGroups, Math.Max(id+1, groups.Length));
                    groups = PointerTypeGroups;
                }

                var objects = new HashSet<object>();
                groups[id] = (ser, objects);
                return objects;
            }

            void RecordStructType(TypeSerializer ser)
            {
                int id = ser.TypeId;
                if (id >= StructTypes.Length)
                    Array.Resize(ref StructTypes, Math.Max(id+1, StructTypes.Length));

                StructTypes[id] = ser;
            }

            // @return false if type analysis should be terminated
            bool Record(TypeSerializer ser, object instance)
            {
                if (ser.IsPointerType)
                {
                    // the types NEED to be recorded here even if instance is null
                    HashSet<object> objects = RecordPointerType(ser);

                    if (instance != null && objects.Add(instance)) // is it unique?
                    {
                        ++NumObjects;
                        // once the fundamental type instance has been recorded, we can stop the scan
                        if (ser.IsFundamentalType)
                            return false;
                        return true; // keep scanning
                    }
                    // no fields for null instances, or this object was already scanned
                    return false;
                }
                else // ValueType:
                {
                    // for user defined structs, return True to allow scanning Fields
                    if (ser.IsUserClass)
                    {
                        RecordStructType(ser);
                        return true;
                    }
                    if (ser.IsEnumType)
                    {
                        RecordStructType(ser);
                    }
                    // nothing else to check for Enums and regular ValueTypes
                    return false;
                }
            }

            // Recursively gathers all UserType instances,
            // the order here is unimportant because they get sorted later
            void Scan(TypeSerializer ser, object instance)
            {
                if (!Record(ser, instance))
                    return; // terminate recursion

                // recurse into objects and collections to find more objects
                if (ser.IsUserClass && ser is UserTypeSerializer userType)
                {
                    foreach (DataField field in userType.Fields)
                    {
                        // HOTSPOT
                        object obj = field.Get(instance);
                        Scan(field.Serializer, obj);
                    }
                }
                else if (ser.IsCollection && ser is CollectionSerializer collectionType)
                {
                    if (collectionType.IsMapType && collectionType is MapSerializer mapType)
                    {
                        // key and element type must always be recorded, otherwise collection cannot be resolved
                        Record(mapType.KeySerializer, null);
                        Record(mapType.ElemSerializer, null);

                        var e = ((IDictionary)instance).GetEnumerator();
                        while (e.MoveNext())
                        {
                            Scan(mapType.KeySerializer, e.Key);
                            Scan(mapType.ElemSerializer, e.Value);
                        }
                    }
                    else
                    {
                        // element type must always be recorded, otherwise collection cannot be resolved
                        Record(collectionType.ElemSerializer, null);

                        int count = collectionType.Count(instance);
                        for (int i = 0; i < count; ++i)
                        {
                            object obj = collectionType.GetElementAt(instance, i);
                            Scan(collectionType.ElemSerializer, obj);
                        }
                    }
                }
                else
                {
                    throw new Exception($"Unexpected type: {ser}");
                }
            }

            // for Map<string, Map<int, Snapshot>> we need to add `Snapshot` type
            // even if there are no instances of it, otherwise we can't construct the Map type
            void FinalizeDependencies()
            {
                for (int i = 0; i < PointerTypeGroups.Length; ++i)
                {
                    if (PointerTypeGroups[i].Ser is CollectionSerializer cs)
                    {
                        RecordPointerType(cs.ElemSerializer);
                        if (cs.IsMapType && cs is MapSerializer ms)
                            RecordPointerType(ms.KeySerializer);
                    }
                }
            }

            void SortTypesByDependency()
            {
                // Make the types somewhat stable by sorting them by name
                // new/deleted types will of course offset this list immediately
                // and deleted types can't be reconstructed during Reading
                //
                // Additionally Sort objects so that if Type B is a generic type
                // which depends on Type A, then A must be first
                //
                // types ordering [incredibly important]:
                // - enums
                // - structs
                // - strings
                // - raw arrays
                // - collections
                // - user classes
                var structs = StructTypes.Filter(ser => ser != null);
                structs.Sort((a, b) =>
                {
                    // enums go first
                    if (a.IsEnumType && !b.IsEnumType) return -1;
                    if (!a.IsEnumType && b.IsEnumType) return +1;
                    return string.CompareOrdinal(a.Type.Name, b.Type.Name);
                });

                var groups = PointerTypeGroups.FilterSelect(
                    kv => kv.Ser != null,
                    kv => new TypeGroup{ Ser = kv.Ser, Objects = kv.Objects.ToArray() }
                );
                groups.Sort((a, b) =>
                {
                    if (a.Ser.Type == typeof(string)) return -1;
                    if (b.Ser.Type == typeof(string)) return +1;

                    // structs go first
                    bool isPointerA = a.Ser.IsPointerType;
                    bool isPointerB = b.Ser.IsPointerType;
                    if (!isPointerA && isPointerB) return -1;
                    if (isPointerA && !isPointerB) return +1;

                    if (a.Ser.Type.IsArray && !b.Ser.Type.IsArray) return -1;
                    if (!a.Ser.Type.IsArray && b.Ser.Type.IsArray) return +1;

                    if (a.Ser.IsCollection && !b.Ser.IsCollection) return -1;
                    if (!a.Ser.IsCollection && b.Ser.IsCollection) return +1;

                    if (TypeDependsOn(b.Ser.Type, a.Ser.Type)) return -1;
                    if (TypeDependsOn(a.Ser.Type, b.Ser.Type)) return +1;

                    // the rest, sort by type name
                    return string.CompareOrdinal(a.Ser.Type.Name, b.Ser.Type.Name);
                });
                
                // for TypeGroups we only use Object groups
                TypeGroups = groups;
                
                // for UsedTypes we take both structs and objects
                UsedTypes = structs.Concat(groups.FilterSelect(kv => kv.Ser.IsUserClass, kv => kv.Ser));
                CollectionTypes = groups.FilterSelect(kv => kv.Ser.IsCollection, kv => (CollectionSerializer)kv.Ser);
            }

            // @return TRUE if `type` is dependent on `on`
            //         Example: type=Array<Ship> on=Ship returns true
            //         Example: type=Map<string,Array<Ship>> on=Ship returns true
            //         Example: type=Ship on=Array<Ship> returns false
            static bool TypeDependsOn(Type type, Type on)
            {
                if (type.IsGenericType)
                {
                    Type[] typeArguments = type.GenericTypeArguments;
                    for (int i = 0; i < typeArguments.Length; ++i)
                    {
                        Type typeArg = typeArguments[i];
                        if (typeArg == on || TypeDependsOn(typeArg, on))
                            return true;
                    }
                }
                return false;
            }
        }

        public void ScanObjects(TypeSerializer rootSer, object rootObject)
        {
            var rs = new RecursiveScanner(rootSer, rootObject);

            NumObjects = rs.NumObjects;
            TypeGroups = rs.TypeGroups;
            UsedTypes = rs.UsedTypes;
            CollectionTypes = rs.CollectionTypes;

            // find the root object index from the neatly sorted type groups
            RootObjectIndex = IndexOfRootObject(rootObject);
        }

        uint IndexOfRootObject(object rootObject)
        {
            uint count = 0;
            foreach (TypeGroup tg in TypeGroups)
            {
                for (uint i = 0; i < tg.Objects.Length; ++i)
                    if (tg.Objects[i] == rootObject)
                        return count + i;
                count += (uint)tg.Objects.Length;
            }
            return 0;
        }

        string[] MapSingle(Func<TypeSerializer, string> selector)
        {
            var names = new HashSet<string>();
            foreach (TypeSerializer s in UsedTypes)
                names.Add(selector(s));
            return names.ToArr();
        }

        string[] MapFields()
        {
            var names = new HashSet<string>();
            foreach (TypeSerializer s in UsedTypes)
                if (s is UserTypeSerializer us)
                    foreach (DataField field in us.Fields)
                        names.Add(field.Name);
            return names.ToArr();
        }

        static Map<string, int> CreateIndexMap(string[] names)
        {
            Array.Sort(names);
            var indexMap = new Map<string, int>();
            for (int i = 0; i < names.Length; ++i)
                indexMap.Add(names[i], i);
            return indexMap;
        }

        (string[] Names, Map<string, int> Index) GetMappedNames(Func<TypeSerializer, string> selector)
        {
            string[] names = MapSingle(selector);
            return (names, CreateIndexMap(names));
        }

        (string[] Names, Map<string, int> Index) GetMappedFields()
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
            (string[] assemblies, var assemblyMap)  = GetMappedNames(GetAssembly);
            (string[] namespaces, var namespaceMap) = GetMappedNames(GetNamespace);
            (string[] typeNames,  var typenameMap)  = GetMappedNames(GetTypeName);
            (string[] fieldNames, var fieldNameMap) = GetMappedFields();
            WriteArray(assemblies);
            WriteArray(namespaces);
            WriteArray(typeNames);
            WriteArray(fieldNames);

            foreach (TypeSerializer s in UsedTypes)
            {
                if (Verbose) Log.Info($"WriteType={s.TypeId} {s.NiceTypeName}");
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
                if (s.IsPointerType) flags |= 0b0000_0001; // pointer, not valuetype
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

            foreach (CollectionSerializer s in CollectionTypes)
            {
                if (Verbose) Log.Info($"WriteCollection={s.TypeId} {s.NiceTypeName}");
                // [type ID]
                // [collection type]   1:T[] 2:Array<T> 3:Map<K,V>
                // [element type ID]
                // [key type ID] (only for Map<K,V>)
                uint type = 0;
                if      (s is RawArraySerializer)  type = 1;
                else if (s is ArrayListSerializer) type = 2;
                else if (s is MapSerializer)       type = 3;
                BW.WriteVLu32((uint)s.TypeId);
                BW.WriteVLu32(type);
                BW.WriteVLu32((uint)s.ElemSerializer.TypeId);
                if (s is MapSerializer ms)
                    BW.WriteVLu32((uint)ms.KeySerializer.TypeId);
            }
        }

        public void WriteObjectTypeGroups()
        {
            foreach (TypeGroup tg in TypeGroups)
            {
                if (tg.Objects.Length == 0)
                    continue;
                if (Verbose) Log.Info($"WriteGroup={tg.Ser.TypeId} {tg.Ser.NiceTypeName} count={tg.Objects.Length}");
                BW.WriteVLu32((uint)tg.Ser.TypeId);
                BW.WriteVLu32((uint)tg.Objects.Length); // int32 because we allow > 65k objects
            }
        }

        public void WriteObjects()
        {
            if (Verbose) Log.Info($"WriteObjects {NumObjects}");
            uint objectPointer = 0;
            Pointers = new Map<object, uint>(NumObjects);

            // pre-pass: create integer pointers of all objects
            foreach (TypeGroup tg in TypeGroups)
            {
                foreach (object o in tg.Objects)
                {
                    if (!Pointers.ContainsKey(o))
                    {
                        uint pointer = ++objectPointer;
                        Pointers[o] = pointer; // pointer = objectIndex + 1
                    }
                }
            }
            
            foreach (TypeGroup tg in TypeGroups)
            {
                if (tg.Objects.Length == 0)
                    continue;

                if (Verbose) Log.Info($"WriteGroupedObjects Count={tg.Objects.Length} {tg.Ser}");

                // for error checking we want to have the correct typeId because
                // if something goes wrong, we need a way to skip over these
                BW.WriteVLu32((uint)tg.Ser.TypeId);
                BW.WriteVLu32((uint)tg.Objects.Length);

                foreach (object o in tg.Objects)
                {
                    WriteObjectRoot(tg.Ser, o);
                }
            }
        }

        public void WriteObjectRoot(TypeSerializer ser, object instance)
        {
            // NOTE: the object typeId is already handled by TypeGroup data

            if (ser is UserTypeSerializer userSer)
            {
                // This is not recursive, because we only write object "Pointers" ID-s
                foreach (DataField field in userSer.Fields)
                {
                    // [field type ID]
                    // [field index]     (in type metadata)
                    TypeSerializer fieldSer = field.Serializer;
                    BW.WriteVLu32((uint)fieldSer.TypeId);
                    BW.WriteVLu32((uint)field.FieldIdx);

                    object fieldObject = field.Get(instance);
                    WriteElement(fieldSer, fieldObject);
                }
            }
            else // string, int, float, object[], Vector2[], etc
            {
                ser.Serialize(this, instance);
            }
        }

        // Writes a single element
        // For pointer types, this writes their pointer reference
        // For primitive value types, this writes an inline value
        // For UserClass value types, this writes struct fields inline
        public void WriteElement(TypeSerializer ser, object element)
        {
            bool isPointerType = ser.IsPointerType;
            if (isPointerType && element == null)
            {
                BW.WriteVLu32(0); // NULL pointer
            }
            else if (ser.IsUserClass)
            {
                if (isPointerType)
                {
                    if (!Pointers.TryGetValue(element, out uint pointer))
                    {
                        // an UserClass object which was somehow missed by Pointer scan
                        Log.Error($"BinarySerializer object pointer is missing: {element}");
                    }
                    BW.WriteVLu32(pointer); // write the object pointer or NULL if not found
                }
                else
                {
                    // an UserClass struct which has to be serialized inline
                    WriteObjectRoot(ser, element);
                }
            }
            // handle strings, T[], Array<T>, Map<T> etc
            else if (isPointerType && Pointers.TryGetValue(element, out uint pointer))
            {
                BW.WriteVLu32(pointer); // write the object pointer
            }
            else // it's a float, int, Vector2, etc. dump it directly
            {
                ser.Serialize(this, element);
            }
        }
    }
}
