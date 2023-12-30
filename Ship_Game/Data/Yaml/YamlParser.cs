#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using SDUtils;

namespace Ship_Game.Data.Yaml
{
    // Simplified text parser for StarDrive data files
    public sealed class YamlParser : IDisposable
    {
        public YamlNode Root { get; }

        readonly Array<Error> LoggedErrors = new();
        public IReadOnlyList<Error> Errors => LoggedErrors;

        /// <summary>
        /// Parses a YAML file, preferring MOD file if it exists.
        /// If MOD file does not exist, vanilla file is parsed instead.
        /// </summary>
        public YamlParser(string modOrVanillaFile)
        {
            FileInfo yamlFile = ResourceManager.GetModOrVanillaFile(modOrVanillaFile);
            using var reader = OpenStream(yamlFile, modOrVanillaFile);
            Root = Parse(reader, yamlFile.NameNoExt(), LoggedErrors);
        }

        /// <summary>
        /// Parses the specified YAML file
        /// </summary>
        public YamlParser(FileInfo yamlFile)
        {
            using var reader = OpenStream(yamlFile);
            Root = Parse(reader, yamlFile.NameNoExt(), LoggedErrors);
        }

        /// <summary>
        /// Parses a named TextReader YAML content
        /// </summary>
        public YamlParser(string name, TextReader reader)
        {
            if (reader == null) throw new NullReferenceException(nameof(reader));
            Root = Parse(reader, name, LoggedErrors);
            reader.Dispose();
        }

        /// <summary>
        /// Combines two YAML files by providing a second YAML file which overrides
        /// values from the original YAML. This is for mod support.
        /// </summary>
        /// <param name="baseYaml">The base YAML file</param>
        /// <param name="overrideYaml">The mod YAML file which overrides and adds new nodes</param>
        public YamlParser(FileInfo baseYaml, FileInfo? overrideYaml)
        {
            using (var baseReader = OpenStream(baseYaml))
                Root = Parse(baseReader, baseYaml.NameNoExt(), LoggedErrors);

            // convenience: allow this to be null, to easily manage parsing if mod file does not exist
            if (overrideYaml != null)
            {
                using var overrideReader = OpenStream(overrideYaml);
                YamlNode overrideRoot = Parse(overrideReader, overrideYaml.NameNoExt(), LoggedErrors);
                Root = MergeRoots(Root, overrideRoot);
            }
        }

        /// <summary>
        /// Loads a list of YAML files in order, and merges them into a single root node.
        /// </summary>
        /// <param name="loadOrder">YAML files to load and merge</param>
        public YamlParser(IReadOnlyList<FileInfo?> loadOrder)
        {
            if (loadOrder.Count == 0)
                throw new ArgumentException("YAML loadOrder must have at least one file");

            using (var baseReader = OpenStream(loadOrder[0]))
                Root = Parse(baseReader, loadOrder[0].NameNoExt(), LoggedErrors);

            for (int i = 1; i < loadOrder.Count; ++i)
            {
                FileInfo? overrideInfo = loadOrder[i];
                // convenience: allow this to be null, to easily manage parsing if mod file does not exist
                if (overrideInfo != null)
                {
                    using var overrideReader = OpenStream(loadOrder[i]);
                    YamlNode overrideRoot = Parse(overrideReader, loadOrder[i].NameNoExt(), LoggedErrors);
                    Root = MergeRoots(Root, overrideRoot);
                }
            }
        }

        /// <summary>
        /// Combines two YAML files by providing a second YAML file which overrides
        /// values from the original YAML. This is for mod support.
        /// </summary>
        public YamlParser(string name, TextReader baseYaml, TextReader? overrideYaml)
        {
            try
            {
                Root = Parse(baseYaml, name, LoggedErrors);
                // convenience: allow this to be null, to easily manage parsing if mod file does not exist
                if (overrideYaml != null)
                {
                    YamlNode overrideRoot = Parse(overrideYaml, name, LoggedErrors);
                    Root = MergeRoots(Root, overrideRoot);
                }
            }
            finally
            {
                baseYaml?.Dispose();
                overrideYaml?.Dispose();
            }
        }

        static StreamReader OpenStream(FileInfo? f, string? nameInfo = null)
        {
            if (f?.Exists != true)
                throw new FileNotFoundException($"Required StarData file not found! {nameInfo ?? f?.FullName}");
            try
            {
                return new StreamReader(f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
            }
            catch (UnauthorizedAccessException e) // file is still open?
            {
                Log.Warning(ConsoleColor.DarkRed, $"Open failed: {e.Message} {nameInfo ?? f.FullName}");
                Thread.Sleep(1); // wait a bit
            }
            // try again or throw
            return new StreamReader(f.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite), Encoding.UTF8);
        }

        public void Dispose()
        {
            // TODO: is Dispose needed anymore?
            LoggedErrors.Clear();
        }

        public struct Error
        {
            public int Line;
            public int Column;
            public string Message;
            public string Document;
            public override readonly string ToString() => $"{Document}:{Line}:{Column} {Message}";
        }

        struct DepthSave
        {
            public int Depth;
            public YamlNode Node;
        }

        /// <summary>
        /// Can parse any YAML content into a YamlNode root.
        /// If parsing completely fails, the root will be empty.
        /// </summary>
        /// <param name="reader">YAML content reader</param>
        /// <param name="rootName">Name of the root element [document name]</param>
        /// <param name="loggedErrors">Output for any errors [if not null]</param>
        /// <returns>The root node. Always non-null.</returns>
        public static YamlNode Parse(TextReader reader, string rootName, Array<Error>? loggedErrors = null)
        {
            Parser parser = new(rootName, loggedErrors);

            var buffer = new char[4096];
            int depth = 0;
            var saved = new Stack<DepthSave>();

            YamlNode mainRoot = new() { Key = parser.RootName, Value = null }; ;
            YamlNode root = mainRoot;
            YamlNode prev = mainRoot;

            while (ReadLineWithDepth(reader, buffer, out StringView line, out int newDepth))
            {
                ++parser.Line;

                // "      " -- line is all spaces
                // "  # comment after spaces "
                if (line.Length == 0 || line.Char0 == '#')
                    continue;

                YamlNode? node = parser.ParseLineAsNode(line, out bool isSequence);
                if (node == null)
                    continue;

                if (newDepth > depth)
                {
                    saved.Push(new DepthSave { Depth = depth, Node = root });
                    root = prev; // root changed
                }
                else if (newDepth < depth)
                {
                    for (; ; ) // try to pop down until we get to right depth
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
                    // we got a sequence element
                    // root:
                    //   - node
                    root.AddSequenceElement(node);
                }
                else
                {
                    // we got a sub node
                    // root:
                    //   node
                    root.AddSubNode(node);
                }

                depth = newDepth;
                prev = node;
            }

            return mainRoot;
        }

        class Parser(string rootName, Array<Error>? loggedErrors)
        {
            public string RootName = rootName ?? "";
            public Array<Error>? LoggedErrors = loggedErrors;
            public int Line;
            public StringBuilder Str = new(); // for efficient string literal parsing

            public YamlNode? ParseLineAsNode(StringView view, out bool isSequence)
            {
                isSequence = view.StartsWith("- ");
                if (isSequence)
                {
                    view.Skip(2);
                    if (view.Length == 0)
                    {
                        LogError(view, "Syntax Error: expected a value");
                        return null;
                    }
                }
                var node = new YamlNode();
                return ParseTokenAsNode(node, ref view);
            }

            YamlNode ParseTokenAsNode(YamlNode node, ref StringView view)
            {
                StringView first = NextToken(ref view);
                if (first.Length == 0)
                    return node; // completely empty node (allowed by YAML spec)

                if (first.Char0 == '{') // anonymous inline Map { X: Y }
                {
                    ParseObject(node, ref view);
                    return node;
                }

                // parse first token as a value
                object? firstValue = ParseValue(first, ref view);

                // see if we can get another token?
                StringView separator = NextToken(ref view);
                if (separator.Length == 0 || separator[0] == '#') // no token, so only value!
                {
                    node.Value = firstValue;
                    return node;
                }
                if (separator != ":") // and now we expect `:`, otherwise the syntax is too bogus
                {
                    LogError(separator, $"Syntax Error: expected ':' for key:value entry but got {separator.Text} instead");
                    return node;
                }

                node.Key = firstValue;

                StringView second = NextToken(ref view);
                second.TrimEnd();
                if (second.Length == 0) // no value! (probably a sequence will follow)
                    return node;

                if (second.Char0 == '{') // KEY: anonymous inline Map { X: Y }
                {
                    ParseObject(node, ref view);
                    return node;
                }

                // parse second token as a value
                node.Value = ParseValue(second, ref view);
                return node;
            }

            object? ParseValue(StringView token, ref StringView view)
            {
                if (token.Length == 0) return null;
                if (token == "null") return null;
                if (token == "true") return true;
                if (token == "false") return false;
                char c = token.Char0;
                if (('0' <= c && c <= '9') || c == '-' || c == '+')
                {
                    if (token.IndexOf('.') != -1)
                        return token.ToFloat();
                    return token.ToInt();
                }
                if (c == '\'' || c == '"')
                {
                    return ParseString(ref view, terminator: c);
                }
                if (c == '[')
                {
                    return ParseArray(ref view);
                }
                if (c == '{')
                {
                    LogError(token, "Parse Error: map not supported in this context");
                    return null;
                }
                token.TrimEnd();
                return token.Text; // probably some text
            }

            void ParseObject(YamlNode node, ref StringView view)
            {
                for (; ; )
                {
                    view.TrimStart();
                    if (view.Length == 0)
                    {
                        LogError(view, "Parse Error: map expected '}' before end of line");
                        break;
                    }

                    if (view.Char0 == '}')
                        break; // end of map

                    var child = new YamlNode();
                    ParseTokenAsNode(child, ref view);
                    node.AddSubNode(child);

                    StringView separator = NextToken(ref view);
                    if (separator.Length == 0)
                    {
                        LogError(separator, "Parse Error: map expected '}' before end of line");
                        break;
                    }

                    if (separator.Char0 == '}')
                        break; // end of map

                    if (separator.Char0 != ',')
                    {
                        LogError(separator, "Parse Error: map expected ',' separator after value entry");
                        break;
                    }
                }
            }

            object ParseArray(ref StringView view)
            {
                var arrayItems = new Array<object?>();

                for (; ; )
                {
                    StringView token = NextToken(ref view);
                    if (token.Length == 0)
                    {
                        LogError(token, "Parse Error: array expected ']' before end of line");
                        break;
                    }

                    if (token.Char0 == ']')
                        break; // end of array

                    object? o = ParseValue(token, ref view);
                    arrayItems.Add(o);

                    StringView separator = NextToken(ref view);
                    if (separator.Length == 0)
                    {
                        LogError(separator, "Parse Error: array expected ']' before end of line");
                        break;
                    }

                    if (separator.Char0 == ']')
                        break; // end of array

                    if (separator.Char0 != ',')
                    {
                        LogError(separator, "Parse Error: array expected ',' separator after an entry");
                        break;
                    }
                }
                return arrayItems.ToArray();
            }

            string ParseString(ref StringView view, char terminator)
            {
                Str.Clear();
                while (view.Length > 0)
                {
                    char c = view.PopFirst();
                    if (c == terminator)
                        break;

                    if (c == '\\') //  "   \\  \r  \n  \t  \'  \"   "
                    {
                        if (view.Length > 0)
                        {
                            c = view.PopFirst();
                            switch (c)
                            {
                                case '\\': Str.Append('\\'); break;
                                case 'r': Str.Append('\r'); break;
                                case 'n': Str.Append('\n'); break;
                                case 't': Str.Append('\t'); break;
                                case '\'': Str.Append('\''); break;
                                case '"': Str.Append('"'); break;
                            }
                        }
                        else
                        {
                            LogError(view, "Parse Error: unexpected end of string");
                            break;
                        }
                    }
                    else
                    {
                        Str.Append(c);
                    }
                }
                return Str.ToString();
            }

            static StringView NextToken(ref StringView view)
            {
                view.TrimStart();

                int start = view.Start;
                int current = start;
                int eos = start + view.Length;
                char[] chars = view.Chars;
                while (current < eos)
                {
                    switch (chars[current]) // is delimiter?
                    {
                        case ':':
                        case '#':
                        case '\'':
                        case '"':
                        case ',':
                        case '{':
                        case '}':
                        case '[':
                        case ']':
                            if (start == current)
                            {
                                view.Skip(1);
                                return new StringView(chars, start, 1);
                            }
                            goto finished;
                    }
                    ++current;
                }

            finished:
                int length = current - start;
                view.Skip(length);
                return new StringView(chars, start, length);
            }

            void LogError(in StringView view, string what)
            {
                var e = new Error { Line = Line, Column = view.Start, Message = what, Document = RootName };
                Log.Error(e.ToString());
                LoggedErrors?.Add(e);
            }
        }

        // @note The most efficient way to read .NET TextReader
        //       Also combines YAML specific whitespace skipping logic
        static bool ReadLineWithDepth(TextReader reader, char[] buffer, out StringView line, out int outDepth)
        {
            int depth = 0;
            int length = 0;
            for (; ; )
            {
                int ch = reader.Read();
                switch (ch)
                {
                    case -1: goto end_of_stream;
                    case 10: goto newline;
                    case 13: goto carriage;
                    default:
                        if (length == 0) // still leading whitespace
                        {
                            if (ch == ' ') { depth += 1; continue; }
                            if (ch == '\t') { depth += 2; continue; }
                        }
                        buffer[length++] = (char)ch;
                        continue;
                }
            }

        carriage:
            if (reader.Peek() == 10) // skip newline
                reader.Read();

            newline:
            line = new StringView(buffer, 0, length); // allow 0 length
            outDepth = depth;
            return true;

        end_of_stream:
            if (length > 0)
            {
                line = new StringView(buffer, 0, length);
                outDepth = depth;
                return true;
            }
            line = default;
            outDepth = 0;
            return false;
        }

        /// <summary>
        /// Merges two roots by overriding base values and adding new from overrideRoot
        /// </summary>
        /// <param name="baseRoot">Base YAML root node</param>
        /// <param name="overrideRoot">YAML node with overriding and new values</param>
        /// <returns>Merged Root node</returns>
        public static YamlNode MergeRoots(YamlNode baseRoot, YamlNode overrideRoot)
        {
            MergeNodes(baseRoot, overrideRoot);
            return baseRoot;
        }

        static void MergeNodes(YamlNode node, YamlNode addFrom)
        {
            // first merge the node.Value if it was specified
            node.Value = addFrom.Value ?? node.Value;

            // for each subnode in the override yaml:
            //  1. Try to find existing node and Merge()
            //  2. If not found, simply add it
            if (addFrom.HasSubNodes) // object
            {
                foreach (YamlNode add in addFrom.Nodes)
                {
                    if (node.FindSubNode(add.Name, out YamlNode? existing))
                        MergeNodes(existing!, add);
                    else
                        node.AddSubNode(add);
                }
            }
            else if (addFrom.HasSequence) // array
            {
                // for primitive sequences we should always overwrite it
                if (addFrom.SequenceIsPrimitives)
                {
                    node.SetSequence(addFrom.Sequence);
                    return;
                }

                // this is a sequence of OBJECTS, with potentially mixed types
                // - Panel: { Name: logo1 }
                // - Button: { Name: new_game }
                foreach (YamlNode add in addFrom.Sequence)
                {
                    // seq item has a key, so it's a TYPE declaration with maybe an ID:
                    // - SoundEffect:
                    //     Id: SD_Theme_Reprise_06
                    // or maybe an anonymous object with ID
                    // - { Id: item1, Data: 0 }
                    YamlNode? id = add.GetIdNode(); // "Id" or "Name"
                    if (id != null)
                    {
                        object? type = add.Key;
                        if (node.FindSeqItemByIdAndType(id, type, out YamlNode? found))
                        {
                            MergeNodes(found!, add);
                            continue; // merged, continue to next
                        }
                    }

                    // there was no matching Id or there is no Id field, so this is a new item
                    node.AddSequenceElement(add);
                }
            }
        }

        /// <summary>
        /// Deserialize a sequence of root elements
        /// ...
        /// Planet:
        ///   Name: Mars
        /// Planet:
        ///   Name: Venus
        /// ...
        /// struct Planet
        /// {
        ///     public string Name;
        /// }
        /// </summary>
        public Array<T> DeserializeArray<T>() where T : new()
        {
            var items = new Array<T>();
            var ser = new YamlSerializer.YamlSerializer(typeof(T));
            foreach (YamlNode child in Root)
            {
                items.Add((T)ser.Deserialize(child));
            }
            return items;
        }

        public static Array<T> DeserializeArray<T>(string modOrVanillaFile) where T : new()
        {
            using (var parser = new YamlParser(modOrVanillaFile))
                return parser.DeserializeArray<T>();
        }

        public static Array<T> DeserializeArray<T>(FileInfo file) where T : new()
        {
            using (var parser = new YamlParser(file))
                return parser.DeserializeArray<T>();
        }

        /// <summary>
        /// Deserialize a single root element
        /// ...
        /// List:
        ///   Rect: [-20, 0.22, 200, 600]
        ///   AxisAlign: CenterRight
        /// ...
        /// struct List
        /// {
        ///     public Rectangle Rect;
        ///     public AxisAlignment AxisAlign;
        /// }
        /// </summary>
        public T? DeserializeOne<T>() where T : new()
        {
            var ser = new YamlSerializer.YamlSerializer(typeof(T));
            foreach (YamlNode child in Root)
            {
                return (T)ser.Deserialize(child);
            }
            return default;
        }

        public static T? DeserializeOne<T>(string modOrVanillaFile) where T : new()
        {
            using var parser = new YamlParser(modOrVanillaFile);
            return parser.DeserializeOne<T>();
        }

        public static T? DeserializeOne<T>(FileInfo file) where T : new()
        {
            using var parser = new YamlParser(file);
            return parser.DeserializeOne<T>();
        }

        public T Deserialize<T>() where T : new()
        {
            var ser = new YamlSerializer.YamlSerializer(typeof(T));
            return (T)ser.Deserialize(Root);
        }

        public static T Deserialize<T>(string modOrVanillaFile) where T : new()
        {
            using var parser = new YamlParser(modOrVanillaFile);
            return parser.Deserialize<T>();
        }

        public static T Deserialize<T>(FileInfo file) where T : new()
        {
            using var parser = new YamlParser(file);
            return parser.Deserialize<T>();
        }

        /// <summary>
        /// Deserializes a map of root elements with unique identifiers
        /// ...
        /// AttackRunsOrder:
        ///  Id: 1
        ///  ENG: "Attack Runs Order"
        /// ArtilleryOrder:
        ///  Id: 2
        ///  ENG: "Artillery Order"
        /// ...
        /// </summary>
        public Array<KeyValuePair<TKey, TValue>> DeserializeMap<TKey, TValue>() where TValue : new()
        {
            var items = new Array<KeyValuePair<TKey, TValue>>();
            var ser = new YamlSerializer.YamlSerializer(typeof(TValue));
            foreach (YamlNode child in Root)
            {
                TKey key = child.Key is TKey k ? k : default!;
                TValue val = (TValue)ser.Deserialize(child);
                items.Add(new KeyValuePair<TKey, TValue>(key, val));
            }
            return items;
        }
    }
}
