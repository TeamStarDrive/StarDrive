using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
	public sealed class ResearchQItem : UIElementContainer
	{
	    public TreeNode Node;
        readonly UIButton BtnUp;
        readonly UIButton BtnDown;
        readonly UIButton BtnCancel;
        readonly ResearchScreenNew Screen;

		public ResearchQItem(Vector2 position, TreeNode node, ResearchScreenNew screen) : base(screen, position)
		{
			Screen = screen;
			var container = new Rectangle((int)position.X, (int)position.Y, 320, 110);
			Node = new TreeNode(container.PosVec() + new Vector2(100f, 20f), node.Entry, screen);
			var rup    = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33, 30, 30);
			var rdown  = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33 + 36, 30, 30);
			var cancel = new Rectangle(container.X + 15 + 30 + 12, container.Y + container.Height / 2 - 15, 30, 30);
			BtnUp     = Button(ButtonStyle.ResearchQueueUp, rup, OnBtnUpPressed);
			BtnDown   = Button(ButtonStyle.ResearchQueueDown, rdown, OnBtnDownPressed);
			BtnCancel = Button(ButtonStyle.ResearchQueueCancel, cancel, OnBtnCancelPressed);
		}

		public override void Draw(SpriteBatch batch)
		{
            base.Draw(batch);
			Node.DrawGlow(batch);
			Node.Draw(batch);
		}

		public void Draw(SpriteBatch batch, Rectangle container)
		{
			Node = new TreeNode(new Vector2(container.X, container.Y) + new Vector2(100f, 20f), Node.Entry, Screen);
			BtnUp.Rect     = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33, 30, 30);
			BtnDown.Rect   = new Rectangle(container.X + 15, container.Y + container.Height / 2 - 33 + 36, 30, 30);
			BtnCancel.Rect = new Rectangle(container.X + 15 + 30 + 12, container.Y + container.Height / 2 - 15, 30, 30);
            this.Draw(batch);
		}

        void SwapQueueItems(int first, int second)
        {
            string tmp = EmpireManager.Player.data.ResearchQueue[first];
            EmpireManager.Player.data.ResearchQueue[first] = Node.Entry.UID;
            EmpireManager.Player.data.ResearchQueue[second] = tmp;
        }

        void OnBtnUpPressed(UIButton up)
        {
            int index = EmpireManager.Player.ResearchQueueIndex(Node.Entry.UID);
            if (index == -1 || index < 1 || AboveIsPreReq(index))
            {
                GameAudio.NegativeClick();
                return;
            }
            SwapQueueItems(index - 1, index);
            Screen.qcomponent.ReloadResearchQueue();
        }

	    void OnBtnDownPressed(UIButton down)
        {
            int index = EmpireManager.Player.ResearchQueueIndex(Node.Entry.UID);
            if (index == -1 || index == EmpireManager.Player.data.ResearchQueue.Count - 1 || ThisIsPreReq(index))
            {
                GameAudio.NegativeClick();
                return;
            }
            SwapQueueItems(index + 1, index);
            Screen.qcomponent.ReloadResearchQueue();
	    }

        void OnBtnCancelPressed(UIButton cancel)
        {
            RemoveTech(Node.Entry.UID);
        }

        string ResearchUidAt(int index)        => EmpireManager.Player.data.ResearchQueue[index];
        Technology PlayerResearchAt(int index) => ResourceManager.TechTree[ResearchUidAt(index)];

        bool AboveIsPreReq(int indexOfThis)
	    {
	        foreach (Technology.LeadsToTech dependent in PlayerResearchAt(indexOfThis - 1).LeadsTo)
	            if (dependent.UID == Node.Entry.UID)
	                return true;
	        return false;
	    }

        bool ThisIsPreReq(int indexOfThis)
	    {
            Technology current = PlayerResearchAt(indexOfThis);
            string next = ResearchUidAt(indexOfThis + 1);
            foreach (Technology.LeadsToTech dependent in current.LeadsTo)
	            if (dependent.UID == next)
                    return true;
            return false;
	    }

        void RemoveTech(string uid)
		{
            void RemoveLeadsToRecursive(string tech)
            {
                EmpireManager.Player.RemoveResearchFromQueue(tech);
                foreach (Technology.LeadsToTech dependent in ResourceManager.TechTree[tech].LeadsTo)
                    RemoveLeadsToRecursive(dependent.UID);
            }

            RemoveLeadsToRecursive(uid);
            Screen.qcomponent.ReloadResearchQueue();
		}
	}
}