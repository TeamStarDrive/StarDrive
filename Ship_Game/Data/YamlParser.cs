using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Ship_Game.Data
{
    // Simplified text parser for StarDrive data files
    public class YamlParser : IDisposable
    {
        TextReader Reader;
        public YamlNode Root { get; }
        int Line;
        readonly StringBuilder StrBuilder = new StringBuilder();

        readonly Array<Error> LoggedErrors = new Array<Error>();
        public IReadOnlyList<Error> Errors => LoggedErrors;

        public YamlParser(string file) : this(ResourceManager.GetModOrVanillaFile(file))
        {
        }

        public YamlParser(FileInfo f) : this(f?.NameNoExt(), OpenStream(f))
        {
        }

        public YamlParser(string name, TextReader reader)
        {
            Reader = reader;
            Root = new YamlNode { Key = name ?? "", Value = null };
            Parse();
        }
        
        static StreamReader OpenStream(FileInfo f, string nameInfo = null)
        {
            if (f == null || !f.Exists)
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
            Reader?.Close(); Reader = null;
        }

        struct DepthSave
        {
            public int Depth;
            public YamlNode Node;
        }
        
        bool ReadLine(out StringView view)
        {
            string line = Reader.ReadLine();
            if (line != null)
            {
                view = new StringView(line);
                return true;
            }
            view = default;
            return false;
        }

        void Parse()
        {
            Line = 0;
            int depth = 0;
            var saved = new Stack<DepthSave>();

            YamlNode root = Root;
            YamlNode prev = Root;

            while (ReadLine(out StringView view))
            {
                Line += 1;
                view.SkipWhiteSpace(out int newDepth);

                // "      " -- line is all spaces
                // "  # comment after spaces "
                if (view.Length == 0 || view.Char0 == '#')
                    continue;

                YamlNode node = ParseLineAsNode(view, out bool isSequence);
                if (node == null)
                    continue;

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
        }

        YamlNode ParseLineAsNode(StringView view, out bool isSequence)
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
                ParseObject(node, ref view, mapSelfToKey: true);
                return node;
            }

            // parse first token as a value
            object firstValue = ParseValue(first, ref view);

            // see if we can get another token?
            StringView separator = NextToken(ref view);
            if (separator.Length == 0) // no token, so only value!
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
                ParseObject(node, ref view, mapSelfToKey: false);
                return node;
            }
            
            // parse second token as a value
            node.Value = ParseValue(second, ref view);
            return node;
        }

        object ParseValue(StringView token, ref StringView view)
        {
            if (token.Length == 0) return null;
            if (token == "null")   return null;
            if (token == "true")   return true;
            if (token == "false")  return false;
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
        
        void ParseObject(YamlNode node, ref StringView view, bool mapSelfToKey)
        {
            for (;;)
            {
                view.TrimStart();
                if (view.Length == 0)
                {
                    LogError(view, "Parse Error: map expected '}' before end of line");
                    break;
                }

                if (view.Char0 == '}')
                    break; // end of map

                // In the case of:
                // - { X: 0, Y: 1 }
                // We map self to a key-value object:
                // X: 0
                //   Y: 1
                if (mapSelfToKey)
                {
                    mapSelfToKey = false;
                    ParseTokenAsNode(node, ref view);
                }
                else
                {
                    var child = new YamlNode();
                    ParseTokenAsNode(child, ref view);
                    node.AddSubNode(child);
                }

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
            var arrayItems = new Array<object>();

            for (;;)
            {
                StringView token = NextToken(ref view);
                if (token.Length == 0)
                {
                    LogError(token, "Parse Error: array expected ']' before end of line");
                    break;
                }
                
                if (token.Char0 == ']')
                    break; // end of array

                object o = ParseValue(token, ref view);
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
            StrBuilder.Clear();
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
                            case '\\': StrBuilder.Append('\\'); break;
                            case 'r':  StrBuilder.Append('\r'); break;
                            case 'n':  StrBuilder.Append('\n'); break;
                            case 't':  StrBuilder.Append('\t'); break;
                            case '\'': StrBuilder.Append('\''); break;
                            case '"':  StrBuilder.Append('"');  break;
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
                    StrBuilder.Append(c);
                }
            }
            return StrBuilder.ToString();
        }

        static StringView NextToken(ref StringView view)
        {
            view.TrimStart();

            int start = view.Start;
            int current = start;
            int eos = start + view.Length;
            string str = view.Str;
            while (current < eos)
            {
                switch (str[current])
                {
                    case ':': case '#': case '\'': case '"': case ',':
                    case '{': case '}':  case '[': case ']':
                        if (start == current)
                        {
                            view.Skip(1);
                            return new StringView(start, 1, str);
                        }
                        goto finished;
                }
                ++current;
            }

            finished:
            int length = current - start;
            view.Skip(length);
            return new StringView(start, length, str);
        }

        public struct Error
        {
            public int Line;
            public int Column;
            public string Message;
            public string Document;

            public override string ToString()
            {
                return $"{Document}:{Line}:{Column} {Message}";
            }
        }

        void LogError(in StringView view, string what)
        {
            var e = new Error{ Line=Line, Column=view.Start, Message=what, Document=Root.Name};
            Log.Error(e.ToString());
            LoggedErrors.Add(e);
        }

        public Array<T> DeserializeArray<T>() where T : new()
        {
            var items = new Array<T>();
            var ser = new StarDataSerializer(typeof(T));
            foreach (YamlNode child in Root)
            {
                items.Add((T)ser.Deserialize(child));
            }
            return items;
        }

        public T DeserializeOne<T>() where T : new()
        {
            var ser = new StarDataSerializer(typeof(T));
            foreach (YamlNode child in Root)
            {
                return (T)ser.Deserialize(child);
            }
            return default;
        }
    }
}
