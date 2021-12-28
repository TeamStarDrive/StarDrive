using System;
using System.IO;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    public class BinarySerializerReader
    {
        public readonly BinaryReader BR;
        public readonly TypeSerializerMap TypeMap;
        BinarySerializerHeader Header;

        // Object counts grouped by their type (includes strings)
        (TypeSerializer Ser, int Count)[] TypeGroups;

        // flat list of deserialized objects
        public object[] ObjectsList;

        // this is only needed for long-term storage, such as savegames
        bool StableMapping;

        // maps stream DataField Idx to actual DataFields in memory
        // this is needed because serialized data will almost always
        // be out of sync with latest data field changes
        // 
        // This mapping allows us to completely avoid field name checks
        // during Deserialize() itself
        ushort[][] StreamFieldsToActual;

        // And this maps Stream Type ID-s to Actual Type ID-s
        // to handle cases where types are added/removed from type list
        ushort[] StreamTypesToActual;

        public BinarySerializerReader(BinaryReader reader, TypeSerializerMap typeMap, in BinarySerializerHeader header)
        {
            BR = reader;
            TypeMap = typeMap;
            Header = header;
            StableMapping = header.UseStableMapping;
            if (StableMapping)
            {
                StreamFieldsToActual = new ushort[header.NumTypes][];
                StreamTypesToActual = new ushort[header.NumTypes];
            }
        }

        // Converts Stream typeId into an Actual typeId
        ushort GetActualTypeId(ushort streamTypeId)
        {
            if (StableMapping && streamTypeId >= TypeSerializer.MaxFundamentalTypes)
            {
                int typeIdx = streamTypeId - TypeSerializer.MaxFundamentalTypes;
                if (typeIdx < StreamTypesToActual.Length)
                    return StreamTypesToActual[typeIdx];

                throw new InvalidDataException($"Invalid streamTypeId={streamTypeId}");
            }
            return streamTypeId;
        }

        // Converts Stream field index into an Actual fieldIdx
        ushort GetActualFieldIdx(ushort streamTypeId, ushort streamFieldIdx)
        {
            if (StableMapping && streamTypeId >= TypeSerializer.MaxFundamentalTypes)
            {
                int typeIdx = streamTypeId - TypeSerializer.MaxFundamentalTypes;
                if (typeIdx < StreamFieldsToActual.Length)
                {
                    ushort[] typeFields = StreamFieldsToActual[typeIdx];
                    if (streamFieldIdx < typeFields.Length)
                        return typeFields[streamFieldIdx];

                    throw new InvalidDataException($"Invalid streamFieldIdx={streamFieldIdx}");
                }

                throw new InvalidDataException($"Invalid streamTypeId={streamTypeId}");
            }
            return streamFieldIdx;
        }

        static string[] ReadStringArray(BinaryReader br)
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
            // [types]
            string[] assemblies = ReadStringArray(BR);
            string[] namespaces = ReadStringArray(BR);

            //                 bw.Write(typeName + "," + assemblyName);
            for (uint i = 0; i < Header.NumTypes; ++i)
            {
                // [type ID]
                // [assembly ID]
                // [namespace ID]
                // [type flags]
                // [type name]
                ushort typeId = (ushort)BR.ReadVLu32();
                uint assemblyId = BR.ReadVLu32();
                uint namespaceId = BR.ReadVLu32();
                uint typeFlags = BR.ReadVLu32();
                string typeName = BR.ReadString();

                bool isPointerType = (typeFlags & 0x01) != 0;
                string fullNameAndAssembly = $"{namespaces[namespaceId]}+{typeName},{assemblies[assemblyId]}";

                // if type is not found, we completely give up
                // TODO: we should mark this type as invalid and use `null` during deserialize
                Type type = Type.GetType(fullNameAndAssembly, throwOnError: true);
                if (!TypeMap.TryGet(type, out TypeSerializer serializer))
                {
                    serializer = TypeMap.AddUserTypeSerializer(type);
                }

                if (StableMapping && serializer is UserTypeSerializer userSer)
                {
                    uint numFields = BR.ReadVLu32();
                    ushort[] mapping = new ushort[numFields];

                    for (uint fieldIdx = 0; fieldIdx < numFields; ++fieldIdx)
                    {
                        string fieldName = BR.ReadString();
                        DataField field = userSer.GetFieldOrNull(fieldName);
                        if (field != null)
                        {
                            mapping[fieldIdx] = (ushort)field.FieldIdx;
                        }
                        else // field not found, which means the DataField is deleted or name has changed
                        {
                            mapping[fieldIdx] = ushort.MaxValue;
                        }
                    }

                    StreamFieldsToActual[typeId - TypeSerializer.MaxFundamentalTypes] = mapping;
                    StreamTypesToActual[typeId - TypeSerializer.MaxFundamentalTypes] = serializer.Id;
                }
            }
        }

        // reads the type groups
        public void ReadTypeGroups()
        {
            TypeGroups = new (TypeSerializer Ser, int Count)[Header.NumTypeGroups];

            int totalCount = 0;
            for (int i = 0; i < TypeGroups.Length; ++i)
            {
                int typeId = GetActualTypeId((ushort)BR.ReadVLu32());
                int count = (int)BR.ReadVLu32();
                totalCount += count;
                TypeGroups[i] = (TypeMap.Get(typeId), count);
            }

            ObjectsList = new object[totalCount];
        }

        // populate all object instances by reading the object fields
        public void ReadObjectsList()
        {
            // read simple types, and create dummies for UserClasses
            int objectIdx = 0;
            foreach ((TypeSerializer ser, int groupCount) in TypeGroups)
            {
                // NOTE: BinarySerializer always writes non-UserClasses first
                //       This is necessary for handling object dependency
                if (!(ser is UserTypeSerializer userType))
                {
                    for (int j = 0; j < groupCount; ++j)
                    {
                        object instance = ser.Deserialize(this);
                        ObjectsList[objectIdx++] = instance;
                    }
                }
                else
                {
                    // we need to pre-instantiate all UserClass instances
                    // so that ReadUserClass can resolve object references
                    for (int j = 0; j < groupCount; ++j)
                    {
                        object instance = Activator.CreateInstance(ser.Type);
                        ObjectsList[objectIdx++] = instance;
                    }
                }
            }

            // now read UserClass data fields
            objectIdx = 0;
            foreach ((TypeSerializer ser, int groupCount) in TypeGroups)
            {
                if (ser is UserTypeSerializer userType)
                {
                    for (int j = 0; j < groupCount; ++j)
                    {
                        object instance = ObjectsList[objectIdx++];
                        ReadUserClass(userType, instance);
                    }
                }
                else
                {
                    objectIdx += groupCount; // skip
                }
            }
        }

        void ReadUserClass(UserTypeSerializer ser, object instance)
        {
            if (instance == null)
            {
                Log.Error($"Failed to deserialize {ser}");
                return;
            }

            uint numFields = BR.ReadVLu32();
            for (uint i = 0; i < numFields; ++i)
            {
                // serializer Id from the stream
                ushort fieldTypeId = GetActualTypeId((ushort)BR.ReadVLu32());
                ushort fieldIdx = GetActualFieldIdx(fieldTypeId, (ushort)BR.ReadVLu32());

                if (TypeMap.TryGet(fieldTypeId, out TypeSerializer fieldSer))
                {
                    object fieldValue = ReadElement(fieldSer);
                    DataField field = ser.GetFieldOrNull(fieldIdx);
                    field?.Set(instance, fieldValue);
                }
                else
                {
                    // it's an unknown user class, try skipping 1 pointer
                    // it will probably crash here with invalid stream error
                    BR.ReadVLu32();
                }
            }
        }

        // Reads an inline element from the stream
        // For pointer types, it reads the pointer value and fetches the right instance
        // For primitive value types, it reads the inline value
        // For UserClass value types, it reads the inline struct fields
        public object ReadElement(TypeSerializer ser)
        {
            if (ser.IsPointerType)
            {
                uint pointer = BR.ReadVLu32();
                if (pointer == 0)
                    return null;
                return ObjectsList[pointer - 1]; // pointer = objectIndex + 1
            }

            if (ser.IsUserClass && ser is UserTypeSerializer fieldUserSer)
            {
                // a custom struct
                object inlineStruct = Activator.CreateInstance(fieldUserSer.Type);
                ReadUserClass(fieldUserSer, inlineStruct);
                return inlineStruct;
            }
            else // int, float, object[], Vector2[], etc
            {
                return ser.Deserialize(this);
            }
        }
    }
}
