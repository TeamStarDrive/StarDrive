using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using SDUtils;

namespace Ship_Game.Data.Yaml;

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
        var sw = new StringWriter();
        SerializeTo(sw);
        return sw.ToString();
    }

    public bool IsLeafNode => Count == 0;

    public bool NodesAreLeafNodes
    {
        get
        {
            if (SubNodes == null) return true;
            foreach (YamlNode node in SubNodes)
                if (node.Count > 0 || node.ValueArray != null)
                    return false;
            return true;
        }
    }

    public bool SequenceIsPrimitives
    {
        get
        {
            if (SeqNodes == null) return false;
            foreach (YamlNode node in SeqNodes)
                if (node.Count > 0) return false;
            return true;
        }
    }

    // @return TRUE if string escape quotes are not needed
    static bool EscapeNotNeeded(string text)
    {
        foreach (char ch in text)
            if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '.')
                return false;
        return true;
    }

    public static TextWriter EscapeString(TextWriter tw, string text)
    {
        tw.Write('"');
        foreach (char ch in text)
        {
            switch (ch)
            {
                case '\\': tw.Write("\\\\"); break;
                case '\r': tw.Write("\\r");  break;
                case '\n': tw.Write("\\n");  break;
                case '\t': tw.Write("\\t");  break;
                case '\'': tw.Write("\\'");  break;
                case '"':  tw.Write("\\\""); break;
                default:   tw.Write(ch);     break;
            }
        }
        tw.Write('"');
        return tw;
    }

    static TextWriter Write(TextWriter tw, object o)
    {
        switch (o)
        {
            case bool boolean: tw.Write(boolean ? "true" : "false"); break;
            case int integer:  tw.Write(integer); break;
            case float number: tw.Write(number.ToString(CultureInfo.InvariantCulture)); break;
            case double number: tw.Write(number.ToString(CultureInfo.InvariantCulture)); break;
            case string str:
                if (EscapeNotNeeded(str))
                {
                    tw.Write(str);
                    break;
                }
                EscapeString(tw, str);
                break;
            case object[] arr:
                tw.Write('[');
                for (int i = 0; i < arr.Length;)
                {
                    Write(tw, arr[i]);
                    if (++i != arr.Length)
                        tw.Write(',');
                }
                tw.Write(']');
                break;
            //case null:
            //    return sb.Append("null");
        }
        return tw;
    }

    static void AppendSpaces(TextWriter tw, int numSpaces)
    {
        for (; numSpaces >= 4; numSpaces -= 4) tw.Write("    ");
        for (; numSpaces >= 2; numSpaces -= 2) tw.Write("  ");
        for (; numSpaces > 0; --numSpaces) tw.Write(' ');
    }

    public void SerializeTo(TextWriter tw, int depth = 0, 
        bool sequenceElement = false,
        bool noSpacePrefix = false)
    {
        if (noSpacePrefix == false)
            AppendSpaces(tw, depth);

        if (SubNodes != null)
        {
            // short form syntax "{ Key:House, Value:1.1 }\n"
            if (Value == null && SubNodes.Count <= 3 && NodesAreLeafNodes)
            {
                if (sequenceElement)
                {
                    // for sequence elements we can't write the key
                    tw.Write("- { ");
                }
                else
                {
                    // for regular Key: Value syntax:
                    // "Key: { "
                    Write(tw, Key).Write(": { ");
                }

                if (Value != null)
                {
                    Log.Warning($"YamlNode.SerializeTo: cannot write Value for a SubNode: Key={Key} Value={Value}");
                }

                for (int i = 0; i < SubNodes.Count; ++i)
                {
                    YamlNode node = SubNodes[i];
                    Write(tw, node.Key).Write(':');
                    Write(tw, node.Value);
                    if (i != SubNodes.Count-1)
                        tw.Write(", ");
                }
                tw.Write(" }\n");
            }
            else
            {
                // long form object syntax
                if (sequenceElement)
                {
                    // for sequence elements, always write a Key
                    // "- ElementType:\n"
                    tw.Write("- ");
                    Write(tw, Key ?? "Item").Write(":\n");
                }
                else
                {
                    // regular sub elements:
                    // "Key:\n"
                    Write(tw, Key ?? "Item").Write(":\n");
                }

                if (Value != null)
                {
                    Log.Warning($"YamlNode.SerializeTo: cannot write Value for a SubNode: Key={Key} Value={Value}");
                }

                for (int i = 0; i < SubNodes.Count; ++i)
                {
                    YamlNode node = SubNodes[i];
                    node.SerializeTo(tw, depth+2);
                }
            }
        }
        else if (SeqNodes != null) // sequence [ 1, 2, ..., 4 ]
        {
            // primitives, use short form in one line [ ... ]\n
            if (Value == null && SequenceIsPrimitives)
            {
                if (sequenceElement)
                {
                    // it's a sequence of sequences:
                    // - [ 1, 2, ..., 4 ]
                    tw.Write("- ");
                }
                else
                {
                    // it's a Key: [ ... ]
                    // 
                    Write(tw, Key).Write(": [ ");
                }

                for (int i = 0; i < SeqNodes.Count;)
                {
                    Write(tw, SeqNodes[i].Value);
                    if (++i != SeqNodes.Count)
                        tw.Write(',');
                }
                tw.Write("]\n");
            }
            // complex sequence elements
            else
            {
                if (sequenceElement)
                {
                    // it's a sequence of sequences, so it always needs a key
                    // "- Item:\n"
                    // "  - sub1\n"
                    // "  - sub2\n"
                    tw.Write("- ");
                    Write(tw, Key ?? "Item").Write(":\n");
                }
                else
                {
                    // just a regular named sequence:
                    // "Item:\n"
                    // "  - sub1\n"
                    // "  - sub2\n"
                    Write(tw, Key ?? "Item").Write(":\n");
                }
                foreach (YamlNode element in SeqNodes)
                {
                    element.SerializeTo(tw, depth+2, sequenceElement: true);
                }
            }
        }
        // regular primitive Key: Value?
        else
        {
            if (sequenceElement)
            {
                tw.Write("- ");
            }

            Write(tw, Key).Write(':');
            if (Value != null)
            {
                tw.Write(' ');
                Write(tw, Value);
            }
            tw.Write('\n');
        }
    }
}
