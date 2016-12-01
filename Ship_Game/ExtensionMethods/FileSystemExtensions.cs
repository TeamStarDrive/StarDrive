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

        public static FileInfo[] GetFiles(string dir)
        {
            try { return new DirectoryInfo(dir).GetFiles("*.*", SearchOption.AllDirectories); }
            catch { return NoFiles; }
        }

        public static FileInfo[] GetFilesNoSub(string dir)
        {
            try { return new DirectoryInfo(dir).GetFiles("*.*", SearchOption.TopDirectoryOnly); }
            catch { return NoFiles; }
        }

        public static void CopyDir(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

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
            if (!info.Exists)
                return default(T);
            using (Stream stream = info.OpenRead())
                return (T)new XmlSerializer(typeof(T)).Deserialize(stream);
        }
    }
}
