using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ScrollList : IDisposable
	{
		private Submenu Parent;

		public Rectangle ScrollUp;

		public Rectangle ScrollDown;

		public Rectangle ScrollBarHousing;

		public Rectangle ScrollBar;

		private int entryHeight = 40;

		private int ScrollBarHover;

		//private int upScrollHover;

		//private int downScrollHover;

		private int startDragPos;

		private bool dragging;

		private float ScrollBarStartDragPos;

		private float ClickTimer;

		private float TimerDelay = 0.05f;

		//private float ScrollBarPercent;

		public int entriesToDisplay;

		public int indexAtTop;

		public BatchRemovalCollection<ScrollList.Entry> Entries = new BatchRemovalCollection<ScrollList.Entry>();

		public bool IsDraggable;

		public ScrollList.Entry DraggedEntry;

		public BatchRemovalCollection<ScrollList.Entry> Copied = new BatchRemovalCollection<ScrollList.Entry>();

		public Rectangle DraggableArea = new Rectangle();

		private Vector2 DraggedOffset = new Vector2();

        //adding for thread safe Dispose because class uses unmanaged resources 
        private bool disposed;


		public ScrollList(Submenu p)
		{
			this.Parent = p;
			this.entriesToDisplay = (p.Menu.Height - 25) / 40;
			this.ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 30, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Height);
			this.ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Height);
			this.ScrollBarHousing = new Rectangle(this.ScrollUp.X + 1, this.ScrollUp.Y + this.ScrollUp.Height + 3, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, this.ScrollDown.Y - this.ScrollUp.Y - this.ScrollUp.Height - 6);
			this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, 0);
		}

		public ScrollList(Submenu p, int eHeight)
		{
			this.entryHeight = eHeight;
			this.Parent = p;
			this.entriesToDisplay = (p.Menu.Height - 25) / this.entryHeight;
			this.ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 30, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Height);
			this.ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Height);
			this.ScrollBarHousing = new Rectangle(this.ScrollUp.X + 1, this.ScrollUp.Y + this.ScrollUp.Height + 3, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, this.ScrollDown.Y - this.ScrollUp.Y - this.ScrollUp.Height - 6);
			this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, 0);
		}

		public ScrollList(Submenu p, int eHeight, bool RealRect)
		{
			this.entryHeight = eHeight;
			this.Parent = p;
			this.entriesToDisplay = p.Menu.Height / this.entryHeight;
			this.ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 5, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Height);
			this.ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Height);
			this.ScrollBarHousing = new Rectangle(this.ScrollUp.X + 1, this.ScrollUp.Y + this.ScrollUp.Height + 3, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, this.ScrollDown.Y - this.ScrollUp.Y - this.ScrollUp.Height - 6);
			this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, 0);
		}

		public ScrollList.Entry AddItem(object o)
		{
			ScrollList.Entry e = new ScrollList.Entry()
			{
				item = o
			};
			this.Entries.Add(e);
			this.Update();
			e.ParentList = this;
			return e;
		}

		public void AddItem(object o, int addrect, int addpencil)
		{
			ScrollList.Entry e = new ScrollList.Entry()
			{
				item = o
			};
			if (addrect > 0)
			{
				e.Plus = 1;
			}
			if (addpencil > 0)
			{
				e.editRect = new Rectangle();
				e.Edit = 1;
			}
			e.addRect = new Rectangle();
			this.Entries.Add(e);
			this.Update();
			e.ParentList = this;
		}

		public void AddQItem(object o)
		{
			ScrollList.Entry e = new ScrollList.Entry()
			{
				item = o,
				Plus = 0,
				up = new Rectangle(),
				down = new Rectangle(),
				apply = new Rectangle(),
				cancel = new Rectangle(),
				clickRect = new Rectangle(),
				QItem = 1
			};
			this.Entries.Add(e);
			this.Update();
			e.ParentList = this;
		}

		public void Draw(SpriteBatch spriteBatch)
		{
			if (this.Copied.Count > this.entriesToDisplay)
			{
				float count = (float)this.entriesToDisplay / (float)this.Copied.Count;
				float single = (float)this.indexAtTop / (float)this.Copied.Count;
				int updownsize = (this.ScrollBar.Height - ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Height) / 2;
				Rectangle up = new Rectangle(this.ScrollBar.X, this.ScrollBar.Y, this.ScrollBar.Width, updownsize);
				Rectangle mid = new Rectangle(this.ScrollBar.X, this.ScrollBar.Y + updownsize, this.ScrollBar.Width, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Height);
				Rectangle bot = new Rectangle(this.ScrollBar.X, mid.Y + mid.Height, this.ScrollBar.Width, updownsize);
				if (this.ScrollBarHover == 0)
				{
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown"], up, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"], mid, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown"], bot, Color.White);
				}
				else if (this.ScrollBarHover == 1)
				{
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover1"], up, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_mid_hover1"], mid, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover1"], bot, Color.White);
				}
				else if (this.ScrollBarHover == 2)
				{
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover2"], up, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_mid_hover2"], mid, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["NewUI/scrollbar_bar_updown_hover2"], bot, Color.White);
				}
				float x = (float)Mouse.GetState().X;
				MouseState state = Mouse.GetState();
				Vector2 mousepos = new Vector2(x, (float)state.Y);
				spriteBatch.Draw((HelperFunctions.CheckIntersection(this.ScrollUp, mousepos) ? ResourceManager.TextureDict["NewUI/scrollbar_arrow_up_hover1"] : ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"]), this.ScrollUp, Color.White);
				spriteBatch.Draw((HelperFunctions.CheckIntersection(this.ScrollDown, mousepos) ? ResourceManager.TextureDict["NewUI/scrollbar_arrow_down_hover1"] : ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"]), this.ScrollDown, Color.White);
			}
			if (this.DraggedEntry != null && this.DraggedEntry.item is QueueItem)
			{
				float x1 = (float)Mouse.GetState().X;
				MouseState mouseState = Mouse.GetState();
				Vector2 MousePos = new Vector2(x1, (float)mouseState.Y);
				if ((this.DraggedEntry.item as QueueItem).isBuilding)
				{
					Vector2 bCursor = MousePos + this.DraggedOffset;
					spriteBatch.Draw(ResourceManager.TextureDict[string.Concat("Buildings/icon_", (this.DraggedEntry.item as QueueItem).Building.Icon, "_48x48")], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					spriteBatch.DrawString(Fonts.Arial12Bold, (this.DraggedEntry.item as QueueItem).Building.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = (this.DraggedEntry.item as QueueItem).Cost,
						Progress = (this.DraggedEntry.item as QueueItem).productionTowards
					};
					pb.Draw(spriteBatch);
				}
				if ((this.DraggedEntry.item as QueueItem).isShip)
				{
					Vector2 bCursor = MousePos + this.DraggedOffset;
					spriteBatch.Draw(ResourceManager.TextureDict[ResourceManager.HullsDict[(this.DraggedEntry.item as QueueItem).sData.Hull].IconPath], new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30), Color.White);
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					spriteBatch.DrawString(Fonts.Arial12Bold, (this.DraggedEntry.item as QueueItem).sData.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = (this.DraggedEntry.item as QueueItem).Cost,
						Progress = (this.DraggedEntry.item as QueueItem).productionTowards
					};
					pb.Draw(spriteBatch);
				}
				if ((this.DraggedEntry.item as QueueItem).isTroop)
				{
					Vector2 bCursor = MousePos + this.DraggedOffset;
					(this.DraggedEntry.item as QueueItem).troop.Draw(spriteBatch, new Rectangle((int)bCursor.X, (int)bCursor.Y, 29, 30));
					Vector2 tCursor = new Vector2(bCursor.X + 40f, bCursor.Y);
					spriteBatch.DrawString(Fonts.Arial12Bold, (this.DraggedEntry.item as QueueItem).troop.Name, tCursor, Color.White);
					tCursor.Y = tCursor.Y + (float)Fonts.Arial12Bold.LineSpacing;
					Rectangle pbRect = new Rectangle((int)tCursor.X, (int)tCursor.Y, 150, 18);
					ProgressBar pb = new ProgressBar(pbRect)
					{
						Max = (this.DraggedEntry.item as QueueItem).Cost,
						Progress = (this.DraggedEntry.item as QueueItem).productionTowards
					};
					pb.Draw(spriteBatch);
				}
			}
		}

		public void DrawBlue(SpriteBatch spriteBatch)
		{
			if (this.Copied.Count > this.entriesToDisplay)
			{
				float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
				float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
				this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
				int updownsize = (this.ScrollBar.Height - ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"].Height) / 2;
				Rectangle up = new Rectangle(this.ScrollBar.X, this.ScrollBar.Y, this.ScrollBar.Width, updownsize);
				Rectangle mid = new Rectangle(this.ScrollBar.X, this.ScrollBar.Y + updownsize, this.ScrollBar.Width, ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"].Height);
				Rectangle bot = new Rectangle(this.ScrollBar.X, mid.Y + mid.Height, this.ScrollBar.Width, updownsize);
				if (this.ScrollBarHover == 0)
				{
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown"], up, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid"], mid, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown"], bot, Color.White);
				}
				else if (this.ScrollBarHover == 1)
				{
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover1"], up, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid_hover1"], mid, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover1"], bot, Color.White);
				}
				else if (this.ScrollBarHover == 2)
				{
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover2"], up, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_mid_hover2"], mid, Color.White);
					spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_bar_updown_hover2"], bot, Color.White);
				}
				spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_arrow_up"], this.ScrollUp, Color.White);
				spriteBatch.Draw(ResourceManager.TextureDict["ResearchMenu/scrollbar_arrow_down"], this.ScrollDown, Color.White);
			}
		}

		public bool HandleInput(InputState input)
		{
			bool hit = false;
			if (HelperFunctions.CheckIntersection(this.ScrollUp, input.CursorPosition) && input.InGameSelect)
			{
				if (this.indexAtTop > 0)
				{
					ScrollList scrollList = this;
					scrollList.indexAtTop = scrollList.indexAtTop - 1;
				}
				hit = true;
				float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
				float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
				this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
			}
			if (HelperFunctions.CheckIntersection(this.ScrollDown, input.CursorPosition) && input.InGameSelect)
			{
				if (this.indexAtTop + this.entriesToDisplay < this.Copied.Count)
				{
					ScrollList scrollList1 = this;
					scrollList1.indexAtTop = scrollList1.indexAtTop + 1;
				}
				float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
				float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
				this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
				hit = true;
			}
			if (HelperFunctions.CheckIntersection(this.ScrollBarHousing, input.CursorPosition))
			{
				this.ScrollBarHover = 1;
				//this.upScrollHover = 1;
				//this.downScrollHover = 1;
				if (HelperFunctions.CheckIntersection(this.ScrollBar, input.CursorPosition))
				{
					this.ScrollBarHover = 2;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
					{
						this.startDragPos = (int)input.CursorPosition.Y;
						this.ScrollBarStartDragPos = (float)this.ScrollBar.Y;
						this.dragging = true;
						hit = true;
					}
				}
			}
			else if (!this.dragging)
			{
				this.ScrollBarHover = 0;
				//this.upScrollHover = 0;
				//this.downScrollHover = 0;
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed && this.dragging)
			{
				float difference = input.CursorPosition.Y - (float)this.startDragPos;
				float count = 1f / (float)this.Copied.Count;
				if (Math.Abs(difference) > 0f)
				{
					this.ScrollBar.Y = (int)(this.ScrollBarStartDragPos + difference);
					if (this.ScrollBar.Y < this.ScrollBarHousing.Y)
					{
						this.ScrollBar.Y = this.ScrollBarHousing.Y;
					}
					else if (this.ScrollBar.Y + this.ScrollBar.Height > this.ScrollBarHousing.Y + this.ScrollBarHousing.Height)
					{
						this.ScrollBar.Y = this.ScrollBarHousing.Y + this.ScrollBarHousing.Height - this.ScrollBar.Height;
					}
				}
				float MousePosAsPct = (input.CursorPosition.Y - (float)this.ScrollBarHousing.Y) / (float)this.ScrollBarHousing.Height;
				if (MousePosAsPct < 0f)
				{
					MousePosAsPct = 0f;
				}
				if (MousePosAsPct > 1f)
				{
					MousePosAsPct = 1f;
				}
				this.indexAtTop = (int)((float)this.Copied.Count * MousePosAsPct);
				if (this.indexAtTop + this.entriesToDisplay >= this.Copied.Count)
				{
					this.indexAtTop = this.Copied.Count - this.entriesToDisplay;
				}
				hit = true;
			}
			if (HelperFunctions.CheckIntersection(this.Parent.Menu, input.CursorPosition))
			{
				if (input.CurrentMouseState.ScrollWheelValue > input.LastMouseState.ScrollWheelValue)
				{
					if (this.indexAtTop > 0)
					{
						ScrollList scrollList2 = this;
						scrollList2.indexAtTop = scrollList2.indexAtTop - 1;
					}
					hit = true;
					float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
					float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
					this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
				}
				if (input.CurrentMouseState.ScrollWheelValue < input.LastMouseState.ScrollWheelValue)
				{
					if (this.indexAtTop + this.entriesToDisplay < this.Copied.Count)
					{
						ScrollList scrollList3 = this;
						scrollList3.indexAtTop = scrollList3.indexAtTop + 1;
					}
					hit = true;
					float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
					float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
					this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
				}
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed && this.dragging)
			{
				this.dragging = false;
			}
			if (this.IsDraggable && this.DraggedEntry == null)
			{
				for (int i = this.indexAtTop; i < this.Copied.Count && i < this.indexAtTop + this.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.Copied[i];
					if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
					{
						if (input.CurrentMouseState.LeftButton != ButtonState.Pressed)
						{
							this.ClickTimer = 0f;
						}
						else
						{
							ScrollList clickTimer = this;
							clickTimer.ClickTimer = clickTimer.ClickTimer + 0.0166666675f;
							if (this.ClickTimer > this.TimerDelay)
							{
								this.DraggedEntry = e;
								this.DraggedOffset = new Vector2((float)e.clickRect.X, (float)e.clickRect.Y) - input.CursorPosition;
								break;
							}
						}
					}
				}
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Released)
			{
				this.ClickTimer = 0f;
				this.DraggedEntry = null;
			}
			if (this.DraggedEntry != null && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
			{
				int Dragged = 0;
				for (int i = this.indexAtTop; i < this.Entries.Count && i < this.indexAtTop + this.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.Entries[i];
					if (e.clickRect == this.DraggedEntry.clickRect)
					{
						Dragged = this.Entries.IndexOf(e);
					}
				}
				for (int i = this.indexAtTop; i < this.Entries.Count && i < this.indexAtTop + this.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.Entries[i];
					if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
					{
						int NewIndex = this.Entries.IndexOf(e);
						if (NewIndex < Dragged)
						{
							ScrollList.Entry toReplace = e;
							this.Entries[NewIndex] = this.Entries[Dragged];
							this.Entries[NewIndex].clickRect = toReplace.clickRect;
							this.Entries[Dragged] = toReplace;
							this.Entries[Dragged].clickRect = this.DraggedEntry.clickRect;
							this.DraggedEntry = this.Entries[NewIndex];
							break;
						}
					}
				}
			}
			if (this.indexAtTop < 0)
			{
				this.indexAtTop = 0;
			}
			this.Update();
			return hit;
		}

		public bool HandleInput(InputState input, Planet p)
		{
			bool hit = false;
			if (HelperFunctions.CheckIntersection(this.ScrollUp, input.CursorPosition) && input.InGameSelect)
			{
				if (this.indexAtTop > 0)
				{
					ScrollList scrollList = this;
					scrollList.indexAtTop = scrollList.indexAtTop - 1;
				}
				hit = true;
				float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
				float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
				this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
			}
			if (HelperFunctions.CheckIntersection(this.ScrollDown, input.CursorPosition) && input.InGameSelect)
			{
				if (this.indexAtTop + this.entriesToDisplay < this.Copied.Count)
				{
					ScrollList scrollList1 = this;
					scrollList1.indexAtTop = scrollList1.indexAtTop + 1;
				}
				hit = true;
				float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
				float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
				this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
			}
			if (HelperFunctions.CheckIntersection(this.ScrollBarHousing, input.CursorPosition))
			{
				this.ScrollBarHover = 1;
				//this.upScrollHover = 1;
				//this.downScrollHover = 1;
				if (HelperFunctions.CheckIntersection(this.ScrollBar, input.CursorPosition))
				{
					this.ScrollBarHover = 2;
					if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
					{
						this.startDragPos = (int)input.CursorPosition.Y;
						this.ScrollBarStartDragPos = (float)this.ScrollBar.Y;
						this.dragging = true;
						hit = true;
					}
				}
			}
			else if (!this.dragging)
			{
				this.ScrollBarHover = 0;
				//this.upScrollHover = 0;
				//this.downScrollHover = 0;
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Pressed && this.dragging)
			{
				float difference = input.CursorPosition.Y - (float)this.startDragPos;
				float count = 1f / (float)this.Copied.Count;
				if (Math.Abs(difference) > 0f)
				{
					this.ScrollBar.Y = (int)(this.ScrollBarStartDragPos + difference);
					if (this.ScrollBar.Y < this.ScrollBarHousing.Y)
					{
						this.ScrollBar.Y = this.ScrollBarHousing.Y;
					}
					else if (this.ScrollBar.Y + this.ScrollBar.Height > this.ScrollBarHousing.Y + this.ScrollBarHousing.Height)
					{
						this.ScrollBar.Y = this.ScrollBarHousing.Y + this.ScrollBarHousing.Height - this.ScrollBar.Height;
					}
				}
				float MousePosAsPct = (input.CursorPosition.Y - (float)this.ScrollBarHousing.Y) / (float)this.ScrollBarHousing.Height;
				if (MousePosAsPct < 0f)
				{
					MousePosAsPct = 0f;
				}
				if (MousePosAsPct > 1f)
				{
					MousePosAsPct = 1f;
				}
				this.indexAtTop = (int)((float)this.Copied.Count * MousePosAsPct);
				if (this.indexAtTop + this.entriesToDisplay >= this.Copied.Count)
				{
					this.indexAtTop = this.Copied.Count - this.entriesToDisplay;
				}
				hit = true;
			}
			if (HelperFunctions.CheckIntersection(this.Parent.Menu, input.CursorPosition))
			{
				if (input.CurrentMouseState.ScrollWheelValue > input.LastMouseState.ScrollWheelValue)
				{
					if (this.indexAtTop > 0)
					{
						ScrollList scrollList2 = this;
						scrollList2.indexAtTop = scrollList2.indexAtTop - 1;
					}
					hit = true;
					float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
					float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
					this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
				}
				if (input.CurrentMouseState.ScrollWheelValue < input.LastMouseState.ScrollWheelValue)
				{
					if (this.indexAtTop + this.entriesToDisplay < this.Copied.Count)
					{
						ScrollList scrollList3 = this;
						scrollList3.indexAtTop = scrollList3.indexAtTop + 1;
					}
					hit = true;
					float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
					float startingPercent = (float)this.indexAtTop / (float)this.Copied.Count;
					this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, this.ScrollBarHousing.Y + (int)(startingPercent * (float)this.ScrollBarHousing.Height), ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, (int)((float)this.ScrollBarHousing.Height * percentViewed));
				}
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed && this.dragging)
			{
				this.dragging = false;
			}
			if (this.IsDraggable && this.DraggedEntry == null)
			{
				for (int i = this.indexAtTop; i < this.Entries.Count && i < this.indexAtTop + this.entriesToDisplay; i++)
				{
					try
					{
						ScrollList.Entry e = this.Copied[i];
						if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
						{
							if (input.CurrentMouseState.LeftButton != ButtonState.Pressed)
							{
								this.ClickTimer = 0f;
							}
							else
							{
								ScrollList clickTimer = this;
								clickTimer.ClickTimer = clickTimer.ClickTimer + 0.0166666675f;
								if (this.ClickTimer > this.TimerDelay)
								{
									this.DraggedEntry = e;
									this.DraggedOffset = new Vector2((float)e.clickRect.X, (float)e.clickRect.Y) - input.CursorPosition;
									break;
								}
							}
						}
					}
					catch
					{
					}
				}
			}
			if (input.CurrentMouseState.LeftButton == ButtonState.Released)
			{
				this.ClickTimer = 0f;
				this.DraggedEntry = null;
			}
			if (this.DraggedEntry != null && input.CurrentMouseState.LeftButton == ButtonState.Pressed)
			{
				int Dragged = 0;
				for (int i = this.indexAtTop; i < this.Entries.Count && i < this.indexAtTop + this.entriesToDisplay; i++)
				{
					ScrollList.Entry e = this.Entries[i];
					if (e.clickRect == this.DraggedEntry.clickRect)
					{
						Dragged = this.Entries.IndexOf(e);
					}
				}
				for (int i = this.indexAtTop; i < this.Entries.Count && i < this.indexAtTop + this.entriesToDisplay; i++)
				{
					try
					{
						ScrollList.Entry e = this.Entries[i];
						if (HelperFunctions.CheckIntersection(e.clickRect, input.CursorPosition))
						{
							int NewIndex = this.Entries.IndexOf(e);
							if (NewIndex < Dragged)
							{
								ScrollList.Entry toReplace = e;
								this.Entries[NewIndex] = this.Entries[Dragged];
								this.Entries[NewIndex].clickRect = toReplace.clickRect;
								p.ConstructionQueue[NewIndex] = p.ConstructionQueue[Dragged];
								this.Entries[Dragged] = toReplace;
								this.Entries[Dragged].clickRect = this.DraggedEntry.clickRect;
								p.ConstructionQueue[Dragged] = toReplace.item as QueueItem;
								this.DraggedEntry = this.Entries[NewIndex];
								break;
							}
							else if (NewIndex > Dragged)
							{
								ScrollList.Entry toRemove = this.Entries[Dragged];
								for (int j = Dragged + 1; j <= NewIndex; j++)
								{
									this.Entries[j].clickRect = this.Entries[j - 1].clickRect;
									this.Entries[j - 1] = this.Entries[j];
									p.ConstructionQueue[j - 1] = p.ConstructionQueue[j];
								}
								toRemove.clickRect = this.Entries[NewIndex].clickRect;
								this.Entries[NewIndex] = toRemove;
								p.ConstructionQueue[NewIndex] = toRemove.item as QueueItem;
								this.DraggedEntry = this.Entries[NewIndex];
							}
						}
					}
					catch
					{
					}
				}
			}
			this.Update();
			return hit;
		}

		public void Recalculate(Submenu p, int eHeight)
		{
			this.entryHeight = eHeight;
			this.Parent = p;
			this.entriesToDisplay = (p.Menu.Height - 25) / this.entryHeight;
			this.ScrollUp = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + 30, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Height);
			this.ScrollDown = new Rectangle(p.Menu.X + p.Menu.Width - 20, p.Menu.Y + p.Menu.Height - 14, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Height);
			this.ScrollBarHousing = new Rectangle(this.ScrollUp.X + 1, this.ScrollUp.Y + this.ScrollUp.Height + 3, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, this.ScrollDown.Y - this.ScrollUp.Y - this.ScrollUp.Height - 6);
			this.ScrollBar = new Rectangle(this.ScrollBarHousing.X, 0, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, 0);
			List<ScrollList.Entry> copy = new List<ScrollList.Entry>();
			foreach (ScrollList.Entry e in this.Entries)
			{
				copy.Add(e);
			}
			this.Entries.Clear();
			foreach (ScrollList.Entry c in copy)
			{
				this.AddItem(c.item);
			}
		}

		public void Reset()
		{
			this.Entries.Clear();
			this.Copied.Clear();
			this.indexAtTop = 0;
		}

		public void TransitionUpdate(Rectangle r)
		{
			this.ScrollUp = new Rectangle(r.X + r.Width - 20, r.Y + 30, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_up"].Height);
			this.ScrollDown = new Rectangle(r.X + r.Width - 20, r.Y + r.Height - 14, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Width, ResourceManager.TextureDict["NewUI/scrollbar_arrow_down"].Height);
			this.ScrollBarHousing = new Rectangle(this.ScrollUp.X + 1, this.ScrollUp.Y + this.ScrollUp.Height + 3, ResourceManager.TextureDict["NewUI/scrollbar_bar_mid"].Width, this.ScrollDown.Y - this.ScrollUp.Y - this.ScrollUp.Height - 6);
			int j = 0;
			for (int i = 0; i < this.Entries.Count; i++)
			{
				if (i < this.indexAtTop)
				{
					this.Entries[i].clickRect = new Rectangle(-500, -500, 0, 0);
				}
				else if (i > this.indexAtTop + this.entriesToDisplay - 1)
				{
					this.Entries[i].clickRect = new Rectangle(-500, -500, 0, 0);
				}
				else
				{
					this.Entries[i].clickRect = new Rectangle(r.X + 20, r.Y + 35 + j * this.entryHeight, this.Parent.Menu.Width - 40, this.entryHeight);
					if (this.Entries[i].Plus != 0)
					{
						Rectangle plusRect = new Rectangle(this.Entries[i].clickRect.X + this.Entries[i].clickRect.Width - 30, this.Entries[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_build_add"].Height / 2, ResourceManager.TextureDict["NewUI/icon_build_add"].Width, ResourceManager.TextureDict["NewUI/icon_build_add"].Height);
						this.Entries[i].addRect = plusRect;
					}
					if (this.Entries[i].Edit != 0)
					{
						Rectangle plusRect = new Rectangle(this.Entries[i].clickRect.X + this.Entries[i].clickRect.Width - 60, this.Entries[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_build_add"].Height / 2, ResourceManager.TextureDict["NewUI/icon_build_add"].Width, ResourceManager.TextureDict["NewUI/icon_build_add"].Height);
						this.Entries[i].editRect = plusRect;
					}
					Rectangle item = this.Entries[i].up;
					this.Entries[i].up = new Rectangle(this.Entries[i].clickRect.X + this.Entries[i].clickRect.Width - 30, this.Entries[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
					Rectangle rectangle = this.Entries[i].down;
					this.Entries[i].down = new Rectangle(this.Entries[i].clickRect.X + this.Entries[i].clickRect.Width - 60, this.Entries[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
					Rectangle item1 = this.Entries[i].apply;
					this.Entries[i].apply = new Rectangle(this.Entries[i].clickRect.X + this.Entries[i].clickRect.Width - 90, this.Entries[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
					Rectangle rectangle1 = this.Entries[i].cancel;
					this.Entries[i].cancel = new Rectangle(this.Entries[i].clickRect.X + this.Entries[i].clickRect.Width - 120, this.Entries[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
					j++;
				}
			}
			this.Entries.ApplyPendingRemovals();
		}

		public void Update()
		{
			this.Copied = new BatchRemovalCollection<ScrollList.Entry>();
			foreach (ScrollList.Entry e in this.Entries)
			{
				this.Copied.Add(e);
				if (!e.ShowingSub)
				{
					continue;
				}
				foreach (ScrollList.Entry sub in e.SubEntries)
				{
					this.Copied.Add(sub);
					foreach (ScrollList.Entry subsub in sub.SubEntries)
					{
						this.Copied.Add(subsub);
					}
				}
			}
			int j = 0;
			for (int i = 0; i < this.Copied.Count; i++)
			{
				if (this.Copied[i] != null)
				{
					if (i >= this.indexAtTop)
					{
						this.Copied[i].clickRect = new Rectangle(this.Parent.Menu.X + 20, this.Parent.Menu.Y + 35 + j * this.entryHeight, this.Parent.Menu.Width - 40, this.entryHeight);
						if (this.Copied[i].Plus != 0)
						{
							Rectangle plusRect = new Rectangle(this.Copied[i].clickRect.X + this.Copied[i].clickRect.Width - 30, this.Copied[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_build_add"].Height / 2, ResourceManager.TextureDict["NewUI/icon_build_add"].Width, ResourceManager.TextureDict["NewUI/icon_build_add"].Height);
							this.Copied[i].addRect = plusRect;
						}
						if (this.Copied[i].Edit != 0)
						{
							Rectangle plusRect = new Rectangle(this.Copied[i].clickRect.X + this.Copied[i].clickRect.Width - 60, this.Copied[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_build_add"].Height / 2, ResourceManager.TextureDict["NewUI/icon_build_add"].Width, ResourceManager.TextureDict["NewUI/icon_build_add"].Height);
							this.Copied[i].editRect = plusRect;
						}
						Rectangle item = this.Copied[i].up;
						this.Copied[i].up = new Rectangle(this.Copied[i].clickRect.X + this.Copied[i].clickRect.Width - 30, this.Copied[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
						Rectangle rectangle = this.Copied[i].down;
						this.Copied[i].down = new Rectangle(this.Copied[i].clickRect.X + this.Copied[i].clickRect.Width - 60, this.Copied[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
						Rectangle item1 = this.Copied[i].apply;
						this.Copied[i].apply = new Rectangle(this.Copied[i].clickRect.X + this.Copied[i].clickRect.Width - 90, this.Copied[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
						Rectangle rectangle1 = this.Copied[i].cancel;
						this.Copied[i].cancel = new Rectangle(this.Copied[i].clickRect.X + this.Copied[i].clickRect.Width - 120, this.Copied[i].clickRect.Y + 15 - ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height / 2, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Width, ResourceManager.TextureDict["NewUI/icon_queue_arrow_up"].Height);
						j++;
					}
					else
					{
						this.Copied[i].clickRect = new Rectangle(-500, -500, 0, 0);
					}
				}
			}
			float percentViewed = (float)this.entriesToDisplay / (float)this.Copied.Count;
			float count = (float)this.indexAtTop / (float)this.Copied.Count;
			this.ScrollBar.Height = (int)((float)this.ScrollBarHousing.Height * percentViewed);
			this.Entries.ApplyPendingRemovals();
			this.Copied.ApplyPendingRemovals();
			if (this.indexAtTop < 0)
			{
				this.indexAtTop = 0;
			}
		}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (this.Entries != null)
                        this.Entries.Dispose();
                    if (this.Copied != null)
                        this.Copied.Dispose();

                }
                this.Entries = null;
                this.Copied = null;
                this.disposed = true;
            }
        }

		public class Entry
		{
			public bool ShowingSub;

			public Rectangle clickRect = new Rectangle();

			public object item;

			public int Hover;

			public int Plus;

			public int PlusHover;

			public int Edit;

			public int EditHover;

			public Rectangle editRect;

			public Rectangle addRect;

			public ScrollList ParentList;

			public int clickRectHover;

			public int QItem;

			public Rectangle up;

			public Rectangle down;

			public Rectangle cancel;

			public Rectangle apply;

			public int uh;

			public int ch;

			public int dh;

			public int ah;
            //moved this here for consistency
			public List<ScrollList.Entry> SubEntries = new List<ScrollList.Entry>();

			public Entry()
			{
               
            }

			public void AddItem(object o)
			{
				ScrollList.Entry e = new ScrollList.Entry();

                e.item = o;
				
				this.SubEntries.Add(e);
			}

			public void AddItem(object o, int addrect, int addpencil)
			{
				ScrollList.Entry e = new ScrollList.Entry()
				{
					item = o
				};
				if (addrect > 0)
				{
					e.Plus = 1;
				}
				if (addpencil > 0)
				{
					e.editRect = new Rectangle();
					e.Edit = 1;
				}
				e.addRect = new Rectangle();
				this.SubEntries.Add(e);
			}

			public void AddItemWithCancel(object o)
			{
				ScrollList.Entry e = new ScrollList.Entry()
				{
					item = o
				};
				this.SubEntries.Add(e);
				e.cancel = new Rectangle();
			}
		}
	}


}