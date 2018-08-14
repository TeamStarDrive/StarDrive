using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class PieMenuNode
	{
		public PieMenuNode parent;

		private Array<PieMenuNode> children;

		private Texture2D icon;

		private string text;

		private SimpleDelegate onSelect;

		public Array<PieMenuNode> Children
		{
			get
			{
				return children;
			}
			set
			{
				children = value;
			}
		}

		public Texture2D Icon
		{
			get
			{
				return icon;
			}
			set
			{
				icon = value;
			}
		}

		public bool IsLeaf
		{
			get
			{
				if (children == null)
				{
					return true;
				}
				return children.Count == 0;
			}
		}

		public SimpleDelegate OnSelect
		{
			get
			{
				return onSelect;
			}
			set
			{
				onSelect = value;
			}
		}

		public string Text
		{
			get
			{
				return text;
			}
			set
			{
				text = value;
			}
		}

		public PieMenuNode()
		{
		}

		public PieMenuNode(string text, Texture2D icon, SimpleDelegate onSelect)
		{
			this.text = text;
			this.icon = icon;
			this.onSelect = onSelect;
		}

		public void Add(PieMenuNode newChild)
		{
			if (children == null)
			{
				children = new Array<PieMenuNode>();
			}
			newChild.parent = this;
			children.Add(newChild);
		}

		public void Select()
		{
			if (OnSelect != null)
			{
				OnSelect(this);
			}
		}
	}
}