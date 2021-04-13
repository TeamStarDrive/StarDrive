using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace Ship_Game.Data.Yaml
{
    // StarDrive data object node with key, value and child items
    public class YamlNode : IEnumerable<YamlNode>
    {
        static readonly IReadOnlyList<YamlNode> EmptyNodes = new ReadOnlyCollection<YamlNode>(Empty<YamlNode>.Array);

        public object Key, Value;

        // SubNode tree,  ie. This: { SubNode1: Value1, SubNode2: Value2 }
        public IReadOnlyList<YamlNode> Nodes => SubNodes ?? EmptyNodes;
        Array<YamlNode> SubNodes;

        // Sequence list, ie. This: [ Element1, Element2, Element3 ]
        public IReadOnlyList<YamlNode> Sequence => SeqNodes ?? EmptyNodes;
        Array<YamlNode> SeqNodes;

        public override string ToString() => SerializedText();

        public bool HasSubNodes => SubNodes != null && SubNodes.NotEmpty;
        public bool HasSequence => SeqNodes != null && SeqNodes.NotEmpty;
        public int Count => SubNodes?.Count ?? SeqNodes?.Count ?? 0;

        // Prefers sequence, but if it's empty,
        // chooses subnodes instead
        public Array<YamlNode> SequenceOrSubNodes => SeqNodes ?? SubNodes;

        // SubNode or SeqNode indexing (depends which one is available)
        // ThisNode:
        //   SubNode0: Value0
        //   SubNode1: Value1
        // this[0] => YamlNode Key=SubNode0 Value=Value0
        // | or |
        // ThisNode:
        //   - SeqNode0: Value0
        //   - SeqNode1: Value1
        // this[0] => YamlNode Key=SeqNode0 Value=Value0
        public YamlNode this[int subNodeIndex]
        {
            get
            {
                var container = SubNodes ?? SeqNodes;
                if (container == null || (uint)subNodeIndex >= SubNodes.Count)
                {
                    ThrowSubNodeIndexOutOfBounds(subNodeIndex);
                }
                return container[subNodeIndex];
            }
        }

        // Separated throw from this[] to enable MSIL inlining
        void ThrowSubNodeIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException(
                $"YamlNode Key('{Key}') SUB-NODE Index [{index}] out of range({(SubNodes ?? SeqNodes)?.Count??0})");
        }

        // SubNode access by name (throws exception if not found)
        // ThisNode:
        //   SubNode0: Value0
        // this["SubNode0"] => YamlNode Key=SubNode0 Value=Value0
        public YamlNode this[string subNodeKey] => GetSubNode(subNodeKey);

        // Sequence indexing
        // ThisNode:
        //   - Element0: Value0
        //   - Element1: Value1
        // this.GetElement(0) => YamlNode Key=Element0 Value=Value0
        public YamlNode GetElement(int index)
        {
            if (SeqNodes == null || (uint)index > (uint)SeqNodes.Count)
                ThrowSequenceIndexOutOfBounds(index);
            return SeqNodes[index];
        }

        // Separated throw from this[] to enable MSIL inlining
        void ThrowSequenceIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException(
                $"YamlNode Key('{Key}') SEQUENCE Index [{index}] out of range({SeqNodes?.Count??0})");
        }

        // Name: ...
        public string Name => Key as string;
        
        // Key: ValueText
        public string ValueText => Value as string;

        // Key: true
        public bool ValueBool => Value is bool b && b;

        // Key: 1234
        public int ValueInt => Value is int i ? i : 0;

        // Key: 33.14
        public float ValueFloat => Value is float f ? f : float.NaN;

        // Key: [Elem1, Elem2, Elem3]
        public object[] ValueArray => Value as object[];

        public void AddSubNode(YamlNode item)
        {
            if (SubNodes == null)
                SubNodes = new Array<YamlNode>();
            SubNodes.Add(item);
        }

        public void AddSequenceElement(YamlNode element)
        {
            if (SeqNodes == null)
                SeqNodes = new Array<YamlNode>();
            SeqNodes.Add(element);
        }

        public YamlNode GetSubNode(string subNodeKey)
        {
            if (!FindSubNode(subNodeKey, out YamlNode found))
                throw new KeyNotFoundException($"YamlNode '{subNodeKey}' not found in node '{Name}'!");
            return found;
        }

        // finds a direct child element
        public bool FindSubNode(string subNodeKey, out YamlNode found)
        {
            if (SubNodes != null)
            {
                int count = SubNodes.Count;
                YamlNode[] fast = SubNodes.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    YamlNode node = fast[i];
                    if (node.Name == subNodeKey)
                    {
                        found = node;
                        return true;
                    }
                }
            }
            found = null;
            return false;
        }

        // recursively searches child elements
        // this can be quite slow if Node tree has hundreds of elements
        public bool FindSubNodeRecursive(string subNodeKey, out YamlNode found)
        {
            // first check direct children
            if (FindSubNode(subNodeKey, out found))
                return true;

            // then go recursive
            if (SubNodes != null)
            {
                int count = SubNodes.Count;
                YamlNode[] fast = SubNodes.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                    if (fast[i].FindSubNodeRecursive(subNodeKey, out found))
                        return true;
            }
            return false;
        }
        
        // Safe SubNode enumerator
        public IEnumerator<YamlNode> GetEnumerator()
        {
            var nodes = SequenceOrSubNodes;
            if (nodes != null)
            {
                for (int i = 0; i < nodes.Count; ++i)
                    yield return nodes[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string SerializedText()
        {
            var sb = new StringBuilder();
            SerializeTo(sb);
            return sb.ToString();
        }

        public bool IsLeafNode => Count == 0;

        public bool NodesAreLeafNodes
        {
            get
            {
                if (SubNodes == null) return true;
                foreach (YamlNode node in SubNodes)
                    if (node.Count > 0) return false;
                return true;
            }
        }

        // @return TRUE if string escape quotes are not needed
        static bool EscapeNotNeeded(string text)
        {
            foreach (char ch in text)
                if (!char.IsLetterOrDigit(ch) && ch != '_')
                    return false;
            return true;
        }

        static StringBuilder EscapeString(StringBuilder sb, string text)
        {
            sb.Append('"');
            foreach (char ch in text)
            {
                switch (ch)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\r': sb.Append("\\r");  break;
                    case '\n': sb.Append("\\n");  break;
                    case '\t': sb.Append("\\t");  break;
                    case '\'': sb.Append("\\'");  break;
                    case '"':  sb.Append("\\\""); break;
                    default:   sb.Append(ch);     break;
                }
            }
            return sb.Append('"');
        }

        static StringBuilder Append(StringBuilder sb, object o)
        {
            switch (o)
            {
                case bool boolean: return sb.Append(boolean ? "true" : "false");
                case int integer:  return sb.Append(integer);
                case float number: return sb.Append(number.ToString(CultureInfo.InvariantCulture));
                case string str:
                    if (EscapeNotNeeded(str))
                        return sb.Append(str);
                    return EscapeString(sb, str);
                case object[] arr:
                    sb.Append('[');
                    for (int i = 0; i < arr.Length; ++i)
                    {
                        Append(sb, arr[i]);
                        if (i != arr.Length-1)
                            sb.Append(", ");
                    }
                    return sb.Append(']');
                //case null:
                //    return sb.Append("null");
            }
            return sb;
        }

        static void AppendSpaces(StringBuilder sb, int numSpaces)
        {
            for (; numSpaces >= 4; numSpaces -= 4) sb.Append("    ");
            for (; numSpaces >= 2; numSpaces -= 2) sb.Append("  ");
            for (; numSpaces > 0; --numSpaces) sb.Append(' ');
        }

        public void SerializeTo(StringBuilder sb, int depth = 0, bool sequenceElement = false)
        {
            AppendSpaces(sb, depth);

            if (sequenceElement)
                sb.Append("- ");

            if (Key != null)
            {
                Append(sb, Key).Append(": ");
            }
            if (Value != null)
            {
                Append(sb, Value);
            }

            if (SubNodes != null)
            {
                if (Value == null && SubNodes.Count <= 2 && NodesAreLeafNodes)
                {
                    sb.Append("{ ");
                    for (int i = 0; i < SubNodes.Count; ++i)
                    {
                        YamlNode node = SubNodes[i];
                        Append(sb, node.Key).Append(": ");
                        Append(sb, node.Value);
                        if (i != SubNodes.Count-1)
                            sb.Append(',');
                    }
                    sb.AppendLine(" }");
                }
                else
                {
                    sb.AppendLine();
                    foreach (YamlNode child in SubNodes)
                        child.SerializeTo(sb, depth+2);
                }
            }
            else if (SeqNodes != null)
            {
                sb.AppendLine();
                foreach (YamlNode element in SeqNodes)
                {
                    element.SerializeTo(sb, depth+2, sequenceElement: true);
                }
            }
            else
            {
                sb.AppendLine();
            }
        }
    }
}