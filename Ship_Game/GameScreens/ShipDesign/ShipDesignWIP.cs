using System.IO;

namespace Ship_Game.GameScreens.ShipDesign
{
    static class ShipDesignWIP
    {
        public static string GetWipSpinUpVersion(string shipName)
        {
            // DesignOrHullName_Ship#_v#_WIP
            string newShipFileName = GetWipFileNameToSave(shipName);
            return newShipFileName;
        }

        public static string GetNewWipName(string shipName)
        {
            FileInfo[] wipFiles = GetWipFiles().Filter(f => f.NameNoExt().StartsWith(shipName));
            if (wipFiles.Length == 0) // first wip
                return $"{shipName}_ship1_v1_WIP";

            // Need to spin up ship version
            string lastModifiedWip = wipFiles.FindMax(f => f.LastWriteTime).NameNoExt();
            int shipVer = GetWipShipVersion(lastModifiedWip);
            return $"{shipName}_ship{shipVer + 1}_v1_WIP";
        }

        static string GetWipFileNameToSave(string wipFileName)
        {
            string defaultShipName = $"{wipFileName}_v1_WIP";
            string shipPrefix      = GetWipShipNameAndNum(wipFileName);
            FileInfo[] wipFiles    = GetWipFiles().Filter(f => f.NameNoExt().StartsWith(shipPrefix));

            if (wipFiles.Length == 0) // first wip
                return defaultShipName;

            FileInfo lastModified = wipFiles.FindMax(f => f.LastWriteTime);
            int version = GetWipSubVersion(lastModified.NameNoExt());
            return $"{GetWipShipNameAndNum(shipPrefix)}_v{version + 1}_WIP";
        }

        // Will return the Ship sub version number
        // Example: "VulcanScout_ship12_v33_WIP" will return "33"
        static int GetWipSubVersion(string wipFileName)
        {
            string[] slicedName = wipFileName.Split('_');
            string subVersion   = slicedName[2].TrimStart('v');
            if (int.TryParse(subVersion, out int versionNum))
                return versionNum;

            Log.Warning($"Get WIP version, could not find WIP version for {wipFileName}, returning version 1.");
            return 1;
        }

        // Will return the Ship version number
        // Example: "VulcanScout_ship12_v33_WIP" will return "12"
        static int GetWipShipVersion(string wipFileName)
        {
            string[] slicedName = wipFileName.Split('_');
            string version = slicedName[1].Replace("ship", "");
            if (int.TryParse(version, out int versionNum))
                return versionNum;

            Log.Warning($"Save WIP, could not find WIP ship version for {wipFileName}, returning ship version 1.");
            return 1;
        }

        // Will return ship name and version number
        // Example: "VulcanScout_ship12_v33_WIP" will return "VulcanScout_ship12"
        static string GetWipShipNameAndNum(string wipFileName)
        {
            string[] slicedName = wipFileName.Split('_');
            return $"{slicedName[0]}_{slicedName[1]}";
        }

        static FileInfo[] GetWipFiles()
        {
            DirectoryInfo wipDir = new DirectoryInfo($"{Dir.StarDriveAppData}/WIP/");
            return wipDir.GetFiles();
        }
    }
}
