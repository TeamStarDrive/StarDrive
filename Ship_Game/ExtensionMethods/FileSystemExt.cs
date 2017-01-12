﻿using System;
using System.IO;
using System.Xml.Serialization;

namespace Ship_Game
{
    public static class Dir
    {
        private static readonly FileInfo[]      NoFiles = new FileInfo[0];
        private static readonly DirectoryInfo[] NoDirs  = new DirectoryInfo[0];

        // Added by RedFox - this is a safe wrapper to DirectoryInfo.GetFiles which assumes 
        //                   dir is optional and if it doesn't exist, returns empty file list
        public static FileInfo[] GetFiles(string dir, string pattern, SearchOption option)
        {
            try
            {
                var info = new DirectoryInfo(dir);
                return info.Exists ? info.GetFiles(pattern, option) : NoFiles;
            }
            catch { return NoFiles; }
        }
        public static FileInfo[] GetFiles(string dir)
        {
            return GetFiles(dir, "*.*", SearchOption.AllDirectories);
        }
        public static FileInfo[] GetFiles(string dir, string ext)
        {
            return GetFiles(dir, "*."+ext, SearchOption.AllDirectories);
        }
        public static FileInfo[] GetFilesNoSub(string dir)
        {
            return GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly);
        }
        public static FileInfo[] GetFilesNoSub(string dir, string ext)
        {
            return GetFiles(dir, "*."+ext, SearchOption.TopDirectoryOnly);
        }

        // Finds all subdirectories
        public static DirectoryInfo[] GetDirs(string dir, SearchOption option = SearchOption.AllDirectories)
        {
            try
            {
                var info = new DirectoryInfo(dir);
                return info.Exists ? info.GetDirectories("*", option) : NoDirs;
            }
            catch { return NoDirs; }
        }

        public static void CopyDir(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDirName}");

            var dirs = dir.GetDirectories();

            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            foreach (FileInfo file in dir.GetFiles())
                file.CopyTo(Path.Combine(destDirName, file.Name), true);

            if (!copySubDirs)
                return;

            foreach (DirectoryInfo subdir in dirs)
                CopyDir(subdir.FullName, Path.Combine(destDirName, subdir.Name), true);
        }

        // System AppData folder
        public static string ApplicationData => Environment.GetFolderPath(
                                                Environment.SpecialFolder.ApplicationData);
    }

    public static class FileSystemExtensions
    {
        private static string AppRoot = Path.GetFullPath(".");

        public static T Deserialize<T>(this FileInfo info)
        {
            return Deserialize<T>(new XmlSerializer(typeof(T)), info);
        }

        public static T Deserialize<T>(this XmlSerializer serializer, FileInfo info)
        {
            if (!info.Exists)
                return default(T);
            using (Stream stream = info.OpenRead())
                return (T)serializer.Deserialize(stream);
        }

        public static string NameNoExt(this FileInfo info)
        {
            string fileName = info.Name;
            int i = fileName.LastIndexOf('.');
            return i == -1 ? fileName : fileName.Substring(0, i);
        }

        public static string PathNoExt(this FileInfo info)
        {
            return (info.DirectoryName??"") + "/" + info.NameNoExt();
        }

        public static string RelPath(this FileInfo info)
        {
            return info.FullName.Substring(AppRoot.Length + 1);
        }

        public static string RelPathNoExt(this FileInfo info)
        {
            string filePath = info.FullName.Substring(AppRoot.Length + 1);
            int i = filePath.LastIndexOf('.');
            return i == -1 ? filePath : filePath.Substring(0, i);
        }

        // Creates a clean relative file path, useful for resource loading
        // Ex: "Content\\Textures\\blank.xnb"    --> "Textures/blank"
        // Ex: "Mods/MyMod/Textures\\blank.xnb"  --> "Textures/blank"
        public static string CleanResPath(this FileInfo info)
        {
            string filePath = info.FullName.Substring(AppRoot.Length + 1);

            if (filePath.StartsWith("Content", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring("Content/".Length);
            else if (filePath.StartsWith("Mods", StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(GlobalStats.ModPath.Length);

            filePath = filePath.Replace('\\', '/');

            int i = filePath.LastIndexOf('.');
            return i == -1 ? filePath : filePath.Substring(0, i);
        }
    }
}
