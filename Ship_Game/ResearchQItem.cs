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

        private EmpireResearch Research => Screen.Player.Research;

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

        void OnBtnUpPressed(UIButton up)
        {
            int index = Research.IndexInQueue(Tech.UID);
            if (!Research.ResearchCanMoveUp(index))
            {
                GameAudio.NegativeClick();
                return;
            }
            
            InputState input = GameBase.ScreenManager.input;
            if (input.IsCtrlKeyDown)
            {
                Research.MoveToTopOrPreReq(index);
            }
            else
            {
                Research.MoveUp(index);
            }
            Screen.Queue.ReloadResearchQueue();
        }

        void OnBtnDownPressed(UIButton down)
        {
            int index = Screen.Player.Research.IndexInQueue(Tech.UID);
            if (!Research.ResearchCanMoveDown(index))
            {
                GameAudio.NegativeClick();
                return;
            }

            Research.MoveDown(index);
            Screen.Queue.ReloadResearchQueue();
        }

        void OnBtnCancelPressed(UIButton cancel)
        {
            Research.RemoveTechFromQueue(Tech.UID);
            Screen.Queue.ReloadResearchQueue();
        }

        void OnBtnToTopPressed(UIButton toTop)
        {
            int index = Screen.Player.Research.IndexInQueue(Tech.UID);
            
            if (index == -1 || index == 0)
            {
                GameAudio.NegativeClick();
                return;
            }
            
            int moved = Research.MoveToTopWithPreReqs(index);
            if (moved == 0)
            {
                GameAudio.NegativeClick();
                return;
            }

            Screen.Queue.ReloadResearchQueue();
        }
    }
}