using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Ships
{
    public class ModuleGridUtils
    {

        public ModuleGridUtils(int gridWidth, int gridHeight)
        {

        }

        static string SafeSub(string s, int start, int count)
        {
            if (start >= s.Length) return PadCentered("", count);
            if (start+count >= s.Length)
                return PadCentered(s.Substring(start), count);
            return s.Substring(start, count);
        }

        static string PadCentered(string source, int length)
        {
            int spaces = length - source.Length;
            int padLeft = spaces/2 + source.Length;
            return source.PadLeft(padLeft).PadRight(length);
        }

        public enum DumpFormat
        {
            ShipModule,
            SlotStruct,
            InternalSlotsBool
        }

        static Func<object, string[]> GetFormat(DumpFormat format)
        {
            string[] EmptySlot()
            {
                return new []
                {
                    "|_____",
                    "|_____",
                    "|_____"
                };
            }

            switch (format)
            {
                case DumpFormat.ShipModule:
                    string[] GetModuleFormat(ShipModule m)
                    {
                        if (m == null)
                            return EmptySlot();
                        return new []
                        {
                            $"|_{m.XSIZE}x{m.YSIZE}_",
                            $"|{SafeSub(m.UID,0,5)}",
                            $"|{SafeSub(m.UID,5,5)}"
                        };
                    }
                    return m => GetModuleFormat(m as ShipModule);

                case DumpFormat.SlotStruct:
                    string[] GetSlotStructFormat(SlotStruct ss)
                    {
                        if (ss == null)
                            return EmptySlot();
                        ss = ss.Parent ?? ss;
                        ShipModule m = ResourceManager.GetModuleTemplate(ss.ModuleUID);
                        return GetModuleFormat(m);
                    }
                    return m => GetSlotStructFormat((SlotStruct)m);

                case DumpFormat.InternalSlotsBool:
                    string[] GetInternalSlotFormat(bool b)
                    {
                        return new []{ b ? " I " : " - " };
                    }
                    return b => GetInternalSlotFormat((bool)b);
            }
            return null;
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
        }
    }
}
