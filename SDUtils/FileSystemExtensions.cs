using System.IO;
using System.Xml.Serialization;

namespace SDUtils
{
    public static class FileSystemExtensions
    {
        static readonly string AppRoot = Directory.GetCurrentDirectory();

        public static T Deserialize<T>(this FileInfo info)
        {
            return Deserialize<T>(new XmlSerializer(typeof(T)), info);
        }

        public static T Deserialize<T>(this XmlSerializer serializer, FileInfo info)
        {
            if (!info.Exists)
                return default;
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
    }
}
