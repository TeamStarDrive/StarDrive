using System;
using System.Collections.Generic;
using System.Linq;

namespace Ship_Game.Ships
{
    public static class ShipExtensions
    {
        public static int SurfaceArea(this ShipModule[] modules, ShipModuleType moduleType)
        {
            int modulesArea = 0;
            for (int i = 0; i < modules.Length; ++i)
            {
                ShipModule m = modules[i];
                if (m.ModuleType == moduleType) modulesArea += m.XSIZE * m.YSIZE;
            }
            return modulesArea;
        }

        public static int SurfaceArea(this ShipModule[] modules, Func<ShipModule, bool> predicate)
        {
            int modulesArea = 0;
            for (int i = 0; i < modules.Length; ++i)
            {
                ShipModule m = modules[i];
                if (predicate(m)) modulesArea += m.XSIZE * m.YSIZE;
            }
            return modulesArea;
        }

        public static ShipModule[] FilterBy(this ShipModule[] modules, ShipModuleType moduleType)
        {
            int total = 0;
            for (int i = 0; i < modules.Length; ++i)
                total += (modules[i].ModuleType == moduleType) ? 1 : 0;

            var filtered = new ShipModule[total];
            for (int i = 0, j = 0; i < modules.Length; ++i)
                if (modules[i].ModuleType == moduleType)
                    filtered[j++] = modules[i];

            return filtered;
        }

        public static bool Any(this ShipModule[] modules, ShipModuleType moduleType)
        {
            for (int i = 0; i < modules.Length; ++i)
                if (modules[i].ModuleType == moduleType)
                    return true;
            return false;
        }
    }
}
