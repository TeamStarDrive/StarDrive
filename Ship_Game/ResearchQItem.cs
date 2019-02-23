using Microsoft.Xna.Framework;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class ResearchQItem
	{
	    public TreeNode Node;

	    private readonly TexturedButton BtnUp;
	    private readonly TexturedButton BtnDown;
	    private readonly TexturedButton BtnCancel;
	    private readonly ResearchScreenNew Screen;
		private readonly Vector2 Pos;

		public ResearchQItem(Vector2 position, TreeNode node, ResearchScreenNew screen)
		{
			Pos = position;
			Screen = screen;
			var container = new Rectangle((int)position.X, (int)position.Y, 320, 110);
			Node = new TreeNode(container.PosVec() + new Vector2(100f, 20f), node.Entry, screen);
			var rup    = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33, 30, 30);
			var rdown  = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33 + 36, 30, 30);
			var cancel = new Rectangle(container.X + 15 + 30 + 12, container.Y + container.Height / 2 - 15, 30, 30);
			BtnUp     = new TexturedButton(rup,    "ResearchMenu/button_queue_up",     "ResearchMenu/button_queue_up_hover",     "ResearchMenu/button_queue_up_press");
			BtnDown   = new TexturedButton(rdown,  "ResearchMenu/button_queue_down",   "ResearchMenu/button_queue_down_hover",   "ResearchMenu/button_queue_down_press");
			BtnCancel = new TexturedButton(cancel, "ResearchMenu/button_queue_cancel", "ResearchMenu/button_queue_cancel_hover", "ResearchMenu/button_queue_cancel_press");
		}

		public void Draw(ScreenManager screenManager)
		{
			BtnUp.Draw(screenManager);
			BtnDown.Draw(screenManager);
			BtnCancel.Draw(screenManager);
			Node.DrawGlow(screenManager);
			Node.Draw(screenManager);
		}

		public void Draw(ScreenManager screenManager, Rectangle container)
		{
			Node = new TreeNode(new Vector2(container.X, container.Y) + new Vector2(100f, 20f), Node.Entry, Screen);
			BtnUp.r     = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33, 30, 30);
			BtnDown.r   = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33 + 36, 30, 30);
			BtnCancel.r = new Rectangle(container.X + 15 + 30 + 12, container.Y + container.Height / 2 - 15, 30, 30);
			BtnUp.Draw(screenManager);
			BtnDown.Draw(screenManager);
			BtnCancel.Draw(screenManager);
			Node.DrawGlow(screenManager);
			Node.Draw(screenManager);
		}

		public bool HandleInput(InputState input)
		{
			if (BtnUp.HandleInput(input))     return HandleResearchButtonUp();
		    if (BtnDown.HandleInput(input))   return HandleResearchButtonDown();
		    if (BtnCancel.HandleInput(input)) return HandleResearchButtonCancel();
		    return false;
		}

        private void SwapQueueItems(int first, int second)
        {
            string tmp = EmpireManager.Player.data.ResearchQueue[first];
            EmpireManager.Player.data.ResearchQueue[first] = Node.Entry.UID;
            EmpireManager.Player.data.ResearchQueue[second] = tmp;
        }

        private void SwapResearchTopic(int queueIndex)
        {
            string tmp = EmpireManager.Player.data.ResearchQueue[queueIndex];
            EmpireManager.Player.data.ResearchQueue[queueIndex] = EmpireManager.Player.ResearchTopic;
            EmpireManager.Player.ResearchTopic = tmp;
        }

        private bool HandleResearchButtonUp()
        {
            if (Node.Entry.UID == EmpireManager.Player.ResearchTopic)
            {
                GameAudio.NegativeClick();
                return true;
            }
            int indexOfThis = Screen.qcomponent.QSL.IndexOf<ResearchQItem>(q => q.Node.Entry.UID == Node.Entry.UID);
            if (indexOfThis > 0) // move it up
            {
                if (AboveIsPreReq(indexOfThis))
                {
                    GameAudio.NegativeClick();
                    return true;
                }
                SwapQueueItems(indexOfThis - 1, indexOfThis);
                Screen.qcomponent.ReloadResearchQueue();
            }
            else // set as current research item
            {
                if (CurrentIsPreReq())
                {
                    GameAudio.NegativeClick();
                    return true;
                }
                SwapResearchTopic(0);
                Screen.qcomponent.ReloadResearchQueue();
            }
            return true;
        }


	    private bool HandleResearchButtonDown()
	    {
	        if (Node.Entry.UID == EmpireManager.Player.ResearchTopic && EmpireManager.Player.data.ResearchQueue.Count == 0)
	        {
	            GameAudio.NegativeClick();
                return true;
	        }
	        if (Node.Entry.UID != EmpireManager.Player.ResearchTopic) // move tech down
	        {
	            int indexOfThis = Screen.qcomponent.QSL.IndexOf<ResearchQItem>(r => r.Node.Entry.UID == Node.Entry.UID);
	            if (indexOfThis == -1 || indexOfThis == EmpireManager.Player.data.ResearchQueue.Count - 1 || ThisIsPreReq(indexOfThis))
	            {
	                GameAudio.NegativeClick();
                    return true;
	            }
	            SwapQueueItems(indexOfThis + 1, indexOfThis);
	            Screen.qcomponent.ReloadResearchQueue();
	        }
	        else // move ResearchTopic into the queue
	        {
	            if (ThisIsPreReq1())
	            {
	                GameAudio.NegativeClick();
                    return true;
	            }
	            SwapResearchTopic(0);
	            Screen.qcomponent.ReloadResearchQueue();
	        }
	        return true;
	    }

	    private string ResearchUidAt(int index)        => EmpireManager.Player.data.ResearchQueue[index];
        private Technology PlayerResearch              => ResourceManager.TechTree[EmpireManager.Player.ResearchTopic];
        private Technology PlayerResearchAt(int index) => ResourceManager.TechTree[ResearchUidAt(index)];

        bool AboveIsPreReq(int indexOfThis)
	    {
	        foreach (Technology.LeadsToTech dependent in PlayerResearchAt(indexOfThis - 1).LeadsTo)
	            if (dependent.UID == Node.Entry.UID)
	                return true;
	        return false;
	    }

	    bool CurrentIsPreReq()
	    {
            if (EmpireManager.Player.ResearchTopic.IsEmpty())
                return false;
            foreach (Technology.LeadsToTech dependent in PlayerResearch.LeadsTo)
	            if (dependent.UID == Node.Entry.UID)
                    return true;
            return false;
	    }

        private bool ThisIsPreReq(int indexOfThis)
	    {
            Technology current = PlayerResearchAt(indexOfThis);
            string next = ResearchUidAt(indexOfThis + 1);
            foreach (Technology.LeadsToTech dependent in current.LeadsTo)
	            if (dependent.UID == next)
                    return true;
            return false;
	    }

	    private bool ThisIsPreReq1()
	    {
            string first = ResearchUidAt(0);
	        foreach (Technology.LeadsToTech dependent in PlayerResearch.LeadsTo)
	            if (dependent.UID == first)
                    return true;
            return false;
	    }



        private bool HandleResearchButtonCancel()
	    {
	        if (Node.Entry.UID != EmpireManager.Player.ResearchTopic)
	        {
	            Screen.qcomponent.QSL.RemoveItem(this);
	            RemoveTech(Node.Entry.UID);
	        }
	        else if (EmpireManager.Player.data.ResearchQueue.Count == 0)
	        {
	            EmpireManager.Player.ResearchTopic = "";
	            Screen.qcomponent.CurrentResearch = null;
	        }
	        else
	        {
	            RemoveTech(Node.Entry.UID);

	            if (EmpireManager.Player.data.ResearchQueue.Count == 0)
	            {
	                EmpireManager.Player.ResearchTopic = "";
	                Screen.qcomponent.CurrentResearch = null;
	            }
	            else
	            {
	                var qItem = Screen.qcomponent.QSL.FirstItem<ResearchQItem>();
	                EmpireManager.Player.ResearchTopic = qItem.Node.Entry.UID;
	                Screen.qcomponent.CurrentResearch = new ResearchQItem(Screen.qcomponent.CurrentResearch.Pos, qItem.Node, Screen);
	                EmpireManager.Player.data.ResearchQueue.Remove(qItem.Node.Entry.UID);
	                Screen.qcomponent.QSL.RemoveFirst();
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
            Screen.qcomponent.QSL.RemoveFirstIf<ResearchQItem>(rqi => rqi.Node.Entry.UID == uid);
		}
	}
}