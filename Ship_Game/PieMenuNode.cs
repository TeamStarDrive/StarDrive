using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;

namespace Ship_Game
{
	public sealed class PieMenuNode
	{
		public PieMenuNode parent;
        public Array<PieMenuNode> Children { get; set; }
        public SubTexture Icon { get; set; }

        public bool IsLeaf
		{
			get
			{
				if (Children == null)
					return true;
				return Children.Count == 0;
			}
		}

		public Action OnSelect { get; set; }

        public LocalizedText Text { get; set; }

        public PieMenuNode()
		{
		}

		public PieMenuNode(LocalizedText text, SubTexture icon, Action onSelect)
		{
			Text = text;
			Icon = icon;
			OnSelect = onSelect;
		}

		public void Add(PieMenuNode newChild)
		{
			if (Children == null)
			{
				Children = new Array<PieMenuNode>();
			}
			newChild.parent = this;
			Children.Add(newChild);
		}

		public void Select()
        {
            OnSelect?.Invoke();
        }
	}
}