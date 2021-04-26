using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public sealed class SearchTechItem : ScrollListItem<SearchTechItem>
    {
        readonly ResearchScreenNew Screen;
        public readonly TechEntry Tech;
        private TreeNode Node;

        public override string ToString() => $"SearchTechItem \"{Tech.UID}\" {ElementDescr}";

        public SearchTechItem(ResearchScreenNew screen, TreeNode node, Vector2 pos)
        {
            Screen = screen;
            Tech    = node.Entry;
            Pos     = pos;
            Node = new TreeNode(Pos + new Vector2(20f, 50f), Tech, Screen);
            PerformLayout();
        }

        public override void PerformLayout()
        {
            Size = new Vector2(160, 110);
            Node.SetPos(Pos + new Vector2(20f, 50f));
            base.PerformLayout();
        }

        public override bool HandleInput(InputState input)
        {
            bool captured = Node.HandleInput(input, Screen.ScreenManager, Screen.camera);
            captured |= base.HandleInput(input);
            return captured;
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            base.Draw(batch, elapsed);
            Node.Draw(batch);
        }
    }
}