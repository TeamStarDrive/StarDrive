using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Ship_Game
{
    public class Dir
    {
        private static readonly FileInfo[] NoFiles = new FileInfo[0];

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
        public static IEnumerable<FileInfo> GetFilesNoThumbs(string dir)
        {
            foreach (FileInfo info in GetFiles(dir))
                if (info.Name != "Thumbs.db")
                    yield return info;
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
    }

    public static class FileSystemExtensions
    {
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
    }
}
