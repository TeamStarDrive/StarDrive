using System.Collections;
using System.Collections.Generic;
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
        public StarDataNode this[int index] => SubNodes[index];

        // Sequence indexing
        public StarDataNode GetElement(int index) => Sequence[index];

        public string Name => Key as string;
        public string ValueText => Value as string;
        public object[] ValueArray => Value as object[];
        public int   ValueInt   => (int)Value;
        public float ValueFloat => (float)Value;

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

        public StarDataNode GetSubNode(string key)
        {
            if (!FindSubNode(key, out StarDataNode found))
                throw new KeyNotFoundException($"StarDataNode {key} not found!");
            return found;
        }

        // finds a direct child element
        public bool FindSubNode(string key, out StarDataNode found)
        {
            if (SubNodes != null)
            {
                int count = SubNodes.Count;
                StarDataNode[] fast = SubNodes.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                {
                    StarDataNode node = fast[i];
                    if (node.Name == key)
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
        public bool FindSubNodeRecursive(string key, out StarDataNode found)
        {
            // first check direct children
            if (FindSubNode(key, out found))
                return true;

            // then go recursive
            if (SubNodes != null)
            {
                int count = SubNodes.Count;
                StarDataNode[] fast = SubNodes.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                    if (fast[i].FindSubNodeRecursive(key, out found))
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

        // Safe sequence enumerator
        public IEnumerator<object> GetSequence()
        {
            if (Sequence == null) yield break;
            foreach (object value in Sequence)
                yield return value;
        }

        public string SerializedText()
        {
            var sb = new StringBuilder();
            SerializeTo(sb);
            return sb.ToString();
        }

        static StringBuilder Append(StringBuilder sb, object o)
        {
            switch (o)
            {
                case string str:   return sb.Append('"').Append(str).Append('"');
                case int integer:  return sb.Append(integer);
                case float number: return sb.Append(number);
                case object[] arr:
                    sb.Append('[');
                    for (int i = 0; i < arr.Length; ++i)
                    {
                        Append(sb, arr[i]);
                        if (i != arr.Length-1)
                            sb.Append(", ");
                    }
                    sb.Append(']');
                    break;
            }
            return sb;
        }

        public void SerializeTo(StringBuilder sb, int depth = 0)
        {
            for (int i = 0; i < depth; ++i)
                sb.Append(' ');

            Append(sb, Key).Append(": ");
            Append(sb, Value).AppendLine();

            if (SubNodes != null)
                foreach (StarDataNode child in SubNodes)
                    child.SerializeTo(sb, depth+2);
        }
    }
}