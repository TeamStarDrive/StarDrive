using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class SearchTechScreen : GameScreen
    {
        private readonly Menu2 Window;
        private readonly Color Cream = Colors.Cream;
        private readonly SpriteFont LargeFont = Fonts.Arial20Bold;
        readonly ScrollList2<SearchTechItem> TechList;
        readonly UITextEntry SearchTech;
        readonly ResearchScreenNew Screen;

        public SearchTechScreen(ResearchScreenNew screen, Array<TreeNode> allTreeNodes) : base(screen)
        {
            IsPopup           = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
            Screen            = screen;
            int height        = (int)(ScreenHeight * 0.8f);
            Window = Add(new Menu2(new Rectangle(ScreenWidth / 2 - 125, ScreenHeight / 2 - (height/2), 250, height)));

            var panelRect   = new Rectangle((int)Window.X + 20, (int)Window.Y + 80, (int)Window.Width - 40, (int)Window.Height - 110);
            var panel       = new Submenu(panelRect, SubmenuStyle.Blue);
            TechList        = Add(new ScrollList2<SearchTechItem>(panel, 125, ListStyle.Blue));
            SearchTech      = Add(new UITextEntry(new Vector2(Window.X + 30, Window.Y + 65), ""));
            SearchTech.Font = Fonts.Arial14Bold;
            SearchTech.ClickableArea = new Rectangle((int)Window.X + 35, (int)Window.Y + 70, (int)Window.Width - 70, 36);
            SearchTech.MaxCharacters = 14;
            Add(new Submenu(Window.X + 30, Window.Y + 40, Window.Y + 60, 50, SubmenuStyle.Blue));
            PerformLayout();
        }

        void PopulateTechs()
        {
            TechList.Reset();
            var items = new Array<SearchTechItem>();

            foreach (var entry in EmpireManager.Player.TechnologyDict)
            {
                TreeNode node = new TreeNode(Vector2.Zero, entry.Value, Screen);
                string lower = node.TechName.ToLower();
                if (entry.Value.Discovered && !entry.Value.IsRoot &&
                    (SearchTech.Text.IsEmpty() || lower.Contains(SearchTech.Text.ToLower())))
                {
                    items.Add(CreateQueueItem(node));
                }
            }

            TechList.SetItems(items);
            TechList.RequiresLayout = true;
    }

        SearchTechItem CreateQueueItem(TreeNode node)
        {
            var defaultPos = new Vector2(Window.X + 5, Window.Y);
            return new SearchTechItem(Screen, node, defaultPos) { List = TechList };
        }

        public override void LoadContent()
        {
            CloseButton(Window.Menu.Right - 40, Window.Menu.Y + 20);
            string title    = Localizer.Token(GameText.SearchTechnology);
            Vector2 menuPos = new Vector2(Window.Menu.CenterTextX(title, LargeFont), Window.Menu.Y + 35);
            Label(menuPos, title, LargeFont, Cream);
            PopulateTechs();
            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            SearchTech.Draw(batch, elapsed);
            base.Draw(batch, elapsed);
            batch.End();
        }

        public override bool HandleInput(InputState input)
        {
            if (SearchTech.HandlingInput && input.Escaped)
                return true;
            
            if (SearchTech.HitTest(input.CursorPosition) && !SearchTech.HandlingInput)
                SearchTech.HandlingInput = true;
            
            if (!SearchTech.HitTest(input.CursorPosition) && (input.RightMouseClick || input.LeftMouseClick))
                SearchTech.HandlingInput = false;
            
            if (SearchTech.HandleInput(input) && SearchTech.HandlingInput)
            {
                PopulateTechs();
                return true;
            }

            if (input.RightMouseClick || input.LeftMouseClick)
            {
                foreach (SearchTechItem item in TechList.AllEntries)
                {
                    if (item.HandleInput(input))
                    {
                        if (input.LeftMouseClick)
                            ResearchToTech(item.Tech);

                        return true;
                    }
                }
            }

            if (!SearchTech.HandlingInput && (input.Escaped || input.RightMouseClick))
            {
                ExitScreen();
                return true;
            }

            return base.HandleInput(input);
        }

        void ResearchToTech(TechEntry entry)
        {
            if (entry.Unlocked || EmpireManager.Player.Research.IsQueued(entry.UID))
            {
                GameAudio.NegativeClick();
                return;
            }

            Array<TechEntry> entries = new Array<TechEntry> {entry};
            while (!entry.IsRoot)
            {
                TechEntry parent = entry.GetPreReq(EmpireManager.Player);
                if (parent.Unlocked || EmpireManager.Player.Research.IsQueued(entry.UID))
                    break;

                if (!parent.IsRoot)
                    entries.Add(parent);

                entry = parent;
            }

            for (int i = entries.Count-1; i >= 0; i--)
            {
                TechEntry te = entries[i];
                Screen.Queue.AddToResearchQueue(new TreeNode(Vector2.Zero, te, Screen));
            }
        }
    }
}
