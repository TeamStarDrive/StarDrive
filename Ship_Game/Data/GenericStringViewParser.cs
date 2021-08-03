using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Data
{
    /// <summary>
    /// This is a General purpose high-performance parser which uses StringViews
    /// </summary>
    public class GenericStringViewParser : IDisposable
    {
        /// <summary>
        /// Name of the Parser for debugging
        /// If it's a File parser, this will be the full file path
        /// </summary>
        public string Name { get; }

        // Reader instance which fetches blocks of data
        TextReader Reader;

        // Temporary buffer, this also defines the maximum length of Data we can view at once
        // For human-readable formats, 4096 characters should be more than enough
        // However, for extremely tight content this may not work, for example uglified json
        char[] Buffer;

        /// <summary>
        /// Uses StreamReader to read file in chunks
        /// Maximum allowed line length: 4096 characters
        /// </summary>
        public GenericStringViewParser(FileInfo file)
        {
            Name = file.FullName;
            Reader = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            Buffer = new char[4096];
        }

        public GenericStringViewParser(string name, string text)
        {
            Name = name;
            Reader = new StringReader(text);
            Buffer = new char[4096];
        }

        public GenericStringViewParser(string name, TextReader reader)
        {
            Name = name;
            Reader = reader;
            Buffer = new char[4096];
        }
        
        ~GenericStringViewParser()
        {
            Reader?.Dispose(ref Reader);
            Buffer = null;
        }
        
        public void Dispose()
        {
            Reader?.Dispose(ref Reader);
            Buffer = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Tries to read a single line of Text into a StringView
        /// The maximum line length is limited
        /// Empty lines and comment lines are IGNORED
        /// </summary>
        public bool ReadLine(out StringView ln)
        {
            while (ReadLine(Reader, Buffer, out ln))
            {
                if (ln.Length == 0 || ln.Char0 == '#')
                    continue; // skips empty lines or comments
                return true;
            }
            return false; // EOF
        }

        /// <summary>
        /// Instead of returning a BOOL, returns either a valid StringView or a StringView.Empty
        /// </summary>
        public StringView ReadLine()
        {
            return ReadLine(out StringView line) ? line : StringView.Empty;
        }

        /**
         * The most efficient way to read .NET StreamReader for lines of data
         * @return FALSE if End of Stream
         */ 
        public static bool ReadLine(TextReader reader, char[] buffer, out StringView line)
        {
            int length = 0;
            for (;;)
            {
                int ch = reader.Read();
                switch (ch)
                {
                    case -1: goto end_of_stream;
                    case 10: goto newline;
                    case 13: goto carriage;
                    default:
                        // this will crash on abnormally long lines
                        buffer[length++] = (char)ch;
                        continue;
                }
            }

            carriage:
            if (reader.Peek() == 10) // skip newline
                reader.Read();

            newline:
            line = new StringView(buffer, 0, length); // allow 0 length
            return true;

            end_of_stream:
            if (length > 0)
            {
                line = new StringView(buffer, 0, length);
                return true;
            }
            line = default;
            return false;
        }
    }
}
