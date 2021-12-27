using System;
using System.IO;
using Ship_Game.Data.Serialization;

namespace Ship_Game.Data.Binary
{
    class BinarySerializerReader
    {
        public BinarySerializerHeader Header;
        public Array<object> ObjectsList;
        public uint[] ObjectOffsetTable;

        // maps stream DataField Idx to actual DataFields in memory
        // this is needed because serialized data will almost always
        // be out of sync with latest data field changes
        // 
        // This mapping allows us to completely avoid field name checks
        // during Deserialize() itself
        public ushort[][] StreamFieldsToActual;

        // And this maps Stream Type ID-s to Actual Type ID-s
        // to handle cases where types are added/removed from type list
        public ushort[] StreamTypesToActual;

        public bool StableMapping;

        public BinarySerializerReader(in BinarySerializerHeader header)
        {
            Header = header;
            StableMapping = header.UseStableMapping;
            if (StableMapping)
            {
                StreamFieldsToActual = new ushort[header.NumTypes][];
                StreamTypesToActual = new ushort[header.NumTypes];
            }

            ObjectsList = new Array<object>();
            ObjectsList.Resize(header.NumObjects);
            ObjectOffsetTable = new uint[header.NumObjects];
        }

        // Converts Stream typeId into an Actual typeId
        public ushort GetActualTypeId(ushort streamTypeId)
        {
            if (StableMapping && streamTypeId >= TypeSerializer.MaxFundamentalTypes)
                return StreamTypesToActual[streamTypeId - TypeSerializer.MaxFundamentalTypes];
            return streamTypeId;
        }

        // Converts Stream field index into an Actual fieldIdx
        public ushort GetActualFieldIdx(ushort streamTypeId, ushort streamFieldIdx)
        {
            if (StableMapping && streamTypeId >= TypeSerializer.MaxFundamentalTypes)
                return StreamFieldsToActual[streamTypeId - TypeSerializer.MaxFundamentalTypes][streamFieldIdx];
            return streamFieldIdx;
        }

        public void ReadTypesList(BinaryReader reader, UserTypeSerializer owner)
        {
            for (int i = 0; i < Header.NumTypes; ++i)
            {
                ushort typeId = reader.ReadUInt16();
                string typeNameAndAssembly = reader.ReadString();

                // if type is not found, we completely give up
                // TODO: we should mark this type as invalid and use `null` during deserialize
                Type type = Type.GetType(typeNameAndAssembly, throwOnError: true);
                if (!owner.TypeMap.TryGet(type, out TypeSerializer serializer))
                {
                    serializer = owner.TypeMap.AddUserTypeSerializer(type);
                }

                if (StableMapping && serializer is UserTypeSerializer userSer)
                {
                    ushort numFields = reader.ReadUInt16();
                    ushort[] mapping = new ushort[numFields];

                    for (ushort fieldIdx = 0; fieldIdx < numFields; ++fieldIdx)
                    {
                        string fieldName = reader.ReadString();
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

        public void ReadOffsetTable(BinaryReader reader)
        {
            for (int objectIdx = 0; objectIdx < ObjectOffsetTable.Length; ++objectIdx)
                ObjectOffsetTable[objectIdx] = reader.ReadUInt32();
        }

        public object CreateObject(BinaryReader reader, TypeSerializerMap typeMap, long streamStart, int objectIdx)
        {
            // if object was already created, just return it
            // this handles multiple refs and cyclic refs
            object instance = ObjectsList[objectIdx];
            if (instance != null)
                return instance;

            var stream = reader.BaseStream;
            long previousPos = stream.Position;
            long objectPos = streamStart + ObjectOffsetTable[objectIdx];
            if (objectPos != previousPos)
                stream.Seek(objectPos, SeekOrigin.Begin);

            ushort typeId = GetActualTypeId(reader.ReadUInt16());
            TypeSerializer serializer = typeMap.Get(typeId);

            if (serializer is UserTypeSerializer userSer)
            {
                instance = Activator.CreateInstance(userSer.Type);
                // need to add it right away, to handle cyclic references
                ObjectsList[objectIdx] = instance;

                int numFields = reader.ReadUInt16();
                for (int i = 0; i < numFields; ++i)
                {
                    // serializer Id from the stream
                    ushort fieldTypeId = GetActualTypeId(reader.ReadUInt16());
                    ushort fieldIdx = GetActualFieldIdx(fieldTypeId, reader.ReadUInt16());

                    if (typeMap.TryGet(fieldTypeId, out TypeSerializer fieldSer))
                    {
                        object fieldValue;
                        if (fieldSer.IsUserClass || fieldSer.Type == typeof(string))
                        {
                            int pointer = reader.ReadInt32();
                            if (pointer == 0)
                                continue;
                            fieldValue = CreateObject(reader, typeMap, streamStart, pointer - 1);
                        }
                        else
                        {
                            fieldValue = fieldSer.Deserialize(reader);
                        }

                        DataField field = userSer.GetFieldOrNull(fieldIdx);
                        field?.Set(instance, fieldValue);

                    }
                    else // it's an unknown user class, try skipping 1 pointer
                    {
                        reader.ReadInt32();
                    }
                }
            }
            else // string, object[], stuff like that
            {
                instance = serializer.Deserialize(reader);
                ObjectsList[objectIdx] = instance;
            }

            if (objectPos != previousPos)
                stream.Seek(previousPos, SeekOrigin.Begin);
            return instance;
        }
    }
}
