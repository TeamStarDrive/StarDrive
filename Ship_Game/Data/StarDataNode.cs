using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ship_Game.Data
{
    // StarDrive Generic Data Node
    public class StarDataNode : IEnumerable<StarDataNode>
    {
        public string Key;
        public object Value;

        public Array<StarDataNode> Items;

        public override string ToString() => SerializedText();
        public bool HasItems => Items != null && Items.Count > 0;
        public int Count => Items?.Count ?? 0;

        public void AddItem(StarDataNode item)
        {
            if (Items == null)
                Items = new Array<StarDataNode>();
            Items.Add(item);
        }

        // finds a direct child element
        public bool FindChild(string key, out StarDataNode found)
        {
            int count = Items.Count;
            StarDataNode[] fast = Items.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
            {
                StarDataNode node = fast[i];
                if (node.Key == key)
                {
                    found = node;
                    return true;
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
            int count = Items.Count;
            StarDataNode[] fast = Items.GetInternalArrayItems();
            for (int i = 0; i < count; ++i)
                if (fast[i].FindChildRecursive(key, out found))
                    return true;
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

        public void SerializeTo(StringBuilder sb, int depth = 0)
        {
            for (int i = 0; i < depth; ++i)
                sb.Append(' ');
            sb.Append(Key).Append(": ").Append(Value).AppendLine();

            if (Items != null)
                foreach (StarDataNode child in Items)
                    child.SerializeTo(sb, depth+2);
        }
    }
}