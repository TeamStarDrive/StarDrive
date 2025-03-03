﻿using System;

namespace Ship_Game.Data.Binary;

public struct BinarySerializerHeader
{
    // Signature of a valid BinarySerializerHeader
    // This also functions as the MAJOR VERSION signature
    // allowing us to ignore older savegame types which are incompatible
    public const uint ValidSignature = 0x2f2f2f2f;

    public uint Signature; // is this a BinarySerializerHeader ?
    public uint Version;
    public OptionFlags Options;
    public uint NumUsedTypes;
    public uint NumCollectionTypes;
    public uint MaxTypeId;
    public uint NumTypeGroups;
    public uint RootObjectId;

    [Flags]
    public enum OptionFlags : uint
    {
        None = 0,
    }

    public BinarySerializerHeader(BinarySerializerWriter writer)
    {
        Signature = ValidSignature;
        Version = BinarySerializer.CurrentVersion;
        NumUsedTypes = (uint)writer.Types.ValuesAndClasses.Length;
        NumCollectionTypes = (uint)writer.Types.Collections.Length;
        MaxTypeId = (uint)writer.MaxTypeId;
        NumTypeGroups = (uint)writer.NumTypeGroups;
        RootObjectId = writer.RootObjectId;

        Options = OptionFlags.None;
    }

    public BinarySerializerHeader(Reader reader)
    {
        Signature = reader.ReadUInt32(); // always UInt32
        Version = reader.ReadVLu32();
        Options = (OptionFlags)reader.ReadVLu32();
        NumUsedTypes = reader.ReadVLu32();
        NumCollectionTypes = reader.ReadVLu32();
        MaxTypeId = reader.ReadVLu32();
        NumTypeGroups = reader.ReadVLu32();
        RootObjectId = reader.ReadVLu32();
    }

    public void Write(Writer writer)
    {
        writer.Write(Signature); // always UInt32
        writer.WriteVLu32(Version);
        writer.WriteVLu32((uint)Options);
        writer.WriteVLu32(NumUsedTypes);
        writer.WriteVLu32(NumCollectionTypes);
        writer.WriteVLu32(MaxTypeId);
        writer.WriteVLu32(NumTypeGroups);
        writer.WriteVLu32(RootObjectId);
    }

    public override string ToString()
    {
        return $"v{Version} Opt={Options} Types={NumUsedTypes} Coll={NumCollectionTypes} Groups={NumTypeGroups} Root={RootObjectId}";
    }
}
