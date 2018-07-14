using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public sealed class ResearchQItem
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
			this.Node = new TreeNode(new Vector2(this.container.X, this.container.Y) + new Vector2(100f, 20f), Node.tech, screen);
			this.Up = new Rectangle(this.container.X + 15, this.container.Y + this.container.Height / 2 - 33, 30, 30);
			this.Down = new Rectangle(this.container.X + 15, this.container.Y + this.container.Height / 2 - 33 + 36, 30, 30);
			this.Cancel = new Rectangle(this.container.X + 15 + 30 + 12, this.container.Y + this.container.Height / 2 - 15, 30, 30);
			this.bup = new TexturedButton(this.Up, "ResearchMenu/button_queue_up", "ResearchMenu/button_queue_up_hover", "ResearchMenu/button_queue_up_press");
			this.bdown = new TexturedButton(this.Down, "ResearchMenu/button_queue_down", "ResearchMenu/button_queue_down_hover", "ResearchMenu/button_queue_down_press");
			this.bcancel = new TexturedButton(this.Cancel, "ResearchMenu/button_queue_cancel", "ResearchMenu/button_queue_cancel_hover", "ResearchMenu/button_queue_cancel_press");
		}

		public void Draw(ScreenManager screenManager)
		{
			this.bup.Draw(screenManager);
			this.bdown.Draw(screenManager);
			this.bcancel.Draw(screenManager);
			this.Node.DrawGlow(screenManager);
			this.Node.Draw(screenManager);
		}

		public void Draw(Rectangle container, ScreenManager screenManager)
		{
			this.Node = new TreeNode(new Vector2((float)container.X, (float)container.Y) + new Vector2(100f, 20f), this.Node.tech, this.screen);
			this.bup.r = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33, 30, 30);
			this.bdown.r = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33 + 36, 30, 30);
			this.bcancel.r = new Rectangle(container.X + 15 + 30 + 12, container.Y + container.Height / 2 - 15, 30, 30);
			this.bup.Draw(screenManager);
			this.bdown.Draw(screenManager);
			this.bcancel.Draw(screenManager);
			this.Node.DrawGlow(screenManager);
			this.Node.Draw(screenManager);
		}

		public bool HandleInput(InputState input)
		{
			if (bup.HandleInput(input))
			{
				if (Node.tech.UID != EmpireManager.Player.ResearchTopic)
				{
					int indexOfThis = screen.qcomponent.QSL.IndexOf<ResearchQItem>(q => q.Node.tech.UID == Node.tech.UID);
					if (indexOfThis != -1)
					{
						bool aboveisPrereq = false;
						foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.Player.data.ResearchQueue[indexOfThis - 1]].LeadsTo)
						{
							if (dependent.UID == Node.tech.UID)
							{
							    aboveisPrereq = true;
                                break;
							}
						}
						if (!aboveisPrereq)
						{
							screen.qcomponent.QSL.Reset();
							string toswitch = EmpireManager.Player.data.ResearchQueue[indexOfThis - 1];
							EmpireManager.Player.data.ResearchQueue[indexOfThis - 1] = Node.tech.UID;
							EmpireManager.Player.data.ResearchQueue[indexOfThis] = toswitch;
							foreach (string uid in EmpireManager.Player.data.ResearchQueue)
							{
								screen.qcomponent.LoadQueue(screen.CompleteSubNodeTree[uid] as TreeNode);
							}
						}
						else
						{
							GameAudio.PlaySfxAsync("UI_Misc20");
						}
					}
					else
					{
						bool currentIsPrereq = false;
						foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.Player.ResearchTopic].LeadsTo)
						{
							if (dependent.UID == Node.tech.UID)
							{
							    currentIsPrereq = true;
							    break;
							}
						}
						if (!currentIsPrereq)
						{
							screen.qcomponent.QSL.Reset();
							screen.qcomponent.CurrentResearch = null;
							string toswitch = EmpireManager.Player.ResearchTopic;
							EmpireManager.Player.ResearchTopic = Node.tech.UID;
							EmpireManager.Player.data.ResearchQueue[0] = toswitch;
							string resTop = EmpireManager.Player.ResearchTopic;
							if (!string.IsNullOrEmpty(resTop))
							{
								screen.qcomponent.LoadQueue(screen.CompleteSubNodeTree[resTop] as TreeNode);
							}
							foreach (string uid in EmpireManager.Player.data.ResearchQueue)
							{
								screen.qcomponent.LoadQueue(screen.CompleteSubNodeTree[uid] as TreeNode);
							}
						}
						else
						{
							GameAudio.PlaySfxAsync("UI_Misc20");
						}
					}
				}
				else
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}
				return true;
			}
			if (!bdown.HandleInput(input))
			{
				if (!bcancel.HandleInput(input))
				{
					return false;
				}
				string uid = Node.tech.UID;
				if (Node.tech.UID != EmpireManager.Player.ResearchTopic)
				{
				    screen.qcomponent.QSL.RemoveItem(this);
                    RemoveTech(uid);
				}
				else if (EmpireManager.Player.data.ResearchQueue.Count == 0)
				{
					EmpireManager.Player.ResearchTopic = "";
					screen.qcomponent.CurrentResearch = null;
				}
				else
				{
				    RemoveTech(uid);

					if (EmpireManager.Player.data.ResearchQueue.Count == 0)
					{
						EmpireManager.Player.ResearchTopic = "";
						screen.qcomponent.CurrentResearch = null;
					}
					else
					{
                        var qItem = screen.qcomponent.QSL.FirstItem<ResearchQItem>();
						EmpireManager.Player.ResearchTopic = qItem.Node.tech.UID;
						screen.qcomponent.CurrentResearch = new ResearchQItem(screen.qcomponent.CurrentResearch.pos, qItem.Node, screen);
						EmpireManager.Player.data.ResearchQueue.Remove(qItem.Node.tech.UID);
						screen.qcomponent.QSL.RemoveFirst();
					}
				}
				return true;
			}

			if (Node.tech.UID == EmpireManager.Player.ResearchTopic && EmpireManager.Player.data.ResearchQueue.Count == 0)
			{
				GameAudio.PlaySfxAsync("UI_Misc20");
			}
			else if (Node.tech.UID != EmpireManager.Player.ResearchTopic)
			{
				int indexOfThis = screen.qcomponent.QSL.IndexOf<ResearchQItem>(r => r.Node.tech.UID == Node.tech.UID);

				if (indexOfThis != -1 && indexOfThis != EmpireManager.Player.data.ResearchQueue.Count - 1)
				{
					bool thisIsPreReq = false;
					foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.Player.data.ResearchQueue[indexOfThis]].LeadsTo)
					{
						if (dependent.UID != EmpireManager.Player.data.ResearchQueue[indexOfThis + 1])
						{
							continue;
						}
						thisIsPreReq = true;
						break;
					}
					if (!thisIsPreReq)
					{
                        screen.qcomponent.QSL.Reset();
						string toswitch = EmpireManager.Player.data.ResearchQueue[indexOfThis + 1];
						EmpireManager.Player.data.ResearchQueue[indexOfThis + 1] = this.Node.tech.UID;
						EmpireManager.Player.data.ResearchQueue[indexOfThis] = toswitch;
						foreach (string uid in EmpireManager.Player.data.ResearchQueue)
						{
							this.screen.qcomponent.LoadQueue(this.screen.CompleteSubNodeTree[uid] as TreeNode);
						}
					}
					else
					{
						GameAudio.PlaySfxAsync("UI_Misc20");
					}
				}
				else
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}
			}
			else
			{
				bool thisIsPreReq = false;
				foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[EmpireManager.Player.ResearchTopic].LeadsTo)
				{
					if (dependent.UID != EmpireManager.Player.data.ResearchQueue[0])
					{
						continue;
					}
					thisIsPreReq = true;
					break;
				}
				if (!thisIsPreReq)
				{
				    screen.qcomponent.QSL.Reset();
					screen.qcomponent.CurrentResearch = null;
					string toswitch = EmpireManager.Player.data.ResearchQueue[0];
					EmpireManager.Player.data.ResearchQueue[0] = EmpireManager.Player.ResearchTopic;
					EmpireManager.Player.ResearchTopic = toswitch;
					string resTop = EmpireManager.Player.ResearchTopic;
					if (!string.IsNullOrEmpty(resTop))
					{
						screen.qcomponent.LoadQueue(screen.CompleteSubNodeTree[resTop] as TreeNode);
					}
					foreach (string uid in EmpireManager.Player.data.ResearchQueue)
					{
						screen.qcomponent.LoadQueue(screen.CompleteSubNodeTree[uid] as TreeNode);
					}
				}
				else
				{
					GameAudio.PlaySfxAsync("UI_Misc20");
				}
			}
			return true;
		}

		private void RemoveTech(string uid)
		{
			foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[uid].LeadsTo)
			{
				RemoveTech(dependent.UID);
			}
			EmpireManager.Player.data.ResearchQueue.Remove(uid);
            screen.qcomponent.QSL.RemoveIf<ResearchQItem>(rqi => rqi.Node.tech.UID == uid);
		}
	}
}