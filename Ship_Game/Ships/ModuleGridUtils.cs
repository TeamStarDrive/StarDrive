﻿using System;
using System.IO;
using SDUtils;
using Ship_Game.Gameplay;

namespace Ship_Game.Ships
{
    public class ModuleGridUtils
    {
        static string SafeSub(string s, int start, int count)
        {
            if (start >= s.Length) return PadCentered("", count);
            if (start+count >= s.Length)
                return PadCentered(s.Substring(start), count);
            return s.Substring(start, count);
        }

        static string PadCentered(string source, int length, char paddingChar = ' ')
        {
            int spaces = length - source.Length;
            int padLeft = spaces/2 + source.Length;
            return source.PadLeft(padLeft, paddingChar).PadRight(length, paddingChar);
        }

        public enum DumpFormat
        {
            ShipModule,
            DesignSlot,
            SlotStruct,
            SlotStructEmptyHull,
            InternalSlotsBool,
            ExternalSlotsBits,
            ExternalSlotModules,
        }

        static string[] EmptySlot(int width, int height)
        {
            var lines = new string[height];
            for (int i = 0; i < height-1; ++i)
                lines[i] = "|" + new string(' ', width);
            lines[lines.Length-1] = "|" + new string('_', width);
            return lines;
        }
        
        static string[] GetModuleFormat7x4(ShipModule m)
        {
            if (m == null)
                return EmptySlot(7, 4);

            string[] lines = {
                "|"+PadCentered($"{m.Restrictions} {m.XSize}x{m.YSize}", 7),
                "|"+SafeSub(m.UID, 0,  7),
                "|"+SafeSub(m.UID, 7,  7),
                "|"+SafeSub(m.UID, 14, 7),
            };
            return lines;
        }

        static string[] GetDesignSlotFormat(DesignSlot s)
        {
            if (s == null)
                return EmptySlot(7, 4);
            return GetModuleFormat7x4(ResourceManager.GetModuleTemplate(s.ModuleUID));
        }

        static string[] GetSlotStructFormat(SlotStruct ss)
        {
            ss = ss?.Parent ?? ss;
            if (ss == null)
                return EmptySlot(7, 4);
            if (ss.ModuleUID == null)
                return GetSlotStructEmptyHullFormat(ss, 7, 4);
            return GetModuleFormat7x4(ResourceManager.GetModuleTemplate(ss.ModuleUID));
        }

        static string[] GetSlotStructEmptyHullFormat(SlotStruct ss, int width, int height)
        {
            string[] lines = EmptySlot(width,height);
            if (ss != null)
            {
                int middle = (height / 2) + (height % 2 == 0 ? -1 : 0);
                lines[middle] = "|"+PadCentered(ss.HullRestrict.ToString(), width);
            }
            return lines;
        }

        static string[] GetInternalSlotFormat(bool b)
        {
            return new []{ b ? " I " : " - " };
        }

        static string[] GetExternalSlotBitsFormat(bool b)
        {
            return new []{ b ? " E " : " - " };
        }

        static string[] GetExternalSlotFormat(ShipModule m)
        {
            if (m == null)
                return EmptySlot(4, 1);
            if (m.IsExternal)
                return new []{ $"|E{m.XSize}x{m.YSize}" };
            return new []{ $" {m.XSize}x{m.YSize}" };
        }

        static Func<object, string[]> GetFormat(DumpFormat format)
        {
            switch (format)
            {
                case DumpFormat.ShipModule:
                    return m => GetModuleFormat7x4(m as ShipModule);
                case DumpFormat.DesignSlot:
                    return m => GetDesignSlotFormat(m as DesignSlot);
                case DumpFormat.SlotStruct:
                    return m => GetSlotStructFormat((SlotStruct)m);
                case DumpFormat.SlotStructEmptyHull:
                    return m => GetSlotStructEmptyHullFormat((SlotStruct)m, 3, 1);
                case DumpFormat.InternalSlotsBool:
                    return b => GetInternalSlotFormat((bool)b);
                case DumpFormat.ExternalSlotsBits:
                    return b => GetExternalSlotBitsFormat((bool)b);
                case DumpFormat.ExternalSlotModules:
                    return m => GetExternalSlotFormat(m as ShipModule);
            }
            throw new Exception("Invalid DumpFormat");
        }

        public static void DebugDumpGrid<T>(string fileName, T[] grid,
                                            int width, int height, DumpFormat fmt)
        {
            string fullPath = Path.Combine(Dir.StarDriveAppData, fileName);
            string exportDir = Path.GetDirectoryName(fullPath) ?? "";
            Directory.CreateDirectory(exportDir);

            Func<object, string[]> format = GetFormat(fmt);

            int columnWidth = 0;
            var formatted = new string[width * height][];
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    int index = x + y * width;
                    string[] text = format(grid[index]);
                    formatted[index] = text;
                    foreach (string s in text)
                        columnWidth = Math.Max(columnWidth, s.Length);
                }
            }

            using (var fs = new StreamWriter(fullPath))
            {
                fs.Write($"W: {width} H:{height}\n");
                fs.Write("   ");
                for (int x = 0; x < width; ++x)
                    fs.Write(PadCentered(x.ToString(), columnWidth));
                fs.Write('\n');
                for (int y = 0; y < height; ++y)
                {
                    fs.Write(PadCentered(y.ToString(), 3));
                    int numLines = formatted[0].Length;
                    for (int line = 0; line < numLines; ++line)
                    {
                        if (line != 0) fs.Write("   ");
                        for (int x = 0; x < width; ++x)
                        {
                            string[] element = formatted[x + y * width];
                            fs.Write(PadCentered(element[line], columnWidth));
                        }
                        fs.Write('\n');
                    }
                }
            }

            Log.Info($"Saved {fullPath}");
        }
    }
}
