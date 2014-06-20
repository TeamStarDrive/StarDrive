using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class PieMenuNode
	{
		public PieMenuNode parent;

		private List<PieMenuNode> children;

		private Texture2D icon;

		private string text;

		private SimpleDelegate onSelect;

		public List<PieMenuNode> Children
		{
			get
			{
				return this.children;
			}
			set
			{
				this.children = value;
			}
		}

		public Texture2D Icon
		{
			get
			{
				return this.icon;
			}
			set
			{
				this.icon = value;
			}
		}

		public bool IsLeaf
		{
			get
			{
				if (this.children == null)
				{
					return true;
				}
				return this.children.Count == 0;
			}
		}

		public SimpleDelegate OnSelect
		{
			get
			{
				return this.onSelect;
			}
			set
			{
				this.onSelect = value;
			}
		}

		public string Text
		{
			get
			{
				return this.text;
			}
			set
			{
				this.text = value;
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
			if (this.children == null)
			{
				this.children = new List<PieMenuNode>();
			}
			newChild.parent = this;
			this.children.Add(newChild);
		}

		public void Select()
		{
			if (this.OnSelect != null)
			{
				this.OnSelect(this);
			}
		}
	}
}