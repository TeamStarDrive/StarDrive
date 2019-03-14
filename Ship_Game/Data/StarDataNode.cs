using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ship_Game.Data
{
    // StarDrive data object node with key, value and child items
    public class StarDataNode : IEnumerable<StarDataNode>
    {
        public object Key;
        public object Value;
        public Array<StarDataNode> Items;

        public override string ToString() => SerializedText();

        public bool HasItems => Items != null && Items.Count > 0;
        public int Count => Items?.Count ?? 0;
        public StarDataNode this[int index] => Items[index];

        public string Name => Key as string;
        public string ValueText => Value as string;
        public object[] ValueArray => Value as object[];
        public int ValueInt => (int)Value;
        public float ValueFloat => (float)Value;

        public void AddItem(StarDataNode item)
        {
            if (Items == null)
                Items = new Array<StarDataNode>();
            Items.Add(item);
        }

        public StarDataNode GetChild(string key)
        {
            if (!FindChild(key, out StarDataNode found))
                throw new KeyNotFoundException($"StarDataNode {key} not found!");
            return found;
        }

        // finds a direct child element
        public bool FindChild(string key, out StarDataNode found)
        {
            if (Items != null)
            {
                int count = Items.Count;
                StarDataNode[] fast = Items.GetInternalArrayItems();
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
        public bool FindChildRecursive(string key, out StarDataNode found)
        {
            // first check direct children
            if (FindChild(key, out found))
                return true;

            // then go recursive
            if (Items != null)
            {
                int count = Items.Count;
                StarDataNode[] fast = Items.GetInternalArrayItems();
                for (int i = 0; i < count; ++i)
                    if (fast[i].FindChildRecursive(key, out found))
                        return true;
            }
            return false;
        }

        public IEnumerator<StarDataNode> GetEnumerator()
        {
            if (Items == null) yield break;
            foreach (StarDataNode node in Items)
                yield return node;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

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

            if (Items != null)
                foreach (StarDataNode child in Items)
                    child.SerializeTo(sb, depth+2);
        }
    }
}