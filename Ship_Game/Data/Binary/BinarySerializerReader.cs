using System;
using System.IO;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    class BinarySerializerReader
    {
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

        public BinarySerializerReader(in BinarySerializerHeader header)
        {
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

        public void ReadTypesList(BinaryReader br, TypeSerializerMap typeMap)
        {
            for (int i = 0; i < Header.NumTypes; ++i)
            {
                ushort typeId = br.ReadUInt16();
                string typeNameAndAssembly = br.ReadString();

                // if type is not found, we completely give up
                // TODO: we should mark this type as invalid and use `null` during deserialize
                Type type = Type.GetType(typeNameAndAssembly, throwOnError: true);
                if (!typeMap.TryGet(type, out TypeSerializer serializer))
                {
                    serializer = typeMap.AddUserTypeSerializer(type);
                }

                if (StableMapping && serializer is UserTypeSerializer userSer)
                {
                    ushort numFields = br.ReadUInt16();
                    ushort[] mapping = new ushort[numFields];

                    for (ushort fieldIdx = 0; fieldIdx < numFields; ++fieldIdx)
                    {
                        string fieldName = br.ReadString();
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
        public void ReadTypeGroups(BinaryReader br, TypeSerializerMap typeMap)
        {
            TypeGroups = new (TypeSerializer Ser, int Count)[Header.NumTypeGroups];

            int totalCount = 0;
            for (int i = 0; i < TypeGroups.Length; ++i)
            {
                int typeId = GetActualTypeId(br.ReadUInt16());
                int count = br.ReadInt32();
                totalCount += count;
                TypeGroups[i] = (typeMap.Get(typeId), count);
            }

            ObjectsList = new object[totalCount];
        }

        // populate all object instances by reading the object fields
        public void ReadObjectsList(BinaryReader br, TypeSerializerMap typeMap)
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
                        object instance = ser.Deserialize(br);
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
                        ReadUserClass(br, typeMap, userType, instance);
                    }
                }
                else
                {
                    objectIdx += groupCount; // skip
                }
            }
        }

        void ReadUserClass(BinaryReader br, TypeSerializerMap typeMap, UserTypeSerializer ser, object instance)
        {
            if (instance == null)
            {
                Log.Error($"Failed to deserialize {ser}");
                return;
            }

            int numFields = br.ReadUInt16();
            for (int i = 0; i < numFields; ++i)
            {
                // serializer Id from the stream
                ushort fieldTypeId = GetActualTypeId(br.ReadUInt16());
                ushort fieldIdx = GetActualFieldIdx(fieldTypeId, br.ReadUInt16());

                if (typeMap.TryGet(fieldTypeId, out TypeSerializer fieldSer))
                {
                    object fieldValue;
                    if (BinarySerializer.IsPointerType(fieldSer))
                    {
                        int pointer = br.ReadInt32();
                        if (pointer == 0)
                            continue;
                        fieldValue = ObjectsList[pointer - 1]; // pointer = objectIndex + 1
                    }
                    else if (fieldSer is UserTypeSerializer fieldUserSer)
                    {
                        // a custom struct
                        fieldValue = Activator.CreateInstance(fieldUserSer.Type);
                        ReadUserClass(br, typeMap, fieldUserSer, fieldValue);
                    }
                    else
                    {
                        fieldValue = fieldSer.Deserialize(br);
                    }

                    DataField field = ser.GetFieldOrNull(fieldIdx);
                    field?.Set(instance, fieldValue);
                }
                else // it's an unknown user class, try skipping 1 pointer
                {
                    br.ReadInt32();
                }
            }
        }
    }
}
