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
    public sealed class GenericStringViewParser : IDisposable
    {
        /// <summary>
        /// Name of the Parser for debugging
        /// If it's a File parser, this will be the full file path
        /// </summary>
        public string Name { get; }

        // Temporary buffer, this also defines the maximum length of Data we can view at once
        // For human-readable formats, 4096 characters should be more than enough
        // However, for extremely tight content this may not work, for example uglified json
        char[] Buffer;

        byte[] Data;
        int SeekPos;

        /// <summary>
        /// Uses StreamReader to read file in chunks
        /// Maximum allowed line length: 4096 characters
        /// </summary>
        public GenericStringViewParser(FileInfo file)
        {
            Name = file.FullName;
            Buffer = new char[4096];
            Data = File.ReadAllBytes(Name);
        }

        public GenericStringViewParser(string name, byte[] bytes)
        {
            Name = name;
            Buffer = new char[4096];
            Data = bytes;
        }

        public GenericStringViewParser(string name, string text)
        {
            Name = name;
            Buffer = new char[4096];
            Data = Encoding.ASCII.GetBytes(text);
        }

        public void Dispose()
        {
            Buffer = null;
            Data = null;
        }

        /// <summary>
        /// Tries to read a single line of Text into a StringView
        /// The maximum line length is limited
        /// Empty lines and comment lines are IGNORED
        /// </summary>
        public bool ReadLine(out StringView ln)
        {
            while (ReadLine(Buffer, out ln))
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
        bool ReadLine(char[] buffer, out StringView line)
        {
            int length = 0;
            for (;;)
            {
                if (SeekPos >= Data.Length)
                    goto end_of_stream;

                byte ch = Data[SeekPos++];
                switch (ch)
                {
                    case 10: goto newline;
                    case 13: goto carriage;
                    default:
                        // this will crash on abnormally long lines
                        buffer[length++] = (char)ch;
                        continue;
                }
            }

            carriage:
            if (SeekPos < Data.Length && Data[SeekPos] == 10) // skip newline
                ++SeekPos;

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
