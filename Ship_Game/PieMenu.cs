using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class PieMenu
	{
		private bool visible;

		private PieMenuNode rootNode;

		private float radius = 100f;

		private Transition t;

		private int selectionIndex = -1;

		private Vector2 drawPosition;

		private SimpleDelegate hideDelegate;

		private SimpleDelegate newMenuDelegate;

		private PieMenuNode newMenuNode;

		private float scaleFactor;

		public Vector2 Position
		{
			get
			{
				return this.drawPosition;
			}
			set
			{
				this.drawPosition = value;
			}
		}

		public float Radius
		{
			get
			{
				return this.radius;
			}
			set
			{
				this.radius = value;
			}
		}

		public PieMenuNode RootNode
		{
			get
			{
				return this.rootNode;
			}
			set
			{
				this.rootNode = value;
			}
		}

		public float ScaleFactor
		{
			get
			{
				return this.scaleFactor;
			}
			set
			{
				this.scaleFactor = value;
			}
		}

		public bool Visible
		{
			get
			{
				return this.visible;
			}
			set
			{
				this.visible = value;
			}
		}

		public PieMenu()
		{
			this.t = new Transition(Direction.Ascending, TransitionCurve.Linear, 0.15f);
			this.hideDelegate = new SimpleDelegate(this.OnHide);
			this.newMenuDelegate = new SimpleDelegate(this.NewMenu);
		}

		public void ChangeTo(PieMenuNode newNode)
		{
			if (newNode == null)
			{
				this.t.OnTransitionEnd = this.hideDelegate;
				this.t.Reset(Direction.Descending);
				return;
			}
			this.t.OnTransitionEnd = this.newMenuDelegate;
			this.newMenuNode = newNode;
			this.t.Reset(Direction.Descending);
		}

		protected void ComputeSelected(Vector2 selectionVector)
		{
			this.selectionIndex = -1;
			if (selectionVector.Length() > 3f)
			{
				this.selectionIndex = -2;
				return;
			}
			if (selectionVector.Length() > 1.5f)
			{
				return;
			}
			if (selectionVector.Length() > 0.3f)
			{
				float angleDivision = 1f / (float)this.rootNode.Children.Count;
				float angle = (float)Math.Atan2((double)selectionVector.Y, (double)selectionVector.X);
				if (angle < 0f)
				{
					angle = angle + 6.28318548f;
				}
				angle = angle / 6.28318548f;
				angle = 1f - angle;
				float rotationBegins = 0.75f - angleDivision / 2f;
				if (angle <= rotationBegins)
				{
					angle = angle + 1f;
				}
				angle = angle - rotationBegins;
				this.selectionIndex = 0;
				while ((float)this.selectionIndex * angleDivision < angle)
				{
					PieMenu pieMenu = this;
					pieMenu.selectionIndex = pieMenu.selectionIndex + 1;
				}
				PieMenu pieMenu1 = this;
				pieMenu1.selectionIndex = pieMenu1.selectionIndex - 1;
			}
		}

		public void Draw(SpriteBatch spriteBatch, SpriteFont font)
		{
			if (!this.visible)
			{
				return;
			}
			Vector2 center = this.drawPosition;
			float scale = this.t.CurrentPosition * this.scaleFactor;
			float currentAngle = 1.57079637f;
			float angleIncrement = 6.28318548f / (float)this.rootNode.Children.Count;
			for (int i = 0; i < this.rootNode.Children.Count; i++)
			{
				Vector2 imagePos = center + (scale * this.radius * new Vector2((float)Math.Cos((double)currentAngle), -(float)Math.Sin((double)currentAngle)));
				int imageSize = (int)(scale * 30f);
				Rectangle rectangle = new Rectangle((int)imagePos.X - imageSize, (int)imagePos.Y - imageSize, 2 * imageSize, 2 * imageSize);
				Color drawColor = Color.White;
				if (currentAngle <= 0f)
				{
					currentAngle = currentAngle + 6.28318548f;
				}
				if (i == this.selectionIndex)
				{
					drawColor = Color.Red;
				}
				Rectangle? nullable = null;
				spriteBatch.Draw(this.rootNode.Children[i].Icon, new Vector2(imagePos.X, imagePos.Y), nullable, drawColor, 0f, new Vector2((float)(this.rootNode.Children[i].Icon.Width / 2), (float)(this.rootNode.Children[i].Icon.Height / 2)), scale, SpriteEffects.None, 1f);
				if (i == this.selectionIndex)
				{
					spriteBatch.DrawString(font, this.rootNode.Children[i].Text, imagePos + new Vector2(-font.MeasureString(this.rootNode.Children[i].Text).X / 2f, (float)imageSize), Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
				}
				currentAngle = currentAngle - angleIncrement;
			}
		}

		public bool HandleInput(InputState input, Vector2 selectionVector)
		{
			if (!this.visible)
			{
				return false;
			}
			this.ComputeSelected(selectionVector);
			if (input.InGameSelect)
			{
				if (this.selectionIndex >= 0)
				{
					if (!this.rootNode.Children[this.selectionIndex].IsLeaf)
					{
						this.ChangeTo(this.rootNode.Children[this.selectionIndex]);
						AudioManager.GetCue("sub_bass_whoosh").Play();
					}
					else
					{
						this.rootNode.Children[this.selectionIndex].Select();
						this.ChangeTo(null);
						AudioManager.GetCue("sub_bass_whoosh").Play();
					}
				}
				else if (this.selectionIndex != -2)
				{
					this.ChangeTo(this.rootNode.parent);
					AudioManager.GetCue("sub_bass_whoosh").Play();
				}
				else
				{
					this.ChangeTo(null);
					AudioManager.GetCue("sub_bass_whoosh").Play();
				}
			}
			if (input.MenuCancel)
			{
				this.ChangeTo(null);
				AudioManager.GetCue("sub_bass_whoosh").Play();
			}
			return true;
		}

		public bool HandleInput(InputState input)
		{
			return this.HandleInput(input, input.CurrentGamePadState.ThumbSticks.Left);
		}

		private void NewMenu(object sender)
		{
			this.rootNode = this.newMenuNode;
			this.t.Reset(Direction.Ascending);
			this.t.OnTransitionEnd = null;
		}

		private void OnHide(object sender)
		{
			this.visible = false;
			this.t.OnTransitionEnd = null;
		}

		public void Show(Vector2 position)
		{
			this.t.Reset(Direction.Ascending);
			this.t.OnTransitionEnd = null;
			this.visible = true;
			this.drawPosition = position;
		}

		public void Show(PieMenuNode rootNode, Vector2 position)
		{
			this.rootNode = rootNode;
			this.Show(position);
		}

		public void Update(GameTime gameTime)
		{
			if (!this.visible)
			{
				return;
			}
			this.t.Update(gameTime.ElapsedGameTime.TotalSeconds);
		}
	}
}