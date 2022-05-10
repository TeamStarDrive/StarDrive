using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using System;
using Ship_Game.GameScreens.Universe.Debug;
using SDGraphics;
using SDUtils;
using Vector2 = SDGraphics.Vector2;
using Rectangle = SDGraphics.Rectangle;

namespace Ship_Game
{
    public sealed class ResearchScreenNew : GameScreen
    {
        public readonly UniverseScreen Universe;
        public readonly Empire Player;
        public Camera2D camera = new Camera2D();

        readonly Map<string, RootNode> RootNodes = new Map<string, RootNode>(StringComparer.OrdinalIgnoreCase);
        public Map<string, Node> AllTechNodes    = new Map<string, Node>(StringComparer.OrdinalIgnoreCase);
        public Map<string, TreeNode> SubNodes    = new Map<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
        public Array<TreeNode> AllTreeNodes      = new Array<TreeNode>();

        CloseButton Close;
        UIButton Search;
        Menu2 MainMenu;
        public EmpireUIOverlay empireUI;

        Vector2 Cursor;
        Vector2 MainMenuOffset;

        public ResearchQueueUIComponent Queue;

        int GridWidth  = 175;
        int GridHeight = 100;

        readonly Array<Vector2> ClaimedSpots = new Array<Vector2>();

        ResearchDebugUnlocks DebugUnlocks;

        public Color ApplyCurrentAlphaColor(Color color)
        {
            color = ApplyCurrentAlphaToColor(color);
            return new Color(color, color.A.LowerBound(100));
        }

        public ResearchScreenNew(GameScreen parent, UniverseScreen u, EmpireUIOverlay empireUi)
            : base(parent, toPause: u)
        {
            Universe = u;
            Player = u.Player;
            empireUI = empireUi;
            IsPopup = false;
            CanEscapeFromScreen = true;
            TransitionOnTime = 0.25f;
            TransitionOffTime = 0.25f;
        }

        public override void LoadContent()
        {
            camera = new Camera2D { Pos = new Vector2(Viewport.Width, Viewport.Height) / 2f };
            var main = new Rectangle(0, 0, ScreenWidth, ScreenHeight);
            MainMenu = new Menu2(main);
            MainMenuOffset = new Vector2(main.X + 20, main.Y + 30);
            Close = Add(new CloseButton(main.Right - 40, main.Y + 20));

            RootNodes.Clear();
            AllTechNodes.Clear();
            SubNodes.Clear();

            int numDiscoveredRoots = Player.TechEntries.Count(t => t.IsRoot && t.Discovered);

            GridHeight = (main.Height - 40) / numDiscoveredRoots;
            MainMenuOffset.Y = main.Y + 12 + GridHeight / 3;
            if (ScreenHeight <= 720)
            {
                MainMenuOffset.Y = MainMenuOffset.Y + 8f;
            }

            foreach (TechEntry tech in Player.TechEntries)
            {
                if (tech.IsRoot && tech.Discovered)
                {
                    Cursor.X = 0f;
                    Cursor.Y = FindDeepestY() + 1;
                    SetNode(tech);
                }
            }

            GridHeight = (main.Height - 40) / 6;
            foreach (RootNode node in RootNodes.Values)
            {
                PopulateAllTechsFromRoot(node);
            }

            RootNode root = RootNodes[GlobalStats.ResearchRootUIDToDisplay];
            PopulateNodesFromRoot(root);

            // Create queue once all techs are populated
            var queue = new Rectangle(main.X + main.Width - 355, main.Y + 40, 330, main.Height - 100);
            Queue = Add(new ResearchQueueUIComponent(this, queue));
            Vector2 searchPos = new Vector2(main.X + main.Width - 360, main.Height - 55);
            Search = Add(new UIButton(ButtonStyle.BigDip, searchPos, "Search"));
            Search.OnClick = OnSearchButtonClicked;

            DebugUnlocks = Add(new ResearchDebugUnlocks(ReloadContent));
            DebugUnlocks.AxisAlign = Align.BottomRight;
            DebugUnlocks.SetLocalPos(-Queue.Width - 50, -25);

            base.LoadContent();
        }

        public override void Update(UpdateTimes elapsed, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            DebugUnlocks.Visible = Universe.Debug || Universe is DeveloperUniverse;
            base.Update(elapsed, otherScreenHasFocus, coveredByOtherScreen);
        }

        public void OnSearchButtonClicked(UIButton button)
        {
            ScreenManager.AddScreen(new SearchTechScreen(this));
        }

        public override void Draw(SpriteBatch batch, DrawTimes elapsed)
        {
            ScreenManager.FadeBackBufferToBlack(TransitionAlpha * 2 / 3);

            batch.Begin();
            batch.FillRectangle(new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.Black);
            MainMenu.Draw(batch, elapsed);
            batch.End();

            batch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, camera.Transform);
            {
                DrawConnectingLines(batch);

                foreach (RootNode rootNode in RootNodes.Values)
                {
                    rootNode.Draw(batch);
                }

                foreach (TreeNode treeNode in SubNodes.Values)
                {
                    treeNode.Draw(batch);
                }
            }
            batch.End();

            batch.Begin();
            base.Draw(batch, elapsed);
            batch.End();
        }

        static Vector2 CenterBetweenPoints(Vector2 left, Vector2 right)
        {
            return left.LerpTo(right, 0.5f).Rounded();
        }

        RootNode GetCurrentlySelectedRootNode()
        {
            foreach (RootNode root in RootNodes.Values)
                if (root.nodeState == NodeState.Press)
                    return root;
            return null;
        }

        void DrawConnectingLines(SpriteBatch batch)
        {
            RootNode rootNode = GetCurrentlySelectedRootNode();

            // Level 0 VERTICAL line coming straight from root nodes
            {
                Vector2 rootNodeRight = rootNode.RightPoint;
                var nextNodeLeft = new Vector2(MainMenuOffset.X + GridWidth, rootNodeRight.Y);
                Vector2 midPoint = CenterBetweenPoints(rootNode.RightPoint, nextNodeLeft);

                bool anyTechsComplete = false;

                foreach (TechEntry child in rootNode.Entry.GetFirstDiscoveredEntries())
                {
                    if (child.Unlocked)
                        anyTechsComplete = true;

                    TreeNode node = SubNodes[child.UID];
                    var midPointOther = new Vector2(midPoint.X, node.BaseRect.CenterY() - 10);
                    batch.DrawResearchLineVertical(midPoint, midPointOther, child.Unlocked);

                    Vector2 destinationPos = midPointOther + new Vector2(rootNodeRight.Distance(nextNodeLeft) + 13f, 0.0f);
                    batch.DrawResearchLineHorizontalGradient(midPointOther, destinationPos, child.Unlocked);
                }

                batch.DrawResearchLineHorizontal(rootNodeRight, midPoint, anyTechsComplete);
            }

            foreach (TreeNode treeNode in SubNodes.Values)
            {
                var vector21 = new Vector2(treeNode.BaseRect.X + treeNode.BaseRect.Width - 25,
                                           treeNode.BaseRect.Y + treeNode.BaseRect.Height / 2 - 10);
                Vector2 vector22 = vector21 + new Vector2(GridWidth / 2f, 0.0f);
                vector22.Y = vector21.Y;

                foreach (Technology.LeadsToTech leadsTo in treeNode.Entry.Tech.LeadsTo)
                {
                    TechEntry techEntry1 = Player.GetTechEntry(leadsTo.UID);
                    techEntry1 = techEntry1.FindNextDiscoveredTech(Player);
                    if (techEntry1 != null)
                    {
                        var treeNode1 = SubNodes[techEntry1.UID];
                        var vector23 = new Vector2(vector22.X, treeNode1.BaseRect.CenterY() - 10);
                        batch.DrawResearchLineVertical(vector22, vector23, techEntry1.Unlocked);
                    }
                }
            }

            foreach (TreeNode node in SubNodes.Values)
            {
                Technology technology2 = node.Entry.Tech;
                if (technology2.AnyChildrenDiscovered(Player))
                {
                    var leftPoint = node.RightPoint;
                    Vector2 rightPoint = leftPoint + new Vector2(GridWidth / 2f, 0.0f);
                    bool complete1 = false;
                    foreach (Technology.LeadsToTech leadsToTech2 in technology2.LeadsTo)
                    {
                        TechEntry techEntry2 = Player.GetTechEntry(leadsToTech2.UID);
                        techEntry2 = techEntry2.FindNextDiscoveredTech(Player);
                        if (techEntry2 != null)
                        {
                            if (techEntry2.Unlocked)
                                complete1 = true;

                            TreeNode treeNode3 = (SubNodes[techEntry2.UID]);
                            var leftPoint2 = new Vector2(rightPoint.X, treeNode3.BaseRect.CenterY() - 10);
                            Vector2 rightPoint2 = leftPoint2 + new Vector2(leftPoint.Distance(rightPoint) + 13f, 0.0f);
                            batch.DrawResearchLineHorizontalGradient(leftPoint2, rightPoint2, techEntry2.Unlocked);
                        }
                    }

                    batch.DrawResearchLineHorizontal(leftPoint, rightPoint, complete1);
                }
            }

        }

        public override void ExitScreen()
        {
            GlobalStats.ResearchRootUIDToDisplay = GetCurrentlySelectedRootNode().Entry.UID;
            base.ExitScreen();
        }

        int FindDeepestY()
        {
            int deepest = 0;
            foreach (RootNode root in RootNodes.Values)
                if (root.NodePosition.Y > deepest)
                    deepest = (int) root.NodePosition.Y;
            return deepest;
        }

        int FindDeepestYSubNodes()
        {
            int deepest = 0;
            foreach (TreeNode node in SubNodes.Values)
                if (node.NodePosition.Y > deepest)
                    deepest = (int)node.NodePosition.Y;
            return deepest;
        }

        public override bool HandleInput(InputState input)
        {
            if (input.ResearchExitScreen)
            {
                GameAudio.EchoAffirmative();
                ExitScreen();
                return true;
            }

            if (input.RightMouseHeldDown)
                camera.MoveClamped(input.CursorVelocity, ScreenCenter, new Vector2(3200));

            foreach (RootNode root in RootNodes.Values)
            {
                if (root.HandleInput(input,camera))
                {
                    PopulateNodesFromRoot(root);
                    GameAudio.ResearchSelect();
                }
            }

            foreach (TreeNode node in SubNodes.Values)
            {
                if (node.HandleInput(input, ScreenManager, camera, Universe))
                {
                    if (input.LeftMouseClick && !input.RightMouseClick)
                    {
                        OnNodeClicked(node);
                    }
                    return true; // input captured
                }
            }
            return base.HandleInput(input);
        }

        void OnNodeClicked(TreeNode node)
        {
            TechEntry techEntry = node.Entry;
            if (!techEntry.CanBeResearched)
            {
                // this tech cannot be researched
                GameAudio.NegativeClick();
            }
            else if (techEntry.HasPreReq(Player))
            {
                // we already have all pre-requisites, so add it directly to queue
                Queue.AddToResearchQueue(node);
                GameAudio.ResearchSelect();
            }
            else
            {
                // we need to research required techs before doing this one
                GameAudio.ResearchSelect();

                // figure out the list of techs to add
                var techsToAdd = GetRequiredTechEntriesToResearch(node.Entry);
                foreach (TechEntry toAdd in techsToAdd)
                {
                    if (toAdd.Discovered)
                        Queue.AddToResearchQueue(SubNodes[toAdd.UID]);
                }
            }
        }

        Array<TechEntry> GetRequiredTechEntriesToResearch(TechEntry toResearch)
        {
            TechEntry techEntry = toResearch;
            var techs = new Array<TechEntry>{ techEntry };
            if (techEntry.Tech.RootNode != 1)
            {
                while (!techEntry.Unlocked)
                {
                    TechEntry preReq = techEntry.GetPreReq(Player);
                    if (preReq == null)
                        break; // done!
                    if (!preReq.Unlocked)
                        techs.Add(preReq);
                    techEntry = preReq;
                }
            }
            techs.Reverse();
            return techs;
        }

        Vector2 GridSize => new Vector2(GridWidth, GridHeight);

        Vector2 GetCurrentCursorOffset(float yOffset = 0)
        {
            var cursor = new Vector2(Cursor.X, Cursor.Y + yOffset);
            return (MainMenuOffset + cursor*GridSize).Rounded();
        }

        public void PopulateAllTechs(Node node)
        {
            bool first = true;
            foreach (TechEntry child in node.Entry.GetPlayerChildEntries())
            {
                Cursor.X = node.NodePosition.X + 1f;
                Cursor.Y = FindDeepestYSubNodes() + (first ? 0 : 1);
                if (first) first = false;

                var newNode = new TreeNode(GetCurrentCursorOffset(), child, this) { NodePosition = Cursor };

                if (child.Discovered)
                    AllTechNodes.Add(newNode.Entry.UID, newNode);
                PopulateAllTechs(newNode);
            }
        }

        void ResetRootNodeStates(RootNode selectedRoot)
        {
            foreach (RootNode node in RootNodes.Values)
                node.nodeState = NodeState.Normal;
            selectedRoot.nodeState = NodeState.Press;
        }

        public void PopulateAllTechsFromRoot(RootNode root)
        {
            ResetRootNodeStates(root);

            Cursor = new Vector2(1f, 1f);
            foreach (TechEntry child in root.Entry.GetPlayerChildEntries())
            {
                var newNode = new TreeNode(GetCurrentCursorOffset(), child, this) { NodePosition = Cursor };

                if (child.Discovered)
                    AllTechNodes.Add(newNode.Entry.UID, newNode);

                PopulateAllTechs(newNode);
            }
        }

        public void PopulateNodesFromRoot(RootNode root)
        {
            ResetRootNodeStates(root);

            SubNodes.Clear();
            ClaimedSpots.Clear();

            int rows = 1;
            int cols = CalculateTreeDimensionsFromRoot(root.Entry.UID, ref rows, 0, 0);
            if (rows < 9) GridHeight = (MainMenu.Menu.Height - 40) / rows;
            else          GridHeight = (MainMenu.Menu.Height - 40) / 9;

            if (cols > 0 && cols < 9) GridWidth = (MainMenu.Menu.Width - 350) / cols;
            else                      GridWidth = 165;


            Cursor = new Vector2(1f, 1f);
            bool first = true;

            foreach (TechEntry discovered in root.Entry.GetFirstDiscoveredEntries())
            {
                Cursor.X = root.NodePosition.X + 1f;
                Cursor.Y = FindDeepestYSubNodes() + (first ? 0 : 1);
                if (first) first = false;

                var newNode = new TreeNode(GetCurrentCursorOffset(), discovered, this) { NodePosition = Cursor };

                SubNodes.Add(newNode.Entry.UID, newNode);
                PopulateNodesFromSubNode(newNode);
            }
        }

        bool PositionIsClaimed(Vector2 position)
        {
            foreach (Vector2 v in ClaimedSpots)
                if (v.AlmostEqual(position)) return true;
            return false;
        }

        void UpdateCursorAndClaimedSpots(bool addToClaimed)
        {
            if (PositionIsClaimed(Cursor))
                Cursor.Y += 1f;
            else if (addToClaimed)
                ClaimedSpots.Add(Cursor);
        }

        public void PopulateNodesFromSubNode(Node node)
        {
            UpdateCursorAndClaimedSpots(node.Entry.Discovered);

            bool first = true;
            foreach (TechEntry child in node.Entry.GetPlayerChildEntries())
            {
                Cursor.X = node.NodePosition.X + 1f;
                Cursor.Y = FindDeepestYSubNodes() + (first ? 0 : 1);
                if (first) first = false;

                var newNode = new TreeNode(GetCurrentCursorOffset(), child, this) { NodePosition = Cursor };

                if (child.Discovered)
                {
                    SubNodes.Add(newNode.Entry.UID, newNode);
                    AllTreeNodes.Add(newNode);
                }

                PopulateNodesFromSubNode(newNode);
            }
        }

        void SetNode(TechEntry tech)
        {
            UpdateCursorAndClaimedSpots(true);

            var newNode = new RootNode(GetCurrentCursorOffset(-1), tech) { NodePosition = Cursor };

            if (Player.HasUnlocked(tech))
            {
                newNode.isResearched = true;
            }
            RootNodes.Add(tech.UID, newNode);
        }

        //Added by McShooterz: find size of tech tree before it is built
        int CalculateTreeDimensionsFromRoot(string uid, ref int rows, int cols, int colmax)
        {
            int rowCount = 0;
            cols++;
            if (cols > colmax)
                colmax = cols;
            Technology technology = ResourceManager.TechTree[uid];
            //look for branches and make space for them
            if (technology.LeadsTo.Count >0)
            {
                //dont count the main branch. use the branch that stars here.
                for (int i = 1; i < technology.LeadsTo.Count; i++)
                {
                    var techChild = Player.GetNextDiscoveredTech(technology.LeadsTo[i].UID);
                    if (techChild != null)
                        rowCount++;
                }
                rows += rowCount;
            }
            foreach (Technology.LeadsToTech tech in technology.LeadsTo)
            {
                var techChild = Player.GetNextDiscoveredTech(tech.UID);
                if (techChild != null)
                {
                    int max = CalculateTreeDimensionsFromRoot(techChild.Tech.UID, ref rows, cols, colmax);
                    if (max > colmax)
                        colmax = max;
                }
                else
                {
                    CalculateTreeDimensionsFromRoot(tech.UID, ref rows, cols, colmax);
                }
            }
            return colmax;
        }
    }
}