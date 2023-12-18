using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game
{
    public sealed class ResearchQItem : ScrollListItem<ResearchQItem>
    {
        readonly ResearchScreenNew Screen;
        public readonly TechEntry Tech;
        private TreeNode Node;
        readonly UIButton BtnUp;
        readonly UIButton BtnDown;
        readonly UIButton BtnCancel;
        readonly UIButton BtnToTop;

        public override string ToString() => $"ResearchQItem \"{Tech.UID}\" {ElementDescr}";

        public ResearchQItem(ResearchScreenNew screen, TechEntry tech, Vector2 pos)
        {
            Screen = screen;
            Tech = tech;
            Pos = pos;
            BtnUp = Button(ButtonStyle.ResearchQueueUp, OnBtnUpPressed);
            BtnDown = Button(ButtonStyle.ResearchQueueDown, OnBtnDownPressed);
            BtnCancel = Button(ButtonStyle.ResearchQueueCancel, OnBtnCancelPressed);
            BtnToTop = Button(ButtonStyle.ResearchQueueToTop, OnBtnToTopPressed);
            Node = new TreeNode(Pos + new Vector2(100f, 20f), Tech, Screen);
            PerformLayout();
        }

        public override void PerformLayout()
        {
            Size = new Vector2(320, 110);
            Node.SetPos(Pos + new Vector2(100f, 20f));
            BtnUp.Rect = new RectF(X + 15, CenterY - 33, 30, 30);
            BtnDown.Rect = new RectF(X + 15, CenterY + 3, 30, 30);
            BtnCancel.Rect = new RectF(X + 57, CenterY - 16 + 30, 30, 30);
            BtnToTop.Rect = new RectF(X + 57, CenterY - 22, 30, 30);
            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            if (Node.HandleInput(input, Screen.ScreenManager, Screen.camera, Screen.Universe))
                return true;

            return base.HandleInput(input);
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            Node.Draw(batch);
        }

        void SwapQueueItems(int first, int second)
        {
            (Screen.Player.data.ResearchQueue[first], Screen.Player.data.ResearchQueue[second])
            = 
            (Screen.Player.data.ResearchQueue[second], Screen.Player.data.ResearchQueue[first]);
        }

        private bool CanMoveUp(int index)
        {
            return index != -1 && index >= 1 && !AboveIsPreReq(index);
        }
        
        /// <summary>
        /// This will move the item at the given index to the top of the queue, or until it hits a PreReq.
        /// </summary>
        private void MoveToTopOrPreReq(int index)
        {
            while (CanMoveUp(index))
            {
                SwapQueueItems(index - 1, index);
                index--;
            }
        }
        
        /// <summary>
        /// If the item at the given index has any enqueued PreReqs, they will be moved to the top of the queue.
        /// </summary>
        private void MovePreReqsToTop(int index)
        {
            Technology current = PlayerResearchAt(index);
            
            foreach (string researchUid in Screen.Player.data.ResearchQueue)
            { 
                int indexOfResearch = Screen.Player.Research.IndexInQueue(researchUid);
                
                Array<Technology> descendantTechs = ResourceManager.Tech(researchUid).DescendantTechs();
                
                foreach (Technology descendant in descendantTechs)
                {
                    if (descendant.UID == current.UID)
                    {
                        MoveToTopOrPreReq(indexOfResearch);
                    }
                }
            }
        }
        
        private void MoveToTopWithPreReqs(int index)
        {
            MovePreReqsToTop(index);
            MoveToTopOrPreReq(index);
        }
        

        void OnBtnUpPressed(UIButton up)
        {
            int index = Screen.Player.Research.IndexInQueue(Tech.UID);
            if (!CanMoveUp(index))
            {
                GameAudio.NegativeClick();
                return;
            }
            
            InputState input = GameBase.ScreenManager.input;
            if (input.IsCtrlKeyDown)
            {
                MoveToTopOrPreReq(index);
            }
            else
            {
                SwapQueueItems(index - 1, index);
            }
            Screen.Queue.ReloadResearchQueue();
        }

        void OnBtnDownPressed(UIButton down)
        {
            int index = Screen.Player.Research.IndexInQueue(Tech.UID);
            if (index == -1 || index == Screen.Player.data.ResearchQueue.Count - 1 || ThisIsPreReq(index))
            {
                GameAudio.NegativeClick();
                return;
            }

            SwapQueueItems(index + 1, index);
            Screen.Queue.ReloadResearchQueue();
        }

        void OnBtnCancelPressed(UIButton cancel)
        {
            RemoveTech(Tech.UID);
        }

        void OnBtnToTopPressed(UIButton toTop)
        {
            int index = Screen.Player.Research.IndexInQueue(Tech.UID);
            
            if (index == -1 || index == 0)
            {
                GameAudio.NegativeClick();
                return;
            }
            
            MoveToTopWithPreReqs(index);

            Screen.Queue.ReloadResearchQueue();
        }

        string ResearchUidAt(int index) => Screen.Player.data.ResearchQueue[index];
        Technology PlayerResearchAt(int index) => ResourceManager.Tech(ResearchUidAt(index));

        bool AboveIsPreReq(int indexOfThis)
        {
            var thisTech = PlayerResearchAt(indexOfThis);
            foreach (Technology.LeadsToTech dependent in PlayerResearchAt(indexOfThis - 1).LeadsTo)
                if (dependent.UID == thisTech.UID)
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
                Screen.Player.Research.RemoveFromQueue(tech);
                foreach (Technology.LeadsToTech dependent in ResourceManager.Tech(tech).LeadsTo)
                    RemoveLeadsToRecursive(dependent.UID);
            }

            RemoveLeadsToRecursive(uid);
            Screen.Queue.ReloadResearchQueue();
        }
    }
}