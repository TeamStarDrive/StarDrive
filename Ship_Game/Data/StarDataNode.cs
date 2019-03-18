using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Ship_Game.Data
{
    // StarDrive data object node with key, value and child items
    public class StarDataNode : IEnumerable<StarDataNode>
    {
        public object Key, Value;
        public Array<StarDataNode> SubNodes; // SubNode tree,  ie. This: { SubNode1: Value1, SubNode2: Value2 }
        public Array<StarDataNode> Sequence; // Sequence list, ie. This: [ Element1, Element2, Element3 ]

        public override string ToString() => SerializedText();

        public bool HasSubNodes => SubNodes != null && SubNodes.NotEmpty;
        public bool HasSequence => Sequence != null && Sequence.NotEmpty;

        public int Count
        {
            get
            {
                if (SubNodes != null) return SubNodes.Count;
                if (Sequence != null) return Sequence.Count;
                return 0;
            }
        }

        // SubNode indexing
        // ThisNode:
        //   SubNode0: Value0
        //   SubNode1: Value1
        // this[0] => StarDataNode Key=SubNode0 Value=Value0
        public StarDataNode this[int subNodeIndex]
        {
            get
            {
                if (SubNodes == null || (uint)subNodeIndex >= SubNodes.Count)
                    ThrowSubNodeIndexOutOfBounds(subNodeIndex);
                return SubNodes[subNodeIndex];
            }
        }

        // Separated throw from this[] to enable MSIL inlining
        void ThrowSubNodeIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException(
                $"StarDataNode Key('{Key}') SUB-NODE Index [{index}] out of range({SubNodes?.Count??0})");
        }

        // SubNode access by name (throws exception if not found)
        // ThisNode:
        //   SubNode0: Value0
        // this["SubNode0"] => StarDataNode Key=SubNode0 Value=Value0
        public StarDataNode this[string subNodeKey] => GetSubNode(subNodeKey);

        // Sequence indexing
        // ThisNode:
        //   - Element0: Value0
        //   - Element1: Value1
        // this.GetElement(0) => StarDataNode Key=Element0 Value=Value0
        public StarDataNode GetElement(int index)
        {
            if (Sequence == null || (uint)index > (uint)Sequence.Count)
                ThrowSequenceIndexOutOfBounds(index);
            return Sequence[index];
        }

        // Separated throw from this[] to enable MSIL inlining
        void ThrowSequenceIndexOutOfBounds(int index)
        {
            throw new IndexOutOfRangeException(
                $"StarDataNode Key('{Key}') SEQUENCE Index [{index}] out of range({Sequence?.Count??0})");
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

        public void AddSubNode(StarDataNode item)
        {
            if (SubNodes == null)
                SubNodes = new Array<StarDataNode>();
            SubNodes.Add(item);
        }

        public void AddSequenceElement(StarDataNode element)
        {
            if (Sequence == null)
                Sequence = new Array<StarDataNode>();
            Sequence.Add(element);
        }

        public StarDataNode GetSubNode(string subNodeKey)
        {
            if (!FindSubNode(subNodeKey, out StarDataNode found))
                throw new KeyNotFoundException($"StarDataNode '{subNodeKey}' not found in node '{Name}'!");
            return found;
        }

        // finds a direct child element
        public bool FindSubNode(string subNodeKey, out StarDataNode found)
        {
            if (SubNodes != null)
            {
                int count = SubNodes.Count;
                StarDataNode[] fast = SubNodes.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    StarDataNode node = fast[i];
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
        public bool FindSubNodeRecursive(string subNodeKey, out StarDataNode found)
        {
            // first check direct children
            if (FindSubNode(subNodeKey, out found))
                return true;

            // then go recursive
            if (SubNodes != null)
            {
                int count = SubNodes.Count;
                StarDataNode[] fast = SubNodes.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                    if (fast[i].FindSubNodeRecursive(subNodeKey, out found))
                        return true;
            }
            return false;
        }
        
        // Safe SubNode enumerator
        public IEnumerator<StarDataNode> GetEnumerator()
        {
            if (SubNodes == null) yield break;
            foreach (StarDataNode node in SubNodes)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        static readonly IReadOnlyList<StarDataNode> EmptyNodes = new ReadOnlyCollection<StarDataNode>(Empty<StarDataNode>.Array);

        // Null-Safe sequence enumerator
        public IReadOnlyList<StarDataNode> SequenceNodes => Sequence ?? EmptyNodes;

        public string SerializedText()
        {
            var sb = new StringBuilder();
            SerializeTo(sb);
            return sb.ToString();
        }

        static bool IsAlphaNumeric(string text)
        {
            foreach (char ch in text)
                if (!char.IsLetterOrDigit(ch))
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
                case float number: return sb.Append(number);
                case string str:
                    if (IsAlphaNumeric(str))
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

        public void SerializeTo(StringBuilder sb, int depth = 0, bool sequenceElement = false)
        {
            for (int i = 0; i < depth; ++i)
                sb.Append(' ');

            if (sequenceElement)
                sb.Append("- ");

            if (Key != null)
            {
                Append(sb, Key).Append(": ");
            }
            Append(sb, Value).AppendLine();

            if (SubNodes != null)
            {
                foreach (StarDataNode child in SubNodes)
                    child.SerializeTo(sb, depth+2);
            }
            if (Sequence != null)
            {
                foreach (StarDataNode element in Sequence)
                {
                    element.SerializeTo(sb, depth+2, sequenceElement: true);
                }
            }
        }
    }
}