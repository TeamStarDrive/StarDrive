using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;

namespace Ship_Game
{
    public sealed class SearchTechScreen : GameScreen
    {
        private readonly Menu2 Window;
        private readonly Color Cream = Colors.Cream;
        private readonly Graphics.Font LargeFont = Fonts.Arial20Bold;
        readonly ScrollList2<SearchTechItem> TechList;
        readonly UITextEntry SearchTech;
        readonly ResearchScreenNew Screen;

        public SearchTechScreen(ResearchScreenNew screen) : base(screen)
        {
            IsPopup = true;
            TransitionOnTime  = 0.25f;
            TransitionOffTime = 0.25f;
            Screen = screen;
            float height = ScreenHeight * 0.8f;
            Window = Add(new Menu2(new RectF(ScreenWidth / 2 - 125, ScreenHeight / 2 - (height/2), 400, height)));

            var panel = new Submenu(Window.X + 20, Window.Y + 100, Window.Width - 40, Window.Height - 130, SubmenuStyle.Blue);
            TechList = Add(new ScrollList2<SearchTechItem>(panel, 125, ListStyle.Blue));
            TechList.OnClick = (item) => ResearchToTech(item.Tech);

            SearchTech = Add(new UITextEntry(Window.X + 20, Window.Y + 66, Window.Width - 40, 16, Fonts.Arial12Bold,
                                             GameText.StartTypingToFindTechs));
            SearchTech.Background = new Submenu(SearchTech.Rect, SubmenuStyle.Blue);
            SearchTech.Color = Color.AliceBlue;
            SearchTech.MaxCharacters = 14;
            SearchTech.OnTextChanged = (text) => PopulateTechs(text.ToLower());
            SearchTech.AutoCaptureOnKeys = true;
            SearchTech.ResetTextOnInput = true;
            PerformLayout();
        }

        void PopulateTechs(string keyword)
        {
            TechList.Reset();
            var items = new Array<SearchTechItem>();

            foreach (var entry in EmpireManager.Player.TechnologyDict)
            {
                TreeNode node = new TreeNode(Vector2.Zero, entry.Value, Screen);
                if (entry.Value.Discovered && !entry.Value.IsRoot &&
                    (keyword.IsEmpty() || node.TechName.ToLower().Contains(keyword)))
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
            LocalizedText title = GameText.SearchTechnology;
            Vector2 titlePos = new Vector2(Window.Menu.CenterTextX(title, LargeFont), Window.Menu.Y + 35);
            Label(titlePos, title, LargeFont, Cream);
            PopulateTechs("");
            base.LoadContent();
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);
            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }
        
        public override bool HandleInput(InputState input)
        {
            if (base.HandleInput(input))
                return true;
            return false;
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
