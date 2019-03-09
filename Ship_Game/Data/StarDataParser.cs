using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace Ship_Game.Data
{
    // Simplified text parser for StarDrive data files
    public class StarDataParser : IDisposable
    {
        StreamReader Reader;
        public StarDataNode Root { get; }
        static readonly char[] Colon = { ':' };
        static readonly char[] Array = { '[', ']' };
        static readonly char[] Commas = { ',' };

        public StarDataParser(string file)
        {
            FileInfo f = ResourceManager.GetModOrVanillaFile(file);
            if (f == null)
                throw new FileNotFoundException($"Required StarData file not found! {file}");
            Reader = OpenStream(f);
            Root = new StarDataNode
            {
                Key = f.NameNoExt(),
                Value = "",
            };
            Parse();
        }
        public StarDataParser(FileInfo f)
        {
            if (f == null || !f.Exists)
                throw new FileNotFoundException($"Required StarData file not found! {f?.FullName}");
            Reader = OpenStream(f);
            Root = new StarDataNode
            {
                Key = f.NameNoExt(),
                Value = "",
            };
            Parse();
        }
        
        static StreamReader OpenStream(FileInfo f)
        {
            try
            {
                return new StreamReader(f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            }
            catch (UnauthorizedAccessException e) // file is still open?
            {
                Log.Warning($"Open failed: {e.Message}");
                Thread.Sleep(1); // wait a bit
            }
            return new StreamReader(f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
        }

        public void Dispose()
        {
            Reader?.Close(); Reader = null;
        }

        struct DepthSave
        {
            public int Depth;
            public StarDataNode Node;
        }

        void Parse()
        {
            int depth = 0;
            string line;
            var saved = new Stack<DepthSave>();

            StarDataNode root = Root;
            StarDataNode prev = Root;

            while ((line = Reader.ReadLine()) != null)
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;

                (int newDepth, int index) = Depth(line);
                // "      " -- line is all spaces
                // "  # comment after spaces "
                if (index >= line.Length || line[newDepth] == '#')
                    continue; 

                StarDataNode node = ParseLineAsNode(line, out bool isSequence);
                if (newDepth > depth)
                {
                    saved.Push(new DepthSave{ Depth=depth, Node=root });
                    root = prev; // root changed
                }
                else if (newDepth < depth)
                {
                    for (;;) // try to pop down until we get to right depth
                    {
                        DepthSave save = saved.Pop();
                        if (save.Depth > newDepth && saved.Count > 0)
                            continue;
                        root = save.Node;
                        break;
                    }
                }

                if (isSequence)
                {
                    var sequence = root.Value as Array<StarDataNode>;
                    if (root.Value == null)
                        root.Value = sequence = new Array<StarDataNode>();
                    if (sequence == null)
                        throw new InvalidOperationException($"Attempted to create sequence on root with non-sequence Value: {root.Value}");
                    sequence.Add(node);
                }
                else
                {
                    root.AddItem(node);
                }

                depth = newDepth;
                prev = node;
            }
        }

        static string RemoveComment(string text)
        {
            int comment = text.IndexOf('#');
            return comment == -1 ? text : text.Substring(0, comment);
        }

        static StarDataNode ParseLineAsNode(string line, out bool isSequence)
        {
            line = line.TrimStart();
            isSequence = line[0] == '-' && line[1] == ' ';
            if (isSequence)
            {
                line = line.Substring(2);
                string[] parts = line.Split(Colon, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    return new StarDataNode
                    {
                        Key  = null,
                        Value = BoxValue(RemoveComment(parts[0]))
                    };
                }
                else
                {
                    return new StarDataNode
                    {
                        Key  = parts[0].Trim(),
                        Value = BoxValue(RemoveComment(parts[1]))
                    };
                }
            }
            else
            {
                string[] parts = line.Split(Colon, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1) // no value
                {
                    return new StarDataNode
                    {
                        Key  = RemoveComment(parts[0]).Trim(),
                        Value = null
                    };
                }
                else
                {
                    return new StarDataNode
                    {
                        Key  = parts[0].Trim(),
                        Value = BoxValue(RemoveComment(parts[1]))
                    };
                }
            }
        }

        static object BoxValue(string value)
        {
            value = value.Trim();
            if (value.Length == 0) return null;
            if (value == "null")   return null;
            if (value == "true")   return true;
            if (value == "false")  return false;
            char c = value[0];
            if (c == '[')
            {
                if (value[1] == '[')
                    throw new InvalidDataException($"Cannot parse nested arrays, use a sequence instead: '{value}'");
                string trimmed = value.Trim(Array);
                string[] elements = trimmed.Split(Commas, StringSplitOptions.None);

                // now individually box each element into an object array
                var array = new object[elements.Length];
                for (int i = 0; i < elements.Length; ++i)
                    array[i] = BoxValue(elements[i]);
                return array;
            }
            if (('0' <= c && c <= '9') || c == '-' || c == '+')
            {
                if (value.IndexOf('.') != -1 && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    return f;
                if (int.TryParse(value, out int i))
                    return i;
            }
            return value; // probably some sort of text?
        }

        static (int,int) Depth(string line)
        {
            int depth = 0, i = 0;
            for (; i < line.Length; ++i)
            {
                char c = line[i];
                if      (c == ' ')  ++depth;
                else if (c == '\t') depth += 4;
                else break;
            }
            return (depth, i);
        }


        public Array<T> DeserializeArray<T>() where T : new()
        {
            var items = new Array<T>();
            var ser = new StarDataSerializer(typeof(T));
            foreach (StarDataNode child in Root)
            {
                items.Add((T)ser.Deserialize(child));
            }
            return items;
        }
    }
}
