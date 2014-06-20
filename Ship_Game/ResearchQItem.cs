using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class ResearchQItem
	{
		public Rectangle container;

		private Rectangle Up;

		private Rectangle Down;

		private Rectangle Cancel;

		private TexturedButton bup;

		private TexturedButton bdown;

		private TexturedButton bcancel;

		public TreeNode Node;

		private ResearchScreenNew screen;

		public Vector2 pos;

		public ResearchQItem(Vector2 Position, TreeNode Node, ResearchScreenNew screen)
		{
			this.pos = Position;
			this.screen = screen;
			this.container = new Rectangle((int)Position.X, (int)Position.Y, 320, 110);
			this.Node = new TreeNode(new Vector2((float)this.container.X, (float)this.container.Y) + new Vector2(100f, 20f), Node.tech, screen);
			this.Up = new Rectangle(this.container.X + 15, this.container.Y + this.container.Height / 2 - 33, 30, 30);
			this.Down = new Rectangle(this.container.X + 15, this.container.Y + this.container.Height / 2 - 33 + 36, 30, 30);
			this.Cancel = new Rectangle(this.container.X + 15 + 30 + 12, this.container.Y + this.container.Height / 2 - 15, 30, 30);
			this.bup = new TexturedButton(this.Up, "ResearchMenu/button_queue_up", "ResearchMenu/button_queue_up_hover", "ResearchMenu/button_queue_up_press");
			this.bdown = new TexturedButton(this.Down, "ResearchMenu/button_queue_down", "ResearchMenu/button_queue_down_hover", "ResearchMenu/button_queue_down_press");
			this.bcancel = new TexturedButton(this.Cancel, "ResearchMenu/button_queue_cancel", "ResearchMenu/button_queue_cancel_hover", "ResearchMenu/button_queue_cancel_press");
		}

		public void Draw(Ship_Game.ScreenManager ScreenManager)
		{
			this.bup.Draw(ScreenManager);
			this.bdown.Draw(ScreenManager);
			this.bcancel.Draw(ScreenManager);
			this.Node.DrawGlow(ScreenManager);
			this.Node.Draw(ScreenManager);
		}

		public void Draw(Rectangle container, Ship_Game.ScreenManager ScreenManager)
		{
			this.Node = new TreeNode(new Vector2((float)container.X, (float)container.Y) + new Vector2(100f, 20f), this.Node.tech, this.screen);
			this.bup.r = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33, 30, 30);
			this.bdown.r = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33 + 36, 30, 30);
			this.bcancel.r = new Rectangle(container.X + 15 + 30 + 12, container.Y + container.Height / 2 - 15, 30, 30);
			this.bup.Draw(ScreenManager);
			this.bdown.Draw(ScreenManager);
			this.bcancel.Draw(ScreenManager);
			this.Node.DrawGlow(ScreenManager);
			this.Node.Draw(ScreenManager);
		}

		public bool HandleInput(InputState input)
		{
			if (this.bup.HandleInput(input))
			{
				if (this.Node.tech.UID != EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic)
				{
					int indexOfThis = 0;
					int i = 0;
					foreach (ScrollList.Entry entry in this.screen.qcomponent.QSL.Entries)
					{
						if ((entry.item as ResearchQItem).Node.tech.UID != this.Node.tech.UID)
						{
							i++;
						}
						else
						{
							indexOfThis = i;
							break;
						}
					}
					if (indexOfThis != 0)
					{
						bool AboveisPrereq = false;
						foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis - 1]].LeadsTo)
						{
							if (dependent.UID != this.Node.tech.UID)
							{
								continue;
							}
							AboveisPrereq = true;
							break;
						}
						if (!AboveisPrereq)
						{
							this.screen.qcomponent.QSL.Entries.Clear();
							this.screen.qcomponent.QSL.Copied.Clear();
							string toswitch = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis - 1];
							EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis - 1] = this.Node.tech.UID;
							EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis] = toswitch;
							foreach (string uid in EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue)
							{
								this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[uid] as TreeNode);
							}
						}
						else
						{
							AudioManager.PlayCue("UI_Misc20");
						}
					}
					else
					{
						bool CurrentIsPrereq = false;
						foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic].LeadsTo)
						{
							if (dependent.UID != this.Node.tech.UID)
							{
								continue;
							}
							CurrentIsPrereq = true;
							break;
						}
						if (!CurrentIsPrereq)
						{
							this.screen.qcomponent.QSL.Entries.Clear();
							this.screen.qcomponent.QSL.Copied.Clear();
							this.screen.qcomponent.CurrentResearch = null;
							string toswitch = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic;
							EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic = this.Node.tech.UID;
							EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[0] = toswitch;
							string resTop = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic;
							if (resTop != "")
							{
								this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[resTop] as TreeNode);
							}
							foreach (string uid in EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue)
							{
								this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[uid] as TreeNode);
							}
						}
						else
						{
							AudioManager.PlayCue("UI_Misc20");
						}
					}
				}
				else
				{
					AudioManager.PlayCue("UI_Misc20");
				}
				return true;
			}
			if (!this.bdown.HandleInput(input))
			{
				if (!this.bcancel.HandleInput(input))
				{
					return false;
				}
				string uid = this.Node.tech.UID;
				if (this.Node.tech.UID != EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic)
				{
					foreach (ScrollList.Entry entry in this.screen.qcomponent.QSL.Entries)
					{
						if (entry.item != this)
						{
							continue;
						}
						this.screen.qcomponent.QSL.Entries.QueuePendingRemoval(entry);
						this.screen.qcomponent.QSL.Copied.QueuePendingRemoval(entry);
					}
					foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[uid].LeadsTo)
					{
						this.RemoveTech(dependent.UID);
					}
					EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Remove(uid);
				}
				else if (EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Count == 0)
				{
					EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic = "";
					this.screen.qcomponent.CurrentResearch = null;
				}
				else
				{
					foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[uid].LeadsTo)
					{
						this.RemoveTech(dependent.UID);
					}
					this.screen.qcomponent.QSL.Entries.ApplyPendingRemovals();
					this.screen.qcomponent.QSL.Copied.ApplyPendingRemovals();
					if (EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Count == 0)
					{
						EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic = "";
						this.screen.qcomponent.CurrentResearch = null;
					}
					else
					{
						EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic = (this.screen.qcomponent.QSL.Copied[0].item as ResearchQItem).Node.tech.UID;
						this.screen.qcomponent.CurrentResearch = new ResearchQItem(this.screen.qcomponent.CurrentResearch.pos, (this.screen.qcomponent.QSL.Copied[0].item as ResearchQItem).Node, this.screen);
						EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Remove((this.screen.qcomponent.QSL.Copied[0].item as ResearchQItem).Node.tech.UID);
						this.screen.qcomponent.QSL.Entries.RemoveAt(0);
						this.screen.qcomponent.QSL.Copied.RemoveAt(0);
					}
				}
				return true;
			}
			if (this.Node.tech.UID == EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic && EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Count == 0)
			{
				AudioManager.PlayCue("UI_Misc20");
			}
			else if (this.Node.tech.UID != EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic)
			{
				int indexOfThis = 0;
				int i = 0;
				foreach (ScrollList.Entry entry in this.screen.qcomponent.QSL.Entries)
				{
					if ((entry.item as ResearchQItem).Node.tech.UID != this.Node.tech.UID)
					{
						i++;
					}
					else
					{
						indexOfThis = i;
						break;
					}
				}
				if (indexOfThis != EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Count - 1)
				{
					bool ThisIsPreReq = false;
					foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis]].LeadsTo)
					{
						if (dependent.UID != EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis + 1])
						{
							continue;
						}
						ThisIsPreReq = true;
						break;
					}
					if (!ThisIsPreReq)
					{
						this.screen.qcomponent.QSL.Entries.Clear();
						this.screen.qcomponent.QSL.Copied.Clear();
						string toswitch = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis + 1];
						EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis + 1] = this.Node.tech.UID;
						EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[indexOfThis] = toswitch;
						foreach (string uid in EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue)
						{
							this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[uid] as TreeNode);
						}
					}
					else
					{
						AudioManager.PlayCue("UI_Misc20");
					}
				}
				else
				{
					AudioManager.PlayCue("UI_Misc20");
				}
			}
			else
			{
				bool ThisIsPreReq = false;
				foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic].LeadsTo)
				{
					if (dependent.UID != EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[0])
					{
						continue;
					}
					ThisIsPreReq = true;
					break;
				}
				if (!ThisIsPreReq)
				{
					this.screen.qcomponent.QSL.Entries.Clear();
					this.screen.qcomponent.QSL.Copied.Clear();
					this.screen.qcomponent.CurrentResearch = null;
					string toswitch = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[0];
					EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue[0] = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic;
					EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic = toswitch;
					string resTop = EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).ResearchTopic;
					if (resTop != "")
					{
						this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[resTop] as TreeNode);
					}
					foreach (string uid in EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue)
					{
						this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[uid] as TreeNode);
					}
				}
				else
				{
					AudioManager.PlayCue("UI_Misc20");
				}
			}
			return true;
		}

		private void RemoveTech(string uid)
		{
			foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[uid].LeadsTo)
			{
				this.RemoveTech(dependent.UID);
			}
			EmpireManager.GetEmpireByName(this.screen.empireUI.screen.PlayerLoyalty).data.ResearchQueue.Remove(uid);
			foreach (ScrollList.Entry entry in this.screen.qcomponent.QSL.Entries)
			{
				if ((entry.item as ResearchQItem).Node.tech.UID != uid)
				{
					continue;
				}
				this.screen.qcomponent.QSL.Entries.QueuePendingRemoval(entry);
				this.screen.qcomponent.QSL.Copied.QueuePendingRemoval(entry);
			}
		}
	}
}